namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

/// <summary>
/// Frozen Machine-3 SCALE96 semantic signal identifiers (signals 49-96).
/// </summary>
public static class AutonomousCellScale96SignalIds
{
	public static readonly IReadOnlyList<string> Additional =
	[
		"Process.HydraulicPressureSupply",
		"Process.HydraulicPressureReturn",
		"Fixture.AlignmentPinPosition",
		"LoadRobot.MotorCurrentPhaseA",
		"LoadRobot.MotorCurrentPhaseB",
		"TransferRobot.MotorCurrentPhaseA",
		"Sorting.LaneOccupancyIndex",
		"Output.ConveyorSpeedActual",
		"Inbound.ScaleGrossWeight",
		"TransferRobot.VacuumPumpCurrent",
		"Process.ClampApproachVelocity",
		"Vision.PartEdgeGradient",
		"Process.ForcePeakFiltered",
		"Fixture.ClampPressureSecondary",
		"TransferRobot.AxisPositionEncoder",
		"Vision.DimensionOffsetDuplicate",
		"Cell.LineFrequencyHz",
		"Output.ContainerFillLevelSmoothed",
		"TransferRobot.BeltTensionActual",
		"Cell.CompressedAirPressure",
		"Cell.CoolantFlowRate",
		"Cell.LineVoltageRms",
		"LoadRobot.BrakeReleasePressure",
		"Process.FilterDifferentialPressure",
		"Process.DieTemperatureZoneA",
		"Fixture.GuideWearIndicator",
		"Vision.LensTemperature",
		"Process.OilTemperatureSump",
		"Vision.FocusDrivePosition",
		"Sorting.DiverterActuationCount",
		"Cell.GroundLeakageMilliamp",
		"Output.LabelApplicatorReady",
		"Cell.PowerFactorInstantaneous",
		"Process.ServoBusUtilization",
		"LoadRobot.FollowingError",
		"Vision.SurfaceReflectanceIndex",
		"Auxiliary.PlantChilledWaterSupplyTemp",
		"Auxiliary.NeighborPressVibration",
		"Auxiliary.BuildingHvacDamperPosition",
		"Auxiliary.UnrelatedStackLightState",
		"Vision.BarcodeReadConfidence",
		"Output.RejectChutePosition",
		"Inbound.BarcodeScannerTrigger",
		"Vision.BacklightIntensity",
		"Cell.DoorInterlockState",
		"Cell.ShiftMaintenanceModeActive",
		"Process.CycleTimeSlidingAverage",
		"Process.PeakForceLastStroke"
	];

	public static readonly IReadOnlyList<string> All =
		AutonomousCellExpandedSignalIds.All.Concat(Additional).ToList();

	private static readonly HashSet<string> Scale96Set = new(Additional, StringComparer.OrdinalIgnoreCase);

	public static bool IsScale96Signal(string signalId) => Scale96Set.Contains(signalId);
}
