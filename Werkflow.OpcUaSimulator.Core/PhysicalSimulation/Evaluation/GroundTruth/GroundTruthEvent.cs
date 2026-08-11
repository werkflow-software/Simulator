using System;

using System.Collections.Generic;

using System.Text.Json.Serialization;



namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;



public sealed class GroundTruthEvent

{

	public required string EventId { get; init; }



	public required string ExperimentId { get; init; }



	public required string RunId { get; init; }



	public required Guid MachineId { get; init; }



	public string? ScenarioId { get; init; }



	public required GroundTruthEventType EventType { get; init; }



	/// <summary>Experiment-absolute simulation time (preferred manifest basis).</summary>

	public TimeSpan ExperimentSimulationTimestamp { get; init; }



	/// <summary>Time since the containing run started (experiment time).</summary>

	public TimeSpan RunRelativeTimestamp { get; init; }



	/// <summary>Time since the active scenario started (scenario-local time).</summary>

	public TimeSpan ScenarioRelativeTimestamp { get; init; }



	/// <summary>Backward-compatible alias for experiment simulation time.</summary>

	public TimeSpan SimulationTimestamp => ExperimentSimulationTimestamp;



	/// <summary>Backward-compatible alias for run-relative time.</summary>

	public TimeSpan RelativeTimeSinceRunStart => RunRelativeTimestamp;



	public DateTimeOffset RealTimestampUtc { get; init; }



	public string? ScenarioPhase { get; init; }



	public string? Severity { get; init; }



	public double Intensity { get; init; }



	public int Seed { get; init; }



	public int FaultRepetitionIndex { get; init; }



	public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();



	[JsonIgnore]

	public string EventTypeName => EventType.ToString();

}


