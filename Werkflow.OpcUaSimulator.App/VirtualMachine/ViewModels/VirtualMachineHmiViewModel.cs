using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.App;
using Werkflow.OpcUaSimulator.App.ViewModels;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Models;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Views;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;

public sealed class VirtualMachineHmiViewModel : ObservableObject
{
	private readonly ISimulationEngine _simulationEngine;
	private readonly IConfigurationService _configurationService;
	private readonly IPhysicalSignalPublishingCoordinator _coordinator;
	private readonly IMachineServerService _machineServerService;
	private readonly IFaultScenarioService _faultScenarioService;
	private readonly IDialogService _dialogService;
	private readonly IJobDispatcher _jobDispatcher;
	private readonly ApplicationSessionCoordinator _sessionCoordinator;
	private readonly DispatcherTimer _refreshTimer;

	private MachineConfiguration? _machine;
	private MachineRuntimeState? _runtime;
	private FaultScenarioListItem? _selectedFaultScenario;
	private bool _activated;
	private int _selectedTabIndex;

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
	private string _statusBadge = "BEREIT";
	private string _nextJobText = "—";
	private string _jobPoolText = "—";
	private string _simulationSpeedText = "1x";
	private string _randomSeedText = "—";
	private string _productionSpeedText = "—";
	private string _jobChangeText = "—";
	private string _jobChangeRemainingText = "—";
	private string _processPhaseText = "—";
	private string _processPhaseEnglish = "—";
	private string _laserActiveText = "NEIN";
	private string _cuttingActiveText = "NEIN";
	private string _positioningActiveText = "NEIN";
	private string _nextActionText = "—";
	private string _pathSpeedText = "—";
	private string _xSpeedText = "—";
	private string _ySpeedText = "—";
	private string _focusText = "—";
	private string _statusTone = "idle";
	private string _remainingCounterText = "—";
	private string _partRemainingText = "—";
	private string _jobRemainingText = "—";
	private string _setupRemainingText = "—";
	private string _nozzleRemainingText = "—";
	private string _jobElapsedText = "—";

	public ObservableCollection<HmiMetricItem> OverviewMetrics { get; } = [];
	public ObservableCollection<HmiAxisPanelViewModel> AxisPanels { get; } = [];
	public ObservableCollection<HmiMotorGroupViewModel> MotorGroups { get; } = [];
	public ObservableCollection<HmiTemperatureTileViewModel> TemperatureTiles { get; } = [];
	public ObservableCollection<HmiMetricItem> ProcessMetrics { get; } = [];
	public ObservableCollection<HmiMetricItem> CoolingMetrics { get; } = [];
	public ObservableCollection<HmiMetricItem> PowerMetrics { get; } = [];
	public ObservableCollection<HmiMetricItem> VibrationMetrics { get; } = [];
	public ObservableCollection<HmiMetricItem> ProductionMetrics { get; } = [];
	public ObservableCollection<HmiSignalDisplayItem> OtherSignals { get; } = [];
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
	public string StatusBadge => _statusBadge;
	public string NextJobText => _nextJobText;
	public string JobPoolText => _jobPoolText;
	public string SimulationSpeedText => _simulationSpeedText;
	public string RandomSeedText => _randomSeedText;
	public string ProductionSpeedText => _productionSpeedText;
	public string JobChangeText => _jobChangeText;
	public string JobChangeRemainingText => _jobChangeRemainingText;
	public string ProcessPhaseText => _processPhaseText;
	public string ProcessPhaseEnglish => _processPhaseEnglish;
	public string LaserActiveText => _laserActiveText;
	public string CuttingActiveText => _cuttingActiveText;
	public string PositioningActiveText => _positioningActiveText;
	public string NextActionText => _nextActionText;
	public string PathSpeedText => _pathSpeedText;
	public string XSpeedText => _xSpeedText;
	public string YSpeedText => _ySpeedText;
	public string FocusText => _focusText;
	public string StatusTone => _statusTone;
	public string RemainingCounterText => _remainingCounterText;
	public string PartRemainingText => _partRemainingText;
	public string JobRemainingText => _jobRemainingText;
	public string SetupRemainingText => _setupRemainingText;
	public string NozzleRemainingText => _nozzleRemainingText;
	public string JobElapsedText => _jobElapsedText;

	public CuttingPlanViewModel CuttingPlan { get; } = new();

	public bool CuttingPlanNeedsGeometryReload { get; private set; }

	public bool CuttingPlanNeedsStateRedraw { get; private set; }

	public int PlanVisualToken { get; private set; }

	private string? _loadedPlanId;

	public int SelectedTabIndex
	{
		get => _selectedTabIndex;
		set => SetProperty(ref _selectedTabIndex, value);
	}

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
	public IAsyncRelayCommand ChangeJobCommand { get; }
	public IAsyncRelayCommand SelectJobCommand { get; }
	public IAsyncRelayCommand SetSimulationSpeed1xCommand { get; }
	public IAsyncRelayCommand SetSimulationSpeed2xCommand { get; }
	public IAsyncRelayCommand SetSimulationSpeed5xCommand { get; }
	public IAsyncRelayCommand SetSimulationSpeed10xCommand { get; }

