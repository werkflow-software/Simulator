using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class GroundTruthTimeline
{
	public required string ExperimentId { get; init; }

	public required string RunId { get; init; }

	public List<GroundTruthEvent> Events { get; init; } = [];
}
