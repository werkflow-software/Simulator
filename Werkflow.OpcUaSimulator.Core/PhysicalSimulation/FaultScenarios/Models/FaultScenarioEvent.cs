using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioEvent
{
	public required FaultScenarioEventType EventType { get; init; }

	public required Guid MachineId { get; init; }

	public required string ScenarioId { get; init; }

	public Guid InstanceId { get; init; }

	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

	public FaultScenarioPhase? Phase { get; init; }

	public string? Detail { get; init; }

	public double? Value { get; init; }
}
