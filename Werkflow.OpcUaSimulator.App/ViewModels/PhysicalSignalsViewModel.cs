using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class PhysicalSignalsViewModel : ObservableObject
{
	private readonly IConfigurationService _configurationService;

	private readonly IPhysicalSignalPublishingCoordinator _coordinator;

	private readonly DispatcherTimer _refreshTimer;

	private MachineConfiguration? _selectedMachine;

	private string _searchText = string.Empty;

	private string _categoryFilter = "Alle";

	private string _activeFilter = "Alle";

	private bool _manualOverrideEnabled;

	private PhysicalSignalRowViewModel? _selectedSignal;

	private string _manualValueInput = string.Empty;

	private string _profileName = "â€”";

	private string _profileVersion = "â€”";

	private int _definedSignals;

	private int _enabledSignals;

	private int _opcUaNodes;

	private string _publisherStatus = "â€”";

	private string _lastPublishAt = "â€”";

	private string _updatesPerSecond = "â€”";

	private string _averageDuration = "â€”";

	private string _lastError = "â€”";

	private string _generationMode = "â€”";

	private string _currentPhase = "â€”";

	private string _simulationSeed = "â€”";

	private int _hiddenStateCount;

	private int _signalDependencyCount;

	private int _hiddenStateDependencyCount;

	private string _engineActive = "â€”";

	private string _lastEngineUpdate = "â€”";

	private string _avgCalcDuration = "â€”";

	private string _maxCalcDuration = "â€”";

	private string _simulationTime = "â€”";

	private string _lastPlausibilityError = "â€”";

	private string _selectedGenerationMode = "Technical";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? refreshCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? refreshMetricsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? applyManualValueCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? applyGenerationModeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? toggleManualOverrideCommand;

	public ObservableCollection<string> GenerationModes { get; } = new ObservableCollection<string> { "Technical", "Physical", "Manual" };

	public ObservableCollection<MachineConfiguration> Machines { get; } = new ObservableCollection<MachineConfiguration>();

	public ObservableCollection<PhysicalSignalRowViewModel> Signals { get; } = new ObservableCollection<PhysicalSignalRowViewModel>();

	public ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "Alle" };

	public ObservableCollection<string> ActiveFilters { get; } = new ObservableCollection<string> { "Alle", "Aktiv", "Inaktiv" };

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
	public string SearchText
	{
		get
		{
			return _searchText;
		}
		[MemberNotNull("_searchText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_searchText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SearchText);
				_searchText = value;
				OnSearchTextChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CategoryFilter
	{
		get
		{
			return _categoryFilter;
		}
		[MemberNotNull("_categoryFilter")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_categoryFilter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CategoryFilter);
				_categoryFilter = value;
				OnCategoryFilterChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CategoryFilter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ActiveFilter
	{
		get
		{
			return _activeFilter;
		}
		[MemberNotNull("_activeFilter")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_activeFilter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActiveFilter);
				_activeFilter = value;
				OnActiveFilterChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActiveFilter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ManualOverrideEnabled
	{
		get
		{
			return _manualOverrideEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_manualOverrideEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ManualOverrideEnabled);
				_manualOverrideEnabled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ManualOverrideEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public PhysicalSignalRowViewModel? SelectedSignal
	{
		get
		{
			return _selectedSignal;
		}
		set
		{
			if (!EqualityComparer<PhysicalSignalRowViewModel>.Default.Equals(_selectedSignal, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedSignal);
				_selectedSignal = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedSignal);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ManualValueInput
	{
		get
		{
			return _manualValueInput;
		}
		[MemberNotNull("_manualValueInput")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_manualValueInput, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ManualValueInput);
				_manualValueInput = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ManualValueInput);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProfileName
	{
		get
		{
			return _profileName;
		}
		[MemberNotNull("_profileName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_profileName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProfileName);
				_profileName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProfileName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProfileVersion
	{
		get
		{
			return _profileVersion;
		}
		[MemberNotNull("_profileVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_profileVersion, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProfileVersion);
				_profileVersion = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProfileVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int DefinedSignals
	{
		get
		{
			return _definedSignals;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_definedSignals, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DefinedSignals);
				_definedSignals = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DefinedSignals);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int EnabledSignals
	{
		get
		{
			return _enabledSignals;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_enabledSignals, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EnabledSignals);
				_enabledSignals = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EnabledSignals);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int OpcUaNodes
	{
		get
		{
			return _opcUaNodes;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_opcUaNodes, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OpcUaNodes);
				_opcUaNodes = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OpcUaNodes);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PublisherStatus
	{
		get
		{
			return _publisherStatus;
		}
		[MemberNotNull("_publisherStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_publisherStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PublisherStatus);
				_publisherStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PublisherStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastPublishAt
	{
		get
		{
			return _lastPublishAt;
		}
		[MemberNotNull("_lastPublishAt")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastPublishAt, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastPublishAt);
				_lastPublishAt = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastPublishAt);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string UpdatesPerSecond
	{
		get
		{
			return _updatesPerSecond;
		}
		[MemberNotNull("_updatesPerSecond")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_updatesPerSecond, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UpdatesPerSecond);
				_updatesPerSecond = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UpdatesPerSecond);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AverageDuration
	{
		get
		{
			return _averageDuration;
		}
		[MemberNotNull("_averageDuration")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_averageDuration, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AverageDuration);
				_averageDuration = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AverageDuration);
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
	public string GenerationMode
	{
		get
		{
			return _generationMode;
		}
		[MemberNotNull("_generationMode")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_generationMode, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GenerationMode);
				_generationMode = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GenerationMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CurrentPhase
	{
		get
		{
			return _currentPhase;
		}
		[MemberNotNull("_currentPhase")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_currentPhase, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentPhase);
				_currentPhase = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentPhase);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SimulationSeed
	{
		get
		{
			return _simulationSeed;
		}
		[MemberNotNull("_simulationSeed")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_simulationSeed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SimulationSeed);
				_simulationSeed = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SimulationSeed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int HiddenStateCount
	{
		get
		{
			return _hiddenStateCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_hiddenStateCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HiddenStateCount);
				_hiddenStateCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HiddenStateCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int SignalDependencyCount
	{
		get
		{
			return _signalDependencyCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_signalDependencyCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SignalDependencyCount);
				_signalDependencyCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SignalDependencyCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int HiddenStateDependencyCount
	{
		get
		{
			return _hiddenStateDependencyCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_hiddenStateDependencyCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HiddenStateDependencyCount);
				_hiddenStateDependencyCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HiddenStateDependencyCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string EngineActive
	{
		get
		{
			return _engineActive;
		}
		[MemberNotNull("_engineActive")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_engineActive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EngineActive);
				_engineActive = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EngineActive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastEngineUpdate
	{
		get
		{
			return _lastEngineUpdate;
		}
		[MemberNotNull("_lastEngineUpdate")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastEngineUpdate, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastEngineUpdate);
				_lastEngineUpdate = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastEngineUpdate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AvgCalcDuration
	{
		get
		{
			return _avgCalcDuration;
		}
		[MemberNotNull("_avgCalcDuration")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_avgCalcDuration, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AvgCalcDuration);
				_avgCalcDuration = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AvgCalcDuration);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MaxCalcDuration
	{
		get
		{
			return _maxCalcDuration;
		}
		[MemberNotNull("_maxCalcDuration")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_maxCalcDuration, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MaxCalcDuration);
				_maxCalcDuration = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MaxCalcDuration);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SimulationTime
	{
		get
		{
			return _simulationTime;
		}
		[MemberNotNull("_simulationTime")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_simulationTime, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SimulationTime);
				_simulationTime = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SimulationTime);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastPlausibilityError
	{
		get
		{
			return _lastPlausibilityError;
		}
		[MemberNotNull("_lastPlausibilityError")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastPlausibilityError, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastPlausibilityError);
				_lastPlausibilityError = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastPlausibilityError);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedGenerationMode
	{
		get
		{
			return _selectedGenerationMode;
		}
		[MemberNotNull("_selectedGenerationMode")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedGenerationMode, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedGenerationMode);
				_selectedGenerationMode = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedGenerationMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RefreshCommand => refreshCommand ?? (refreshCommand = new RelayCommand(Refresh));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RefreshMetricsCommand => refreshMetricsCommand ?? (refreshMetricsCommand = new RelayCommand(RefreshMetrics));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ApplyManualValueCommand => applyManualValueCommand ?? (applyManualValueCommand = new AsyncRelayCommand(ApplyManualValueAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ApplyGenerationModeCommand => applyGenerationModeCommand ?? (applyGenerationModeCommand = new RelayCommand(ApplyGenerationMode));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleManualOverrideCommand => toggleManualOverrideCommand ?? (toggleManualOverrideCommand = new RelayCommand(ToggleManualOverride));

	public PhysicalSignalsViewModel(IConfigurationService configurationService, IPhysicalSignalPublishingCoordinator coordinator)
	{
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		_configurationService = configurationService;
		_coordinator = coordinator;
		_refreshTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(2.0)
		};
		_refreshTimer.Tick += delegate
		{
			RefreshMetrics();
		};
		_refreshTimer.Start();
		ReloadMachines();
	}

	public void ReloadMachines()
	{
		Machines.Clear();
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines)
		{
			Machines.Add(machine);
		}
		if (SelectedMachine == null)
		{
			MachineConfiguration machineConfiguration2 = (SelectedMachine = Machines.FirstOrDefault((MachineConfiguration m) => !string.IsNullOrWhiteSpace(m.PhysicalProfileId)));
		}
		Refresh();
	}

	private void Refresh()
	{
		if (SelectedMachine == null)
		{
			ClearDiagnostics();
			Signals.Clear();
			return;
		}
		PhysicalMachineSession session = _coordinator.GetSession(SelectedMachine.Id);
		if (session == null)
		{
			ClearDiagnostics();
			Signals.Clear();
			return;
		}
		ProfileName = session.Profile.DisplayName;
		ProfileVersion = session.Profile.ProfileVersion;
		DefinedSignals = session.Profile.Signals.Count;
		EnabledSignals = session.Profile.Signals.Count((SignalDefinition s) => s.IsEnabled);
		SignalDependencyCount = session.Profile.Dependencies.Count;
		HiddenStateDependencyCount = session.Profile.HiddenStateDependencies.Count;
		OpcUaNodes = session.OpcUaNodeCount;
		PublisherStatus = session.Metrics.State.ToString();
		LastPublishAt = session.Metrics.LastPublishAt?.ToLocalTime().ToString("HH:mm:ss") ?? "â€”";
		UpdatesPerSecond = session.Metrics.UpdatesPerSecond.ToString("F1");
		AverageDuration = $"{session.Metrics.AveragePublishDurationMs:F2} ms";
		LastError = session.Metrics.LastError ?? "â€”";
		GenerationMode = session.Simulation.GenerationMode.ToString();
		SelectedGenerationMode = GenerationMode;
		CurrentPhase = session.Simulation.CurrentPhase.ToString();
		SimulationSeed = session.Simulation.Seed.ToString();
		HiddenStateCount = session.Profile.HiddenProcessStates.Count;
		EngineActive = (session.Simulation.IsEngineActive ? "Ja" : "Nein");
		LastEngineUpdate = session.Simulation.LastCalculationAt?.ToLocalTime().ToString("HH:mm:ss") ?? "â€”";
		AvgCalcDuration = $"{session.Simulation.Metrics.AverageCalculationDurationMs:F2} ms";
		MaxCalcDuration = $"{session.Simulation.Metrics.MaxCalculationDurationMs:F2} ms";
		SimulationTime = session.Simulation.SimulationTime.ToString("hh\\:mm\\:ss");
		LastPlausibilityError = session.Simulation.Metrics.LastPlausibilityError ?? "â€”";
		List<string> list = (from c in session.Profile.Signals.Select((SignalDefinition s) => s.Category.ToString()).Distinct()
			orderby c
			select c).ToList();
		Categories.Clear();
		Categories.Add("Alle");
		foreach (string item in list)
		{
			Categories.Add(item);
		}
		List<PhysicalSignalRowViewModel> list2 = session.Profile.Signals.OrderBy<SignalDefinition, string>((SignalDefinition s) => s.SignalId, StringComparer.Ordinal).Select(delegate(SignalDefinition signal)
		{
			SignalRuntimeState signalRuntimeState = session.Runtime.Signals.First((SignalRuntimeState r) => r.SignalId == signal.SignalId);
			return new PhysicalSignalRowViewModel
			{
				SignalId = signal.SignalId,
				NodeId = signal.NodeId,
				DisplayName = signal.DisplayName,
				DataType = signal.DataType.ToString(),
				Unit = signal.EngineeringUnit,
				CurrentValue = FormatValue(signal, signalRuntimeState),
				NormalRange = $"{signal.NormalMinimum}..{signal.NormalMaximum}",
				UpdateInterval = signal.UpdateInterval.ToString(),
				LastTimestamp = signalRuntimeState.LastUpdatedAt.ToLocalTime().ToString("HH:mm:ss"),
				IsEnabled = signal.IsEnabled,
				Category = signal.Category.ToString(),
				IsRegistered = (session.OpcUaNodeCount > 0 && signal.IsEnabled)
			};
		}).ToList();
		Signals.Clear();
		foreach (PhysicalSignalRowViewModel item2 in list2)
		{
			Signals.Add(item2);
		}
		ApplyFilters();
	}

	private void RefreshMetrics()
	{
		Refresh();
	}

	private async Task ApplyManualValueAsync()
	{
		if (SelectedMachine != null && SelectedSignal != null)
		{
			if (!TechnicalSignalValueGenerator.TryConvertManualValue(ManualValueInput, Enum.Parse<PhysicalSignalDataType>(SelectedSignal.DataType), out object value, out string error))
			{
				LastError = error ?? "UngÃ¼ltiger Wert";
			}
			else if (!(await _coordinator.SetManualValueAsync(SelectedMachine.Id, SelectedSignal.SignalId, value)))
			{
				LastError = "Manuelle WertÃ¤nderung fehlgeschlagen";
			}
			else
			{
				Refresh();
			}
		}
	}

	private void ApplyGenerationMode()
	{
		if (SelectedMachine != null && Enum.TryParse<SignalGenerationMode>(SelectedGenerationMode, out var result))
		{
			if (!_coordinator.TrySetGenerationMode(SelectedMachine.Id, result))
			{
				LastError = "Moduswechsel nur bei gestopptem Publisher mÃ¶glich.";
			}
			else
			{
				Refresh();
			}
		}
	}

	private void ToggleManualOverride()
	{
		if (SelectedMachine != null)
		{
			ManualOverrideEnabled = !ManualOverrideEnabled;
			_coordinator.EnableManualOverride(SelectedMachine.Id, ManualOverrideEnabled);
		}
	}

	private void ApplyFilters()
	{
		if (SelectedMachine == null)
		{
			return;
		}
		PhysicalMachineSession session = _coordinator.GetSession(SelectedMachine.Id);
		if (session == null)
		{
			return;
		}
		List<PhysicalSignalRowViewModel> list = (from s in session.Profile.Signals.OrderBy<SignalDefinition, string>((SignalDefinition s) => s.SignalId, StringComparer.Ordinal)
			where string.IsNullOrWhiteSpace(SearchText) || s.SignalId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || s.NodeId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
			where CategoryFilter == "Alle" || s.Category.ToString() == CategoryFilter
			select s).Where(delegate(SignalDefinition s)
		{
			string activeFilter = ActiveFilter;
			if (1 == 0)
			{
			}
			bool result = ((activeFilter == "Aktiv") ? s.IsEnabled : (!(activeFilter == "Inaktiv") || !s.IsEnabled));
			if (1 == 0)
			{
			}
			return result;
		}).Select(delegate(SignalDefinition signal)
		{
			SignalRuntimeState signalRuntimeState = session.Runtime.Signals.First((SignalRuntimeState r) => r.SignalId == signal.SignalId);
			return new PhysicalSignalRowViewModel
			{
				SignalId = signal.SignalId,
				NodeId = signal.NodeId,
				DisplayName = signal.DisplayName,
				DataType = signal.DataType.ToString(),
				Unit = signal.EngineeringUnit,
				CurrentValue = FormatValue(signal, signalRuntimeState),
				NormalRange = $"{signal.NormalMinimum}..{signal.NormalMaximum}",
				UpdateInterval = signal.UpdateInterval.ToString(),
				LastTimestamp = signalRuntimeState.LastUpdatedAt.ToLocalTime().ToString("HH:mm:ss"),
				IsEnabled = signal.IsEnabled,
				Category = signal.Category.ToString(),
				IsRegistered = (session.OpcUaNodeCount > 0 && signal.IsEnabled)
			};
		}).ToList();
		Signals.Clear();
		foreach (PhysicalSignalRowViewModel item in list)
		{
			Signals.Add(item);
		}
	}

	private static string FormatValue(SignalDefinition signal, SignalRuntimeState runtime)
	{
		PhysicalSignalDataType dataType = signal.DataType;
		if (1 == 0)
		{
		}
		string result = dataType switch
		{
			PhysicalSignalDataType.Boolean => (runtime.CurrentValue >= 0.5) ? "true" : "false", 
			PhysicalSignalDataType.Int32 => ((int)Math.Round(runtime.CurrentValue)).ToString(), 
			PhysicalSignalDataType.Int64 => ((long)Math.Round(runtime.CurrentValue)).ToString(), 
			PhysicalSignalDataType.Float => ((float)runtime.CurrentValue).ToString("F2"), 
			_ => runtime.CurrentValue.ToString($"F{signal.DecimalPlaces}"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void ClearDiagnostics()
	{
		ProfileName = "â€”";
		ProfileVersion = "â€”";
		DefinedSignals = 0;
		EnabledSignals = 0;
		OpcUaNodes = 0;
		PublisherStatus = "â€”";
		LastPublishAt = "â€”";
		UpdatesPerSecond = "â€”";
		AverageDuration = "â€”";
		LastError = "â€”";
		GenerationMode = "â€”";
		CurrentPhase = "â€”";
		SimulationSeed = "â€”";
		HiddenStateCount = 0;
		SignalDependencyCount = 0;
		HiddenStateDependencyCount = 0;
		EngineActive = "â€”";
		LastEngineUpdate = "â€”";
		AvgCalcDuration = "â€”";
		MaxCalcDuration = "â€”";
		SimulationTime = "â€”";
		LastPlausibilityError = "â€”";
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedMachineChanged(MachineConfiguration? value)
	{
		Refresh();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSearchTextChanged(string value)
	{
		ApplyFilters();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnCategoryFilterChanged(string value)
	{
		ApplyFilters();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnActiveFilterChanged(string value)
	{
		ApplyFilters();
	}
}
