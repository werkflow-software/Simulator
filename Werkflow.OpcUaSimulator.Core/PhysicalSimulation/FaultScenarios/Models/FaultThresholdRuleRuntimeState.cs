using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultThresholdRuleRuntimeState
{
	public bool IsCurrentlySatisfied { get; set; }

	public bool PhysicallySatisfied { get; set; }

	public TimeSpan? CurrentSatisfiedSinceSimulationTime { get; set; }

	public DateTimeOffset? LastEnteredAtUtc { get; set; }

	public TimeSpan? LastEnteredSimulationTime { get; set; }

	public DateTimeOffset? LastExitedAtUtc { get; set; }

	public TimeSpan? LastExitedSimulationTime { get; set; }

	public TimeSpan? FirstEverReachedSimulationTime { get; set; }

	public bool HasEverBeenSatisfied { get; set; }

	public bool IsConfirmed { get; set; }

	public bool IsApproaching { get; set; }

	public int EnterCount { get; set; }

	public int ExitCount { get; set; }
}
