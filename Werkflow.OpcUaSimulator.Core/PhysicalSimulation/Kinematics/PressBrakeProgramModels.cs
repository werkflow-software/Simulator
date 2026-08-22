namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public sealed class PressBrakeBendStepDefinition
{
	public required int StepIndex { get; init; }

	public required double TargetAngleDeg { get; init; }

	public required double BackgaugePositionMm { get; init; }

	public required double PeakForceKn { get; init; }

	public double ApproachDurationSeconds { get; init; } = 2.8;

	public double FormingDurationSeconds { get; init; } = 1.6;

	public double HoldDurationSeconds { get; init; } = 0.7;

	public double ReturnDurationSeconds { get; init; } = 2.2;

	public double InterStepWaitSeconds { get; init; } = 1.1;
}

public sealed class PressBrakePartDefinition
{
	public required string PartId { get; init; }

	public required IReadOnlyList<PressBrakeBendStepDefinition> BendSteps { get; init; }

	public double InterPartWaitSeconds { get; init; } = 4.5;

	public double OperatorWaitChance { get; init; } = 0.12;
}

public sealed class PressBrakeProgramDefinition
{
	public required string ProgramId { get; init; }

	public required IReadOnlyList<PressBrakePartDefinition> Parts { get; init; }

	public double SetupDurationSeconds { get; init; } = 28.0;

	public double ToolChangeDurationSeconds { get; init; } = 42.0;

	public double ProgramTransitionSeconds { get; init; } = 18.0;
}
