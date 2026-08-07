using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioStartRequest
{
	public required Guid MachineId { get; init; }

	public required string ScenarioId { get; init; }

	public double Intensity { get; init; } = 1.0;

	public double TimeFactor { get; init; } = 1.0;

	public int? Seed { get; init; }

	public bool AutoThresholdFaultEnabled { get; init; } = true;

	public bool AutoScenarioEndEnabled { get; init; } = true;

	public FaultScenarioRunMode RunMode { get; init; } = FaultScenarioRunMode.Normal;
}
