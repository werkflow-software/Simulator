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
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class MachinesViewModel : ObservableObject
{
	private readonly IConfigurationService _configurationService;

	private readonly ISimulationEngine _simulationEngine;

	private readonly IValidationService _validationService;

	private readonly IDialogService _dialogService;

	private MachineListItemViewModel? _selectedMachine;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? addMachineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<MachineListItemViewModel?>? copyMachineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<MachineListItemViewModel?>? deleteMachineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<MachineListItemViewModel?>? saveMachineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<MachineListItemViewModel?>? startServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<MachineListItemViewModel?>? stopServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? validateAllCommand;

	public ObservableCollection<MachineListItemViewModel> Machines { get; } = new ObservableCollection<MachineListItemViewModel>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public MachineListItemViewModel? SelectedMachine
	{
		get
		{
			return _selectedMachine;
		}
		set
		{
			if (!EqualityComparer<MachineListItemViewModel>.Default.Equals(_selectedMachine, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedMachine);
				_selectedMachine = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedMachine);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand AddMachineCommand => addMachineCommand ?? (addMachineCommand = new AsyncRelayCommand(AddMachineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<MachineListItemViewModel?> CopyMachineCommand => copyMachineCommand ?? (copyMachineCommand = new AsyncRelayCommand<MachineListItemViewModel>(CopyMachineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<MachineListItemViewModel?> DeleteMachineCommand => deleteMachineCommand ?? (deleteMachineCommand = new AsyncRelayCommand<MachineListItemViewModel>(DeleteMachineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<MachineListItemViewModel?> SaveMachineCommand => saveMachineCommand ?? (saveMachineCommand = new AsyncRelayCommand<MachineListItemViewModel>(SaveMachineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<MachineListItemViewModel?> StartServerCommand => startServerCommand ?? (startServerCommand = new AsyncRelayCommand<MachineListItemViewModel>(StartServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<MachineListItemViewModel?> StopServerCommand => stopServerCommand ?? (stopServerCommand = new AsyncRelayCommand<MachineListItemViewModel>(StopServerAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ValidateAllCommand => validateAllCommand ?? (validateAllCommand = new RelayCommand(ValidateAll));

	public MachinesViewModel(IConfigurationService configurationService, ISimulationEngine simulationEngine, IValidationService validationService, IDialogService dialogService)
	{
		_configurationService = configurationService;
		_simulationEngine = simulationEngine;
		_validationService = validationService;
		_dialogService = dialogService;
		Refresh();
	}

	public void Refresh()
	{
		Guid? selectedId = SelectedMachine?.Id;
		List<MachineConfiguration> machines = _configurationService.Configuration.Machines;
		int i;
		for (i = Machines.Count - 1; i >= 0; i--)
		{
			if (machines.All((MachineConfiguration m) => m.Id != Machines[i].Id))
			{
				Machines.RemoveAt(i);
			}
		}
		foreach (MachineConfiguration machine in machines)
		{
			MachineRuntimeState runtimeState = _simulationEngine.GetRuntimeState(machine.Id);
			MachineListItemViewModel machineListItemViewModel = Machines.FirstOrDefault((MachineListItemViewModel m) => m.Id == machine.Id);
			if (machineListItemViewModel == null)
			{
				Machines.Add(new MachineListItemViewModel(machine, runtimeState, _simulationEngine));
				continue;
			}
			machineListItemViewModel.SyncFromConfiguration(machine);
			machineListItemViewModel.SyncRuntimeState(runtimeState);
		}
		if (selectedId.HasValue)
		{
			SelectedMachine = Machines.FirstOrDefault((MachineListItemViewModel m) => m.Id == selectedId.Value);
		}
	}

	public void RefreshRuntimeStates()
	{
		foreach (MachineListItemViewModel machine in Machines)
		{
			machine.SyncRuntimeState(_simulationEngine.GetRuntimeState(machine.Id));
		}
	}

	private async Task AddMachineAsync()
	{
		int nextPort = _configurationService.Configuration.Machines.Select((MachineConfiguration m) => m.Port).DefaultIfEmpty(4839).Max() + 1;
		int index = _configurationService.Configuration.Machines.Count + 1;
		MachineConfiguration machine = new MachineConfiguration
		{
			Name = $"Maschine {index}",
			Port = nextPort,
			NamespaceUri = $"urn:werkflow:simulator:machine{index}",
			Nodes = (from n in NodeMappingPresets.CreateStandard()
				select n.Clone()).ToList()
		};
		machine.UpdateEndpointFromHostPort();
		_configurationService.Configuration.Machines.Add(machine);
		await _configurationService.SaveMachinesAsync();
		Refresh();
	}

	private async Task CopyMachineAsync(MachineListItemViewModel? item)
	{
		if (item != null)
		{
			MachineConfiguration source = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == item.Id);
			MachineConfiguration copy = source.Clone();
			copy.Port = _configurationService.Configuration.Machines.Max((MachineConfiguration m) => m.Port) + 1;
			copy.UpdateEndpointFromHostPort();
			_configurationService.Configuration.Machines.Add(copy);
			await _configurationService.SaveMachinesAsync();
			Refresh();
		}
	}

	private async Task DeleteMachineAsync(MachineListItemViewModel? item)
	{
		if (item != null && _dialogService.ShowConfirmation("Maschine lÃ¶schen", "Maschine '" + item.Name + "' wirklich lÃ¶schen?"))
		{
			await _simulationEngine.StopMachineServerAsync(item.Id);
			_configurationService.Configuration.Machines.RemoveAll((MachineConfiguration m) => m.Id == item.Id);
			await _configurationService.SaveMachinesAsync();
			Refresh();
		}
	}

	private async Task SaveMachineAsync(MachineListItemViewModel? item)
	{
		if (item != null)
		{
			MachineConfiguration machine = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == item.Id);
			item.ApplyTo(machine);
			machine.UpdateEndpointFromHostPort();
			ValidationResult validation = _validationService.ValidateMachine(machine, _configurationService.Configuration.Machines);
			if (!validation.IsValid)
			{
				_dialogService.ShowError("Validierung", string.Join(Environment.NewLine, validation.Errors));
				return;
			}
			await _configurationService.SaveMachinesAsync();
			Refresh();
		}
	}

	private async Task StartServerAsync(MachineListItemViewModel? item)
	{
		if (item == null)
		{
			return;
		}
		try
		{
			await _simulationEngine.StartMachineServerAsync(item.Id);
			Refresh();
		}
		catch (Exception ex)
		{
			_dialogService.ShowError("Server", ex.Message);
		}
	}

	private async Task StopServerAsync(MachineListItemViewModel? item)
	{
		if (item != null)
		{
			await _simulationEngine.StopMachineServerAsync(item.Id);
			Refresh();
		}
	}

	private void ValidateAll()
	{
		ValidationResult validationResult = _validationService.ValidateForSimulationStart(_configurationService.Configuration);
		if (validationResult.IsValid)
		{
			_dialogService.ShowInfo("Validierung", "Konfiguration ist gÃ¼ltig.");
		}
		else
		{
			_dialogService.ShowError("Validierung", string.Join(Environment.NewLine, validationResult.Errors));
		}
	}
}