	public VirtualMachineHmiViewModel(
		ISimulationEngine simulationEngine,
		IConfigurationService configurationService,
		IPhysicalSignalPublishingCoordinator coordinator,
		IMachineServerService machineServerService,
		IFaultScenarioService faultScenarioService,
		IDialogService dialogService,
		IJobDispatcher jobDispatcher,
		ApplicationSessionCoordinator sessionCoordinator)
	{
		_simulationEngine = simulationEngine;
		_configurationService = configurationService;
		_coordinator = coordinator;
		_machineServerService = machineServerService;
		_faultScenarioService = faultScenarioService;
		_dialogService = dialogService;
		_jobDispatcher = jobDispatcher;
		_sessionCoordinator = sessionCoordinator;

		StartMachineCommand = new AsyncRelayCommand(StartMachineAsync, () => CanStartMachine);
		StartProductionCommand = new AsyncRelayCommand(StartProductionAsync, CanStartProduction);
		StopProductionCommand = new AsyncRelayCommand(StopProductionAsync, CanStopProduction);
		PauseProductionCommand = new AsyncRelayCommand(PauseProductionAsync, CanPauseProduction);
		ResumeProductionCommand = new AsyncRelayCommand(ResumeProductionAsync, CanResumeProduction);
		ResetMachineCommand = new AsyncRelayCommand(ResetMachineAsync, CanResetMachine);
		ShutdownMachineCommand = new AsyncRelayCommand(ShutdownMachineAsync, () => _machine != null && _isMachineRunning);
		StartFaultScenarioCommand = new AsyncRelayCommand(StartFaultScenarioAsync, CanStartFault);
		PauseFaultScenarioCommand = new AsyncRelayCommand(PauseFaultScenarioAsync, () => SelectedFaultScenario != null);
		ResumeFaultScenarioCommand = new AsyncRelayCommand(ResumeFaultScenarioAsync, () => SelectedFaultScenario != null);
		StopFaultScenarioCommand = new AsyncRelayCommand(StopFaultScenarioAsync, () => SelectedFaultScenario != null);
		NormalOperationCommand = new AsyncRelayCommand(NormalOperationAsync, () => _machine != null);
		ChangeJobCommand = new AsyncRelayCommand(ChangeJobAsync, CanChangeOrSelectJob);
		SelectJobCommand = new AsyncRelayCommand(SelectJobAsync, CanChangeOrSelectJob);
		SetSimulationSpeed1xCommand = new AsyncRelayCommand(() => SetSimulationSpeedAsync(1.0));
		SetSimulationSpeed2xCommand = new AsyncRelayCommand(() => SetSimulationSpeedAsync(2.0));
		SetSimulationSpeed5xCommand = new AsyncRelayCommand(() => SetSimulationSpeedAsync(5.0));
		SetSimulationSpeed10xCommand = new AsyncRelayCommand(() => SetSimulationSpeedAsync(10.0));

		_refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
		_refreshTimer.Tick += (_, _) => Refresh();

		_simulationEngine.MachineStateChanged += (_, _) => UiDispatcher.Run(Refresh);
		_machineServerService.ServerStatusChanged += (_, _) => UiDispatcher.Run(Refresh);

		BuildStaticCollections();
	}

	public FaultScenarioListItem? SelectedFaultScenario => _selectedFaultScenario;

	public void SetSelectedFaultScenario(FaultScenarioListItem? item)
	{
		_selectedFaultScenario = item;
		NotifyCommandStates();
	}

	public void SetFaultIntensity(double value) => _faultIntensity = value;

	public void SetFaultTimeFactor(double value) => _faultTimeFactor = value;

	public void EnsureActivated()
	{
		if (_activated)
		{
			return;
		}

		_activated = true;
		EnsureMachineBound();
		LoadFaultScenarios();
		_refreshTimer.Start();
		Refresh();
	}

	private static readonly HmiSemantic[] ProcessSemantics =
	[
		HmiSemantic.ProcessPhase, HmiSemantic.FeedRate, HmiSemantic.ProcessDemand,
		HmiSemantic.ProcessSpeed, HmiSemantic.FocusPosition, HmiSemantic.MaterialThickness,
		HmiSemantic.CycleTime, HmiSemantic.QualityIndex, HmiSemantic.JobName, HmiSemantic.PartName
	];

	private static readonly HmiSemantic[] CoolingSemantics =
	[
		HmiSemantic.CoolingTemperature, HmiSemantic.CoolingFlow, HmiSemantic.CoolingPressure,
		HmiSemantic.CoolingPumpCurrent, HmiSemantic.CoolingPumpSpeed, HmiSemantic.CoolingFanSpeed,
		HmiSemantic.CoolingStatus
	];

	private static readonly HmiSemantic[] PowerSemantics =
		[HmiSemantic.PowerDemand, HmiSemantic.Voltage, HmiSemantic.Current];

	private static readonly HmiSemantic[] VibrationSemantics =
		[HmiSemantic.VibrationRms, HmiSemantic.VibrationPeak];

