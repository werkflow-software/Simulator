using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Models;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

internal static class VirtualMachineHmiContextHarness
{
	public static VirtualMachineHmiViewModel CreateViewModel(
		FaultScenarioTestStack stack,
		IReadOnlyList<MachineConfiguration> machines,
		IReadOnlyDictionary<Guid, MachineRuntimeState> runtimeStates)
	{
		var configuration = new HmiTestConfigurationService(machines);
		var simulation = new HmiTestSimulationEngine(runtimeStates);
		var server = new MachineServerService(new TestLogService(), stack.Coordinator);
		return new VirtualMachineHmiViewModel(
			simulation,
			configuration,
			stack.Coordinator,
			server,
			stack.FaultScenarioService,
			new HmiTestDialogService(),
			new HmiTestJobDispatcher(),
			new HmiTestSessionNavigator());
	}

	public static VirtualMachineHmiViewModel CreateViewModel(
		FaultScenarioTestStack stack,
		IReadOnlyList<MachineConfiguration> machines,
		ISimulationEngine simulationEngine,
		IMachineServerService? serverService = null)
	{
		var configuration = new HmiTestConfigurationService(machines);
		IMachineServerService server = serverService ?? new MachineServerService(new TestLogService(), stack.Coordinator);
		return new VirtualMachineHmiViewModel(
			simulationEngine,
			configuration,
			stack.Coordinator,
			server,
			stack.FaultScenarioService,
			new HmiTestDialogService(),
			new HmiTestJobDispatcher(),
			new HmiTestSessionNavigator());
	}

	public static void SetAxisPosition(PhysicalMachineSession session, string axisKey, double position)
	{
		string signalId = axisKey + ".Position";
		SignalRuntimeState? signal = session.Runtime.Signals.FirstOrDefault(s =>
			s.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase));
		if (signal != null)
		{
			signal.CurrentValue = position;
		}
	}

	public static string? ReadAxisPositionDisplay(VirtualMachineHmiViewModel viewModel, string axisKey)
	{
		HmiAxisPanelViewModel? panel = viewModel.AxisPanels.FirstOrDefault(p =>
			p.AxisName.Equals(axisKey, StringComparison.OrdinalIgnoreCase));
		return panel?.Position;
	}

	public static MachineConfiguration CreateVirtualMachineConfiguration() =>
		DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);

	public static MachineConfiguration CreateVigilLabConfiguration() =>
		DefaultMachines.CreateVigilLabMachine();

	public static void PrepareBothMachines(
		FaultScenarioTestStack stack,
		out MachineConfiguration existingLaser,
		out MachineConfiguration vigilLab,
		out PhysicalMachineSession existingSession,
		out PhysicalMachineSession vigilSession)
	{
		existingLaser = CreateVirtualMachineConfiguration();
		vigilLab = CreateVigilLabConfiguration();
		stack.Coordinator.PrepareMachine(existingLaser, 42);
		stack.Coordinator.PrepareMachine(vigilLab, 42);
		existingSession = stack.Coordinator.GetSession(existingLaser.Id)!;
		vigilSession = stack.Coordinator.GetSession(vigilLab.Id)!;
		stack.Coordinator.TrySetGenerationMode(existingLaser.Id, SignalGenerationMode.Physical);
		stack.Coordinator.TrySetGenerationMode(vigilLab.Id, SignalGenerationMode.Physical);
	}
}

internal sealed class HmiTestConfigurationService : IConfigurationService
{
	public HmiTestConfigurationService(IReadOnlyList<MachineConfiguration> machines)
	{
		Configuration = new AppConfiguration
		{
			Machines = machines.Select(CloneMachine).ToList()
		};
	}

	public ApplicationOperatingMode OperatingMode => ApplicationOperatingMode.VirtualMachine;
	public AppConfiguration Configuration { get; }
	public string ConfigurationDirectory => string.Empty;
	public event EventHandler? ConfigurationChanged;

	public Task InitializeAsync(ApplicationOperatingMode operatingMode, CancellationToken cancellationToken = default) =>
		Task.CompletedTask;

