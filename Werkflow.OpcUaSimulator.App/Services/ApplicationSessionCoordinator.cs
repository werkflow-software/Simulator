using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Werkflow.OpcUaSimulator.App.Services;
using Werkflow.OpcUaSimulator.App.ViewModels;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

namespace Werkflow.OpcUaSimulator.App;

public sealed class ApplicationSessionCoordinator : IVirtualMachineSessionNavigator
{
	private readonly IHost _host;
	private readonly IApplicationSessionContext _sessionContext;
	private OperatingModeSelectorWindow? _selectorWindow;
	private MainWindow? _mainWindow;

	public ApplicationSessionCoordinator(IHost host, IApplicationSessionContext sessionContext)
	{
		_host = host;
		_sessionContext = sessionContext;
	}

	public void ShowModeSelector()
	{
		_sessionContext.ClearMode();
		_selectorWindow ??= new OperatingModeSelectorWindow(this);
		_selectorWindow.ResetForDisplay();
		if (!_selectorWindow.IsVisible)
		{
			_selectorWindow.Show();
		}
		_selectorWindow.ShowInTaskbar = true;
		_selectorWindow.WindowState = WindowState.Normal;
		_selectorWindow.Activate();
		_selectorWindow.Focus();
	}

	public async Task StartClassicSimulatorAsync()
	{
		_sessionContext.SetMode(ApplicationOperatingMode.ClassicSimulator);
		await InitializeSessionAsync(ApplicationOperatingMode.ClassicSimulator);
		_mainWindow = _host.Services.GetRequiredService<MainWindow>();
		Application.Current.MainWindow = _mainWindow;
		_mainWindow.ShowInTaskbar = true;
		_mainWindow.Visibility = Visibility.Visible;
		_mainWindow.WindowState = WindowState.Normal;
		_mainWindow.Show();
		_mainWindow.Activate();
		_mainWindow.Focus();
		_host.Services.GetRequiredService<SimulatorTrayService>().EnsureInitialized();
	}

	public async Task StartVirtualMachineAsync()
	{
		_sessionContext.SetMode(ApplicationOperatingMode.VirtualMachine);
		await InitializeSessionAsync(ApplicationOperatingMode.VirtualMachine);
		VirtualMachineWindowService vmWindowService = _host.Services.GetRequiredService<VirtualMachineWindowService>();
		vmWindowService.ShowOrFocus(null);
		if (vmWindowService.Window != null)
		{
			Application.Current.MainWindow = vmWindowService.Window;
		}
		_host.Services.GetRequiredService<SimulatorTrayService>().EnsureInitialized();
	}

	public async Task EndSessionAndReturnToSelectorAsync()
	{
		ISimulationEngine simulationEngine = _host.Services.GetRequiredService<ISimulationEngine>();
		IFaultScenarioService faultScenarioService = _host.Services.GetRequiredService<IFaultScenarioService>();
		SimulatorTrayService trayService = _host.Services.GetRequiredService<SimulatorTrayService>();
		IConfigurationService configurationService = _host.Services.GetRequiredService<IConfigurationService>();

		foreach (MachineConfiguration machine in configurationService.Configuration.Machines)
		{
			await faultScenarioService.ResetMachineAsync(machine.Id);
		}

		await simulationEngine.StopAsync();
		trayService.Dispose();

		if (_sessionContext.IsVirtualMachine)
		{
			_host.Services.GetRequiredService<VirtualMachineWindowService>().HideToTray();
		}
		else if (_mainWindow != null)
		{
			_mainWindow.Hide();
			_mainWindow.ShowInTaskbar = false;
		}

		_sessionContext.ClearMode();
		ShowModeSelector();
	}

	private async Task InitializeSessionAsync(ApplicationOperatingMode operatingMode)
	{
		await _host.StartAsync();
		IConfigurationService configurationService = _host.Services.GetRequiredService<IConfigurationService>();
		await configurationService.InitializeAsync(operatingMode);
		IFaultScenarioService faultScenarioService = _host.Services.GetRequiredService<IFaultScenarioService>();
		await faultScenarioService.InitializeAsync();

		if (operatingMode == ApplicationOperatingMode.ClassicSimulator)
		{
			_host.Services.GetRequiredService<OverviewViewModel>().Refresh();
			_host.Services.GetRequiredService<FaultScenariosViewModel>().Refresh();
			_host.Services.GetRequiredService<PhysicalSignalsViewModel>().ReloadMachines();
		}
		else
		{
			_host.Services.GetRequiredService<VirtualMachineHmiViewModel>().EnsureActivated();
		}
	}
}
