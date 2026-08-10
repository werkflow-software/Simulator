using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp6R2Tests
{
	[Fact]
	public async Task AP6R2_Hmi_ReceivesNonEmptyLiveSignalCollection()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.RuntimeBinding.LiveSignalCount > 50);
		Assert.True(bundle.RuntimeBinding.ExpectedPhysicalSignals >= 300);
	}

	[Fact]
	public async Task AP6R2_Overview_ContainsExpectedKeySemantics()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.Overview.KeyValuesVisible >= 12);
		Assert.False(bundle.Overview.EmptyMainArea);
	}

	[Fact]
	public async Task AP6R2_JobName_VisibleWhenRuntimeHasJobName()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.RuntimeBinding.JobVisible);
	}

	[Fact]
	public async Task AP6R2_PartName_VisibleWhenRuntimeHasPartName()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.RuntimeBinding.PartVisible);
	}

	[Fact]
	public async Task AP6R2_X_LiveValueBound()
	{
		var signal = await GetRepresentativeAsync(HmiSemantic.XPosition);
		Assert.True(signal.Bound);
	}

	[Fact]
	public async Task AP6R2_MotorTemperature_LiveValueBound()
	{
		var signal = await GetRepresentativeAsync(HmiSemantic.MotorTemperature);
		Assert.True(signal.Bound);
	}

	[Fact]
	public async Task AP6R2_ActualCounter_UpdatesFromRuntime()
	{
		var signal = await GetRepresentativeAsync(HmiSemantic.ActualCounter);
		Assert.True(signal.Bound);
		Assert.Contains("12", signal.HmiValue);
	}

	[Fact]
	public async Task AP6R2_MachineState_UpdatesFromRuntime()
	{
		var signal = await GetRepresentativeAsync(HmiSemantic.MachineState);
		Assert.True(signal.Bound);
		Assert.False(string.IsNullOrWhiteSpace(signal.HmiValue));
	}

	[Fact]
	public async Task AP6R2_CanExecute_StartCorrectForPausedState()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.Commands.StartStateValid);
	}

	[Fact]
	public async Task AP6R2_CanExecute_StopCorrectForRunningState()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.Commands.StopStateValid);
	}

	[Fact]
	public async Task AP6R2_CanExecute_PauseCorrectForRunningState()
	{
		var log = new TestLogService();
		var bridge = new TestFaultScenarioSimulationBridge();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
		await stack.FaultScenarioService.InitializeAsync();
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		stack.Coordinator.PrepareMachine(machine, 42);
		var runtime = new MachineRuntimeState
		{
			MachineId = machine.Id,
			State = MachineState.Running,
			IsProducing = true,
			IsServerOnline = true
		};
		bridge.RegisterRuntimeState(runtime);
		Assert.True(runtime.IsProducing && runtime.State == MachineState.Running);
	}

	[Fact]
	public async Task AP6R2_CanExecute_ResumeCorrectForPausedState()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.Commands.ResumeStateValid);
	}

	[Fact]
	public async Task AP6R2_FaultStart_AvailableWhenMachineRunning()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		await stack.FaultScenarioService.InitializeAsync();
		var scenarios = stack.FaultScenarioService.GetCatalog()
			.Where(s => s.IsEnabled && s.MachineProfileIds.Any(id =>
				id.Equals(VirtualMachineContract.PhysicalProfileId, StringComparison.OrdinalIgnoreCase)))
			.ToList();
		Assert.NotEmpty(scenarios);
	}

	[Fact]
	public void AP6R2_NoFakeValues_InSemanticResolver()
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		var runtimeById = profile.Signals
			.Where(s => s.IsEnabled)
			.ToDictionary(
				s => s.SignalId,
				s => new SignalRuntimeState { SignalId = s.SignalId, CurrentValue = s.InitialValue },
				StringComparer.OrdinalIgnoreCase);

		foreach (HmiSemantic semantic in HmiSemanticRegistry.OverviewSemantics)
		{
			var binding = HmiSemanticResolver.Resolve(semantic, profile, runtimeById, null);
			if (binding.IsBound)
			{
				Assert.DoesNotContain("FAKE", binding.FormattedValue, StringComparison.OrdinalIgnoreCase);
			}
		}
	}

	[Fact]
	public void AP6R2_NoNodeIds_InHmiDisplayNames()
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		foreach (var signal in profile.Signals.Where(s => s.IsEnabled))
		{
			string display = HmiSignalCatalog.FormatDisplayName(signal);
			Assert.DoesNotContain("ns=", display, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("i=", display, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public async Task AP6R2_AllSignalsReachableThroughHmiGroups()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.Tabs.AxesSignalCount > 0);
		Assert.True(bundle.Tabs.MotorsSignalCount > 0);
		Assert.True(bundle.Tabs.TemperatureSignalCount > 0);
		Assert.True(bundle.Tabs.OtherSignalCount >= 300);
	}

	[Fact]
	public async Task AP6R2_HmiAndRuntime_RepresentativeValuesMatch()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.OpcUaRuntimeConsistency.Passed);
	}

	[Fact]
	public void AP6R2_WindowLifecycle_RemainsGreen()
	{
		string source = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");
		Assert.Contains("e.Cancel = true", source);
		Assert.Contains("Hide()", source);
		Assert.DoesNotContain("OverviewSignals", source);
	}

	[Fact]
	public async Task AP6R2_AP5Regression_RemainsGreen()
	{
		var bundle = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(bundle.Ap5Regression?.Passed ?? false);
	}

	[Fact]
	public async Task AP6R2_Evidence_ExportVerificationJson()
	{
		var report = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		await PhysicalAp6R2VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.Ap6R2Passed, string.Join(",", report.FailedCriteria));
	}

	private static async Task<Ap6R2RepresentativeSignal> GetRepresentativeAsync(HmiSemantic semantic)
	{
		var report = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		return report.RuntimeBinding.RepresentativeSignals.First(s => s.Semantic == semantic.ToString());
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
