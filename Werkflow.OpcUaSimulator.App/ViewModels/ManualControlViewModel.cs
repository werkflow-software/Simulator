using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class ManualControlViewModel : ObservableObject
{
	private readonly ISimulationEngine _simulationEngine;

	private readonly IConfigurationService _configurationService;

	private Guid _machineId;

	private string _machineName = string.Empty;

	private string _partName = string.Empty;

	private string _jobName = string.Empty;

	private int _actualCounter;

	private int _targetCounter;

	private MachineState _selectedState = MachineState.Idle;

	private bool _errorActive;

	private string _errorMessage = string.Empty;

	private int _productionIntervalMs = 2000;

	private int _stepSize = 1;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? applyValuesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? startProductionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? pauseProductionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? produceNextPartCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? triggerErrorCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? clearErrorCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? setOfflineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? setOnlineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? completeJobCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resetCountersCommand;

	public Array MachineStates => Enum.GetValues<MachineState>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MachineName
	{
		get
		{
			return _machineName;
		}
		[MemberNotNull("_machineName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_machineName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MachineName);
				_machineName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MachineName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PartName
	{
		get
		{
			return _partName;
		}
		[MemberNotNull("_partName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_partName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PartName);
				_partName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PartName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string JobName
	{
		get
		{
			return _jobName;
		}
		[MemberNotNull("_jobName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_jobName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.JobName);
				_jobName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.JobName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ActualCounter
	{
		get
		{
			return _actualCounter;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_actualCounter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActualCounter);
				_actualCounter = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActualCounter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int TargetCounter
	{
		get
		{
			return _targetCounter;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_targetCounter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TargetCounter);
				_targetCounter = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TargetCounter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public MachineState SelectedState
	{
		get
		{
			return _selectedState;
		}
		set
		{
			if (!EqualityComparer<MachineState>.Default.Equals(_selectedState, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedState);
				_selectedState = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedState);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ErrorActive
	{
		get
		{
			return _errorActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_errorActive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ErrorActive);
				_errorActive = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ErrorActive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		[MemberNotNull("_errorMessage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_errorMessage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ErrorMessage);
				_errorMessage = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ErrorMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ProductionIntervalMs
	{
		get
		{
			return _productionIntervalMs;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_productionIntervalMs, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProductionIntervalMs);
				_productionIntervalMs = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProductionIntervalMs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int StepSize
	{
		get
		{
			return _stepSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_stepSize, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StepSize);
				_stepSize = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StepSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ApplyValuesCommand => applyValuesCommand ?? (applyValuesCommand = new RelayCommand(ApplyValues));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartProductionCommand => startProductionCommand ?? (startProductionCommand = new AsyncRelayCommand(StartProductionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand PauseProductionCommand => pauseProductionCommand ?? (pauseProductionCommand = new AsyncRelayCommand(PauseProductionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ProduceNextPartCommand => produceNextPartCommand ?? (produceNextPartCommand = new AsyncRelayCommand(ProduceNextPartAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TriggerErrorCommand => triggerErrorCommand ?? (triggerErrorCommand = new AsyncRelayCommand(TriggerErrorAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ClearErrorCommand => clearErrorCommand ?? (clearErrorCommand = new AsyncRelayCommand(ClearErrorAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SetOfflineCommand => setOfflineCommand ?? (setOfflineCommand = new AsyncRelayCommand(SetOfflineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SetOnlineCommand => setOnlineCommand ?? (setOnlineCommand = new AsyncRelayCommand(SetOnlineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CompleteJobCommand => completeJobCommand ?? (completeJobCommand = new AsyncRelayCommand(CompleteJobAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResetCountersCommand => resetCountersCommand ?? (resetCountersCommand = new AsyncRelayCommand(ResetCountersAsync));

	public ManualControlViewModel(ISimulationEngine simulationEngine, IConfigurationService configurationService)
	{
		_simulationEngine = simulationEngine;
		_configurationService = configurationService;
	}

	public void LoadMachine(Guid machineId)
	{
		_machineId = machineId;
		MachineConfiguration machineConfiguration = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == machineId);
		MachineRuntimeState runtimeState = _simulationEngine.GetRuntimeState(machineId);
		MachineName = machineConfiguration.Name;
		PartName = runtimeState?.PartName ?? string.Empty;
		JobName = runtimeState?.JobName ?? string.Empty;
		ActualCounter = runtimeState?.ActualCounter ?? 0;
		TargetCounter = runtimeState?.TargetCounter ?? 100;
		SelectedState = runtimeState?.State ?? MachineState.Idle;
		ErrorActive = runtimeState?.ErrorActive ?? false;
		ErrorMessage = runtimeState?.ErrorMessage ?? string.Empty;
		ProductionIntervalMs = machineConfiguration.ProductionIntervalMs;
		StepSize = machineConfiguration.ProductionStepSize;
	}

	private void ApplyValues()
	{
		_simulationEngine.ApplyManualValues(_machineId, PartName, JobName, ActualCounter, TargetCounter, SelectedState, ErrorActive, ErrorMessage, ProductionIntervalMs, StepSize);
	}

	private Task StartProductionAsync()
	{
		return _simulationEngine.StartProductionAsync(_machineId);
	}

	private Task PauseProductionAsync()
	{
		return _simulationEngine.PauseProductionAsync(_machineId);
	}

	private Task ProduceNextPartAsync()
	{
		return _simulationEngine.ProduceNextPartAsync(_machineId);
	}

	private Task TriggerErrorAsync()
	{
		return _simulationEngine.TriggerErrorAsync(_machineId, ErrorMessage);
	}

	private Task ClearErrorAsync()
	{
		return _simulationEngine.ClearErrorAsync(_machineId);
	}

	private Task SetOfflineAsync()
	{
		return _simulationEngine.SetMachineOfflineAsync(_machineId);
	}

	private Task SetOnlineAsync()
	{
		return _simulationEngine.SetMachineOnlineAsync(_machineId);
	}

	private Task CompleteJobAsync()
	{
		return _simulationEngine.CompleteJobAsync(_machineId);
	}

	private Task ResetCountersAsync()
	{
		return _simulationEngine.ResetCountersAsync(_machineId);
	}
}
