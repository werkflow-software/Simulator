using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class SettingsViewModel : ObservableObject
{
	private readonly IConfigurationService _configurationService;

	private readonly IDialogService _dialogService;

	private readonly IPhysicalMachineProfileLoader _profileLoader;

	private readonly IPhysicalMachineProfileValidator _profileValidator;

	private int _heartbeatIntervalMs;

	private int _logMaxEntries;

	private string _physicalProfileName = "â€”";

	private string _physicalProfileVersion = "â€”";

	private int _physicalSignalCount;

	private int _physicalHiddenStateCount;

	private int _physicalDependencyCount;

	private string _physicalValidationStatus = "Nicht geladen";

	private string _physicalProfilePath = "â€”";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? reloadPhysicalProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? saveCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restoreFactoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openConfigFolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? exportConfigCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? importConfigCommand;

	public string InfoText => "Die Simulation verwendet feste Teile, Mengen, Fehler und Warnungen. Nur die Zykluszeiten sind zufÃ¤llig. OPC-UA-Node-Namen passen Sie unter â€žNodesâ€œ pro Maschine an.";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int HeartbeatIntervalMs
	{
		get
		{
			return _heartbeatIntervalMs;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_heartbeatIntervalMs, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HeartbeatIntervalMs);
				_heartbeatIntervalMs = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HeartbeatIntervalMs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int LogMaxEntries
	{
		get
		{
			return _logMaxEntries;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_logMaxEntries, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LogMaxEntries);
				_logMaxEntries = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LogMaxEntries);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PhysicalProfileName
	{
		get
		{
			return _physicalProfileName;
		}
		[MemberNotNull("_physicalProfileName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_physicalProfileName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalProfileName);
				_physicalProfileName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalProfileName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PhysicalProfileVersion
	{
		get
		{
			return _physicalProfileVersion;
		}
		[MemberNotNull("_physicalProfileVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_physicalProfileVersion, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalProfileVersion);
				_physicalProfileVersion = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalProfileVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int PhysicalSignalCount
	{
		get
		{
			return _physicalSignalCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_physicalSignalCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalSignalCount);
				_physicalSignalCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalSignalCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int PhysicalHiddenStateCount
	{
		get
		{
			return _physicalHiddenStateCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_physicalHiddenStateCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalHiddenStateCount);
				_physicalHiddenStateCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalHiddenStateCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int PhysicalDependencyCount
	{
		get
		{
			return _physicalDependencyCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_physicalDependencyCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalDependencyCount);
				_physicalDependencyCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalDependencyCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PhysicalValidationStatus
	{
		get
		{
			return _physicalValidationStatus;
		}
		[MemberNotNull("_physicalValidationStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_physicalValidationStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalValidationStatus);
				_physicalValidationStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalValidationStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PhysicalProfilePath
	{
		get
		{
			return _physicalProfilePath;
		}
		[MemberNotNull("_physicalProfilePath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_physicalProfilePath, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PhysicalProfilePath);
				_physicalProfilePath = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PhysicalProfilePath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ReloadPhysicalProfileCommand => reloadPhysicalProfileCommand ?? (reloadPhysicalProfileCommand = new AsyncRelayCommand(ReloadPhysicalProfileAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveCommand => saveCommand ?? (saveCommand = new AsyncRelayCommand(SaveAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestoreFactoryCommand => restoreFactoryCommand ?? (restoreFactoryCommand = new AsyncRelayCommand(RestoreFactoryAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenConfigFolderCommand => openConfigFolderCommand ?? (openConfigFolderCommand = new RelayCommand(OpenConfigFolder));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ExportConfigCommand => exportConfigCommand ?? (exportConfigCommand = new AsyncRelayCommand(ExportConfigAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ImportConfigCommand => importConfigCommand ?? (importConfigCommand = new AsyncRelayCommand(ImportConfigAsync));

	public SettingsViewModel(IConfigurationService configurationService, IDialogService dialogService, IPhysicalMachineProfileLoader profileLoader, IPhysicalMachineProfileValidator profileValidator)
	{
		_configurationService = configurationService;
		_dialogService = dialogService;
		_profileLoader = profileLoader;
		_profileValidator = profileValidator;
		Load();
		LoadPhysicalProfileAsync();
	}

	private void Load()
	{
		SimulationSettings settings = _configurationService.Configuration.Settings;
		HeartbeatIntervalMs = settings.HeartbeatIntervalMs;
		LogMaxEntries = settings.LogMaxEntries;
	}

	private async Task LoadPhysicalProfileAsync()
	{
		try
		{
			string path = (PhysicalProfilePath = PhysicalMachineProfilePaths.ResolveReferenceProfilePath());
			if (!File.Exists(path))
			{
				PhysicalValidationStatus = "Referenzprofil nicht gefunden";
				return;
			}
			PhysicalMachineProfile profile = await _profileLoader.LoadFromFileAsync(path);
			PhysicalProfileValidationResult validation = _profileValidator.Validate(profile);
			ApplyProfileDiagnostics(profile, validation);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			PhysicalProfileName = "â€”";
			PhysicalProfileVersion = "â€”";
			PhysicalSignalCount = 0;
			PhysicalHiddenStateCount = 0;
			PhysicalDependencyCount = 0;
			PhysicalValidationStatus = "Fehler: " + ex2.Message;
		}
	}

	private void ApplyProfileDiagnostics(PhysicalMachineProfile profile, PhysicalProfileValidationResult validation)
	{
		PhysicalProfileName = (string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.ProfileId : profile.DisplayName);
		PhysicalProfileVersion = profile.ProfileVersion;
		PhysicalSignalCount = profile.Signals.Count;
		PhysicalHiddenStateCount = profile.HiddenProcessStates.Count;
		PhysicalDependencyCount = profile.Dependencies.Count;
		PhysicalValidationStatus = (validation.IsValid ? "Valide" : $"UngÃ¼ltig ({validation.Errors.Count} Fehler)");
	}

	private async Task ReloadPhysicalProfileAsync()
	{
		await LoadPhysicalProfileAsync();
	}

	private async Task SaveAsync()
	{
		SimulationSettings s = _configurationService.Configuration.Settings;
		s.HeartbeatIntervalMs = HeartbeatIntervalMs;
		s.LogMaxEntries = LogMaxEntries;
		await _configurationService.SaveSettingsAsync();
		_dialogService.ShowInfo("Einstellungen", "Einstellungen gespeichert.");
	}

	private async Task RestoreFactoryAsync()
	{
		if (_dialogService.ShowConfirmation("Werkseinstellungen", "Alle Einstellungen auf Werkzustand zurÃ¼cksetzen? OPC-UA-Node-Anpassungen an Maschinen werden ebenfalls zurÃ¼ckgesetzt."))
		{
			await _configurationService.RestoreFactoryDefaultsAsync();
			Load();
		}
	}

	private void OpenConfigFolder()
	{
		_configurationService.OpenConfigurationDirectory();
	}

	private async Task ExportConfigAsync()
	{
		string path = _dialogService.ShowSaveFileDialog("JSON (*.json)|*.json", "werkflow-opcua-config.json");
		if (path != null)
		{
			await _configurationService.ExportAllAsync(path);
		}
	}

	private async Task ImportConfigAsync()
	{
		string path = _dialogService.ShowOpenFileDialog("JSON (*.json)|*.json");
		if (path != null)
		{
			await _configurationService.ImportAllAsync(path);
			Load();
		}
	}
}
