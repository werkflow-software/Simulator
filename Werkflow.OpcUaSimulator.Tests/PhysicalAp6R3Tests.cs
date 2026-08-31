using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalAp6R3Tests
{
	private static string ReadAppSource(string relativePath)
	{
		string path = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"Werkflow.OpcUaSimulator.App",
			relativePath));
		return File.ReadAllText(path);
	}

	[Fact]
	public void AP6R3_HmiVisualTheme_DefinesContrastBrushes()
	{
		string source = ReadAppSource("VirtualMachine/Views/HmiVisualTheme.cs");
		Assert.Contains("TextPrimary", source);
		Assert.Contains("ButtonDisabledFg", source);
		Assert.Contains("SectionTitle", source);
		Assert.Contains("CreateButtonStyle", source);
	}

	[Fact]
	public void AP6R3_HmiWindow_UsesCentralTheme()
	{
		string source = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");
		Assert.Contains("HmiVisualTheme", source);
		Assert.Contains("ApplyButtonStyle", source);
	}

	[Fact]
	public void AP6R3_HmiWindow_OnClosing_CancelsAndHides()
	{
		string source = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");
		Assert.Contains("e.Cancel = true", source);
		Assert.Contains("Hide()", source);
		Assert.Contains("NotifyHmiHidden", source);
	}

	[Fact]
	public void AP6R3_App_UsesExplicitShutdownOnly()
	{
		string source = ReadAppSource("App.cs");
		Assert.Contains("ShutdownMode.OnExplicitShutdown", source);
	}

	[Fact]
	public void AP6R3_TrayService_HasNotifyIconAndMenu()
	{
		string source = ReadAppSource("VirtualMachine/Services/SimulatorTrayService.cs");
		Assert.Contains("NotifyIcon", source);
		Assert.Contains("Virtuelle Maschine öffnen", source);
		Assert.Contains("Maschine beenden", source);
		Assert.Contains("Beenden", source);
		Assert.Contains("EnsureInitialized", source);
	}

	[Fact]
	public void AP6R3_WindowService_KeepsSingleInstance()
	{
		string source = ReadAppSource("VirtualMachine/Services/VirtualMachineWindowService.cs");
		Assert.DoesNotContain("Closed +=", source);
		Assert.Contains("if (_window == null)", source);
	}

	[Machine12IntegrationFact]
	public async Task AP6R3_Evidence_ExportVerificationJson()
	{
		var report = await PhysicalAp6R3VerificationHarness.RunVerificationAsync();
		await PhysicalAp6R3VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.Ap6R3Passed, string.Join(",", report.FailedCriteria));
	}

	[Fact]
	public async Task AP6R3_Runtime_RemainsOnlineAfterSimulatedHide()
	{
		var report = await PhysicalAp6R3VerificationHarness.RunVerificationAsync();
		Assert.True(report.OpcUaRuntimeAfterClose.ServerStillOnlineAfterX);
		Assert.True(report.OpcUaRuntimeAfterClose.MachineStillRunningAfterX);
	}

	[Fact]
	public async Task AP6R3_AP6R2Regression_RemainsGreen()
	{
		var r2 = await PhysicalAp6R2VerificationHarness.RunVerificationAsync();
		Assert.True(r2.Ap6R2Passed);
	}
}
