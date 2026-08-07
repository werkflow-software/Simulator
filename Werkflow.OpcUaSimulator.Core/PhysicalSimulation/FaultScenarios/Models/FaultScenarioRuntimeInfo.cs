using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioRuntimeInfo
{
	public Guid InstanceId { get; init; }

	public string ScenarioId { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public FaultScenarioLifecycleState LifecycleState { get; init; }

	public FaultScenarioPhase CurrentPhase { get; init; }

	public FaultScenarioCategory Category { get; init; }

	public FaultScenarioSeverity Severity { get; init; }

	public double Intensity { get; init; }

	public double TimeFactor { get; init; }

	public TimeSpan RealElapsed { get; init; }

	public TimeSpan SimulationElapsed { get; init; }

	public bool ThresholdFaultTriggered { get; init; }

	public double RecoveryProgress { get; init; }

	public FaultScenarioRunMode RunMode { get; init; }

	public DateTimeOffset? NextPhaseChangeAt { get; init; }
}