	private static readonly HmiSemantic[] ProductionSemantics =
	[
		HmiSemantic.JobName, HmiSemantic.PartName, HmiSemantic.ActualCounter,
		HmiSemantic.TargetCounter, HmiSemantic.RemainingCounter, HmiSemantic.CycleTime,
		HmiSemantic.ProductionRunning, HmiSemantic.MachineState
	];

	public int LiveSignalCount { get; private set; }

	public string? TryGetOverviewSemanticValue(HmiSemantic semantic)
	{
		var semantics = HmiSemanticRegistry.OverviewSemantics;
		for (int i = 0; i < semantics.Count; i++)
		{
			if (semantics[i] == semantic && i < OverviewMetrics.Count)
			{
				return OverviewMetrics[i].Value;
			}
		}

		return null;
	}

	public bool IsOverviewSemanticBound(HmiSemantic semantic) =>
		TryGetOverviewSemanticValue(semantic) is string value && value != "—";


	private void BuildStaticCollections()
	{
		OverviewMetrics.Clear();
		foreach (HmiSemantic semantic in HmiSemanticRegistry.OverviewSemantics)
		{
			OverviewMetrics.Add(new HmiMetricItem(HmiSemanticLabels.German(semantic)));
		}

		ProcessMetrics.Clear();
		foreach (HmiSemantic semantic in ProcessSemantics)
		{
			ProcessMetrics.Add(new HmiMetricItem(HmiSemanticLabels.German(semantic)));
		}

		CoolingMetrics.Clear();
		foreach (HmiSemantic semantic in CoolingSemantics)
		{
			CoolingMetrics.Add(new HmiMetricItem(HmiSemanticLabels.German(semantic)));
		}

		PowerMetrics.Clear();
		foreach (HmiSemantic semantic in PowerSemantics)
		{
			PowerMetrics.Add(new HmiMetricItem(HmiSemanticLabels.German(semantic)));
		}

		VibrationMetrics.Clear();
		foreach (HmiSemantic semantic in VibrationSemantics)
		{
			VibrationMetrics.Add(new HmiMetricItem(HmiSemanticLabels.German(semantic)));
		}

		ProductionMetrics.Clear();
		foreach (HmiSemantic semantic in ProductionSemantics)
		{
			ProductionMetrics.Add(new HmiMetricItem(HmiSemanticLabels.German(semantic)));
		}
	}

	private void EnsureMachineBound()
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

	private void LoadFaultScenarios()
	{
		LaserFaultScenarios.Clear();
		if (_machine == null)
		{
			return;
		}

		foreach (var definition in _faultScenarioService.GetCatalog()
			.Where(s => s.IsEnabled && s.MachineProfileIds.Any(id =>
				id.Equals(VirtualMachineContract.PhysicalProfileId, StringComparison.OrdinalIgnoreCase)))
			.OrderBy(s => s.DisplayName))
		{
			LaserFaultScenarios.Add(new FaultScenarioListItem(definition));
		}
	}

	private async Task EnsurePhysicalModeAsync()
	{
		if (_machine == null)
		{
			return;
		}

		_coordinator.TrySetGenerationMode(_machine.Id, SignalGenerationMode.Physical);
	}

