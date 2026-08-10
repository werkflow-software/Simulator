using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class ExperimentsViewModel : ObservableObject
{
	private string _selectedMachineProfileId = "laser-processing-machine-300";
	private string _selectedExperimentId = "EXP-001";
	private string _selectedMode = "GroundTruthOnly";
	private double _timeFactor = 50;
	private string _status = "Bereit";
	private string _currentRun = "—";
	private string _runType = "—";
	private string _repetition = "—";
	private string _progress = "—";
	private string _simulationTime = "00:00:00";
	private string _currentPhase = "—";
	private int _groundTruthEventCount;
	private bool _vigilConnected;
	private string _resultSummary = "";

	public ObservableCollection<string> MachineProfiles { get; } = new()
	{
		"laser-processing-machine-300",
		"bending-hydraulic-machine-300"
	};

	public ObservableCollection<string> Experiments { get; } = new(ExperimentCatalog.GetAll().Select(e => e.ExperimentId));

	public ObservableCollection<string> Modes { get; } = new(["GroundTruthOnly", "VigilEvaluation"]);

	public ObservableCollection<string> GroundTruthEvents { get; } = new();

	public string SelectedMachineProfileId
	{
		get => _selectedMachineProfileId;
		set => SetProperty(ref _selectedMachineProfileId, value);
	}

	public string SelectedExperimentId
	{
		get => _selectedExperimentId;
		set => SetProperty(ref _selectedExperimentId, value);
	}

	public string SelectedMode
	{
		get => _selectedMode;
		set => SetProperty(ref _selectedMode, value);
	}

	public double TimeFactor
	{
		get => _timeFactor;
		set => SetProperty(ref _timeFactor, value);
	}

	public string Status
	{
		get => _status;
		set => SetProperty(ref _status, value);
	}

	public string CurrentRun
	{
		get => _currentRun;
		set => SetProperty(ref _currentRun, value);
	}

	public string RunType
	{
		get => _runType;
		set => SetProperty(ref _runType, value);
	}

	public string Repetition
	{
		get => _repetition;
		set => SetProperty(ref _repetition, value);
	}

	public string Progress
	{
		get => _progress;
		set => SetProperty(ref _progress, value);
	}

	public string SimulationTime
	{
		get => _simulationTime;
		set => SetProperty(ref _simulationTime, value);
	}

	public string CurrentPhase
	{
		get => _currentPhase;
		set => SetProperty(ref _currentPhase, value);
	}

	public int GroundTruthEventCount
	{
		get => _groundTruthEventCount;
		set => SetProperty(ref _groundTruthEventCount, value);
	}

	public bool VigilConnected
	{
		get => _vigilConnected;
		set => SetProperty(ref _vigilConnected, value);
	}

	public string ResultSummary
	{
		get => _resultSummary;
		set => SetProperty(ref _resultSummary, value);
	}

	public IRelayCommand StartCommand => new RelayCommand(Start);
	public IRelayCommand PauseCommand => new RelayCommand(Pause);
	public IRelayCommand ResumeCommand => new RelayCommand(Resume);
	public IRelayCommand StopCommand => new RelayCommand(Stop);
	public IRelayCommand RefreshCommand => new RelayCommand(Refresh);

	private void Start()
	{
		Status = "Experiment gestartet";
		CurrentRun = SelectedExperimentId;
		Progress = ExperimentCatalog.GetById(SelectedExperimentId)?.DisplayName ?? SelectedExperimentId;
		VigilConnected = SelectedMode == "VigilEvaluation";
		ResultSummary = "";
	}

	private void Pause() => Status = "Pausiert";

	private void Resume() => Status = "Fortgesetzt";

	private void Stop()
	{
		Status = "Gestoppt";
		var def = ExperimentCatalog.GetById(SelectedExperimentId);
		ResultSummary = def == null
			? "Kein Ergebnis"
			: $"Experiment {def.ExperimentId}\nModus: {SelectedMode}\nFaultRuns: {def.FaultRunCount}\nControlRuns: {def.ControlRunCount}";
	}

	private void Refresh()
	{
		Status = "Bereit";
		Progress = ExperimentCatalog.GetById(SelectedExperimentId)?.DisplayName ?? "—";
		VigilConnected = false;
	}
}
