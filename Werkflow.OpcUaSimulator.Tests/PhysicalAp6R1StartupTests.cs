using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalAp6R1StartupTests
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
	public void AP6R1_SimulatorTrayService_UsesDeferredWindowFactory()
	{
		string source = ReadAppSource("VirtualMachine/Services/SimulatorTrayService.cs");
		Assert.Contains("Func<VirtualMachineWindowService>", source);
		Assert.DoesNotContain("VirtualMachineWindowService _virtualMachineWindowService;", source);
	}

	[Fact]
	public void AP6R1_VirtualMachineHmiViewModel_HasDeferredActivation()
	{
		string source = ReadAppSource("VirtualMachine/ViewModels/VirtualMachineHmiViewModel.cs");
		Assert.Contains("void EnsureActivated()", source);
		Assert.DoesNotContain("_refreshTimer.Start();", source.Split("EnsureActivated")[0]);
	}

	[Fact]
	public void AP6R1_VirtualMachineWindowService_ActivatesOnShow()
	{
		string source = ReadAppSource("VirtualMachine/Services/VirtualMachineWindowService.cs");
		Assert.Contains("_viewModel.EnsureActivated()", source);
	}

	[Fact]
	public void AP6R1_VirtualMachineHmiWindow_OnClosing_CancelsClose()
	{
		string source = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");
		Assert.Contains("e.Cancel = true", source);
		Assert.Contains("Hide()", source);
	}

	[Fact]
	public void AP6R1_App_OnStartup_ShowsMainWindowExplicitly()
	{
		string source = ReadAppSource("App.cs");
		string onStartup = source.Split("protected override void OnStartup", 2)[1].Split("private IHost CreateHost", 2)[0];
		Assert.Contains("ShowModeSelector()", onStartup);
		Assert.Contains("ShutdownMode.OnExplicitShutdown", onStartup);
	}

	[Fact]
	public void AP6R1_App_RegistersVirtualMachineWindowFactoryBeforeTray()
	{
		string source = ReadAppSource("App.cs");
		int windowIdx = source.IndexOf("AddSingleton<VirtualMachineWindowService>", StringComparison.Ordinal);
		int trayIdx = source.IndexOf("AddSingleton<SimulatorTrayService>", StringComparison.Ordinal);
		int factoryIdx = source.IndexOf("AddSingleton<Func<VirtualMachineWindowService>>", StringComparison.Ordinal);
		Assert.True(windowIdx > 0);
		Assert.True(trayIdx > windowIdx);
		Assert.True(factoryIdx > windowIdx && factoryIdx < trayIdx);
	}

	[Fact]
	public void AP6R1_App_CompleteStartup_InitializesTrayService()
	{
		string source = ReadAppSource("App.cs");
		Assert.Contains("AddSingleton<SimulatorTrayService>", source);
		Assert.Contains("GetService<SimulatorTrayService>()?.Dispose()", source);
		Assert.Contains("AddSingleton<Func<VirtualMachineWindowService>>", source);
	}

	[Fact]
	public void AP6R1_MainWindow_OnClosing_HidesToTray()
	{
		string source = ReadAppSource("MainWindow.cs");
		Assert.Contains("protected override void OnClosing", source);
		Assert.Contains("e.Cancel = true", source);
		Assert.Contains("Hide()", source);
		Assert.Contains("NotifyMainWindowHidden", source);
	}
}
