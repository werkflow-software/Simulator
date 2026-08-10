using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.App.ViewModels;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;

public sealed class VirtualMachineHmiViewModel : ObservableObject
{
	private static readonly string[] OverviewSignalIds =
	[
		"Axis01.Position",
		"Axis02.Position",
		"Axis03.Position",
		"Process.FeedRate",
		"Process.LaserPowerDemand",
		"Axis01.MotorCurrent",
		"Axis01.MotorTemperature",
		"Cooling.CoolantTemperature",
		"Electrical.PowerDemand",
		"Vibration.SystemRms"
	];

	private readonly ISimulationEngine _simulationEngine;
	private readonly IConfigurationService _configurationService;
	private readonly IPhysicalSignalPublishingCoordinator _coordinator;
	private readonly IMachineServerService _machineServerService;
	private readonly IFaultScenarioService _faultScenarioService;
	private readonly IDialogService _dialogService;
	private readonly DispatcherTimer _refreshTimer;

	private MachineConfiguration? _machine;
	private FaultScenarioListItem? _selectedFaultScenario;

	private string _machineTitle = VirtualMachineContract.DisplayName;
	private string _machineStateText = "—";
	private string _opcUaStatus = "OFFLINE";
	private string _jobName = "—";
	private string _partName = "—";
	private string _counterText = "—";
	private string _clockText = DateTime.Now.ToString("HH:mm:ss");
	private string _modeText = "—";
	private string _errorMessage = "—";
	private bool _errorActive;
	private bool _isMachineRunning;
	private bool _canStartMachine;
	private string _activeTestScenario = "";
	private bool _hasActiveTestScenario;
	private double _faultIntensity = 1.0;
	private double _faultTimeFactor = 25.0;
	private string _faultRuntimeStatus = "—";

	public ObservableCollection<HmiSignalDisplayItem> OverviewSignals { get; } = [];
	public ObservableCollection<HmiTabContent> Tabs { get; } = [];
	public ObservableCollection<FaultScenarioListItem> LaserFaultScenarios { get; } = [];

	public Guid MachineId => VirtualMachineContract.MachineId;
	public string Endpoint => VirtualMachineContract.Endpoint;

	public string MachineTitle => _machineTitle;
	public string MachineStateText => _machineStateText;
	public string OpcUaStatus => _opcUaStatus;
	public string JobName => _jobName;
	public string PartName => _partName;
	public string CounterText => _counterText;
	public string ClockText => _clockText;
	public string ModeText => _modeText;
	public string ErrorMessage => _errorMessage;
	public bool ErrorActive => _errorActive;
	public bool IsMachineRunning => _isMachineRunning;
	public bool CanStartMachine => _canStartMachine;
	public string ActiveTestScenario => _activeTestScenario;
	public bool HasActiveTestScenario => _hasActiveTestScenario;
	public double FaultIntensity => _faultIntensity;
	public double FaultTimeFactor => _faultTimeFactor;
	public string FaultRuntimeStatus => _faultRuntimeStatus;

	public IAsyncRelayCommand StartMachineCommand { get; }
	public IAsyncRelayCommand StartProductionCommand { get; }
	public IAsyncRelayCommand StopProductionCommand { get; }
	public IAsyncRelayCommand PauseProductionCommand { get; }
	public IAsyncRelayCommand ResumeProductionCommand { get; }
	public IAsyncRelayCommand ResetMachineCommand { get; }
	public IAsyncRelayCommand ShutdownMachineCommand { get; }
	public IAsyncRelayCommand StartFaultScenarioCommand { get; }
	public IAsyncRelayCommand PauseFaultScenarioCommand { get; }
	public IAsyncRelayCommand ResumeFaultScenarioCommand { get; }
	public IAsyncRelayCommand StopFaultScenarioCommand { get; }
	public IAsyncRelayCommand NormalOperationCommand { get; }