	private async Task StartMachineAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.StartMachineServerAsync(_machine.Id);
		await EnsurePhysicalModeAsync();
		await _simulationEngine.AssignJobIfMissingAsync(_machine.Id);
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
			await EnsurePhysicalModeAsync();
		}

		await _simulationEngine.AssignJobIfMissingAsync(_machine.Id);
		await _simulationEngine.StartProductionAsync(_machine.Id);
		Refresh();
	}

	private async Task StopProductionAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.StopProductionAsync(_machine.Id);
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
		await _sessionCoordinator.EndSessionAndReturnToSelectorAsync();
	}

	private async Task ChangeJobAsync()
	{
		if (_machine == null)
		{
			return;
		}

		await _simulationEngine.ChangeJobAsync(_machine.Id);
		Refresh();
	}

	private async Task SelectJobAsync()
	{
		if (_machine == null)
		{
			return;
		}

		var dialog = new JobSelectionWindow();
		if (Application.Current?.MainWindow is Window owner && owner.IsLoaded)
		{
			dialog.Owner = owner;
		}

		if (dialog.ShowDialog() != true || dialog.SelectedCatalogIndex == null)
		{
			return;
		}

		if (!_machineServerService.IsRunning(_machine.Id))
		{
			await _simulationEngine.StartMachineServerAsync(_machine.Id);
			await EnsurePhysicalModeAsync();
		}

		await _simulationEngine.SelectJobAsync(_machine.Id, dialog.SelectedCatalogIndex.Value);
		Refresh();
	}

	private async Task SetSimulationSpeedAsync(double factor)
	{
		_configurationService.Configuration.Settings.SimulationSpeedFactor = factor;
		await _configurationService.SaveSettingsAsync();
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
			await EnsurePhysicalModeAsync();
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
		_runtime = _simulationEngine.GetRuntimeState(_machine.Id);

		if (_runtime != null)
		{
			_machineStateText = _runtime.State.ToGermanLabel();
			_jobName = string.IsNullOrWhiteSpace(_runtime.JobName) ? "—" : _runtime.JobName;
			_partName = string.IsNullOrWhiteSpace(_runtime.PartName) ? "—" : _runtime.PartName;
			_counterText = $"{_runtime.ActualCounter} / {_runtime.TargetCounter}";
			_errorActive = _runtime.ErrorActive;
			_errorMessage = string.IsNullOrWhiteSpace(_runtime.ErrorMessage) ? "—" : _runtime.ErrorMessage;
			_modeText = _runtime.IsJobChangeActive
				? "Jobwechsel / Einrichten"
				: _runtime.IsProducing ? "Produktion" : "Bereit";
			_statusBadge = _runtime.ErrorActive
				? "FEHLER"
				: _runtime.IsJobChangeActive
					? "EINRICHTEN"
					: _runtime.State.ToGermanLabel().ToUpperInvariant();
			if (_runtime.IsJobChangeActive)
			{
				_jobChangeText = $"Nächster Job: {_runtime.NextJobNamePreview} / {_runtime.NextPartNamePreview}";
				if (_runtime.JobChangeEndsAtUtc.HasValue)
				{
					double remainingSeconds = Math.Max(0, (_runtime.JobChangeEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds);
					int minutes = (int)remainingSeconds / 60;
					int seconds = (int)remainingSeconds % 60;
					_jobChangeRemainingText = $"Restzeit: {minutes:D2}:{seconds:D2}";
				}
				else
				{
					_jobChangeRemainingText = $"Pause: {_runtime.JobChangePauseSeconds / 60}:{_runtime.JobChangePauseSeconds % 60:D2}";
				}
			}
			else
			{
				_jobChangeText = "—";
				_jobChangeRemainingText = "—";
			}
		}

		OnPropertyChanged(nameof(OpcUaStatus));
		OnPropertyChanged(nameof(IsMachineRunning));
		OnPropertyChanged(nameof(CanStartMachine));
		OnPropertyChanged(nameof(MachineStateText));
		OnPropertyChanged(nameof(JobName));
		OnPropertyChanged(nameof(PartName));
		OnPropertyChanged(nameof(CounterText));
		OnPropertyChanged(nameof(ErrorActive));
		OnPropertyChanged(nameof(ErrorMessage));
		OnPropertyChanged(nameof(ModeText));
		OnPropertyChanged(nameof(StatusBadge));
		OnPropertyChanged(nameof(JobChangeText));
		OnPropertyChanged(nameof(JobChangeRemainingText));

		UpdateJobPoolSummary();
		_simulationSpeedText = $"{_configurationService.Configuration.Settings.SimulationSpeedFactor:0.#}x";
		_randomSeedText = _simulationEngine.CurrentSeed.ToString();
		_productionSpeedText = _machine.ProductionSpeedFactor.ToString("0.#");
		OnPropertyChanged(nameof(SimulationSpeedText));
		OnPropertyChanged(nameof(RandomSeedText));
		OnPropertyChanged(nameof(ProductionSpeedText));

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
			RefreshProcessMotionState(session);
			RefreshCuttingPlan(session);
			RefreshBoundSignals(session);
			RefreshTimeDisplays(session);
		}
		else
		{
			RefreshTimeDisplays(null);
		}

		NotifyProcessStateProperties();
		NotifyCommandStates();
	}

	public void ClearCuttingPlanRefreshFlags()
	{
		CuttingPlanNeedsGeometryReload = false;
		CuttingPlanNeedsStateRedraw = false;
	}

	private void RefreshCuttingPlan(PhysicalMachineSession session)
	{
		LaserKinematicsState kinematics = session.Simulation.Kinematics;
		CuttingPlan? displayPlan = kinematics.DisplayCuttingPlan ?? kinematics.ActiveCuttingPlan;
		if (!kinematics.IsEnabled || displayPlan == null)
		{
			return;
		}

		CuttingPlan.PlanId = displayPlan.PlanId;
		CuttingPlan.JobId = displayPlan.JobId;
		CuttingPlan.PartName = _partName;
		CuttingPlan.MaterialText = session.Simulation.Job.MaterialName;
		CuttingPlan.ThicknessText = $"{session.Simulation.Job.MaterialThicknessMm:0.#} mm";
		CuttingPlan.PartsOnSheet = displayPlan.PartCount;
		CuttingPlan.PartsProcessedOnSheet = displayPlan.Parts.Count(p => p.State == CuttingPartState.Completed);
		CuttingPlan.CurrentSheetPartIndex = kinematics.SheetPartIndex;
		CuttingPlan.CurrentContourIndex = kinematics.CurrentContourIndex + 1;
		if (kinematics.SheetPartIndex >= 0 && kinematics.SheetPartIndex < displayPlan.Parts.Count)
		{
			CuttingPlan.ContourCountOnPart = displayPlan.Parts[kinematics.SheetPartIndex].Contours.Count;
		}
		CuttingPlan.CurrentPhaseText = _processPhaseText;
		CuttingPlan.HeadX = kinematics.X;
		CuttingPlan.HeadY = kinematics.Y;
		CuttingPlan.SegmentStartX = kinematics.SegmentStartX;
		CuttingPlan.SegmentStartY = kinematics.SegmentStartY;
		CuttingPlan.ShowRapidLine = kinematics.MotionPhase is LaserMotionPhase.RapidPositioning or LaserMotionPhase.Repositioning;
		CuttingPlan.IsPiercing = kinematics.MotionPhase == LaserMotionPhase.Piercing;
		CuttingPlan.NextJobPreview = _nextJobText;

		if (_loadedPlanId != displayPlan.PlanId)
		{
			_loadedPlanId = displayPlan.PlanId;
			CuttingPlan.Parts.Clear();
			foreach (CuttingPlanPart part in displayPlan.Parts)
			{
				var partVm = new CuttingPlanPartViewModel
				{
					PartIndex = part.PartIndex,
					Label = part.Label,
					State = part.State,
					Contours = part.Contours.Select(c => new CuttingPlanContourViewModel
					{
						ContourIndex = c.ContourIndex,
						IsInnerContour = c.IsInnerContour,
						Points = c.Vertices.Select(v => (v.X, v.Y)).ToList(),
						State = c.State
					}).ToList()
				};
				CuttingPlan.Parts.Add(partVm);
			}
			CuttingPlanNeedsGeometryReload = true;
		}
		else
		{
			for (int i = 0; i < displayPlan.Parts.Count && i < CuttingPlan.Parts.Count; i++)
			{
				CuttingPlanPart source = displayPlan.Parts[i];
				CuttingPlanPartViewModel target = CuttingPlan.Parts[i];
				if (target.State != source.State)
				{
					target.State = source.State;
					CuttingPlanNeedsStateRedraw = true;
				}

				for (int c = 0; c < source.Contours.Count && c < target.Contours.Count; c++)
				{
					if (target.Contours[c].State != source.Contours[c].State)
					{
						target.Contours[c].State = source.Contours[c].State;
						CuttingPlanNeedsStateRedraw = true;
					}
				}
			}
		}

		PlanVisualToken++;
		OnPropertyChanged(nameof(PlanVisualToken));
		if (CuttingPlanNeedsGeometryReload || CuttingPlanNeedsStateRedraw)
		{
			ClearCuttingPlanRefreshFlags();
		}

		CuttingPlan.NotifyDisplayRefresh();
	}

	private void RefreshProcessMotionState(PhysicalMachineSession session)
	{
		LaserKinematicsState kinematics = session.Simulation.Kinematics;
		if (kinematics.IsEnabled)
		{
			_processPhaseText = LaserMotionPhaseLabels.ToGerman(kinematics.MotionPhase);
			_processPhaseEnglish = LaserMotionPhaseLabels.ToEnglish(kinematics.MotionPhase);
			bool laser = kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting;
			bool cutting = kinematics.MotionPhase == LaserMotionPhase.Cutting;
			bool positioning = kinematics.MotionPhase is LaserMotionPhase.RapidPositioning
				or LaserMotionPhase.Repositioning or LaserMotionPhase.JobChange;
			_laserActiveText = laser ? "JA" : "NEIN";
			_cuttingActiveText = cutting ? "JA" : "NEIN";
			_positioningActiveText = positioning ? "JA" : "NEIN";
			_nextActionText = kinematics.NextActionHint;
			_pathSpeedText = $"{kinematics.PathSpeedMmPerS:0} mm/s";
			_statusTone = ResolveStatusTone(kinematics.MotionPhase, _errorActive);
		}
		else
		{
			_processPhaseText = session.Simulation.CurrentPhase.ToString();
			_processPhaseEnglish = session.Simulation.CurrentPhase.ToString();
			_laserActiveText = "NEIN";
			_cuttingActiveText = "NEIN";
			_positioningActiveText = "NEIN";
			_nextActionText = "—";
			_pathSpeedText = "—";
			_statusTone = _errorActive ? "error" : "idle";
		}

		if (_runtime != null)
		{
			_remainingCounterText = Math.Max(0, _runtime.TargetCounter - _runtime.ActualCounter).ToString();
		}
	}

	private static string ResolveStatusTone(LaserMotionPhase phase, bool errorActive)
	{
		if (errorActive)
		{
			return "error";
		}

		return phase switch
		{
			LaserMotionPhase.Cutting => "cutting",
			LaserMotionPhase.Piercing => "running",
			LaserMotionPhase.RapidPositioning or LaserMotionPhase.Repositioning => "running",
			LaserMotionPhase.Setup or LaserMotionPhase.JobChange or LaserMotionPhase.NozzleChange => "setup",
			LaserMotionPhase.Idle => "idle",
			_ => "idle"
		};
	}

	private void RefreshTimeDisplays(PhysicalMachineSession? session)
	{
		if (_machine == null)
		{
			return;
		}

		bool isPaused = _runtime?.State == MachineState.Paused;
		bool isJobChange = _runtime?.IsJobChangeActive == true
			|| session?.Simulation.IsJobChangePauseActive == true
			|| session?.Simulation.Kinematics.MotionPhase is LaserMotionPhase.JobChange
				or LaserMotionPhase.Setup
				or LaserMotionPhase.NozzleChange;

		if (isJobChange)
		{
			_partRemainingText = "—";
			_jobRemainingText = "—";
			double setupSeconds = _simulationEngine.GetSetupRemainingSeconds(_machine.Id);
			_setupRemainingText = setupSeconds > 0.0 ? FormatDuration(setupSeconds) : "—";
			double nozzleSeconds = _simulationEngine.GetNozzleChangeRemainingSeconds(_machine.Id);
			_nozzleRemainingText = nozzleSeconds > 0.0 ? FormatDuration(nozzleSeconds) : "—";
		}
		else
		{
			(double partSeconds, double jobSeconds) = _simulationEngine.GetProductionTimeEstimates(_machine.Id);
			_partRemainingText = partSeconds > 0.0 ? FormatDuration(partSeconds) : "—";
			_jobRemainingText = jobSeconds > 0.0 ? FormatDuration(jobSeconds) : "—";
			_setupRemainingText = "—";
			_nozzleRemainingText = "—";
		}

		if (session != null && (_runtime?.IsProducing == true || isPaused))
		{
			double elapsedSeconds = isPaused
				? session.Simulation.FrozenProductionElapsedSeconds
				: session.Simulation.ProductionRunStartedAtUtc.HasValue
					? (DateTimeOffset.UtcNow - session.Simulation.ProductionRunStartedAtUtc.Value).TotalSeconds
					: session.Simulation.FrozenProductionElapsedSeconds;
			_jobElapsedText = elapsedSeconds > 0.0 ? FormatDuration(elapsedSeconds) : "—";
		}
		else
		{
			_jobElapsedText = "—";
		}

		OnPropertyChanged(nameof(PartRemainingText));
		OnPropertyChanged(nameof(JobRemainingText));
		OnPropertyChanged(nameof(SetupRemainingText));
		OnPropertyChanged(nameof(NozzleRemainingText));
		OnPropertyChanged(nameof(JobElapsedText));
	}

	private static string FormatDuration(double seconds)
	{
		if (seconds <= 0.0)
		{
			return "00:00";
		}

		int total = (int)Math.Ceiling(seconds);
		int hours = total / 3600;
		int minutes = (total % 3600) / 60;
		int secs = total % 60;
		return hours > 0 ? $"{hours:D2}:{minutes:D2}:{secs:D2}" : $"{minutes:D2}:{secs:D2}";
	}

	private void NotifyProcessStateProperties()
	{
		OnPropertyChanged(nameof(ProcessPhaseText));
		OnPropertyChanged(nameof(ProcessPhaseEnglish));
		OnPropertyChanged(nameof(LaserActiveText));
		OnPropertyChanged(nameof(CuttingActiveText));
		OnPropertyChanged(nameof(PositioningActiveText));
		OnPropertyChanged(nameof(NextActionText));
		OnPropertyChanged(nameof(PathSpeedText));
		OnPropertyChanged(nameof(XSpeedText));
		OnPropertyChanged(nameof(YSpeedText));
		OnPropertyChanged(nameof(FocusText));
		OnPropertyChanged(nameof(StatusTone));
		OnPropertyChanged(nameof(RemainingCounterText));
	}

	private void UpdateJobPoolSummary()
	{
		if (_machine == null)
		{
			return;
		}

		var config = _configurationService.Configuration;
		_jobPoolText = $"{FixedSimulationCatalog.JobCount} Jobs im festen Pool";

		if (_runtime != null && _runtime.IsJobChangeActive)
		{
			_nextJobText = $"{_runtime.NextJobNamePreview} / {_runtime.NextPartNamePreview} ({_runtime.NextTargetQuantityPreview})";
		}
		else if (_runtime != null && _runtime.CurrentJobCatalogIndex >= 0)
		{
			int nextIndex = FixedSimulationCatalog.GetNextCatalogIndex(_runtime.CurrentJobCatalogIndex);
			var nextDef = FixedSimulationCatalog.GetDefinition(nextIndex);
			_nextJobText = $"{nextDef.JobName} / {nextDef.PartName} ({nextDef.TargetQuantity})";
		}
		else
		{
			var first = FixedSimulationCatalog.GetDefinition(0);
			_nextJobText = $"{first.JobName} / {first.PartName} ({first.TargetQuantity})";
		}
		OnPropertyChanged(nameof(JobPoolText));
		OnPropertyChanged(nameof(NextJobText));
	}

	private void RefreshBoundSignals(PhysicalMachineSession session)
	{
		var runtimeById = session.Runtime.Signals.ToDictionary(s => s.SignalId, StringComparer.OrdinalIgnoreCase);
		var enabled = session.Profile.Signals.Where(s => s.IsEnabled).ToList();
		int liveCount = 0;

		UpdateMetricCollection(OverviewMetrics, HmiSemanticRegistry.OverviewSemantics, session.Profile, runtimeById, _runtime, ref liveCount);
		UpdateMetricCollection(ProcessMetrics, ProcessSemantics, session.Profile, runtimeById, _runtime, ref liveCount);
		UpdateMetricCollection(CoolingMetrics, CoolingSemantics, session.Profile, runtimeById, _runtime, ref liveCount);
		UpdateMetricCollection(PowerMetrics, PowerSemantics, session.Profile, runtimeById, _runtime, ref liveCount);
		UpdateMetricCollection(VibrationMetrics, VibrationSemantics, session.Profile, runtimeById, _runtime, ref liveCount);
		UpdateMetricCollection(ProductionMetrics, ProductionSemantics, session.Profile, runtimeById, _runtime, ref liveCount);

		RefreshAxisPanels(enabled, runtimeById);
		RefreshMotorGroups(enabled, runtimeById);
		RefreshTemperatureTiles(enabled, runtimeById);
		RefreshOtherSignals(enabled, runtimeById);

		_xSpeedText = FormatSignalValue(runtimeById, "Axis01.Speed", "mm/s");
		_ySpeedText = FormatSignalValue(runtimeById, "Axis02.Speed", "mm/s");
		_focusText = FormatSignalValue(runtimeById, "Process.FocusPosition", "mm");

		LiveSignalCount = liveCount;
		OnPropertyChanged(nameof(LiveSignalCount));
	}

	private static void UpdateMetricCollection(
		ObservableCollection<HmiMetricItem> metrics,
		IReadOnlyList<HmiSemantic> semantics,
		PhysicalMachineProfile profile,
		IReadOnlyDictionary<string, SignalRuntimeState> runtimeById,
		MachineRuntimeState? machineRuntime,
		ref int liveCount)
	{
		for (int i = 0; i < semantics.Count && i < metrics.Count; i++)
		{
			var binding = HmiSemanticResolver.Resolve(semantics[i], profile, runtimeById, machineRuntime);
			metrics[i].Value = binding.FormattedValue;
			if (binding.IsBound)
			{
				liveCount++;
			}
		}
	}

	private void RefreshAxisPanels(IReadOnlyList<SignalDefinition> enabled, IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		var axisGroups = enabled
			.Where(s => s.Category == SignalCategory.Axis)
			.GroupBy(s => HmiSignalCatalog.ExtractAxisKey(s.SignalId) ?? "Allgemein")
			.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
			.ToList();

		while (AxisPanels.Count < axisGroups.Count)
		{
			AxisPanels.Add(new HmiAxisPanelViewModel());
		}

		while (AxisPanels.Count > axisGroups.Count)
		{
			AxisPanels.RemoveAt(AxisPanels.Count - 1);
		}

		for (int i = 0; i < axisGroups.Count; i++)
		{
			var group = axisGroups[i];
			var panel = AxisPanels[i];
			panel.AxisName = group.Key;
			panel.Position = FormatBySuffix(group, "Position", runtimeById);
			panel.TargetPosition = FormatBySuffix(group, "TargetPosition", runtimeById);
			panel.Speed = FormatBySuffix(group, "Speed", runtimeById);
			panel.Current = FormatBySuffix(group, "MotorCurrent", runtimeById);
			panel.Torque = FormatBySuffix(group, "MotorTorque", runtimeById);
			panel.Temperature = FormatBySuffix(group, "MotorTemperature", runtimeById);
			panel.Load = FormatBySuffix(group, "Load", runtimeById);
			panel.PositionError = FormatBySuffix(group, "PositionError", runtimeById);
			panel.ServoState = FormatBySuffix(group, "ServoState", runtimeById);
		}
	}

	private void RefreshMotorGroups(IReadOnlyList<SignalDefinition> enabled, IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		MotorGroups.Clear();
		var driveSignals = enabled.Where(s => s.Category == SignalCategory.Drive).ToList();
		var groups = new (string name, Func<SignalDefinition, bool> filter)[]
		{
			("Axis Drives", s => s.SignalId.Contains("Axis", StringComparison.OrdinalIgnoreCase)),
			("Pumps", s => s.SignalId.Contains("Pump", StringComparison.OrdinalIgnoreCase)),
			("Fans", s => s.SignalId.Contains("Fan", StringComparison.OrdinalIgnoreCase)),
			("Process Drive", s => s.SignalId.Contains("Spindle", StringComparison.OrdinalIgnoreCase) || s.SignalId.Contains("Laser", StringComparison.OrdinalIgnoreCase))
		};

		foreach (var (name, filter) in groups)
		{
			var signals = driveSignals.Where(filter).OrderBy(s => s.SignalId).ToList();
			if (signals.Count == 0)
			{
				continue;
			}

			var groupVm = new HmiMotorGroupViewModel { GroupName = name };
			foreach (var def in signals)
			{
				var metric = new HmiMetricItem(HmiSignalCatalog.FormatDisplayName(def), def.EngineeringUnit);
				double value = runtimeById.TryGetValue(def.SignalId, out var rt) ? rt.CurrentValue : def.InitialValue;
				metric.Value = HmiSignalCatalog.FormatValue(def, value);
				groupVm.Metrics.Add(metric);
			}
			MotorGroups.Add(groupVm);
		}
	}

	private void RefreshTemperatureTiles(IReadOnlyList<SignalDefinition> enabled, IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		TemperatureTiles.Clear();
		foreach (var def in enabled.Where(s => s.Category == SignalCategory.Thermal).OrderBy(s => s.SignalId))
		{
			double value = runtimeById.TryGetValue(def.SignalId, out var rt) ? rt.CurrentValue : def.InitialValue;
			var tile = new HmiTemperatureTileViewModel
			{
				Label = HmiSignalCatalog.FormatDisplayName(def),
				Unit = def.EngineeringUnit ?? "",
				Value = HmiSignalCatalog.FormatValue(def, value),
				NormalRange = def.NormalMaximum > def.NormalMinimum
					? $"{def.NormalMinimum:0.##} – {def.NormalMaximum:0.##} {def.EngineeringUnit}".Trim()
					: ""
			};

			if (def.HardMaximum > 0 && value > def.HardMaximum * 0.95)
			{
				tile.IsError = true;
			}
			else if (def.NormalMaximum > def.NormalMinimum && value > def.NormalMaximum)
			{
				tile.IsWarning = true;
			}

			TemperatureTiles.Add(tile);
		}
	}

	private void RefreshOtherSignals(IReadOnlyList<SignalDefinition> enabled, IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		var mappedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var tab in HmiSignalCatalog.TabDefinitions.Where(t => t.TabKey != "other"))
		{
			foreach (var s in enabled.Where(s => tab.Categories.Contains(s.Category)))
			{
				mappedIds.Add(s.SignalId);
			}
		}

		OtherSignals.Clear();
		foreach (var def in enabled.Where(s => !mappedIds.Contains(s.SignalId)).OrderBy(s => s.SignalId))
		{
			double value = runtimeById.TryGetValue(def.SignalId, out var rt) ? rt.CurrentValue : def.InitialValue;
			OtherSignals.Add(new HmiSignalDisplayItem
			{
				SignalId = def.SignalId,
				DisplayName = HmiSignalCatalog.FormatDisplayName(def),
				FormattedValue = HmiSignalCatalog.FormatValue(def, value),
				Unit = def.EngineeringUnit ?? "",
				GroupKey = def.Category.ToString()
			});
		}
	}

	private static string FormatSignalValue(
		IReadOnlyDictionary<string, SignalRuntimeState> runtimeById,
		string signalId,
		string unit)
	{
		if (!runtimeById.TryGetValue(signalId, out SignalRuntimeState? state))
		{
			return "—";
		}

		return $"{state.CurrentValue:0.##} {unit}".Trim();
	}

	private static string FormatBySuffix(IEnumerable<SignalDefinition> group, string suffix, IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		var def = group.FirstOrDefault(s => s.SignalId.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase));
		if (def == null)
		{
			return "—";
		}

		double value = runtimeById.TryGetValue(def.SignalId, out var rt) ? rt.CurrentValue : def.InitialValue;
		return HmiSignalCatalog.FormatValue(def, value);
	}

	private bool CanChangeOrSelectJob() =>
		_machine != null && _isMachineRunning && _runtime != null && !_runtime.IsJobChangeActive;

	private bool CanStartProduction() =>
		_machine != null && _isMachineRunning && _runtime != null &&
		!_runtime.ErrorActive && !_runtime.IsJobChangeActive &&
		_runtime.State is MachineState.Idle or MachineState.Paused;

	private bool CanStopProduction() =>
		_machine != null && _isMachineRunning && _runtime != null &&
		!_runtime.ErrorActive &&
		_runtime.State is MachineState.Running or MachineState.Paused;

	private bool CanPauseProduction() =>
		_machine != null && _isMachineRunning && _runtime != null &&
		_runtime.State == MachineState.Running && _runtime.IsProducing;

	private bool CanResumeProduction() =>
		_machine != null && _isMachineRunning && _runtime != null &&
		_runtime.State == MachineState.Paused;

	private bool CanResetMachine() => _machine != null && _isMachineRunning;

	private bool CanStartFault() => _machine != null && SelectedFaultScenario != null && _isMachineRunning;

	private void NotifyCommandStates()
	{
		StartMachineCommand.NotifyCanExecuteChanged();
		StartProductionCommand.NotifyCanExecuteChanged();
		StopProductionCommand.NotifyCanExecuteChanged();
		PauseProductionCommand.NotifyCanExecuteChanged();
		ResumeProductionCommand.NotifyCanExecuteChanged();
		ResetMachineCommand.NotifyCanExecuteChanged();
		ShutdownMachineCommand.NotifyCanExecuteChanged();
		StartFaultScenarioCommand.NotifyCanExecuteChanged();
		PauseFaultScenarioCommand.NotifyCanExecuteChanged();
		ResumeFaultScenarioCommand.NotifyCanExecuteChanged();
		StopFaultScenarioCommand.NotifyCanExecuteChanged();
		NormalOperationCommand.NotifyCanExecuteChanged();
		ChangeJobCommand.NotifyCanExecuteChanged();
		SelectJobCommand.NotifyCanExecuteChanged();
	}
}
