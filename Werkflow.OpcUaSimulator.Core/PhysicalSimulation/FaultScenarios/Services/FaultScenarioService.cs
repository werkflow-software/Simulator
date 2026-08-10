using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public sealed class FaultScenarioService : IFaultScenarioService
{
	private readonly IFaultScenarioRepository _repository;

	private readonly IFaultScenarioValidator _validator;

	private readonly IFaultScenarioRuntimeFactory _runtimeFactory;

	private readonly IFaultRecoveryEngine _recoveryEngine;

	private readonly IFaultScenarioSimulationBridge? _bridge;

	private readonly IFaultScenarioEventSink? _eventSink;

	private readonly object _sync = new object();

	private readonly Dictionary<Guid, PhysicalMachineSession> _sessions = new Dictionary<Guid, PhysicalMachineSession>();

	private bool _initialized;

	public event EventHandler<FaultScenarioEvent>? ScenarioEvent;

	public FaultScenarioService(
		IFaultScenarioRepository repository,
		IFaultScenarioValidator validator,
		IFaultScenarioRuntimeFactory runtimeFactory,
		IFaultRecoveryEngine recoveryEngine,
		IFaultScenarioSimulationBridge? bridge = null,
		IFaultScenarioEventSink? eventSink = null)
	{
		_repository = repository;
		_validator = validator;
		_runtimeFactory = runtimeFactory;
		_recoveryEngine = recoveryEngine;
		_bridge = bridge;
		_eventSink = eventSink;
	}

	public async Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		await _repository.LoadAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_initialized = true;
		FaultScenarioService faultScenarioService = this;
		Guid empty = Guid.Empty;
		string empty2 = string.Empty;
		string detail = $"{_repository.GetAll().Count} Szenarien geladen";
		faultScenarioService.RaiseEvent(FaultScenarioEventType.ScenarioLoaded, empty, empty2, default(Guid), detail);
	}

	public IReadOnlyList<FaultScenarioDefinition> GetCatalog()
	{
		if (!_initialized)
		{
			return Array.Empty<FaultScenarioDefinition>();
		}

		return (from s in _repository.GetAll()
			where s.IsEnabled
			select s).ToList();
	}

	public FaultScenarioValidationResult ValidateCatalog()
	{
		return _validator.ValidateCatalog(_repository.GetAll());
	}

	public IReadOnlyList<FaultScenarioRuntimeInfo> GetActiveScenarios(Guid machineId)
	{
		if (!_initialized)
		{
			return Array.Empty<FaultScenarioRuntimeInfo>();
		}

		lock (_sync)
		{
			if (!_sessions.TryGetValue(machineId, out PhysicalMachineSession value))
			{
				return Array.Empty<FaultScenarioRuntimeInfo>();
			}
			return value.Simulation.FaultScenarios.ActiveInstances.Values.Select(MapRuntimeInfo).ToList();
		}
	}

	public async Task<FaultScenarioInstance> StartAsync(FaultScenarioStartRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		EnsureInitialized();
		FaultScenarioDefinition definition = _repository.GetById(request.ScenarioId) ?? throw new InvalidOperationException("Szenario '" + request.ScenarioId + "' nicht gefunden.");
		lock (_sync)
		{
			if (!_sessions.TryGetValue(request.MachineId, out PhysicalMachineSession session))
			{
				throw new InvalidOperationException($"Keine physikalische Session für Maschine {request.MachineId}.");
			}
			FaultScenarioMachineContext ctx = session.Simulation.FaultScenarios;
			if (ctx.ScenarioIdToInstance.ContainsKey(definition.ScenarioId))
			{
				throw new InvalidOperationException("Szenario '" + definition.ScenarioId + "' läuft bereits.");
			}
			if (ctx.ActiveInstances.Count >= ctx.MaxParallelScenarios)
			{
				throw new InvalidOperationException("Maximale parallele Szenarien erreicht.");
			}
			FaultScenarioValidationResult validation = _validator.ValidateForProfile(definition, session.Profile);
			if (!validation.IsValid)
			{
				throw new InvalidOperationException(string.Join("; ", validation.Errors.Select((FaultScenarioValidationError e) => e.Message)));
			}
			foreach (FaultScenarioInstance active in ctx.ActiveInstances.Values)
			{
				if (definition.MutuallyExclusiveScenarioIds.Any((string id) => id.Equals(active.ScenarioId, StringComparison.OrdinalIgnoreCase)))
				{
					throw new InvalidOperationException("Konflikt mit aktivem Szenario '" + active.ScenarioId + "'.");
				}
				if (!definition.CanRunInParallel || !active.Definition.CanRunInParallel)
				{
					throw new InvalidOperationException("Parallele Szenarien nicht erlaubt.");
				}
			}
			FaultScenarioInstance instance = _runtimeFactory.CreateInstance(definition, request, session.Simulation.Seed);
			ctx.ActiveInstances[instance.InstanceId] = instance;
			ctx.ScenarioIdToInstance[definition.ScenarioId] = instance.InstanceId;
			if (definition.Effects.Any((FaultEffectDefinition e) => e.EffectType == FaultEffectType.ConnectionDrop))
			{
				_bridge?.StopServerAsync(request.MachineId, cancellationToken).GetAwaiter().GetResult();
			}
			RaiseEvent(FaultScenarioEventType.ScenarioStarted, request.MachineId, definition.ScenarioId, instance.InstanceId);
			return instance;
		}
	}

	public Task PauseAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			FaultScenarioInstance instance = GetInstance(machineId, scenarioId);
			if (instance.LifecycleState == FaultScenarioLifecycleState.Running)
			{
				instance.LifecycleState = FaultScenarioLifecycleState.Paused;
				instance.PausedAt = DateTimeOffset.UtcNow;
				RaiseEvent(FaultScenarioEventType.ScenarioPaused, machineId, scenarioId, instance.InstanceId);
			}
		}
		return Task.CompletedTask;
	}

	public Task ResumeAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			FaultScenarioInstance instance = GetInstance(machineId, scenarioId);
			if (instance.LifecycleState == FaultScenarioLifecycleState.Paused)
			{
				instance.LifecycleState = FaultScenarioLifecycleState.Running;
				instance.PausedAt = null;
				RaiseEvent(FaultScenarioEventType.ScenarioResumed, machineId, scenarioId, instance.InstanceId);
			}
		}
		return Task.CompletedTask;
	}

	public Task StopAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			FaultScenarioInstance instance = GetInstance(machineId, scenarioId);
			_recoveryEngine.BeginRecovery(instance);
			RaiseEvent(FaultScenarioEventType.RecoveryStarted, machineId, scenarioId, instance.InstanceId);
		}
		return Task.CompletedTask;
	}

	public Task CancelAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			if (!_sessions.TryGetValue(machineId, out PhysicalMachineSession value))
			{
				return Task.CompletedTask;
			}
			FaultScenarioInstance instance = GetInstance(machineId, scenarioId);
			instance.LifecycleState = FaultScenarioLifecycleState.Cancelled;
			instance.CurrentPhase = FaultScenarioPhase.Cancelled;
			if (instance.ActiveFaultCode != null && _bridge != null)
			{
				value.Simulation.FaultScenarios.ActiveFaultCodes.Remove(instance.ActiveFaultCode);
				_bridge.ClearMachineFault(machineId, instance.ActiveFaultCode);
			}
			value.Simulation.FaultScenarios.ActiveInstances.Remove(instance.InstanceId);
			value.Simulation.FaultScenarios.ScenarioIdToInstance.Remove(scenarioId);
			if (definitionHasConnectionDrop(instance))
			{
				_bridge?.StartServerAsync(machineId, cancellationToken).GetAwaiter().GetResult();
			}
			RaiseEvent(FaultScenarioEventType.ScenarioCancelled, machineId, scenarioId, instance.InstanceId);
		}
		return Task.CompletedTask;
	}

	public Task ResetMachineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			if (!_sessions.TryGetValue(machineId, out PhysicalMachineSession value))
			{
				return Task.CompletedTask;
			}
			List<string> list = value.Simulation.FaultScenarios.ScenarioIdToInstance.Keys.ToList();
			foreach (string item in list)
			{
				CancelAsync(machineId, item, cancellationToken).GetAwaiter().GetResult();
			}
			value.Simulation.FaultScenarios.ActiveFaultCodes.Clear();
			if (_bridge != null)
			{
				_bridge.ClearMachineFault(machineId, "reset");
			}
		}
		return Task.CompletedTask;
	}

	public void SetIntensity(Guid machineId, string scenarioId, double intensity)
	{
		lock (_sync)
		{
			FaultScenarioInstance instance = GetInstance(machineId, scenarioId);
			instance.Intensity = Math.Clamp(intensity, instance.Definition.MinimumIntensity, instance.Definition.MaximumIntensity);
		}
	}

	public void SetTimeFactor(Guid machineId, string scenarioId, double timeFactor)
	{
		lock (_sync)
		{
			FaultScenarioInstance instance = GetInstance(machineId, scenarioId);
			instance.TimeFactor = Math.Clamp(timeFactor, 0.1, 50.0);
		}
	}

	public void SetAutoThresholdFault(Guid machineId, string scenarioId, bool enabled)
	{
		lock (_sync)
		{
			GetInstance(machineId, scenarioId).AutoThresholdFaultEnabled = enabled;
		}
	}

	public void SetAutoScenarioEnd(Guid machineId, string scenarioId, bool enabled)
	{
		lock (_sync)
		{
			GetInstance(machineId, scenarioId).AutoScenarioEndEnabled = enabled;
		}
	}

	public void SetDiagnosisMode(Guid machineId, bool enabled)
	{
		lock (_sync)
		{
			if (_sessions.TryGetValue(machineId, out PhysicalMachineSession value))
			{
				value.Simulation.DiagnosisModeEnabled = enabled;
			}
		}
	}

	public bool IsDiagnosisModeEnabled(Guid machineId)
	{
		lock (_sync)
		{
			PhysicalMachineSession value;
			return _sessions.TryGetValue(machineId, out value) && value.Simulation.DiagnosisModeEnabled;
		}
	}

	public void RegisterSession(PhysicalMachineSession session)
	{
		lock (_sync)
		{
			_sessions[session.MachineId] = session;
		}
	}

	public void UnregisterSession(Guid machineId)
	{
		lock (_sync)
		{
			_sessions.Remove(machineId);
		}
	}

	public PhysicalMachineSession? GetSession(Guid machineId)
	{
		lock (_sync)
		{
			PhysicalMachineSession value;
			return _sessions.TryGetValue(machineId, out value) ? value : null;
		}
	}

	private FaultScenarioInstance GetInstance(Guid machineId, string scenarioId)
	{
		if (!_sessions.TryGetValue(machineId, out PhysicalMachineSession value))
		{
			throw new InvalidOperationException($"Keine Session für Maschine {machineId}.");
		}
		if (!value.Simulation.FaultScenarios.ScenarioIdToInstance.TryGetValue(scenarioId, out var value2) || !value.Simulation.FaultScenarios.ActiveInstances.TryGetValue(value2, out FaultScenarioInstance value3))
		{
			throw new InvalidOperationException("Szenario '" + scenarioId + "' nicht aktiv.");
		}
		return value3;
	}

	private static bool definitionHasConnectionDrop(FaultScenarioInstance instance)
	{
		return instance.Definition.Effects.Any((FaultEffectDefinition e) => e.EffectType == FaultEffectType.ConnectionDrop);
	}

	private static FaultScenarioRuntimeInfo MapRuntimeInfo(FaultScenarioInstance instance)
	{
		return new FaultScenarioRuntimeInfo
		{
			InstanceId = instance.InstanceId,
			ScenarioId = instance.ScenarioId,
			DisplayName = instance.Definition.DisplayName,
			LifecycleState = instance.LifecycleState,
			CurrentPhase = instance.CurrentPhase,
			Category = instance.Definition.Category,
			Severity = instance.Definition.Severity,
			Intensity = instance.Intensity,
			TimeFactor = instance.TimeFactor,
			RealElapsed = DateTimeOffset.UtcNow - instance.StartedAt - instance.PausedAccumulated,
			SimulationElapsed = instance.ScenarioElapsedTime,
			ThresholdFaultTriggered = instance.ThresholdFaultTriggered,
			RecoveryProgress = instance.RecoveryProgress,
			RunMode = instance.RunMode,
			NextPhaseChangeAt = instance.NextPhaseChangeAt
		};
	}

	private void EnsureInitialized()
	{
		if (!_initialized)
		{
			throw new InvalidOperationException("FaultScenarioService nicht initialisiert.");
		}
	}

	private void RaiseEvent(FaultScenarioEventType type, Guid machineId, string scenarioId, Guid instanceId = default(Guid), string? detail = null)
	{
		var evt = new FaultScenarioEvent
		{
			EventType = type,
			MachineId = machineId,
			ScenarioId = scenarioId,
			InstanceId = instanceId,
			Detail = detail
		};
		_eventSink?.Publish(evt);
		this.ScenarioEvent?.Invoke(this, evt);
	}
}
