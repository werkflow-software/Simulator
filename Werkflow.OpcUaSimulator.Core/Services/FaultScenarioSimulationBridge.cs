using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class FaultScenarioSimulationBridge : IFaultScenarioSimulationBridge
{
	private readonly Lazy<ISimulationEngine> _simulationEngine;

	private readonly Lazy<IMachineServerService> _machineServerService;

	private readonly IConfigurationService _configurationService;

	private readonly object _sync = new object();

	private readonly Dictionary<Guid, Dictionary<string, int>> _faultPriorities = new Dictionary<Guid, Dictionary<string, int>>();

	public FaultScenarioSimulationBridge(Lazy<ISimulationEngine> simulationEngine, Lazy<IMachineServerService> machineServerService, IConfigurationService configurationService)
	{
		_simulationEngine = simulationEngine;
		_machineServerService = machineServerService;
		_configurationService = configurationService;
	}

	public void SetMachineFault(Guid machineId, string faultCode, string message, bool stopProduction, bool keepServerOnline, int priority)
	{
		lock (_sync)
		{
			Dictionary<string, int> dictionary = _faultPriorities.GetValueOrDefault(machineId) ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			dictionary[faultCode] = priority;
			_faultPriorities[machineId] = dictionary;
			KeyValuePair<string, int> keyValuePair = dictionary.OrderBy((KeyValuePair<string, int> kv) => kv.Value).First();
			if (keyValuePair.Key.Equals(faultCode, StringComparison.OrdinalIgnoreCase) || priority <= keyValuePair.Value)
			{
				_simulationEngine.Value.TriggerErrorAsync(machineId, message).GetAwaiter().GetResult();
			}
			if (stopProduction)
			{
				_simulationEngine.Value.PauseProductionAsync(machineId).GetAwaiter().GetResult();
			}
		}
	}

	public void ClearMachineFault(Guid machineId, string faultCode)
	{
		lock (_sync)
		{
			if (!_faultPriorities.TryGetValue(machineId, out Dictionary<string, int> value))
			{
				return;
			}
			value.Remove(faultCode);
			if (value.Count == 0)
			{
				_faultPriorities.Remove(machineId);
				_simulationEngine.Value.ClearErrorAsync(machineId).GetAwaiter().GetResult();
				return;
			}
			KeyValuePair<string, int> keyValuePair = value.OrderBy((KeyValuePair<string, int> kv) => kv.Value).First();
			MachineRuntimeState runtimeState = _simulationEngine.Value.GetRuntimeState(machineId);
			if (runtimeState != null && runtimeState.ErrorActive)
			{
				_simulationEngine.Value.TriggerErrorAsync(machineId, "Aktiver Fehler: " + keyValuePair.Key).GetAwaiter().GetResult();
			}
		}
	}

	public async Task StopServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		await _simulationEngine.Value.StopMachineServerAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task StartServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		await _simulationEngine.Value.StartMachineServerAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public void StopProduction(Guid machineId)
	{
		_simulationEngine.Value.PauseProductionAsync(machineId).GetAwaiter().GetResult();
	}

	public void ResumeProduction(Guid machineId)
	{
		_simulationEngine.Value.StartProductionAsync(machineId).GetAwaiter().GetResult();
	}
}
