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

public class OverviewViewModel : ObservableObject
{
	private readonly ISimulationEngine _simulationEngine;

	private readonly IConfigurationService _configurationService;

	private readonly IScenarioService _scenarioService;

	private readonly IDialogService _dialogService;

	private readonly IFaultScenarioService _faultScenarioService;

	private int _activeJobs;

	private int _totalProduced;

	private int _activeErrors;

	private int _connectedClients;

	private string _simulationState = "Gestoppt";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? startSimulationCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? pauseSimulationCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resumeSimulationCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? stopSimulationCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resetSimulationCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<ScenarioDefinition?>? startScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<ScenarioDefinition?>? stopScenarioCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<MachineCardViewModel?>? openManualControlCommand;

	public ObservableCollection<MachineCardViewModel> MachineCards { get; } = new ObservableCollection<MachineCardViewModel>();

	public ObservableCollection<ScenarioDefinition> Scenarios { get; } = new ObservableCollection<ScenarioDefinition>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ActiveJobs
	{
		get
		{
			return _activeJobs;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_activeJobs, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActiveJobs);
				_activeJobs = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActiveJobs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int TotalProduced
	{
		get
		{
			return _totalProduced;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_totalProduced, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TotalProduced);
				_totalProduced = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TotalProduced);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ActiveErrors
	{
		get
		{
			return _activeErrors;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_activeErrors, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActiveErrors);
				_activeErrors = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActiveErrors);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ConnectedClients
	{
		get
		{
			return _connectedClients;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_connectedClients, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConnectedClients);
				_connectedClients = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConnectedClients);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SimulationState
	{
		get
		{
			return _simulationState;
		}
		[MemberNotNull("_simulationState")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_simulationState, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SimulationState);
				_simulationState = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SimulationState);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartSimulationCommand => startSimulationCommand ?? (startSimulationCommand = new AsyncRelayCommand(StartSimulationAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand PauseSimulationCommand => pauseSimulationCommand ?? (pauseSimulationCommand = new AsyncRelayCommand(PauseSimulationAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResumeSimulationCommand => resumeSimulationCommand ?? (resumeSimulationCommand = new AsyncRelayCommand(ResumeSimulationAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StopSimulationCommand => stopSimulationCommand ?? (stopSimulationCommand = new AsyncRelayCommand(StopSimulationAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResetSimulationCommand => resetSimulationCommand ?? (resetSimulationCommand = new AsyncRelayCommand(ResetSimulationAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ScenarioDefinition?> StartScenarioCommand => startScenarioCommand ?? (startScenarioCommand = new AsyncRelayCommand<ScenarioDefinition>(StartScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ScenarioDefinition?> StopScenarioCommand => stopScenarioCommand ?? (stopScenarioCommand = new AsyncRelayCommand<ScenarioDefinition>(StopScenarioAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<MachineCardViewModel?> OpenManualControlCommand => openManualControlCommand ?? (openManualControlCommand = new RelayCommand<MachineCardViewModel>(OpenManualControl));

	public event EventHandler<Guid>? ManualControlRequested;

	public event EventHandler<Guid>? FaultScenariosRequested;

	public OverviewViewModel(ISimulationEngine simulationEngine, IConfigurationService configurationService, IScenarioService scenarioService, IDialogService dialogService, IFaultScenarioService faultScenarioService)
	{
		_simulationEngine = simulationEngine;
		_configurationService = configurationService;
		_scenarioService = scenarioService;
		_dialogService = dialogService;
		_faultScenarioService = faultScenarioService;
		foreach (ScenarioDefinition scenario in _scenarioService.Scenarios)
		{
			Scenarios.Add(scenario);
		}
		Refresh();
	}

	public void Refresh()
	{
		SimulationState = _simulationEngine.State.ToGermanLabel();
		TotalProduced = _simulationEngine.TotalProducedParts;
		ActiveErrors = _simulationEngine.ActiveErrorCount;
		ConnectedClients = _simulationEngine.TotalConnectedClients;
		ActiveJobs = _configurationService.Configuration.Jobs.Count(delegate(SimulationJob j)
		{
			JobState status = j.Status;
			return (uint)(status - 1) <= 1u;
		});
		MachineCards.Clear();
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines)
		{
			MachineRuntimeState runtimeState = _simulationEngine.GetRuntimeState(machine.Id);
			IReadOnlyList<FaultScenarioRuntimeInfo> activeScenarios = _faultScenarioService.GetActiveScenarios(machine.Id);
			MachineCards.Add(new MachineCardViewModel(machine, runtimeState, activeScenarios));
		}
	}

	private async Task StartSimulationAsync()
	{
		try
		{
			await _simulationEngine.StartAsync();
			Refresh();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_dialogService.ShowError("Simulation", ex2.Message);
		}
	}

	private async Task PauseSimulationAsync()
	{
		await _simulationEngine.PauseAsync();
		Refresh();
	}

	private async Task ResumeSimulationAsync()
	{
		await _simulationEngine.ResumeAsync();
		Refresh();
	}

	private async Task StopSimulationAsync()
	{
		if (_dialogService.ShowConfirmation("Simulation stoppen", "Simulation wirklich stoppen? Alle Server werden beendet."))
		{
			await _simulationEngine.StopAsync();
			Refresh();
		}
	}

	private async Task ResetSimulationAsync()
	{
		await _simulationEngine.ResetAsync();
		Refresh();
	}

	private async Task StartScenarioAsync(ScenarioDefinition? scenario)
	{
		if (scenario != null)
		{
			MachineConfiguration machine = _configurationService.Configuration.Machines.FirstOrDefault((MachineConfiguration m) => m.IsActive);
			if (machine == null)
			{
				_dialogService.ShowWarning("Szenario", "Keine aktive Maschine verfÃ¼gbar.");
				return;
			}
			await _scenarioService.StartScenarioAsync(scenario.Id, machine.Id, scenario.DurationMs);
			Refresh();
		}
	}

	private async Task StopScenarioAsync(ScenarioDefinition? scenario)
	{
		if (scenario != null)
		{
			await _scenarioService.StopScenarioAsync(scenario.Id);
			Refresh();
		}
	}

	private void OpenManualControl(MachineCardViewModel? card)
	{
		if (card != null)
		{
			this.ManualControlRequested?.Invoke(this, card.MachineId);
		}
	}
}