	public VirtualMachineHmiViewModel(
		ISimulationEngine simulationEngine,
		IConfigurationService configurationService,
		IPhysicalSignalPublishingCoordinator coordinator,
		IMachineServerService machineServerService,
		IFaultScenarioService faultScenarioService,
		IDialogService dialogService)
	{
		_simulationEngine = simulationEngine;
		_configurationService = configurationService;
		_coordinator = coordinator;
		_machineServerService = machineServerService;
		_faultScenarioService = faultScenarioService;
		_dialogService = dialogService;

		StartMachineCommand = new AsyncRelayCommand(StartMachineAsync);
		StartProductionCommand = new AsyncRelayCommand(StartProductionAsync);
		StopProductionCommand = new AsyncRelayCommand(StopProductionAsync);
		PauseProductionCommand = new AsyncRelayCommand(PauseProductionAsync);
		ResumeProductionCommand = new AsyncRelayCommand(ResumeProductionAsync);
		ResetMachineCommand = new AsyncRelayCommand(ResetMachineAsync);
		ShutdownMachineCommand = new AsyncRelayCommand(ShutdownMachineAsync);
		StartFaultScenarioCommand = new AsyncRelayCommand(StartFaultScenarioAsync);
		PauseFaultScenarioCommand = new AsyncRelayCommand(PauseFaultScenarioAsync);
		ResumeFaultScenarioCommand = new AsyncRelayCommand(ResumeFaultScenarioAsync);
		StopFaultScenarioCommand = new AsyncRelayCommand(StopFaultScenarioAsync);
		NormalOperationCommand = new AsyncRelayCommand(NormalOperationAsync);

		_refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
		_refreshTimer.Tick += (_, _) => Refresh();
		_refreshTimer.Start();

		_simulationEngine.MachineStateChanged += (_, _) => UiDispatcher.Run(Refresh);
		_machineServerService.ServerStatusChanged += (_, _) => UiDispatcher.Run(Refresh);

		EnsureMachineBound();
		BuildTabs();
		LoadFaultScenarios();
		Refresh();
	}

	public FaultScenarioListItem? SelectedFaultScenario => _selectedFaultScenario;

	public void SetSelectedFaultScenario(FaultScenarioListItem? item) => _selectedFaultScenario = item;

	public void SetFaultIntensity(double value) => _faultIntensity = value;

	public void SetFaultTimeFactor(double value) => _faultTimeFactor = value;

	public void EnsureMachineBound()
	{
		_machine = _configurationService.Configuration.Machines
			.FirstOrDefault(m => m.Port == VirtualMachineContract.Port)
			?? _configurationService.Configuration.Machines.FirstOrDefault();

		if (_machine != null && _machine.Port == VirtualMachineContract.Port)
		{
			_machine.Id = VirtualMachineContract.MachineId;
			_machine.Name = VirtualMachineContract.DisplayName;
			_machine.PhysicalProfileId = VirtualMachineContract.PhysicalProfileId;
			_machine.UpdateEndpointFromHostPort();
		}
	}

	private void BuildTabs()
	{
		Tabs.Clear();
		foreach (var (tabKey, title, _) in HmiSignalCatalog.TabDefinitions)
		{
			Tabs.Add(new HmiTabContent { TabKey = tabKey, Title = title });
		}
	}

	private void LoadFaultScenarios()
	{
		LaserFaultScenarios.Clear();
		if (_machine == null)
		{
			return;
		}

		foreach (var definition in _faultScenarioService.GetCatalog()
			.Where(s => s.MachineProfileIds.Any(id =>
				id.Equals(VirtualMachineContract.PhysicalProfileId, StringComparison.OrdinalIgnoreCase))
				&& s.IsEnabled)
			.OrderBy(s => s.DisplayName))
		{
			LaserFaultScenarios.Add(new FaultScenarioListItem(definition));
		}
	}

