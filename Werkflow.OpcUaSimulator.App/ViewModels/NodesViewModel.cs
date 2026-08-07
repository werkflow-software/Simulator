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
using Werkflow.OpcUaSimulator.Core.Utilities;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class NodesViewModel : ObservableObject
{
	private readonly IConfigurationService _configurationService;

	private readonly ISimulationEngine _simulationEngine;

	private readonly IMachineValuePublisher _valuePublisher;

	private readonly IDialogService _dialogService;

	private MachineSelectionItem? _selectedMachine;

	private NodeMappingPresetInfo? _selectedPreset;

	private string _serverHint = string.Empty;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? saveNodesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? applyPresetCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restoreDefaultsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? copyToAllMachinesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restartServerCommand;

	public ObservableCollection<MachineSelectionItem> MachineSelections { get; } = new ObservableCollection<MachineSelectionItem>();

	public ObservableCollection<NodeEditViewModel> Nodes { get; } = new ObservableCollection<NodeEditViewModel>();

	public ObservableCollection<NodeMappingPresetInfo> Presets { get; } = new ObservableCollection<NodeMappingPresetInfo>(NodeMappingPresets.All);

	public string HelpText => "Hier passen Sie nur die OPC-UA-Sicht nach auÃŸen an (Anzeigename, BrowseName, NodeId). Die Simulation arbeitet intern immer mit festen Bedeutungen (Teilename, IstzÃ¤hler, â€¦). Nach Ã„nderungen Server der Maschine stoppen, speichern und neu starten.";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public MachineSelectionItem? SelectedMachine
	{
		get
		{
			return _selectedMachine;
		}
		set
		{
			if (!EqualityComparer<MachineSelectionItem>.Default.Equals(_selectedMachine, value))
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
	public NodeMappingPresetInfo? SelectedPreset
	{
		get
		{
			return _selectedPreset;
		}
		set
		{
			if (!EqualityComparer<NodeMappingPresetInfo>.Default.Equals(_selectedPreset, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedPreset);
				_selectedPreset = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedPreset);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ServerHint
	{
		get
		{
			return _serverHint;
		}
		[MemberNotNull("_serverHint")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_serverHint, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ServerHint);
				_serverHint = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ServerHint);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveNodesCommand => saveNodesCommand ?? (saveNodesCommand = new AsyncRelayCommand(SaveNodesAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ApplyPresetCommand => applyPresetCommand ?? (applyPresetCommand = new AsyncRelayCommand(ApplyPresetAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestoreDefaultsCommand => restoreDefaultsCommand ?? (restoreDefaultsCommand = new AsyncRelayCommand(RestoreDefaultsAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CopyToAllMachinesCommand => copyToAllMachinesCommand ?? (copyToAllMachinesCommand = new AsyncRelayCommand(CopyToAllMachinesAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestartServerCommand => restartServerCommand ?? (restartServerCommand = new AsyncRelayCommand(RestartServerAsync));

	public NodesViewModel(IConfigurationService configurationService, ISimulationEngine simulationEngine, IMachineValuePublisher valuePublisher, IDialogService dialogService)
	{
		_configurationService = configurationService;
		_simulationEngine = simulationEngine;
		_valuePublisher = valuePublisher;
		_dialogService = dialogService;
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines)
		{
			MachineSelections.Add(new MachineSelectionItem(machine.Id, machine.Name));
		}
		SelectedMachine = MachineSelections.FirstOrDefault();
		SelectedPreset = Presets.FirstOrDefault();
		LoadNodes();
	}

	private void LoadNodes()
	{
		Nodes.Clear();
		if (SelectedMachine == null)
		{
			return;
		}
		MachineConfiguration machineConfiguration = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == SelectedMachine.Id);
		bool flag = _simulationEngine.GetRuntimeState(machineConfiguration.Id)?.IsServerOnline ?? false;
		ServerHint = (flag ? "Server lÃ¤uft â€“ Node-Ã„nderungen erst nach Stopp speichern und Server neu starten." : "Server gestoppt â€“ OPC-UA-Struktur kann angepasst und gespeichert werden.");
		foreach (NodeMapping node in machineConfiguration.Nodes)
		{
			object liveValue = _valuePublisher.GetLiveValue(machineConfiguration.Id, node.SemanticType);
			Nodes.Add(new NodeEditViewModel(node, liveValue?.ToString() ?? "â€”"));
		}
	}

	public void RefreshLiveValues()
	{
		if (SelectedMachine == null)
		{
			return;
		}
		foreach (NodeEditViewModel node in Nodes)
		{
			node.LiveValue = _valuePublisher.GetLiveValue(SelectedMachine.Id, node.SemanticType)?.ToString() ?? "â€”";
		}
	}

	private async Task SaveNodesAsync()
	{
		if (SelectedMachine == null)
		{
			return;
		}
		MachineConfiguration machine = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == SelectedMachine.Id);
		if (_simulationEngine.GetRuntimeState(machine.Id)?.IsServerOnline ?? false)
		{
			_dialogService.ShowWarning("Nodes", "Bitte stoppen Sie zuerst den Server dieser Maschine.");
			return;
		}
		foreach (NodeEditViewModel nodeVm in Nodes)
		{
			NodeMapping node = machine.Nodes.First((NodeMapping n) => n.SemanticType == nodeVm.SemanticType);
			nodeVm.ApplyTo(node);
		}
		ValidationResult validation = NodeIdParser.ValidateNodeMappings(machine.Nodes);
		if (!validation.IsValid)
		{
			_dialogService.ShowError("Nodes", string.Join(Environment.NewLine, validation.Errors));
			return;
		}
		await _configurationService.SaveMachinesAsync();
		LoadNodes();
		_dialogService.ShowInfo("Nodes", "OPC-UA-Nodes gespeichert. Server neu starten, damit UaExpert die Struktur sieht.");
	}

	private async Task ApplyPresetAsync()
	{
		if (SelectedMachine == null || SelectedPreset == null)
		{
			return;
		}
		if (_simulationEngine.GetRuntimeState(SelectedMachine.Id)?.IsServerOnline ?? false)
		{
			_dialogService.ShowWarning("Nodes", "Bitte stoppen Sie zuerst den Server dieser Maschine.");
			return;
		}
		MachineConfiguration machine = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == SelectedMachine.Id);
		machine.Nodes = (from n in SelectedPreset.Factory()
			select n.Clone()).ToList();
		await _configurationService.SaveMachinesAsync();
		LoadNodes();
	}

	private async Task RestoreDefaultsAsync()
	{
		if (!(SelectedMachine == null))
		{
			SelectedPreset = Presets.First();
			await ApplyPresetAsync();
		}
	}

	private async Task CopyToAllMachinesAsync()
	{
		if (SelectedMachine == null)
		{
			return;
		}
		if (_simulationEngine.RunningServerCount > 0)
		{
			_dialogService.ShowWarning("Nodes", "Bitte stoppen Sie zuerst alle Server.");
			return;
		}
		MachineConfiguration source = _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == SelectedMachine.Id);
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines.Where((MachineConfiguration m) => m.Id != source.Id))
		{
			machine.Nodes = source.Nodes.Select((NodeMapping n) => n.Clone()).ToList();
		}
		await _configurationService.SaveMachinesAsync();
		_dialogService.ShowInfo("Nodes", "Node-Konfiguration auf alle Maschinen kopiert.");
	}

	private async Task RestartServerAsync()
	{
		if (!(SelectedMachine == null))
		{
			await _simulationEngine.StopMachineServerAsync(SelectedMachine.Id);
			await _simulationEngine.StartMachineServerAsync(SelectedMachine.Id);
			LoadNodes();
			_dialogService.ShowInfo("Nodes", "Server neu gestartet.");
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedMachineChanged(MachineSelectionItem? value)
	{
		LoadNodes();
	}
}
