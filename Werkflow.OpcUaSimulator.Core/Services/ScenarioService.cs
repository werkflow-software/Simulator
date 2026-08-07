using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class ScenarioService : IScenarioService
{
	private readonly ISimulationEngine _simulationEngine;

	private readonly IConfigurationService _configurationService;

	private readonly ILogService _logService;

	private readonly Dictionary<string, CancellationTokenSource> _runningScenarios = new Dictionary<string, CancellationTokenSource>();

	public List<ScenarioDefinition> Scenarios { get; }

	IReadOnlyList<ScenarioDefinition> IScenarioService.Scenarios => Scenarios;

	public event EventHandler? ScenariosChanged;

	public ScenarioService(ISimulationEngine simulationEngine, IConfigurationService configurationService, ILogService logService)
	{
		_simulationEngine = simulationEngine;
		_configurationService = configurationService;
		_logService = logService;
		Scenarios = ScenarioCatalog.CreateDefaults().ToList();
	}

	public async Task StartScenarioAsync(string scenarioId, Guid? targetMachineId, int? durationMs, CancellationToken cancellationToken = default(CancellationToken))
	{
		ScenarioDefinition scenario = Scenarios.FirstOrDefault((ScenarioDefinition s) => s.Id == scenarioId) ?? throw new InvalidOperationException("Szenario nicht gefunden.");
		Guid machineId = targetMachineId ?? _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.IsActive).Id;
		scenario.TargetMachineId = machineId;
		scenario.IsRunning = true;
		scenario.DurationMs = durationMs;
		CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_runningScenarios[scenarioId] = cts;
		_logService.Log(LogCategory.Production, "Szenario gestartet: " + scenario.Name);
		switch (scenarioId)
		{
		case "normal-production":
			if (_simulationEngine.State == SimulationState.Stopped)
			{
				await _simulationEngine.StartAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case "counter-freeze":
			await EnsureServerOnlineAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			await _simulationEngine.StartProductionAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			_simulationEngine.SetCounterFrozen(machineId, frozen: true);
			break;
		case "machine-error":
			await EnsureServerOnlineAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			await _simulationEngine.TriggerErrorAsync(machineId, null, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case "no-connection":
		{
			await _simulationEngine.SetMachineOfflineAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			int offlineDuration = durationMs ?? 15000;
			Task.Run(async delegate
			{
				await Task.Delay(offlineDuration, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				await _simulationEngine.SetMachineOnlineAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}, cts.Token);
			break;
		}
		case "job-near-complete":
		{
			await EnsureServerOnlineAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			MachineRuntimeState state = _simulationEngine.GetRuntimeState(machineId);
			MachineConfiguration machine = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == machineId);
			int target2 = Math.Max(10, state.TargetCounter);
			int actual = (int)((double)target2 * 0.9);
			_simulationEngine.ApplyManualValues(machineId, state.PartName, state.JobName, actual, target2, MachineState.Running, errorActive: false, string.Empty, machine.ProductionIntervalMs, 1);
			await _simulationEngine.StartProductionAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			break;
		}
		case "job-completed":
			await EnsureServerOnlineAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			await _simulationEngine.CompleteJobAsync(machineId, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case "mixed-disturbances":
			if (_simulationEngine.State == SimulationState.Stopped)
			{
				await _simulationEngine.StartAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			Task.Run(async delegate
			{
				Random random = new Random();
				while (!cts.Token.IsCancellationRequested)
				{
					List<MachineConfiguration> machines = _configurationService.Configuration.Machines.Where((MachineConfiguration m) => m.IsActive).ToList();
					MachineConfiguration target = machines[random.Next(machines.Count)];
					SimulationEventType eventType = (SimulationEventType)random.Next(0, 5);
					await TriggerEventAsync(eventType, target.Id, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
					await Task.Delay(random.Next(5000, 15000), cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}, cts.Token);
			break;
		}
		if (durationMs.HasValue && scenarioId != "no-connection")
		{
			Task.Run(async delegate
			{
				await Task.Delay(durationMs.Value, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				await StopScenarioAsync(scenarioId, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			}, cts.Token);
		}
		this.ScenariosChanged?.Invoke(this, EventArgs.Empty);
	}

	public Task StopScenarioAsync(string scenarioId, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_runningScenarios.TryGetValue(scenarioId, out CancellationTokenSource value))
		{
			value.Cancel();
			_runningScenarios.Remove(scenarioId);
		}
		ScenarioDefinition scenarioDefinition = Scenarios.FirstOrDefault((ScenarioDefinition s) => s.Id == scenarioId);
		if (scenarioDefinition != null)
		{
			scenarioDefinition.IsRunning = false;
			_logService.Log(LogCategory.Production, "Szenario gestoppt: " + scenarioDefinition.Name);
		}
		this.ScenariosChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public async Task StopAllScenariosAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		foreach (string id in _runningScenarios.Keys.ToList())
		{
			await StopScenarioAsync(id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task TriggerEventAsync(SimulationEventType eventType, Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		switch (eventType)
		{
		case SimulationEventType.Error:
			await _simulationEngine.TriggerErrorAsync(machineId, null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.Warning:
			await _simulationEngine.SetMachineStateManualAsync(machineId, MachineState.Warning, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.ProductionStop:
		case SimulationEventType.CounterFreeze:
			_simulationEngine.SetCounterFrozen(machineId, frozen: true);
			break;
		case SimulationEventType.OpcUaDisconnect:
			await _simulationEngine.SetMachineOfflineAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.JobChange:
			await _simulationEngine.CompleteJobAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.SetupState:
			await _simulationEngine.SetMachineStateManualAsync(machineId, MachineState.Setup, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		default:
			await _simulationEngine.ProduceNextPartAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		}
		_logService.Log(LogCategory.Production, "Ereignis manuell ausgelöst: " + eventType.ToGermanLabel(), _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == machineId).Name);
	}

	private async Task EnsureServerOnlineAsync(Guid machineId, CancellationToken cancellationToken)
	{
		MachineRuntimeState runtime = _simulationEngine.GetRuntimeState(machineId);
		if (runtime == null || !runtime.IsServerOnline)
		{
			await _simulationEngine.SetMachineOnlineAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
