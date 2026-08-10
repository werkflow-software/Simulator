using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class GroundTruthEvent
{
	public required string EventId { get; init; }

	public required string ExperimentId { get; init; }

	public required string RunId { get; init; }

	public required Guid MachineId { get; init; }

	public string? ScenarioId { get; init; }

	public required GroundTruthEventType EventType { get; init; }

	public TimeSpan SimulationTimestamp { get; init; }

	public TimeSpan RelativeTimeSinceRunStart { get; init; }

	public DateTimeOffset RealTimestampUtc { get; init; }

	public string? ScenarioPhase { get; init; }

	public string? Severity { get; init; }

	public double Intensity { get; init; }

	public int Seed { get; init; }

	public int FaultRepetitionIndex { get; init; }

	public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
