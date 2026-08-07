using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

namespace Werkflow.OpcUaSimulator.Tests;

/// <summary>
/// Test bridge tracking machine fault/production state and optional OPC UA server lifecycle.
/// </summary>
public sealed class TestFaultScenarioSimulationBridge : IFaultScenarioSimulationBridge
{
    private readonly IMachineServerService? _serverService;

    public IMachineServerService? ServerService { get; set; }
    private readonly object _sync = new();
    private readonly Dictionary<Guid, MachineRuntimeState> _states = new();
    private readonly Dictionary<Guid, Dictionary<string, int>> _faultPriorities = new();
    private readonly Dictionary<Guid, Dictionary<string, string>> _faultMessages = new();

    public TestFaultScenarioSimulationBridge(IMachineServerService? serverService = null)
    {
        _serverService = serverService;
    }

    public IReadOnlyDictionary<Guid, MachineRuntimeState> States => _states;

    public void RegisterRuntimeState(MachineRuntimeState state)
    {
        lock (_sync)
        {
            _states[state.MachineId] = state;
        }
    }

    public MachineRuntimeState GetOrCreate(Guid machineId)
    {
        lock (_sync)
        {
            if (!_states.TryGetValue(machineId, out var state))
            {
                state = new MachineRuntimeState { MachineId = machineId, State = MachineState.Idle };
                _states[machineId] = state;
            }

            return state;
        }
    }

    public void SetMachineFault(Guid machineId, string faultCode, string message, bool stopProduction, bool keepServerOnline, int priority)
    {
        lock (_sync)
        {
            var priorities = _faultPriorities.GetValueOrDefault(machineId) ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            priorities[faultCode] = priority;
            _faultPriorities[machineId] = priorities;

            var messages = _faultMessages.GetValueOrDefault(machineId) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            messages[faultCode] = message;
            _faultMessages[machineId] = messages;

            var dominant = priorities.OrderBy(kv => kv.Value).First();
            if (dominant.Key.Equals(faultCode, StringComparison.OrdinalIgnoreCase) || priority <= dominant.Value)
            {
                var runtime = GetOrCreate(machineId);
                runtime.ErrorActive = true;
                runtime.ErrorMessage = message;
                runtime.State = MachineState.Error;
                runtime.DisruptedStateStartedAt = DateTime.UtcNow;
            }

            if (stopProduction)
            {
                var runtime = GetOrCreate(machineId);
                runtime.IsProducing = false;
                runtime.IsCounterFrozen = true;
            }
        }
    }

    public void ClearMachineFault(Guid machineId, string faultCode)
    {
        lock (_sync)
        {
            if (!_faultPriorities.TryGetValue(machineId, out var priorities))
            {
                return;
            }

            priorities.Remove(faultCode);
            _faultMessages.GetValueOrDefault(machineId)?.Remove(faultCode);

            if (priorities.Count == 0)
            {
                _faultPriorities.Remove(machineId);
                _faultMessages.Remove(machineId);
                var runtime = GetOrCreate(machineId);
                runtime.ErrorActive = false;
                runtime.ErrorMessage = string.Empty;
                runtime.State = MachineState.Idle;
                runtime.IsCounterFrozen = false;
                return;
            }

            var dominant = priorities.OrderBy(kv => kv.Value).First();
            var message = _faultMessages.GetValueOrDefault(machineId)?.GetValueOrDefault(dominant.Key) ?? dominant.Key;
            var remainingRuntime = GetOrCreate(machineId);
            remainingRuntime.ErrorActive = true;
            remainingRuntime.ErrorMessage = message;
            remainingRuntime.State = MachineState.Error;
        }
    }

    public async Task StopServerAsync(Guid machineId, CancellationToken cancellationToken = default)
    {
        var runtime = GetOrCreate(machineId);
        runtime.IsServerOnline = false;
        var server = ServerService ?? _serverService;
        if (server != null)
        {
            await server.StopServerAsync(machineId, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StartServerAsync(Guid machineId, CancellationToken cancellationToken = default)
    {
        var server = ServerService ?? _serverService;
        if (server != null)
        {
            await server.StartServerAsync(
                new MachineConfiguration { Id = machineId, Name = machineId.ToString(), Port = 14999 },
                GetOrCreate(machineId),
                cancellationToken).ConfigureAwait(false);
        }

        GetOrCreate(machineId).IsServerOnline = true;
    }

    public void StopProduction(Guid machineId)
    {
        var runtime = GetOrCreate(machineId);
        runtime.IsProducing = false;
        runtime.IsCounterFrozen = true;
    }

    public void ResumeProduction(Guid machineId)
    {
        var runtime = GetOrCreate(machineId);
        runtime.IsProducing = true;
        runtime.IsCounterFrozen = false;
        if (!runtime.ErrorActive)
        {
            runtime.State = MachineState.Running;
        }
    }
}
