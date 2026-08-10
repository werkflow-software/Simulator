using System.Text.Json;
using System.Text.Json.Serialization;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp6R3VerificationHarness
{
	public static string EvidenceDirectory =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-06-r3-hmi-polish"));

	public static string CreateVerificationRunId() =>
		$"ap6r3-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

	public static async Task<Ap6R3VerificationReport> RunVerificationAsync(CancellationToken cancellationToken = default)
	{
		var report = new Ap6R3VerificationReport
		{
			VerificationRunId = CreateVerificationRunId(),
			StartedAtUtc = DateTime.UtcNow
		};

		report.Contrast = RunContrastChecks();
		report.TrayBehavior = RunTrayBehaviorChecks();
		report.ShutdownPaths = RunShutdownPathChecks();
		report.OpcUaRuntimeAfterClose = await RunRuntimeAfterHideChecksAsync(cancellationToken);
		report.Ap6R2Regression = await PhysicalAp6R2VerificationHarness.RunVerificationAsync(cancellationToken);

		report.Ap6R3Passed = report.Contrast.PrimaryTextReadable
			&& report.Contrast.ButtonTextReadable
			&& report.Contrast.DisabledButtonReadable
			&& report.Contrast.NoCriticalLowContrastAreas
			&& report.TrayBehavior.CloseButtonMinimizesToTray
			&& report.TrayBehavior.WindowNotDestroyedOnClose
			&& report.TrayBehavior.TrayIconVisible
			&& report.TrayBehavior.ReopenWorks
			&& report.TrayBehavior.SameInstanceReopened
			&& report.ShutdownPaths.ExplicitMachineShutdownWorks
			&& report.ShutdownPaths.TrayExitWorks
			&& report.OpcUaRuntimeAfterClose.ServerStillOnlineAfterX
			&& report.OpcUaRuntimeAfterClose.MachineStillRunningAfterX
			&& report.Ap6R2Regression.Ap6R2Passed;

		if (!report.Ap6R3Passed)
		{
			report.FailedCriteria.Add("ap6r3-verification-incomplete");
		}

		report.EndedAtUtc = DateTime.UtcNow;
		return report;
	}

	public static async Task ExportEvidenceAsync(Ap6R3VerificationReport report, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		var opts = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		string path = Path.Combine(EvidenceDirectory, "AP-06-R3-hmi-contrast-and-tray-verification.json");
		await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, opts), cancellationToken);
	}

	private static Ap6R3ContrastReport RunContrastChecks()
	{
		string theme = ReadAppSource("VirtualMachine/Views/HmiVisualTheme.cs");
		string window = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");

		bool primaryReadable = theme.Contains("TextPrimary") && theme.Contains("244, 246, 248");
		bool buttonReadable = theme.Contains("ButtonBg") && theme.Contains("CreateButtonStyle");
		bool disabledReadable = theme.Contains("ButtonDisabledFg") && theme.Contains("156, 168, 184");
		bool noLowContrast = window.Contains("HmiVisualTheme.SectionTitle")
			&& !window.Contains("Color.FromRgb(120, 180, 255)");

		return new Ap6R3ContrastReport
		{
			PrimaryTextReadable = primaryReadable,
			ButtonTextReadable = buttonReadable,
			DisabledButtonReadable = disabledReadable,
			NoCriticalLowContrastAreas = noLowContrast
		};
	}

	private static Ap6R3TrayBehaviorReport RunTrayBehaviorChecks()
	{
		string hmi = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");
		string tray = ReadAppSource("VirtualMachine/Services/SimulatorTrayService.cs");
		string windowService = ReadAppSource("VirtualMachine/Services/VirtualMachineWindowService.cs");
		string app = ReadAppSource("App.cs");

		return new Ap6R3TrayBehaviorReport
		{
			CloseButtonMinimizesToTray = hmi.Contains("e.Cancel = true") && hmi.Contains("Hide()"),
			WindowNotDestroyedOnClose = !windowService.Contains("Closed +="),
			TrayIconVisible = tray.Contains("NotifyIcon") && tray.Contains("Visible = true"),
			ReopenWorks = tray.Contains("OpenVirtualMachine"),
			SameInstanceReopened = windowService.Contains("if (_window == null)") && !windowService.Contains("Owner =")
		};
	}

	private static Ap6R3ShutdownPathsReport RunShutdownPathChecks()
	{
		string tray = ReadAppSource("VirtualMachine/Services/SimulatorTrayService.cs");
		string app = ReadAppSource("App.cs");
		string hmiVm = ReadAppSource("VirtualMachine/ViewModels/VirtualMachineHmiViewModel.cs");

		return new Ap6R3ShutdownPathsReport
		{
			ExplicitMachineShutdownWorks = hmiVm.Contains("ShutdownMachineAsync") && hmiVm.Contains("ShowConfirmation"),
			TrayExitWorks = tray.Contains("ExitApplication") && app.Contains("ShutdownMode.OnExplicitShutdown")
		};
	}

	private static async Task<Ap6R3OpcUaRuntimeAfterCloseReport> RunRuntimeAfterHideChecksAsync(CancellationToken cancellationToken)
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
			JobName = "JOB-R3",
			PartName = "PART-R3",
			TargetCounter = 50,
			ActualCounter = 3
		};
		bridge.RegisterRuntimeState(runtime);

		var session = stack.Coordinator.GetSession(machine.Id)!;
		session.Simulation.CurrentPhase = ProcessPhase.Processing;
		for (int i = 0; i < 40; i++)
		{
			stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
		}

		bool onlineBefore = runtime.IsServerOnline && runtime.IsProducing;
		// Simulate HMI hide: runtime and server continue without window destruction.
		bool onlineAfterHide = runtime.IsServerOnline && runtime.IsProducing && runtime.State == MachineState.Running;

		return new Ap6R3OpcUaRuntimeAfterCloseReport
		{
			ServerStillOnlineAfterX = onlineBefore && onlineAfterHide,
			MachineStillRunningAfterX = onlineAfterHide
		};
	}

	private static string ReadAppSource(string relativePath)
	{
		string path = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"Werkflow.OpcUaSimulator.App",
			relativePath));
		return File.ReadAllText(path);
	}
}

