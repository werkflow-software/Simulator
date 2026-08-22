namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public sealed class PressBrakeKinematicsState
{
	private static readonly HashSet<string> ControlledSignals = new(StringComparer.OrdinalIgnoreCase)
	{
		"Machine.MachineState",
		"Machine.ProgramId",
		"Machine.PartId",
		"Machine.ActualCounter",
		"Machine.TargetCounter",
		"Machine.LastProductionChange",
		"Ram.Position",
		"Ram.Velocity",
		"Backgauge.Position",
		"Process.BendAngle",
		"Process.FormingForce",
		"Tool.StationState",
		"Thermal.HydraulicOilTemp",
		"Cycle.ActivityState"
	};

	public bool IsEnabled { get; set; }

	public PressBrakeMotionPhase MotionPhase { get; set; } = PressBrakeMotionPhase.Idle;

	public PressBrakeProgramDefinition? ActiveProgram { get; set; }

	public int ProgramIndex { get; set; }

	public int PartIndex { get; set; }

	public int BendStepIndex { get; set; }

	public int ProducedParts { get; set; }

	public int TargetParts { get; set; }

	public double PhaseElapsedSeconds { get; set; }

	public double RamPositionMm { get; set; } = VirtualPressBrakeKinematicsConfig.RamOpenPositionMm;

	public double RamVelocityMmPerS { get; set; }

	public double BackgaugePositionMm { get; set; }

	public double TargetBackgaugeMm { get; set; }

	public double BendAngleDeg { get; set; }

	public double TargetBendAngleDeg { get; set; }

	public double FormingForceKn { get; set; }

	public double HydraulicOilTempC { get; set; } = VirtualPressBrakeKinematicsConfig.BaseHydraulicOilTempC;

	public string MachineStateToken { get; set; } = "PB_ST_00";

	public string ActivityStateToken { get; set; } = "CY_AC_00";

	public string ToolStationToken { get; set; } = "TL_ST_00";

	public string ProgramId { get; set; } = "—";

	public string PartId { get; set; } = "—";

	public int PendingPartCompletions { get; set; }

	public bool InterruptRequested { get; set; }

	public bool ToolChangeRequired { get; set; }

	public string NextActionHint { get; set; } = "Bereit";

	public bool ControlsSignal(string signalId) => IsEnabled && ControlledSignals.Contains(signalId);
}
