using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class MainViewModel : ObservableObject
{
	private readonly ISimulationEngine _simulationEngine;

	private readonly IConfigurationService _configurationService;

	private readonly DispatcherTimer _timer;

	private object? _currentViewModel;

	private string _selectedNavigation = "Ãœbersicht";

	private string _simulationStatus = "Gestoppt";

	private string _serverStatus = "0/0 Server online";

	private string _runtimeText = "00:00:00";

	private string _lastError = "â€”";

	private string _seedText = "â€”";

	private string _saveStatus = "Bereit";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string>? navigateCommand;

	public OverviewViewModel Overview { get; }

	public MachinesViewModel Machines { get; }

	public NodesViewModel Nodes { get; }

	public JobsViewModel Jobs { get; }

	public EventsViewModel Events { get; }

	public LogViewModel Log { get; }

	public SettingsViewModel Settings { get; }

	public PhysicalSignalsViewModel PhysicalSignals { get; }

	public FaultScenariosViewModel FaultScenarios { get; }

	public ExperimentsViewModel Experiments { get; }

	public ObservableCollection<string> NavigationItems { get; } = new ObservableCollection<string> { "Übersicht", "Maschinen", "Nodes", "Physikalische Signale", "Fehlerszenarien", "Experimente", "Aufträge", "Ereignisse", "Protokoll", "Einstellungen" };

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public object? CurrentViewModel
	{
		get
		{
			return _currentViewModel;
		}
		set
		{
			if (!EqualityComparer<object>.Default.Equals(_currentViewModel, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentViewModel);
				_currentViewModel = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentViewModel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedNavigation
	{
		get
		{
			return _selectedNavigation;
		}
		[MemberNotNull("_selectedNavigation")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedNavigation, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedNavigation);
				_selectedNavigation = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedNavigation);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SimulationStatus
	{
		get
		{
			return _simulationStatus;
		}
		[MemberNotNull("_simulationStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_simulationStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SimulationStatus);
				_simulationStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SimulationStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ServerStatus
	{
		get
		{
			return _serverStatus;
		}
		[MemberNotNull("_serverStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_serverStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ServerStatus);
				_serverStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ServerStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string RuntimeText
	{
		get
		{
			return _runtimeText;
		}
		[MemberNotNull("_runtimeText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_runtimeText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RuntimeText);
				_runtimeText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RuntimeText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastError
	{
		get
		{
			return _lastError;
		}
		[MemberNotNull("_lastError")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastError, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastError);
				_lastError = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastError);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SeedText
	{
		get
		{
			return _seedText;
		}
		[MemberNotNull("_seedText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_seedText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SeedText);
				_seedText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SeedText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SaveStatus
	{
		get
		{
			return _saveStatus;
		}
		[MemberNotNull("_saveStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_saveStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SaveStatus);
				_saveStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SaveStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> NavigateCommand => navigateCommand ?? (navigateCommand = new RelayCommand<string>(Navigate));

	public MainViewModel(ISimulationEngine simulationEngine, IConfigurationService configurationService, OverviewViewModel overview, MachinesViewModel machines, NodesViewModel nodes, JobsViewModel jobs, EventsViewModel eventsViewModel, LogViewModel log, SettingsViewModel settings, PhysicalSignalsViewModel physicalSignals, FaultScenariosViewModel faultScenarios, ExperimentsViewModel experiments)
	{
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected O, but got Unknown
		_simulationEngine = simulationEngine;
		_configurationService = configurationService;
		Overview = overview;
		Machines = machines;
		Nodes = nodes;
		Jobs = jobs;
		Events = eventsViewModel;
		Log = log;
		Settings = settings;
		PhysicalSignals = physicalSignals;
		FaultScenarios = faultScenarios;
		Experiments = experiments;
		CurrentViewModel = Overview;
		_simulationEngine.StateChanged += delegate
		{
			UiDispatcher.Run(UpdateStatusBar, (DispatcherPriority)4);
		};
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.0)
		};
		_timer.Tick += delegate
		{
			RefreshAll();
		};
		_timer.Start();
		RefreshAll();
	}

	private void Navigate(string page)
	{
		SelectedNavigation = page;
		if (1 == 0)
		{
		}
		object currentViewModel = page switch
		{
			"Übersicht" => Overview,
			"Maschinen" => Machines,
			"Nodes" => Nodes,
			"Physikalische Signale" => PhysicalSignals,
			"Fehlerszenarien" => FaultScenarios,
			"Experimente" => Experiments,
			"Aufträge" => Jobs,
			"Ereignisse" => Events,
			"Protokoll" => Log,
			"Einstellungen" => Settings,
			_ => Overview,
		};
		if (1 == 0)
		{
		}
		CurrentViewModel = currentViewModel;
	}

	private void UpdateStatusBar()
	{
		SimulationStatus = _simulationEngine.State.ToGermanLabel();
		int value = _configurationService.Configuration.Machines.Count((MachineConfiguration m) => m.IsActive);
		ServerStatus = $"{_simulationEngine.RunningServerCount}/{value} Server online";
		SeedText = $"Seed: {_simulationEngine.CurrentSeed}";
		if (_simulationEngine.StartedAt.HasValue)
		{
			RuntimeText = (DateTime.UtcNow - _simulationEngine.StartedAt.Value).ToString("hh\\:mm\\:ss");
		}
		else
		{
			RuntimeText = "00:00:00";
		}
		string text = (from s in _simulationEngine.GetRuntimeStates().Values
			where s.ErrorActive
			select s.ErrorMessage).FirstOrDefault();
		LastError = (string.IsNullOrWhiteSpace(text) ? "â€”" : text);
	}

	private void RefreshAll()
	{
		UpdateStatusBar();
		Overview.Refresh();
		FaultScenarios.Refresh();
		Machines.RefreshRuntimeStates();
		Nodes.RefreshLiveValues();
	}
}
