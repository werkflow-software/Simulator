using System.Text.Json;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class AutonomousCellGroundTruthEvent
{
	public DateTimeOffset TimestampUtc { get; init; }

	public Guid MachineId { get; init; }

	public string EventType { get; init; } = string.Empty;

	public int? PartOrdinal { get; init; }

	public string? ProductVariant { get; init; }

	public string? CellPhase { get; init; }

	public string? StationState { get; init; }

	public string? AmrTaskState { get; init; }

	public string? QualityClassification { get; init; }

	public string? SignalRelevanceClass { get; init; }

	public string Source { get; init; } = string.Empty;
}

public interface IAutonomousCellGroundTruthRecorder
{
	string? ArtifactPath { get; }

	void BeginSession(Guid machineId, int seed, string? artifactPath = null);

	void Record(AutonomousCellGroundTruthEvent evt);

	IReadOnlyList<AutonomousCellGroundTruthEvent> GetEvents();

	void Flush();
}

public sealed class AutonomousCellGroundTruthRecorder : IAutonomousCellGroundTruthRecorder
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly object _sync = new();
	private readonly List<AutonomousCellGroundTruthEvent> _events = [];
	private int _seed;
	private string? _artifactPath;

	public string? ArtifactPath
	{
		get
		{
			lock (_sync)
			{
				return _artifactPath;
			}
		}
	}

	public void BeginSession(Guid machineId, int seed, string? artifactPath = null)
	{
		lock (_sync)
		{
			_seed = seed;
			_events.Clear();
			_artifactPath = artifactPath ?? Path.Combine(
				Path.GetTempPath(),
				$"m3-gt-{machineId:N}-{seed}.jsonl");
			Directory.CreateDirectory(Path.GetDirectoryName(_artifactPath)!);
		}
	}

	public void Record(AutonomousCellGroundTruthEvent evt)
	{
		lock (_sync)
		{
			_events.Add(evt);
			if (_artifactPath != null)
			{
				string line = JsonSerializer.Serialize(new
				{
					timestamp = evt.TimestampUtc.ToString("O"),
					machineId = evt.MachineId,
					eventType = evt.EventType,
					partOrdinal = evt.PartOrdinal,
					productVariant = evt.ProductVariant,
					cellPhase = evt.CellPhase,
					stationState = evt.StationState,
					amrTaskState = evt.AmrTaskState,
					qualityClassification = evt.QualityClassification,
					signalRelevanceClass = evt.SignalRelevanceClass,
					source = evt.Source,
					seed = _seed
				}, JsonOptions);
				File.AppendAllText(_artifactPath, line + Environment.NewLine);
			}
		}
	}

	public IReadOnlyList<AutonomousCellGroundTruthEvent> GetEvents()
	{
		lock (_sync)
		{
			return _events.ToList();
		}
	}

	public void Flush()
	{
	}
}
