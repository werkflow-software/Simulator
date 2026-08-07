using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class ProcessPhaseTransition
{
	public ProcessPhase FromPhase { get; init; }

	public ProcessPhase ToPhase { get; init; }

	public DateTimeOffset TimestampUtc { get; init; }

	public TimeSpan PhaseDuration { get; init; }

	public int JobIndex { get; init; }
}
