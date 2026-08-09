using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class FaultScenariosViewModel : ObservableObject
{
	private readonly IFaultScenarioService _faultScenarioService;

	private readonly IConfigurationService _configurationService;

	private readonly IDialogService _dialogService;

	private MachineConfiguration? _selectedMachine;

	private FaultScenarioListItem? _selectedScenario;

	private double _intensity = 1.0;

	private double _timeFactor = 10.0;

	private bool _autoThresholdFault = true;

	private bool _controlRun = false;

	private string _runtimeStatus = "â€”";

	private string _scenarioPhase = "â€”";

	private string _recoveryProgress = "â€”";

	private bool _diagnosisMode;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? startScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? pauseScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resumeScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? stopScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? cancelScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resetMachineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? toggleDiagnosisModeCommand;

	public ObservableCollection<MachineConfiguration> Machines { get; } = new ObservableCollection<MachineConfiguration>();

	public ObservableCollection<FaultScenarioListItem> Scenarios { get; } = new ObservableCollection<FaultScenarioListItem>();

	public ObservableCollection<FaultScenarioRuntimeInfo> ActiveScenarios { get; } = new ObservableCollection<FaultScenarioRuntimeInfo>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public MachineConfiguration? SelectedMachine
	{
		get
		{
			return _selectedMachine;
		}
		set
		{
			if (!EqualityComparer<MachineConfiguration>.Default.Equals(_selectedMachine, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedMachine);
				_selectedMachine = value;
				OnSelectedMachineChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedMachine);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public FaultScenarioListItem? SelectedScenario
	{
		get
		{
			return _selectedScenario;
		}
		set
		{
			if (!EqualityComparer<FaultScenarioListItem>.Default.Equals(_selectedScenario, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedScenario);
				_selectedScenario = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedScenario);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double Intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_intensity, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Intensity);
				_intensity = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Intensity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double TimeFactor
	{
		get
		{
			return _timeFactor;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_timeFactor, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TimeFactor);
				_timeFactor = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TimeFactor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AutoThresholdFault
	{
		get
		{
			return _autoThresholdFault;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_autoThresholdFault, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AutoThresholdFault);
				_autoThresholdFault = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AutoThresholdFault);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ControlRun
	{
		get
		{
			return _controlRun;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_controlRun, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ControlRun);
				_controlRun = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ControlRun);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string RuntimeStatus
	{
		get
		{
			return _runtimeStatus;
		}
		[MemberNotNull("_runtimeStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_runtimeStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RuntimeStatus);
				_runtimeStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RuntimeStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ScenarioPhase
	{
		get
		{
			return _scenarioPhase;
		}
		[MemberNotNull("_scenarioPhase")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_scenarioPhase, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ScenarioPhase);
				_scenarioPhase = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ScenarioPhase);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string RecoveryProgress
	{
		get
		{
			return _recoveryProgress;
		}
		[MemberNotNull("_recoveryProgress")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_recoveryProgress, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RecoveryProgress);
				_recoveryProgress = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RecoveryProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DiagnosisMode
	{
		get
		{
			return _diagnosisMode;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_diagnosisMode, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DiagnosisMode);
				_diagnosisMode = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DiagnosisMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartScenarioCommand => startScenarioCommand ?? (startScenarioCommand = new AsyncRelayCommand(StartScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand PauseScenarioCommand => pauseScenarioCommand ?? (pauseScenarioCommand = new AsyncRelayCommand(PauseScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResumeScenarioCommand => resumeScenarioCommand ?? (resumeScenarioCommand = new AsyncRelayCommand(ResumeScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StopScenarioCommand => stopScenarioCommand ?? (stopScenarioCommand = new AsyncRelayCommand(StopScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CancelScenarioCommand => cancelScenarioCommand ?? (cancelScenarioCommand = new AsyncRelayCommand(CancelScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResetMachineCommand => resetMachineCommand ?? (resetMachineCommand = new AsyncRelayCommand(ResetMachineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleDiagnosisModeCommand => toggleDiagnosisModeCommand ?? (toggleDiagnosisModeCommand = new RelayCommand(ToggleDiagnosisMode));

	public FaultScenariosViewModel(IFaultScenarioService faultScenarioService, IConfigurationService configurationService, IDialogService dialogService)
	{
		_faultScenarioService = faultScenarioService;
		_configurationService = configurationService;
		_dialogService = dialogService;
		foreach (MachineConfiguration item in configurationService.Configuration.Machines.Where((MachineConfiguration m) => m.IsActive && !string.IsNullOrWhiteSpace(m.PhysicalProfileId)))
		{
			Machines.Add(item);
		}
		SelectedMachine = Machines.FirstOrDefault();
		LoadScenarios();
	}

	public void Refresh()
	{
		Machines.Clear();
		foreach (MachineConfiguration item in _configurationService.Configuration.Machines.Where((MachineConfiguration m) => m.IsActive && !string.IsNullOrWhiteSpace(m.PhysicalProfileId)))
		{
			Machines.Add(item);
		}
		if (SelectedMachine == null || !Machines.Any((MachineConfiguration m) => m.Id == SelectedMachine.Id))
		{
			SelectedMachine = Machines.FirstOrDefault();
		}

		LoadScenarios();

		if (SelectedMachine == null)
		{
			return;
		}
		ActiveScenarios.Clear();
		foreach (FaultScenarioRuntimeInfo activeScenario in _faultScenarioService.GetActiveScenarios(SelectedMachine.Id))
		{
			ActiveScenarios.Add(activeScenario);
		}
		FaultScenarioRuntimeInfo faultScenarioRuntimeInfo = ActiveScenarios.FirstOrDefault();
		RuntimeStatus = faultScenarioRuntimeInfo?.LifecycleState.ToString() ?? "â€”";
		ScenarioPhase = faultScenarioRuntimeInfo?.CurrentPhase.ToString() ?? "â€”";
		RecoveryProgress = ((faultScenarioRuntimeInfo != null) ? $"{faultScenarioRuntimeInfo.RecoveryProgress:P0}" : "â€”");
	}

	private void LoadScenarios()
	{
		Scenarios.Clear();
		string profileId = SelectedMachine?.PhysicalProfileId;
		foreach (FaultScenarioDefinition item in _faultScenarioService.GetCatalog())
		{
			if (profileId != null && item.MachineProfileIds.Any((string p) => p.Equals(profileId, StringComparison.OrdinalIgnoreCase)))
			{
				Scenarios.Add(new FaultScenarioListItem(item));
			}
		}
		SelectedScenario = Scenarios.FirstOrDefault();
	}

	private async Task StartScenarioAsync()
	{
		if (SelectedMachine == null || SelectedScenario == null)
		{
			return;
		}
		try
		{
			await _faultScenarioService.StartAsync(new FaultScenarioStartRequest
			{
				MachineId = SelectedMachine.Id,
				ScenarioId = SelectedScenario.ScenarioId,
				Intensity = Intensity,
				TimeFactor = TimeFactor,
				AutoThresholdFaultEnabled = AutoThresholdFault,
				RunMode = (ControlRun ? FaultScenarioRunMode.NonFaultingControlRun : FaultScenarioRunMode.Normal)
			});
			Refresh();
		}
		catch (Exception ex)
		{
			_dialogService.ShowError("Fehlerszenario", ex.Message);
		}
	}

	private async Task PauseScenarioAsync()
	{
		if (SelectedMachine != null && SelectedScenario != null)
		{
			await _faultScenarioService.PauseAsync(SelectedMachine.Id, SelectedScenario.ScenarioId);
			Refresh();
		}
	}

	private async Task ResumeScenarioAsync()
	{
		if (SelectedMachine != null && SelectedScenario != null)
		{
			await _faultScenarioService.ResumeAsync(SelectedMachine.Id, SelectedScenario.ScenarioId);
			Refresh();
		}
	}

	private async Task StopScenarioAsync()
	{
		if (SelectedMachine != null && SelectedScenario != null)
		{
			await _faultScenarioService.StopAsync(SelectedMachine.Id, SelectedScenario.ScenarioId);
			Refresh();
		}
	}

	private async Task CancelScenarioAsync()
	{
		if (SelectedMachine != null && SelectedScenario != null)
		{
			await _faultScenarioService.CancelAsync(SelectedMachine.Id, SelectedScenario.ScenarioId);
			Refresh();
		}
	}

	private async Task ResetMachineAsync()
	{
		if (SelectedMachine != null)
		{
			await _faultScenarioService.ResetMachineAsync(SelectedMachine.Id);
			Refresh();
		}
	}

	private void ToggleDiagnosisMode()
	{
		if (SelectedMachine != null)
		{
			DiagnosisMode = !DiagnosisMode;
			_faultScenarioService.SetDiagnosisMode(SelectedMachine.Id, DiagnosisMode);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedMachineChanged(MachineConfiguration? value)
	{
		LoadScenarios();
		Refresh();
	}
}