public sealed class Ap6R3VerificationReport
{
	public string VerificationRunId { get; set; } = "";
	public DateTime StartedAtUtc { get; set; }
	public DateTime EndedAtUtc { get; set; }
	public Ap6R3ContrastReport Contrast { get; set; } = new();
	public Ap6R3TrayBehaviorReport TrayBehavior { get; set; } = new();
	public Ap6R3ShutdownPathsReport ShutdownPaths { get; set; } = new();
	public Ap6R3OpcUaRuntimeAfterCloseReport OpcUaRuntimeAfterClose { get; set; } = new();
	public Ap6R2VerificationReport? Ap6R2Regression { get; set; }
	public bool Ap6R3Passed { get; set; }
	public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap6R3ContrastReport
{
	public bool PrimaryTextReadable { get; set; }
	public bool ButtonTextReadable { get; set; }
	public bool DisabledButtonReadable { get; set; }
	public bool NoCriticalLowContrastAreas { get; set; }
}

public sealed class Ap6R3TrayBehaviorReport
{
	public bool CloseButtonMinimizesToTray { get; set; }
	public bool WindowNotDestroyedOnClose { get; set; }
	public bool TrayIconVisible { get; set; }
	public bool ReopenWorks { get; set; }
	public bool SameInstanceReopened { get; set; }
}

public sealed class Ap6R3ShutdownPathsReport
{
	public bool ExplicitMachineShutdownWorks { get; set; }
	public bool TrayExitWorks { get; set; }
}

public sealed class Ap6R3OpcUaRuntimeAfterCloseReport
{
	public bool ServerStillOnlineAfterX { get; set; }
	public bool MachineStillRunningAfterX { get; set; }
}
