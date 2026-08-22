using System.Text.Json;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class PressBrakeGroundTruthRecorder : IPressBrakeGroundTruthRecorder
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly object _sync = new();
	private readonly List<PressBrakeGroundTruthEvent> _events = [];
	private Guid _machineId;
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
			_machineId = machineId;
			_seed = seed;
			_events.Clear();
			_artifactPath = artifactPath ?? ResolveDefaultArtifactPath(machineId, seed);
			Directory.CreateDirectory(Path.GetDirectoryName(_artifactPath)!);
		}
	}

	public void Record(PressBrakeGroundTruthEvent evt)
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
					programReference = evt.ProgramReference,
					partReference = evt.PartReference,
					bendStepReference = evt.BendStepReference,
					physicalPhase = evt.PhysicalPhase,
					source = evt.Source,
					seed = _seed
				}, JsonOptions);
				File.AppendAllText(_artifactPath, line + Environment.NewLine);
			}
		}
	}

	public IReadOnlyList<PressBrakeGroundTruthEvent> GetEvents()
	{
		lock (_sync)
		{
			return _events.ToList();
		}
	}

	public void Flush()
	{
		lock (_sync)
		{
			// append-only jsonl; nothing else required
		}
	}

	private static string ResolveDefaultArtifactPath(Guid machineId, int seed)
	{
		string root = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Werkflow",
			"OpcUaSimulator",
			"press-brake-ground-truth");
		return Path.Combine(root, $"pb-gt-{machineId:N}-{seed}.jsonl");
	}
}
