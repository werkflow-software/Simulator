namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class PressBrakeGroundTruthEvent
{
	public DateTimeOffset TimestampUtc { get; init; }

	public Guid MachineId { get; init; }

	public string EventType { get; init; } = string.Empty;

	public string ProgramReference { get; init; } = string.Empty;

	public string PartReference { get; init; } = string.Empty;

	public int BendStepReference { get; init; }

	public string PhysicalPhase { get; init; } = string.Empty;

	public string Source { get; init; } = string.Empty;
}
