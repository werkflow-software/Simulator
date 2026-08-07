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

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class LogViewModel : ObservableObject
{
	private readonly ILogService _logService;

	private readonly IDialogService _dialogService;

	private string _searchText = string.Empty;

	private string? _selectedMachineFilter;

	private LogCategory? _selectedCategoryFilter;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? refreshCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? clearCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? exportCsvCommand;

	public ObservableCollection<SimulationLogEntry> Entries { get; } = new ObservableCollection<SimulationLogEntry>();

	public Array Categories => Enum.GetValues<LogCategory>();

	public ObservableCollection<string> MachineFilters { get; } = new ObservableCollection<string> { "Alle" };

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
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SelectedMachineFilter
	{
		get
		{
			return _selectedMachineFilter;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedMachineFilter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedMachineFilter);
				_selectedMachineFilter = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedMachineFilter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public LogCategory? SelectedCategoryFilter
	{
		get
		{
			return _selectedCategoryFilter;
		}
		set
		{
			if (!EqualityComparer<LogCategory?>.Default.Equals(_selectedCategoryFilter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedCategoryFilter);
				_selectedCategoryFilter = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedCategoryFilter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RefreshCommand => refreshCommand ?? (refreshCommand = new RelayCommand(Refresh));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearCommand => clearCommand ?? (clearCommand = new RelayCommand(Clear));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ExportCsvCommand => exportCsvCommand ?? (exportCsvCommand = new AsyncRelayCommand(ExportCsvAsync));

	public LogViewModel(ILogService logService, IConfigurationService configurationService, IDialogService dialogService)
	{
		_logService = logService;
		_dialogService = dialogService;
		foreach (MachineConfiguration machine in configurationService.Configuration.Machines)
		{
			MachineFilters.Add(machine.Name);
		}
		Refresh();
		_logService.EntryAdded += delegate
		{
			UiDispatcher.Run(Refresh, (DispatcherPriority)4);
		};
	}

	private void Refresh()
	{
		Entries.Clear();
		IEnumerable<SimulationLogEntry> source = _logService.Entries.AsEnumerable();
		if (!string.IsNullOrWhiteSpace(SearchText))
		{
			source = source.Where((SimulationLogEntry e) => e.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
		}
		if (!string.IsNullOrWhiteSpace(SelectedMachineFilter) && SelectedMachineFilter != "Alle")
		{
			source = source.Where((SimulationLogEntry e) => e.MachineName == SelectedMachineFilter);
		}
		if (SelectedCategoryFilter.HasValue)
		{
			source = source.Where((SimulationLogEntry e) => e.Category == SelectedCategoryFilter.Value);
		}
		foreach (SimulationLogEntry item in source.Take(500))
		{
			Entries.Add(item);
		}
	}

	private void Clear()
	{
		_logService.Clear();
		Refresh();
	}

	private async Task ExportCsvAsync()
	{
		string path = _dialogService.ShowSaveFileDialog("CSV (*.csv)|*.csv", "simulation-log.csv");
		if (path != null)
		{
			await _logService.ExportCsvAsync(path);
		}
	}
}
