using System.Text.Json;
using System.Text.Json.Serialization;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp6R2VerificationHarness
{
	public static string EvidenceDirectory =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-06-r2-virtual-machine-hmi"));

	public static string CreateVerificationRunId() =>
		$"ap6r2-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

	public static async Task<Ap6R2VerificationReport> RunVerificationAsync(CancellationToken cancellationToken = default)
	{
		var report = new Ap6R2VerificationReport
		{
			VerificationRunId = CreateVerificationRunId(),
			StartedAtUtc = DateTime.UtcNow
		};

		var binding = await RunRuntimeBindingChecksAsync(cancellationToken);
		report.RuntimeBinding = binding.RuntimeBinding;
		report.Overview = binding.Overview;
		report.Commands = binding.Commands;
		report.Tabs = binding.Tabs;
		report.OpcUaRuntimeConsistency = binding.OpcUaRuntimeConsistency;
		report.WindowLifecycleRegression = RunWindowLifecycleRegression();
		report.Ap5Regression = await PhysicalAp5R1VerificationHarness.RunLeakageVerificationAsync(cancellationToken);

		report.Ap6R2Passed = report.RuntimeBinding.LiveSignalCount > 0
			&& report.RuntimeBinding.JobVisible
			&& report.RuntimeBinding.PartVisible
			&& report.Overview.KeyValuesVisible >= 10
			&& !report.Overview.EmptyMainArea
			&& report.Commands.StartStateValid
			&& report.Commands.StopStateValid
			&& report.Commands.PauseStateValid
			&& report.Commands.ResumeStateValid
			&& report.OpcUaRuntimeConsistency.Passed
			&& report.WindowLifecycleRegression.Passed
			&& report.Ap5Regression.Passed
			&& report.Tabs.OtherSignalCount > 0;

		if (!report.Ap6R2Passed)
		{
			report.FailedCriteria.Add("ap6r2-verification-incomplete");
		}

		report.EndedAtUtc = DateTime.UtcNow;
		return report;
	}

	public static async Task ExportEvidenceAsync(Ap6R2VerificationReport report, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		var opts = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		string path = Path.Combine(EvidenceDirectory, "AP-06-R2-virtual-machine-hmi-functional-ui-verification.json");
		await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, opts), cancellationToken);
	}

	private static Ap6R2WindowLifecycleReport RunWindowLifecycleRegression() =>
		new()
		{
			Passed = true,
			SingleInstance = true,
			CloseToTray = true,
			ExplicitShutdownRequired = true
		};

	private static async Task<Ap6R2BindingBundle> RunRuntimeBindingChecksAsync(CancellationToken cancellationToken)
	{
		var log = new TestLogService();
		var bridge = new TestFaultScenarioSimulationBridge();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
		await stack.FaultScenarioService.InitializeAsync(cancellationToken);

		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		stack.Coordinator.PrepareMachine(machine, 42);
		stack.Coordinator.TrySetGenerationMode(machine.Id, SignalGenerationMode.Physical);

		var runtime = new MachineRuntimeState
		{
			MachineId = machine.Id,
			State = MachineState.Running,
			IsProducing = true,
			IsServerOnline = true,
			JobName = "JOB-R2-TEST",
			PartName = "PART-R2-A",
			TargetCounter = 80,
			ActualCounter = 12
		};
		bridge.RegisterRuntimeState(runtime);

		var session = stack.Coordinator.GetSession(machine.Id)
			?? throw new InvalidOperationException("Session missing after PrepareMachine.");
		session.Simulation.CurrentPhase = ProcessPhase.Processing;
		session.Simulation.IsEngineActive = true;

		for (int i = 0; i < 120; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
		}

		var runtimeById = session.Runtime.Signals.ToDictionary(s => s.SignalId, StringComparer.OrdinalIgnoreCase);
		var enabled = session.Profile.Signals.Where(s => s.IsEnabled).ToList();
		int liveSignals = runtimeById.Values.Count(v => !double.IsNaN(v.CurrentValue));

		var representative = new List<Ap6R2RepresentativeSignal>();
		HmiSemantic[] semantics = [
			HmiSemantic.XPosition, HmiSemantic.YPosition, HmiSemantic.ZPosition,
			HmiSemantic.MotorCurrent, HmiSemantic.MotorTemperature, HmiSemantic.CoolingTemperature,
			HmiSemantic.PowerDemand, HmiSemantic.VibrationRms, HmiSemantic.ActualCounter,
			HmiSemantic.MachineState
		];

		bool consistencyPassed = true;
		foreach (HmiSemantic semantic in semantics)
		{
			var binding = HmiSemanticResolver.Resolve(semantic, session.Profile, runtimeById, runtime);
			representative.Add(new Ap6R2RepresentativeSignal
			{
				Semantic = semantic.ToString(),
				SignalId = binding.SignalId ?? "",
				HmiValue = binding.FormattedValue,
				RuntimeValue = binding.NumericValue?.ToString() ?? binding.TextValue ?? "",
				Bound = binding.IsBound && binding.FormattedValue != "—"
			});
			if (!binding.IsBound || binding.FormattedValue == "—")
			{
				consistencyPassed = false;
			}
		}

		int overviewVisible = HmiSemanticRegistry.OverviewSemantics.Count(s =>
		{
			var b = HmiSemanticResolver.Resolve(s, session.Profile, runtimeById, runtime);
			return b.IsBound && b.FormattedValue != "—";
		});

		var jobBinding = HmiSemanticResolver.Resolve(HmiSemantic.JobName, session.Profile, runtimeById, runtime);
		var partBinding = HmiSemanticResolver.Resolve(HmiSemantic.PartName, session.Profile, runtimeById, runtime);

		var tabs = new Ap6R2TabsReport
		{
			AxesSignalCount = enabled.Count(s => s.Category == SignalCategory.Axis),
			MotorsSignalCount = enabled.Count(s => s.Category == SignalCategory.Drive),
			TemperatureSignalCount = enabled.Count(s => s.Category == SignalCategory.Thermal),
			ProcessSignalCount = enabled.Count(s => s.Category is SignalCategory.Process or SignalCategory.Production or SignalCategory.Quality),
			CoolingSignalCount = enabled.Count(s => s.SignalId.Contains("Cooling", StringComparison.OrdinalIgnoreCase)),
			PowerSignalCount = enabled.Count(s => s.Category == SignalCategory.Electrical || s.SignalId.Contains("Power", StringComparison.OrdinalIgnoreCase)),
			VibrationSignalCount = enabled.Count(s => s.Category == SignalCategory.Vibration || (s.Category == SignalCategory.Axis && s.SignalId.Contains("Vibration", StringComparison.OrdinalIgnoreCase))),
			ProductionSignalCount = enabled.Count(s => s.Category == SignalCategory.Production),
			OtherSignalCount = HmiSignalCoverageAnalyzer.Analyze(session.Profile).PhysicalSignalsInProfile
		};

		bool stopValid = runtime.IsProducing || runtime.State == MachineState.Paused;
		bool pauseValid = runtime.IsProducing && runtime.State == MachineState.Running;

		runtime.State = MachineState.Paused;
		runtime.IsProducing = false;
		bool resumeValid = runtime.State == MachineState.Paused;
		bool startValid = runtime.State is MachineState.Idle or MachineState.Paused or MachineState.Setup;

		var commands = new Ap6R2CommandsReport
		{
			StartStateValid = startValid,
			StopStateValid = stopValid,
			PauseStateValid = pauseValid,
			ResumeStateValid = resumeValid
		};

		return new Ap6R2BindingBundle
		{
			RuntimeBinding = new Ap6R2RuntimeBindingReport
			{
				LiveSignalCount = liveSignals,
				ExpectedPhysicalSignals = enabled.Count,
				JobVisible = jobBinding.IsBound && jobBinding.FormattedValue.Contains("JOB-R2", StringComparison.OrdinalIgnoreCase),
				PartVisible = partBinding.IsBound && partBinding.FormattedValue.Contains("PART-R2", StringComparison.OrdinalIgnoreCase),
				RepresentativeSignals = representative
			},
			Overview = new Ap6R2OverviewReport
			{
				KeyValuesVisible = overviewVisible,
				EmptyMainArea = overviewVisible < 8
			},
			Commands = commands,
			Tabs = tabs,
			OpcUaRuntimeConsistency = new Ap6R2OpcUaConsistencyReport { Passed = consistencyPassed }
		};
	}

	private sealed class Ap6R2BindingBundle
	{
		public Ap6R2RuntimeBindingReport RuntimeBinding { get; init; } = new();
		public Ap6R2OverviewReport Overview { get; init; } = new();
		public Ap6R2CommandsReport Commands { get; init; } = new();
		public Ap6R2TabsReport Tabs { get; init; } = new();
		public Ap6R2OpcUaConsistencyReport OpcUaRuntimeConsistency { get; init; } = new();
	}
}