	private async Task StartMachineAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.StartMachineServerAsync(_machine.Id);
		Refresh();
	}

	private async Task StartProductionAsync()
	{
		if (_machine == null)
		{
			return;
		}

		if (!_machineServerService.IsRunning(_machine.Id))
		{
			await _simulationEngine.StartMachineServerAsync(_machine.Id);
		}

		await _simulationEngine.StartProductionAsync(_machine.Id);
		Refresh();
	}

	private async Task StopProductionAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.PauseProductionAsync(_machine.Id);
		await _simulationEngine.SetMachineStateManualAsync(_machine.Id, MachineState.Idle);
		Refresh();
	}

	private async Task PauseProductionAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.PauseProductionAsync(_machine.Id);
		Refresh();
	}

	private async Task ResumeProductionAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.StartProductionAsync(_machine.Id);
		Refresh();
	}

	private async Task ResetMachineAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _faultScenarioService.ResetMachineAsync(_machine.Id);
		await _simulationEngine.ClearErrorAsync(_machine.Id);
		Refresh();
	}

	private async Task ShutdownMachineAsync()
	{
		if (_machine == null)
		{
			return;
		}

		bool confirmed = _dialogService.ShowConfirmation(
			"Maschine beenden",
			"Die virtuelle Maschine und ihr OPC-UA-Server werden beendet.\nBestehende Verbindungen werden getrennt.\n\nMaschine wirklich beenden?");

		if (!confirmed)
		{
			return;
		}

		await _simulationEngine.PauseProductionAsync(_machine.Id);
		await _simulationEngine.StopMachineServerAsync(_machine.Id);
		Refresh();
	}

	private async Task StartFaultScenarioAsync()
	{
		if (_machine == null || SelectedFaultScenario == null)
		{
			return;
		}

		if (!_machineServerService.IsRunning(_machine.Id))
		{
			await _simulationEngine.StartMachineServerAsync(_machine.Id);
		}

		await _faultScenarioService.StartAsync(new FaultScenarioStartRequest
		{
			MachineId = _machine.Id,
			ScenarioId = SelectedFaultScenario.ScenarioId,
			Intensity = _faultIntensity,
			TimeFactor = _faultTimeFactor,
			AutoThresholdFaultEnabled = true,
			AutoScenarioEndEnabled = true
		});
		Refresh();
	}

	private async Task PauseFaultScenarioAsync()
	{
		if (_machine == null || SelectedFaultScenario == null)
		{
			return;
		}

		await _faultScenarioService.PauseAsync(_machine.Id, SelectedFaultScenario.ScenarioId);
		Refresh();
	}

	private async Task ResumeFaultScenarioAsync()
	{
		if (_machine == null || SelectedFaultScenario == null)
		{
			return;
		}

		await _faultScenarioService.ResumeAsync(_machine.Id, SelectedFaultScenario.ScenarioId);
		Refresh();
	}

	private async Task StopFaultScenarioAsync()
	{
		if (_machine == null || SelectedFaultScenario == null)
		{
			return;
		}

		await _faultScenarioService.StopAsync(_machine.Id, SelectedFaultScenario.ScenarioId);
		Refresh();
	}

	private async Task NormalOperationAsync()
	{
		if (_machine == null)
		{
			return;
		}

		foreach (var active in _faultScenarioService.GetActiveScenarios(_machine.Id))
		{
			await _faultScenarioService.StopAsync(_machine.Id, active.ScenarioId);
		}

		await _faultScenarioService.ResetMachineAsync(_machine.Id);
		Refresh();
	}

	public void Refresh()
	{
		_clockText = DateTime.Now.ToString("HH:mm:ss");
		OnPropertyChanged(nameof(ClockText));
		EnsureMachineBound();

		if (_machine == null)
		{
			return;
		}

		bool serverOnline = _machineServerService.IsRunning(_machine.Id);
		_opcUaStatus = serverOnline ? "ONLINE" : "OFFLINE";
		_isMachineRunning = serverOnline;
		_canStartMachine = !serverOnline;
		OnPropertyChanged(nameof(OpcUaStatus));
		OnPropertyChanged(nameof(IsMachineRunning));
		OnPropertyChanged(nameof(CanStartMachine));

		MachineRuntimeState? runtime = _simulationEngine.GetRuntimeState(_machine.Id);
		if (runtime != null)
		{
			_machineStateText = runtime.State.ToGermanLabel();
			_jobName = runtime.JobName;
			_partName = runtime.PartName;
			_counterText = $"{runtime.ActualCounter} / {runtime.TargetCounter}";
			_errorActive = runtime.ErrorActive;
			_errorMessage = string.IsNullOrWhiteSpace(runtime.ErrorMessage) ? "—" : runtime.ErrorMessage;
			_modeText = runtime.IsProducing ? "Produktion" : "Bereit";
			OnPropertyChanged(nameof(MachineStateText));
			OnPropertyChanged(nameof(JobName));
			OnPropertyChanged(nameof(PartName));
			OnPropertyChanged(nameof(CounterText));
			OnPropertyChanged(nameof(ErrorActive));
			OnPropertyChanged(nameof(ErrorMessage));
			OnPropertyChanged(nameof(ModeText));
		}

		var active = _faultScenarioService.GetActiveScenarios(_machine.Id).FirstOrDefault();
		_hasActiveTestScenario = active != null;
		_activeTestScenario = active?.ScenarioId ?? "";
		_faultRuntimeStatus = active?.LifecycleState.ToString() ?? "Inactive";
		OnPropertyChanged(nameof(HasActiveTestScenario));
		OnPropertyChanged(nameof(ActiveTestScenario));
		OnPropertyChanged(nameof(FaultRuntimeStatus));

		PhysicalMachineSession? session = _coordinator.GetSession(_machine.Id);
		if (session != null)
		{
			RefreshSignals(session);
		}
	}

	private void RefreshSignals(PhysicalMachineSession session)
	{
		var runtimeById = session.Runtime.Signals.ToDictionary(s => s.SignalId, StringComparer.OrdinalIgnoreCase);
		var enabled = session.Profile.Signals.Where(s => s.IsEnabled).ToList();

		OverviewSignals.Clear();
		foreach (string signalId in OverviewSignalIds)
		{
			var def = enabled.FirstOrDefault(s => s.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase));
			if (def == null)
			{
				continue;
			}

			OverviewSignals.Add(CreateDisplayItem(def, runtimeById));
		}

		foreach (var tab in Tabs)
		{
			var tabDef = HmiSignalCatalog.TabDefinitions.First(t => t.TabKey == tab.TabKey);
			tab.Signals.Clear();
			tab.AxisPanels.Clear();

			var tabSignals = enabled
				.Where(s => tabDef.Categories.Contains(s.Category))
				.OrderBy(s => s.SignalId, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (tab.TabKey == "axes")
			{
				foreach (var group in tabSignals
					.GroupBy(s => HmiSignalCatalog.ExtractAxisKey(s.SignalId) ?? "Allgemein")
					.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
				{
					var panel = new HmiAxisPanel { AxisName = group.Key };
					foreach (var def in group)
					{
						panel.Signals.Add(CreateDisplayItem(def, runtimeById));
					}
					tab.AxisPanels.Add(panel);
				}
			}
			else
			{
				foreach (var def in tabSignals)
				{
					tab.Signals.Add(CreateDisplayItem(def, runtimeById));
				}
			}
		}
	}

	private static HmiSignalDisplayItem CreateDisplayItem(
		SignalDefinition definition,
		IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		double value = runtimeById.TryGetValue(definition.SignalId, out var runtime)
			? runtime.CurrentValue
			: definition.InitialValue;

		return new HmiSignalDisplayItem
		{
			SignalId = definition.SignalId,
			DisplayName = HmiSignalCatalog.FormatDisplayName(definition),
			FormattedValue = HmiSignalCatalog.FormatValue(definition, value),
			Unit = definition.EngineeringUnit,
			GroupKey = definition.Category.ToString()
		};
	}
}
