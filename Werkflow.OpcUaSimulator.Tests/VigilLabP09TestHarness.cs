using System.Collections.Concurrent;
using System.Reflection;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

internal sealed class VigilLabP09TestHarness
{
	public RecordingMachineValuePublisher Publisher { get; } = new();

	public StubMachineServerService Server { get; } = new();

	public ConfigurationService ConfigurationService { get; }

	public SimulationEngine Engine { get; }

	public MachineConfiguration VigilLab { get; }

	public MachineConfiguration ExistingLaser { get; }

	public PhysicalSignalPublishingCoordinator Coordinator { get; }

	public FaultScenarioTestStack Stack { get; }

	public VigilLabP09TestHarness()
	{
		var log = new TestLogService();
		Stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		Coordinator = Stack.Coordinator;
		VigilLab = DefaultMachines.CreateVigilLabMachine();
		ExistingLaser = DefaultMachines.Create().First(m => m.Id == VirtualMachineContract.MachineId);
		ConfigurationService = new ConfigurationService(log, new JobGenerator());
		ConfigurationService.Configuration.Machines = [ExistingLaser, VigilLab];
		ConfigurationService.Configuration.Settings = FixedSimulationCatalog.CreateDefaultSettings();
		VigilLabRunProfile.ApplyDeterministicSettings(ConfigurationService.Configuration.Settings);
		ConfigurationService.Configuration.Jobs = FixedSimulationCatalog.CreateJobs();
		Engine = new SimulationEngine(
			ConfigurationService,
			Server,
			Publisher,
			new JobGenerator(),
			new JobDispatcher(),
			new ValidationService(),
			log,
			Coordinator);
	}

	public async Task StartVigilLabAsync()
	{
		await Engine.StartMachineServerAsync(VigilLab.Id);
		await Engine.AssignJobIfMissingAsync(VigilLab.Id);
	}

	public MachineRuntimeState Runtime => Engine.GetRuntimeState(VigilLab.Id)!;

	public async Task AdvanceDueJobChangeAsync()
	{
		MachineRuntimeState liveRuntime = GetLiveRuntime(VigilLab.Id);
		liveRuntime.JobChangeEndsAtUtc = DateTime.UtcNow.AddSeconds(-1);
		await Task.Delay(350);
	}

	private MachineRuntimeState GetLiveRuntime(Guid machineId)
	{
		FieldInfo? field = typeof(SimulationEngine).GetField("_runtimeStates", BindingFlags.Instance | BindingFlags.NonPublic);
		var states = (Dictionary<Guid, MachineRuntimeState>)field!.GetValue(Engine)!;
		return states[machineId];
	}
}

internal sealed class RecordingMachineValuePublisher : IMachineValuePublisher
{
	private readonly ConcurrentDictionary<(Guid MachineId, NodeSemanticType Semantic), List<object?>> _history = new();

	public IReadOnlyList<object?> GetHistory(Guid machineId, NodeSemanticType semantic) =>
		_history.TryGetValue((machineId, semantic), out List<object?>? values)
			? values
			: Array.Empty<object?>();

	public object? GetLatest(Guid machineId, NodeSemanticType semantic)
	{
		IReadOnlyList<object?> history = GetHistory(machineId, semantic);
		return history.Count == 0 ? null : history[^1];
	}

	public int GetChangeCount(Guid machineId, NodeSemanticType semantic) => GetHistory(machineId, semantic).Count;

	public void PublishAll(Guid machineId, MachineRuntimeState state, IReadOnlyList<NodeMapping> nodes)
	{
		PublishValue(machineId, NodeSemanticType.PartName, state.PartName, nodes);
		PublishValue(machineId, NodeSemanticType.JobName, state.JobName, nodes);
		PublishValue(machineId, NodeSemanticType.ActualCounter, state.ActualCounter, nodes);
		PublishValue(machineId, NodeSemanticType.TargetCounter, state.TargetCounter, nodes);
	}

	public void PublishValue(Guid machineId, NodeSemanticType semanticType, object? value, IReadOnlyList<NodeMapping> nodes)
	{
		if (nodes.All(n => n.SemanticType != semanticType || !n.IsEnabled))
		{
			return;
		}

		List<object?> history = _history.GetOrAdd((machineId, semanticType), _ => []);
		if (history.Count == 0 || !Equals(history[^1], value))
		{
			history.Add(value);
		}
	}

	public object? GetLiveValue(Guid machineId, NodeSemanticType semanticType) =>
		GetLatest(machineId, semanticType);
}

internal sealed class StubMachineServerService : IMachineServerService
{
	public event EventHandler<(Guid MachineId, bool IsOnline, int ClientCount)>? ServerStatusChanged;

	public bool IsRunning(Guid machineId) => true;

	public int GetConnectedClients(Guid machineId) => 0;

	public Task StartServerAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken = default)
	{
		runtime.IsServerOnline = true;
		return Task.CompletedTask;
	}

	public Task StopServerAsync(Guid machineId, CancellationToken cancellationToken = default) => Task.CompletedTask;

	public Task StopAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	public ushort? GetNamespaceIndex(Guid machineId) => 2;
}
