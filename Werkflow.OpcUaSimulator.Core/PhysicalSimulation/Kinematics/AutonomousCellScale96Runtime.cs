namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public sealed class AutonomousCellScale96Runtime
{
	public double HydraulicSupplyBar { get; set; }

	public double HydraulicReturnBar { get; set; }

	public double AlignmentPinMm { get; set; }

	public double LoadMotorCurrentA { get; set; }

	public double LoadMotorCurrentB { get; set; }

	public double TransferMotorCurrentA { get; set; }

	public int LaneOccupancyIndex { get; set; }

	public double ConveyorSpeedMmPerS { get; set; }

	public double ScaleGrossKg { get; set; }

	public double VacuumPumpCurrentA { get; set; }

	public double ClampApproachVelocityMmPerS { get; set; }

	public double PartEdgeGradient { get; set; }

	public double ForcePeakFilteredKn { get; set; }

	public double ClampPressureSecondary { get; set; }

	public double TransferEncoderMm { get; set; }

	public double DimensionOffsetDuplicateMm { get; set; }

	public double LineFrequencyHz { get; set; } = 50.0;

	public double ContainerFillSmoothed { get; set; }

	public double BeltTensionN { get; set; }

	public double CompressedAirBar { get; set; } = 6.2;

	public double CoolantFlowLpm { get; set; } = 4.5;

	public double LineVoltageRms { get; set; } = 230.0;

	public double BrakeReleaseBar { get; set; }

	public double FilterDiffPressureBar { get; set; }

	public double DieTemperatureC { get; set; } = 28.0;

	public double GuideWearIndex { get; set; }

	public double LensTemperatureC { get; set; } = 24.0;

	public double OilTemperatureC { get; set; } = 30.0;

	public double FocusDriveMm { get; set; }

	public int DiverterActuationCount { get; set; }

	public double GroundLeakageMa { get; set; }

	public bool LabelApplicatorReady { get; set; }

	public double PowerFactor { get; set; } = 0.92;

	public double ServoBusUtilizationPercent { get; set; }

	public double FollowingErrorMm { get; set; }

	public double SurfaceReflectanceIndex { get; set; }

	public double ChilledWaterSupplyC { get; set; } = 12.0;

	public double NeighborPressVibration { get; set; }

	public double HvacDamperPercent { get; set; } = 42.0;

	public int StackLightState { get; set; }

	public double BarcodeConfidence { get; set; }

	public double RejectChuteMm { get; set; }

	public bool BarcodeScannerTrigger { get; set; }

	public double BacklightPercent { get; set; } = 65.0;

	public int DoorInterlockState { get; set; } = 1;

	public bool ShiftMaintenanceModeActive { get; set; }

	public double CycleTimeSlidingAverageSeconds { get; set; }

	public double PeakForceLastStrokeKn { get; set; }

	public bool FocusDriveAvailable { get; set; }

	public bool GroundLeakageAvailable { get; set; }

	public bool LabelApplicatorPulse { get; set; }

	public bool BarcodeConfidenceAvailable { get; set; }

	public bool RejectChuteAvailable { get; set; }

	public bool BarcodeTriggerPulse { get; set; }

	public double SimulationElapsedSeconds { get; set; }

	public double LastStrokePeakForceKn { get; set; }

	public double ForcePeakFilterState { get; set; }

	public double ContainerFillEma { get; set; }

	public int LastCompletedPartsObserved { get; set; }
}
