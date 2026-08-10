using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;

public sealed class VigilEvent
{
	public required string EventId { get; init; }

	public required string ExperimentId { get; init; }

	public required string RunId { get; init; }

	public required Guid MachineId { get; init; }

	public DateTimeOffset Timestamp { get; init; }

	public TimeSpan SimulationTimestamp { get; init; }

	public required VigilEventType EventType { get; init; }

	public double Confidence { get; init; }

	public string? Severity { get; init; }

	public IReadOnlyList<string> SignalReferences { get; init; } = [];

	public string? PatternId { get; init; }

	public string? Message { get; init; }

	public string? Source { get; init; }
}
