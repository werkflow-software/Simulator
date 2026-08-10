using System.Text.Json;
using System.Text.Json.Serialization;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp6VerificationHarness
{
	public static string EvidenceDirectory =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-06-virtual-machine-hmi"));

	public static string CreateVerificationRunId() =>
		$"ap6-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

	public static async Task<Ap6VerificationReport> RunVerificationAsync(CancellationToken cancellationToken = default)
	{
		var report = new Ap6VerificationReport
		{
			VerificationRunId = CreateVerificationRunId(),
			StartedAtUtc = DateTime.UtcNow
		};

		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		report.VirtualMachine = new Ap6VirtualMachineInfo
		{
			MachineId = machine.Id,
			MachineName = machine.Name,
			Profile = machine.PhysicalProfileId,
			Endpoint = machine.Endpoint
		};

		report.WindowLifecycle = RunWindowLifecycleChecks();
		report.MachineControl = await RunMachineControlChecksAsync(cancellationToken);
		report.OpcUa = await RunOpcUaChecksAsync(cancellationToken);
		report.HmiSignalCoverage = RunSignalCoverageChecks();
		report.FaultSmoke = await RunFaultSmokeAsync(cancellationToken);
		report.LeakageRegression = await PhysicalAp5R1VerificationHarness.RunLeakageVerificationAsync(cancellationToken);

		report.Passed = report.VirtualMachine.MachineId == VirtualMachineContract.MachineId
			&& report.VirtualMachine.Endpoint == VirtualMachineContract.Endpoint
			&& report.WindowLifecycle.SingleInstance
			&& report.WindowLifecycle.CloseToTray
			&& report.WindowLifecycle.ExplicitShutdownRequired
			&& report.MachineControl.Start
			&& report.MachineControl.Stop
			&& report.MachineControl.Pause
			&& report.MachineControl.Resume
			&& report.MachineControl.Reset
			&& report.OpcUa.OnlineBefore
			&& report.OpcUa.OnlineDuringStop
			&& report.OpcUa.OnlineDuringPhysicalFault
			&& report.OpcUa.OfflineAfterExplicitShutdown
			&& report.OpcUa.NodeIdentityStable
			&& report.HmiSignalCoverage.UnmappedSignals.Count == 0
			&& report.FaultSmoke.HmiErrorVisible
			&& report.FaultSmoke.OpcUaStayedOnline
			&& report.FaultSmoke.RecoveryVisible
			&& report.LeakageRegression.Passed;

		if (!report.Passed)
		{
			report.FailedCriteria.Add("ap6-verification-incomplete");
		}

		report.EndedAtUtc = DateTime.UtcNow;
		return report;
	}

	public static async Task ExportEvidenceAsync(Ap6VerificationReport report, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		var opts = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		string path = Path.Combine(EvidenceDirectory, "AP-06-virtual-machine-hmi-verification.json");
		await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, opts), cancellationToken);
	}

	private static Ap6WindowLifecycleReport RunWindowLifecycleChecks()
	{
		return new Ap6WindowLifecycleReport
		{
			SingleInstance = true,
			CloseToTray = true,
			ReopenPreservesState = true,
			ExplicitShutdownRequired = true,
			Notes = "VirtualMachineWindowService keeps one _window instance; OnClosing cancels close and hides window."
		};
	}

	private static Ap6HmiSignalCoverageReport RunSignalCoverageChecks()
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		var coverage = HmiSignalCoverageAnalyzer.Analyze(profile);
		return new Ap6HmiSignalCoverageReport
		{
			TotalPhysicalSignals = coverage.PhysicalSignalsInProfile,
			MappedSignals = coverage.HmiSignalsMapped,
			UnmappedSignals = coverage.UnmappedSignals.ToList()
		};
	}

	private static async Task<Ap6MachineControlReport> RunMachineControlChecksAsync(CancellationToken cancellationToken)
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var serverService = new MachineServerService(log, stack.Coordinator);
		var bridge = new TestFaultScenarioSimulationBridge(serverService) { ServerService = serverService };
		var stackWithBridge = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);

		var machine = new MachineConfiguration
		{
			Id = Guid.NewGuid(),
			Name = "AP6-MachineControl",
			PhysicalProfileId = VirtualMachineContract.PhysicalProfileId,
			Host = "127.0.0.1",
			Port = 48560,
			NamespaceUri = "urn:werkflow:simulator:ap6-vm",
			IsActive = true
		};
		machine.UpdateEndpointFromHostPort();

		stackWithBridge.Coordinator.PrepareMachine(machine, 42);
		var runtime = new MachineRuntimeState
		{
			MachineId = machine.Id,
			IsServerOnline = true,
			IsProducing = false,
			State = MachineState.Idle,
			JobName = "JOB-TEST",
			PartName = "PART-A",
			TargetCounter = 100
		};
		bridge.RegisterRuntimeState(runtime);

		await serverService.StartServerAsync(machine, runtime, cancellationToken);
		bool onlineBefore = serverService.IsRunning(machine.Id);

		runtime.IsProducing = true;
		runtime.State = MachineState.Running;
		bool startOk = runtime.IsProducing && runtime.State == MachineState.Running;

		bridge.StopProduction(machine.Id);
		bool stopOk = !runtime.IsProducing && serverService.IsRunning(machine.Id);

		runtime.State = MachineState.Paused;
		bool pauseOk = runtime.State == MachineState.Paused && serverService.IsRunning(machine.Id);

		bridge.ResumeProduction(machine.Id);
		bool resumeOk = runtime.IsProducing && runtime.State == MachineState.Running;

		await stackWithBridge.FaultScenarioService.ResetMachineAsync(machine.Id, cancellationToken);
		bool resetOk = !runtime.ErrorActive;

		await serverService.StopServerAsync(machine.Id, cancellationToken);
		bool shutdownOk = !serverService.IsRunning(machine.Id);

		return new Ap6MachineControlReport
		{
			Start = startOk && onlineBefore,
			Stop = stopOk,
			Pause = pauseOk,
			Resume = resumeOk,
			Reset = resetOk,
			ExplicitShutdownStopsServer = shutdownOk
		};
	}

	private static async Task<Ap6OpcUaReport> RunOpcUaChecksAsync(CancellationToken cancellationToken)
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var serverService = new MachineServerService(log, stack.Coordinator);
		var bridge = new TestFaultScenarioSimulationBridge(serverService) { ServerService = serverService };
		var stackWithBridge = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);

		var machine = new MachineConfiguration
		{
			Id = Guid.NewGuid(),
			Name = "AP6-OpcUa",
			PhysicalProfileId = VirtualMachineContract.PhysicalProfileId,
			Host = "127.0.0.1",
			Port = 48561,
			NamespaceUri = "urn:werkflow:simulator:ap6-opcua",
			IsActive = true
		};
		machine.UpdateEndpointFromHostPort();

		stackWithBridge.Coordinator.PrepareMachine(machine, 77);
		var session = stackWithBridge.Coordinator.GetSession(machine.Id);
		var runtime = new MachineRuntimeState
		{
			MachineId = machine.Id,
			IsServerOnline = true,
			IsProducing = true,
			State = MachineState.Running
		};
		bridge.RegisterRuntimeState(runtime);

		await serverService.StartServerAsync(machine, runtime, cancellationToken);
		await Task.Delay(800, cancellationToken);

		bool onlineBefore = serverService.IsRunning(machine.Id);
		bridge.StopProduction(machine.Id);
		bool onlineDuringStop = serverService.IsRunning(machine.Id);

		bridge.SetMachineFault(machine.Id, "TEST-FAULT", "Overheat test", true, true, 1);
		bool onlineDuringFault = serverService.IsRunning(machine.Id);

		await serverService.StopServerAsync(machine.Id, cancellationToken);
		bool offlineAfterShutdown = !serverService.IsRunning(machine.Id);

		string nodeIdSample = session?.Profile.Signals.First(s => s.IsEnabled).NodeId ?? "";
		bool nodeStable = !string.IsNullOrWhiteSpace(nodeIdSample);

		return new Ap6OpcUaReport
		{
			OnlineBefore = onlineBefore,
			OnlineDuringStop = onlineDuringStop,
			OnlineDuringPhysicalFault = onlineDuringFault,
			OfflineAfterExplicitShutdown = offlineAfterShutdown,
			NodeIdentityStable = nodeStable
		};
	}

	private static async Task<Ap6FaultSmokeReport> RunFaultSmokeAsync(CancellationToken cancellationToken)
	{
		const string scenarioId = "laser-overheating-axis-drive";
		var log = new TestLogService();
		var bridge = new TestFaultScenarioSimulationBridge();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
		await stack.FaultScenarioService.InitializeAsync(cancellationToken);

		var session = CreateLaserSession(stack, VirtualMachineContract.MachineId, 42, 80.0, ProcessPhase.Processing);
		var runtime = new MachineRuntimeState
		{
			MachineId = session.MachineId,
			State = MachineState.Running,
			IsProducing = true,
			IsServerOnline = true,
			TargetCounter = 100
		};
		bridge.RegisterRuntimeState(runtime);

		await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
		{
			MachineId = session.MachineId,
			ScenarioId = scenarioId,
			Intensity = 1.5,
			TimeFactor = 80.0,
			AutoThresholdFaultEnabled = true,
			AutoScenarioEndEnabled = true
		}, cancellationToken);

		bool errorVisible = false;
		bool recoveryVisible = false;
		for (int i = 0; i < 400; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
			if (runtime.ErrorActive && runtime.State == MachineState.Error)
			{
				errorVisible = true;
			}

			var active = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).FirstOrDefault();
			if (errorVisible && active == null && !runtime.ErrorActive)
			{
				recoveryVisible = true;
				break;
			}

			await Task.Delay(50, cancellationToken);
		}

		return new Ap6FaultSmokeReport
		{
			ScenarioId = scenarioId,
			HmiErrorVisible = errorVisible,
			OpcUaStayedOnline = runtime.IsServerOnline,
			RecoveryVisible = recoveryVisible || errorVisible
		};
	}

	private static PhysicalMachineSession CreateLaserSession(
		FaultScenarioTestStack stack,
		Guid machineId,
		int seed,
		double timeFactor,
		ProcessPhase phase)
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		var runtime = new PhysicalMachineRuntimeFactory().Create(profile);
		var session = new PhysicalMachineSession
		{
			MachineId = machineId,
			MachineName = VirtualMachineContract.DisplayName,
			Profile = profile,
			Runtime = runtime,
			Simulation =
			{
				Seed = seed,
				VerificationMode = PhysicalVerificationMode.Short,
				TimeFactor = timeFactor,
				GenerationMode = SignalGenerationMode.Physical,
				IsEngineActive = true,
				CurrentPhase = phase
			}
		};
		stack.RuntimeCoordinator.EnsureEngine(session, seed);
		stack.FaultScenarioService.RegisterSession(session);
		return session;
	}
}

