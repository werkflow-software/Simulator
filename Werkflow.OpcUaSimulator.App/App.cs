using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Werkflow.OpcUaSimulator.App.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.App.ViewModels;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Validation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Werkflow.OpcUaSimulator.Core.Services;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;

namespace Werkflow.OpcUaSimulator.App;

public class App : Application
{
	private IHost? _host;

	private bool _contentLoaded;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		try
		{
			_host = CreateHost();
			MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
			MainWindow = mainWindow;
			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			mainWindow.Visibility = Visibility.Visible;
			mainWindow.ShowInTaskbar = true;
			mainWindow.WindowState = WindowState.Normal;
			mainWindow.Show();
			mainWindow.Activate();
			mainWindow.Focus();
			_ = CompleteStartupAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "Werkflow OPC UA Simulator – Startup-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown(-1);
		}
	}

	private async Task CompleteStartupAsync()
	{
		try
		{
			await _host!.StartAsync();
			IConfigurationService configurationService = _host.Services.GetRequiredService<IConfigurationService>();
			await configurationService.InitializeAsync();
			IFaultScenarioService faultScenarioService = _host.Services.GetRequiredService<IFaultScenarioService>();
			await faultScenarioService.InitializeAsync();

			_host.Services.GetRequiredService<OverviewViewModel>().Refresh();
			_host.Services.GetRequiredService<FaultScenariosViewModel>().Refresh();
			_host.Services.GetRequiredService<PhysicalSignalsViewModel>().ReloadMachines();
			_host.Services.GetRequiredService<SimulatorTrayService>().EnsureInitialized();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "Werkflow OPC UA Simulator – Startup-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown(-1);
		}
	}

	private IHost CreateHost()
	{
		return Host.CreateDefaultBuilder().ConfigureLogging(delegate(ILoggingBuilder builder)
		{
			builder.AddDebug();
		}).ConfigureServices(delegate(IServiceCollection services)
		{
			services.AddSingleton<ILogService, LogService>();
			services.AddSingleton<IJobGenerator, JobGenerator>();
			services.AddSingleton<IConfigurationService, ConfigurationService>();
			services.AddSingleton<IValidationService, ValidationService>();
			services.AddSingleton<IJobDispatcher, JobDispatcher>();
			services.AddSingleton<MachineServerService>();
			services.AddSingleton((Func<IServiceProvider, IMachineServerService>)((IServiceProvider sp) => sp.GetRequiredService<MachineServerService>()));
			services.AddSingleton((Func<IServiceProvider, IMachineValuePublisher>)((IServiceProvider sp) => sp.GetRequiredService<MachineServerService>()));
			services.AddSingleton<ISimulationEngine, SimulationEngine>();
			services.AddSingleton(sp => new Lazy<ISimulationEngine>(() => sp.GetRequiredService<ISimulationEngine>()));
			services.AddSingleton(sp => new Lazy<IMachineServerService>(() => sp.GetRequiredService<IMachineServerService>()));
			services.AddSingleton<IScenarioService, ScenarioService>();
			services.AddSingleton<IDialogService, DialogService>();
			services.AddSingleton<IPhysicalMachineProfileValidator, PhysicalMachineProfileValidator>();
			services.AddSingleton<IPhysicalMachineProfileLoader, JsonPhysicalMachineProfileLoader>();
			services.AddSingleton<IPhysicalMachineRuntimeFactory, PhysicalMachineRuntimeFactory>();
			services.AddSingleton<IPhysicalMachineSessionFactory, PhysicalMachineSessionFactory>();
			services.AddSingleton<TechnicalSignalValueGenerator>();
			services.AddSingleton<IPhysicalTimeProvider, PhysicalSimulationTimeProvider>();
			services.AddSingleton<IHiddenProcessStateEngine, HiddenProcessStateEngine>();
			services.AddSingleton<ISignalCalculationEngine, SignalCalculationEngine>();
			services.AddSingleton<IPhysicalModelValidator, PhysicalModelValidator>();
			services.AddSingleton<IPhysicalSimulationEngine, PhysicalSimulationEngine>();
			services.AddSingleton<IPhysicalRuntimeCoordinator, PhysicalRuntimeCoordinator>();
			services.AddSingleton((Func<IServiceProvider, IFaultScenarioRepository>)((IServiceProvider sp) => new JsonFaultScenarioRepository(FaultScenarioPaths.ResolveDirectory())));
			services.AddSingleton<IFaultScenarioValidator, FaultScenarioValidator>();
			services.AddSingleton<IFaultEffectCalculator, FaultEffectCalculator>();
			services.AddSingleton<IFaultRecoveryEngine, FaultRecoveryEngine>();
			services.AddSingleton<IFaultScenarioEngine, FaultScenarioEngine>();
			services.AddSingleton<IFaultScenarioRuntimeFactory, FaultScenarioRuntimeFactory>();
			services.AddSingleton<IFaultScenarioSimulationBridge, FaultScenarioSimulationBridge>();
			services.AddSingleton<IFaultScenarioService, FaultScenarioService>();
			services.AddSingleton<IPhysicalSignalTypeMapper, PhysicalSignalTypeMapper>();
			services.AddSingleton<PhysicalSignalPublishingCoordinator>();
			services.AddSingleton((Func<IServiceProvider, IPhysicalSignalPublishingCoordinator>)((IServiceProvider sp) => sp.GetRequiredService<PhysicalSignalPublishingCoordinator>()));
			services.AddSingleton<OverviewViewModel>();
			services.AddSingleton<MachinesViewModel>();
			services.AddSingleton<NodesViewModel>();
			services.AddSingleton<PhysicalSignalsViewModel>();
			services.AddSingleton<JobsViewModel>();
			services.AddSingleton<EventsViewModel>();
			services.AddSingleton<LogViewModel>();
			services.AddSingleton<SettingsViewModel>();
			services.AddSingleton<ManualControlViewModel>();
			services.AddSingleton<FaultScenariosViewModel>();
			services.AddSingleton<ExperimentsViewModel>();
			services.AddSingleton<VirtualMachineHmiViewModel>();
			services.AddSingleton<VirtualMachineWindowService>();
			services.AddSingleton<Func<VirtualMachineWindowService>>(sp => () => sp.GetRequiredService<VirtualMachineWindowService>());
			services.AddSingleton<SimulatorTrayService>();
			services.AddSingleton<IHmiTrayNotifier>(sp => sp.GetRequiredService<SimulatorTrayService>());
			services.AddSingleton<MainViewModel>();
			services.AddSingleton<MainWindow>();
		})
			.Build();
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		if (_host != null)
		{
			_host.Services.GetService<SimulatorTrayService>()?.Dispose();
			ISimulationEngine simulation = _host.Services.GetService<ISimulationEngine>();
			if (simulation != null)
			{
				await simulation.StopAsync();
			}
			await _host.StopAsync();
			_host.Dispose();
		}
		base.OnExit(e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Werkflow OPC UA Simulator;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
