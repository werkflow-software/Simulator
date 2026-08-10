using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public partial class ExperimentsViewModel : ObservableObject
{
	private string _selectedExperimentId = "EXP-001";
	private string _status = "Bereit";
	private string _progress = "—";
	private bool _vigilConnected;

	public ObservableCollection<string> Experiments { get; } = new(ExperimentCatalog.GetAll().Select(e => e.ExperimentId));

	public ObservableCollection<string> GroundTruthEvents { get; } = new();

	public string SelectedExperimentId => _selectedExperimentId;

	public string Status => _status;

	public string Progress => _progress;

	public bool VigilConnected => _vigilConnected;

	[RelayCommand]
	private void Refresh()
	{
		_status = "Experiment-Framework bereit (Headless-Tests)";
		_vigilConnected = false;
		_progress = ExperimentCatalog.GetById(_selectedExperimentId)?.DisplayName ?? "—";
		OnPropertyChanged(nameof(Status));
		OnPropertyChanged(nameof(Progress));
		OnPropertyChanged(nameof(VigilConnected));
	}
}
