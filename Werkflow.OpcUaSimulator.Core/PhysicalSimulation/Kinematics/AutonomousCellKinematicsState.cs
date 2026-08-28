namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

using Werkflow.OpcUaSimulator.Core.VirtualMachine;

public sealed class AutonomousCellKinematicsState
{
	public static readonly IReadOnlyList<string> CoreSignalIds =
	[
		"Cell.OperationalState",
		"Cell.CurrentProductId",
		"Cell.CompletedPartCount",
		"Inbound.RawMaterialPresent",
		"Inbound.PalletQuantityRemaining",
		"LoadRobot.ActivityState",
		"LoadRobot.AxisPosition",
		"LoadRobot.GripperPressure",
		"LoadRobot.VelocityActual",
		"Fixture.ClampForce",
		"Fixture.ClampState",
		"Process.ActivityState",
		"Process.ForceActual",
		"Process.StrokePosition",
		"Process.ServoDriveTemperature",
		"TransferRobot.ActivityState",
		"TransferRobot.AxisPosition",
		"TransferRobot.GripperVacuum",
		"Vision.DimensionOffset",
		"Vision.SurfaceScore",
		"Vision.AlignmentDeviation",
		"Sorting.PositionIndex",
		"Output.ContainerFillLevel",
		"Output.ContainerExchangeRequested"
	];

	private static readonly HashSet<string> ControlledSignals = new(CoreSignalIds, StringComparer.OrdinalIgnoreCase);

	public bool IsEnabled { get; set; }

	public AutonomousCellMotionPhase MotionPhase { get; set; } = AutonomousCellMotionPhase.Idle;

	public int PartIndex { get; set; }

	public int CompletedParts { get; set; }

	public int TargetParts { get; set; } = VirtualAutonomousCellRunProfile.TotalParts;

	public char CurrentVariant { get; set; } = 'A';

	public int PalletQuantityRemaining { get; set; } = VirtualAutonomousCellRunProfile.PalletCapacity;

	public bool RawMaterialPresent { get; set; }

	public double ContainerFillLevel { get; set; }

	public bool ContainerExchangeRequested { get; set; }

	public bool EmptyContainerPresent { get; set; } = true;

	public int ContainerParts { get; set; }

	public double PhaseElapsedSeconds { get; set; }

	public double PhaseDurationSeconds { get; set; }

	public string CellOperationalToken { get; set; } = "CELL_ST_00";

	public string LoadActivityToken { get; set; } = "LR_AC_00";

	public string FixtureClampToken { get; set; } = "FX_ST_00";

	public string ProcessActivityToken { get; set; } = "PR_AC_00";

	public string TransferActivityToken { get; set; } = "TR_AC_00";

	public double LoadAxisPositionMm { get; set; }

	public double LoadVelocityMmPerS { get; set; }

	public double LoadGripperPressureBar { get; set; }

	public double LoadJointTorqueNm { get; set; }

	public double FixtureClampForceN { get; set; }

	public double FixturePartSeatForceN { get; set; }

	public double FixtureVibrationRms { get; set; }

	public long FixtureMaintenanceCounter { get; set; }

	public double ProcessForceKn { get; set; }

	public double ProcessStrokeMm { get; set; }

	public double ProcessEnergyIntegralKj { get; set; }

	public double ProcessRamVelocityMmPerS { get; set; }

	public double ProcessServoTempC { get; set; } = 32.0;

	public double ProcessToolWearIndex { get; set; }

	public double TransferAxisPositionMm { get; set; }

	public double TransferPathProgress { get; set; }

	public double TransferGripperVacuumKpa { get; set; }

	public double VisionDimensionOffsetMm { get; set; }

	public double VisionSurfaceScore { get; set; }

	public double VisionAlignmentDeviationMm { get; set; }

	public double VisionEdgeDeviationMm { get; set; }

	public double VisionDimensionOffsetFilteredMm { get; set; }

	public int VisionCameraExposureIndex { get; set; } = 100;

	public bool VisionCalibrationPulse { get; set; }

	public double VisionRawPixelContrast { get; set; }

	public bool VisionAlignmentIntermittentAvailable { get; set; } = true;

	public double VisionAlignmentIntermittentMm { get; set; }

	public int SortingPositionIndex { get; set; }

	public double CellAmbientHumidity { get; set; } = 45.0;

	public double CellEnclosureTemperatureC { get; set; } = 22.0;

	public double InboundMaterialWidthMm { get; set; } = 42.0;

	public double ProcessForceMirrorKn { get; set; }

	public double LoadAxisSecondaryMm { get; set; }

	public double OutputContainerFillPercent { get; set; }

	public double ProcessForceNoiseKn { get; set; }

	public double AuxiliaryPowerRippleV { get; set; }

	public long AuxiliaryConveyorEncoder { get; set; }

	public int CellOperationalStateCode { get; set; }

	public bool UnattendedBaselineEnabled { get; set; } = true;

	public int ReplenishmentEvents { get; set; }

	public int ContainerExchangeEvents { get; set; }

	public string HiddenAmrTaskState { get; set; } = "idle";

	public double HiddenAmrPositionM { get; set; }

	public double HiddenAmrBatteryPercent { get; set; } = 100.0;

	public string? HiddenAmrMissionId { get; set; }

	public string QualityClassificationGt { get; set; } = "accept";

	public bool ControlsSignal(string signalId) => IsEnabled && ControlledSignals.Contains(signalId);

	public bool ControlsExpandedSignal(string signalId) =>
		IsEnabled && AutonomousCellExpandedSignalIds.All.Contains(signalId);
}

public static class AutonomousCellExpandedSignalIds
{
	public static readonly IReadOnlyList<string> Additional =
	[
		"Process.EnergyIntegral",
		"Vision.EdgeDeviation",
		"Fixture.PartSeatForce",
		"LoadRobot.JointTorqueActual",
		"TransferRobot.PathProgress",
		"Output.EmptyContainerPresent",
		"Inbound.MaterialWidthRaw",
		"Process.RamVelocityActual",
		"Process.ForceActualMirror",
		"LoadRobot.AxisPositionSecondary",
		"Vision.DimensionOffsetFiltered",
		"Output.ContainerFillPercent",
		"Cell.AmbientHumidity",
		"Fixture.VibrationRms",
		"Vision.CameraExposureIndex",
		"Process.ToolWearIndex",
		"Cell.EnclosureTemperature",
		"Vision.CalibrationPulse",
		"Fixture.MaintenanceCounter",
		"Process.ForceSensorNoiseChannel",
		"Vision.RawPixelContrast",
		"Auxiliary.FacilityPowerRipple",
		"Auxiliary.UnrelatedConveyorEncoder",
		"Vision.AlignmentDeviationIntermittent"
	];

	public static readonly IReadOnlyList<string> All = CoreSignalIds().Concat(Additional).ToList();

	private static IEnumerable<string> CoreSignalIds() => AutonomousCellKinematicsState.CoreSignalIds;
}