	public Task SaveAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SaveMachinesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SaveJobsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SaveSettingsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SaveEventsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task RestoreFactoryDefaultsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ExportAllAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ImportAllAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public void OpenConfigurationDirectory() { }

	private static MachineConfiguration CloneMachine(MachineConfiguration machine) =>
		new()
		{
			Id = machine.Id,
			Name = machine.Name,
			Description = machine.Description,
			Host = machine.Host,
			Port = machine.Port,
			NamespaceUri = machine.NamespaceUri,
			PhysicalProfileId = machine.PhysicalProfileId,
			IsActive = machine.IsActive,
			ProductionIntervalMs = machine.ProductionIntervalMs,
			ProductionSpeedFactor = machine.ProductionSpeedFactor
		};
}

internal sealed class HmiTestSimulationEngine : ISimulationEngine
{
	private readonly IReadOnlyDictionary<Guid, MachineRuntimeState> _states;

	public HmiTestSimulationEngine(IReadOnlyDictionary<Guid, MachineRuntimeState> states) => _states = states;

	public SimulationState State => SimulationState.Running;
	public DateTime? StartedAt => DateTime.UtcNow;
	public int TotalProducedParts => 0;
	public int ActiveErrorCount => 0;
	public int RunningServerCount => _states.Values.Count(s => s.IsServerOnline);
	public int TotalConnectedClients => 0;
	public int CurrentSeed => 42;
	public event EventHandler? StateChanged;
	public event EventHandler<MachineRuntimeState>? MachineStateChanged;

	public IReadOnlyDictionary<Guid, MachineRuntimeState> GetRuntimeStates() =>
		_states.ToDictionary(kv => kv.Key, kv => kv.Value.CloneValues());

	public MachineRuntimeState? GetRuntimeState(Guid machineId) =>
		_states.TryGetValue(machineId, out MachineRuntimeState? state) ? state.CloneValues() : null;

	public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task PauseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ResumeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task StartMachineServerAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task StopMachineServerAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public void ApplyManualValues(Guid machineId, string partName, string jobName, int actualCounter, int targetCounter, MachineState state, bool errorActive, string errorMessage, int productionIntervalMs, int stepSize) { }
	public Task StartProductionAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task PauseProductionAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ResumeProductionAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task StopProductionAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ProduceNextPartAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task TriggerErrorAsync(Guid machineId, string? message = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ClearErrorAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SetMachineOfflineAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SetMachineOnlineAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task CompleteJobAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ResetCountersAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public void SetCounterFrozen(Guid machineId, bool frozen) { }
	public Task SetMachineStateManualAsync(Guid machineId, MachineState state, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task AssignJobIfMissingAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task ChangeJobAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task SelectJobAsync(Guid machineId, int catalogIndex, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public (double partRemainingSeconds, double jobRemainingSeconds) GetProductionTimeEstimates(Guid machineId) => (0, 0);
	public double GetSetupRemainingSeconds(Guid machineId) => 0;
	public double GetNozzleChangeRemainingSeconds(Guid machineId) => 0;
}

internal sealed class HmiTestDialogService : IDialogService
{
	public void ShowInfo(string title, string message) { }
	public void ShowWarning(string title, string message) { }
	public void ShowError(string title, string message) { }
	public bool ShowConfirmation(string title, string message) => true;
	public string? ShowSaveFileDialog(string filter, string defaultFileName) => null;
	public string? ShowOpenFileDialog(string filter) => null;
}

internal sealed class HmiTestJobDispatcher : IJobDispatcher
{
	public void AssignJobs(AppConfiguration config, Random random) { }
	public SimulationJob? GetNextJobForMachine(Guid machineId, AppConfiguration config, Random random) => null;
	public SimulationJob? GetJobByCatalogIndex(int catalogIndex, AppConfiguration config) => null;
	public void CompleteJob(SimulationJob job, MachineRuntimeState runtime) { }
}

internal sealed class HmiTestSessionNavigator : IVirtualMachineSessionNavigator
{
	public Task EndSessionAndReturnToSelectorAsync() => Task.CompletedTask;
}