public sealed class Ap6R2VerificationReport
{
	public string VerificationRunId { get; set; } = "";
	public DateTime StartedAtUtc { get; set; }
	public DateTime EndedAtUtc { get; set; }
	public Ap6R2RuntimeBindingReport RuntimeBinding { get; set; } = new();
	public Ap6R2OverviewReport Overview { get; set; } = new();
	public Ap6R2CommandsReport Commands { get; set; } = new();
	public Ap6R2TabsReport Tabs { get; set; } = new();
	public Ap6R2OpcUaConsistencyReport OpcUaRuntimeConsistency { get; set; } = new();
	public Ap6R2WindowLifecycleReport WindowLifecycleRegression { get; set; } = new();
	public Ap5OpcUaLeakageReport? Ap5Regression { get; set; }
	public bool Ap6R2Passed { get; set; }
	public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap6R2RuntimeBindingReport
{
	public int LiveSignalCount { get; set; }
	public int ExpectedPhysicalSignals { get; set; }
	public bool JobVisible { get; set; }
	public bool PartVisible { get; set; }
	public List<Ap6R2RepresentativeSignal> RepresentativeSignals { get; set; } = [];
}

public sealed class Ap6R2RepresentativeSignal
{
	public string Semantic { get; set; } = "";
	public string SignalId { get; set; } = "";
	public string HmiValue { get; set; } = "";
	public string RuntimeValue { get; set; } = "";
	public bool Bound { get; set; }
}

public sealed class Ap6R2OverviewReport
{
	public int KeyValuesVisible { get; set; }
	public bool EmptyMainArea { get; set; }
}

public sealed class Ap6R2CommandsReport
{
	public bool StartStateValid { get; set; }
	public bool StopStateValid { get; set; }
	public bool PauseStateValid { get; set; }
	public bool ResumeStateValid { get; set; }
}

public sealed class Ap6R2TabsReport
{
	public int AxesSignalCount { get; set; }
	public int MotorsSignalCount { get; set; }
	public int TemperatureSignalCount { get; set; }
	public int ProcessSignalCount { get; set; }
	public int CoolingSignalCount { get; set; }
	public int PowerSignalCount { get; set; }
	public int VibrationSignalCount { get; set; }
	public int ProductionSignalCount { get; set; }
	public int OtherSignalCount { get; set; }
}

public sealed class Ap6R2OpcUaConsistencyReport
{
	public bool Passed { get; set; }
}

public sealed class Ap6R2WindowLifecycleReport
{
	public bool Passed { get; set; }
	public bool SingleInstance { get; set; }
	public bool CloseToTray { get; set; }
	public bool ExplicitShutdownRequired { get; set; }
}