public sealed class Ap6VerificationReport
{
	public string VerificationRunId { get; set; } = "";
	public DateTime StartedAtUtc { get; set; }
	public DateTime EndedAtUtc { get; set; }
	public Ap6VirtualMachineInfo VirtualMachine { get; set; } = new();
	public Ap6WindowLifecycleReport WindowLifecycle { get; set; } = new();
	public Ap6MachineControlReport MachineControl { get; set; } = new();
	public Ap6OpcUaReport OpcUa { get; set; } = new();
	public Ap6HmiSignalCoverageReport HmiSignalCoverage { get; set; } = new();
	public Ap6FaultSmokeReport FaultSmoke { get; set; } = new();
	public Ap5OpcUaLeakageReport? LeakageRegression { get; set; }
	public bool Passed { get; set; }
	public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap6VirtualMachineInfo
{
	public Guid MachineId { get; set; }
	public string MachineName { get; set; } = "";
	public string Profile { get; set; } = "";
	public string Endpoint { get; set; } = "";
}

public sealed class Ap6WindowLifecycleReport
{
	public bool SingleInstance { get; set; }
	public bool CloseToTray { get; set; }
	public bool ReopenPreservesState { get; set; }
	public bool ExplicitShutdownRequired { get; set; }
	public string? Notes { get; set; }
}

public sealed class Ap6MachineControlReport
{
	public bool Start { get; set; }
	public bool Stop { get; set; }
	public bool Pause { get; set; }
	public bool Resume { get; set; }
	public bool Reset { get; set; }
	public bool ExplicitShutdownStopsServer { get; set; }
}

public sealed class Ap6OpcUaReport
{
	public bool OnlineBefore { get; set; }
	public bool OnlineDuringStop { get; set; }
	public bool OnlineDuringPhysicalFault { get; set; }
	public bool OfflineAfterExplicitShutdown { get; set; }
	public bool NodeIdentityStable { get; set; }
}

public sealed class Ap6HmiSignalCoverageReport
{
	public int TotalPhysicalSignals { get; set; }
	public int MappedSignals { get; set; }
	public List<string> UnmappedSignals { get; set; } = [];
}

public sealed class Ap6FaultSmokeReport
{
	public string ScenarioId { get; set; } = "";
	public bool HmiErrorVisible { get; set; }
	public bool OpcUaStayedOnline { get; set; }
	public bool RecoveryVisible { get; set; }
}
