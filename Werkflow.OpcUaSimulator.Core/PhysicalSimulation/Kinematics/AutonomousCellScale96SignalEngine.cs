using System;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class AutonomousCellScale96SignalEngine
{
	public static void Reset(AutonomousCellScale96Runtime scale96, int seed)
	{
		Random init = new(Machine3SeedArchitecture.ProfileTierSeed("scale96") ^ seed);
		scale96.HydraulicSupplyBar = 0;
		scale96.HydraulicReturnBar = 0;
		scale96.AlignmentPinMm = 0;
		scale96.LoadMotorCurrentA = 0;
		scale96.LoadMotorCurrentB = 0;
		scale96.TransferMotorCurrentA = 0;
		scale96.LaneOccupancyIndex = 0;
		scale96.ConveyorSpeedMmPerS = 0;
		scale96.ScaleGrossKg = 48 + init.NextDouble() * 4;
		scale96.VacuumPumpCurrentA = 0;
		scale96.ClampApproachVelocityMmPerS = 0;
		scale96.PartEdgeGradient = 0;
		scale96.ForcePeakFilteredKn = 0;
		scale96.ClampPressureSecondary = 0;
		scale96.TransferEncoderMm = 0;
		scale96.DimensionOffsetDuplicateMm = 0;
		scale96.LineFrequencyHz = 49.8 + init.NextDouble() * 0.4;
		scale96.ContainerFillSmoothed = 0;
		scale96.BeltTensionN = 120 + init.NextDouble() * 10;
		scale96.CompressedAirBar = 6.0 + init.NextDouble() * 0.4;
		scale96.CoolantFlowLpm = 4.2 + init.NextDouble() * 0.6;
		scale96.LineVoltageRms = 228 + init.NextDouble() * 4;
		scale96.BrakeReleaseBar = 0;
		scale96.FilterDiffPressureBar = 0.15 + init.NextDouble() * 0.05;
		scale96.DieTemperatureC = 27.5 + init.NextDouble();
		scale96.GuideWearIndex = 0.01;
		scale96.LensTemperatureC = 23.5 + init.NextDouble();
		scale96.OilTemperatureC = 29.5 + init.NextDouble();
		scale96.FocusDriveMm = 12.0;
		scale96.DiverterActuationCount = 0;
		scale96.GroundLeakageMa = 0.8 + init.NextDouble() * 0.2;
		scale96.LabelApplicatorReady = false;
		scale96.PowerFactor = 0.9 + init.NextDouble() * 0.05;
		scale96.ServoBusUtilizationPercent = 0;
		scale96.FollowingErrorMm = 0;
		scale96.SurfaceReflectanceIndex = 0.5;
		scale96.ChilledWaterSupplyC = 11.5 + init.NextDouble();
		scale96.NeighborPressVibration = 0.2 + init.NextDouble() * 0.1;
		scale96.HvacDamperPercent = 40 + init.NextDouble() * 5;
		scale96.StackLightState = init.Next(0, 4);
		scale96.BarcodeConfidence = 0;
		scale96.RejectChuteMm = 0;
		scale96.BarcodeScannerTrigger = false;
		scale96.BacklightPercent = 60 + init.NextDouble() * 10;
		scale96.DoorInterlockState = 1;
		scale96.ShiftMaintenanceModeActive = false;
		scale96.CycleTimeSlidingAverageSeconds = 0;
		scale96.PeakForceLastStrokeKn = 0;
		scale96.FocusDriveAvailable = false;
		scale96.GroundLeakageAvailable = false;
		scale96.LabelApplicatorPulse = false;
		scale96.BarcodeConfidenceAvailable = false;
		scale96.RejectChuteAvailable = false;
		scale96.BarcodeTriggerPulse = false;
		scale96.SimulationElapsedSeconds = 0;
		scale96.LastStrokePeakForceKn = 0;
		scale96.ForcePeakFilterState = 0;
		scale96.ContainerFillEma = 0;
		scale96.LastCompletedPartsObserved = 0;
	}

	public static void Tick(AutonomousCellKinematicsState cell, AutonomousCellScale96Runtime scale96, double dt, int seed)
	{
		scale96.SimulationElapsedSeconds += dt;
		bool processActive = IsProcessMotion(cell.MotionPhase);
		bool loadActive = IsLoadMotion(cell.MotionPhase);
		bool transferActive = IsTransferMotion(cell.MotionPhase);
		bool visionActive = cell.MotionPhase == AutonomousCellMotionPhase.VisionInspect;
		bool outputActive = cell.MotionPhase is AutonomousCellMotionPhase.SortOutput or AutonomousCellMotionPhase.ContainerFill;

		double progress = cell.PhaseDurationSeconds <= 0 ? 0 : Math.Min(1.0, cell.PhaseElapsedSeconds / cell.PhaseDurationSeconds);
		Random noise = new(Machine3SeedArchitecture.SignalNoiseSeed ^ seed ^ (int)(scale96.SimulationElapsedSeconds * 10));

		scale96.HydraulicSupplyBar = processActive ? 120 + progress * 40 + cell.ProcessForceKn * 0.5 : 15;
		scale96.HydraulicReturnBar = scale96.HydraulicSupplyBar * (0.86 + noise.NextDouble() * 0.02);
		scale96.AlignmentPinMm = cell.MotionPhase is AutonomousCellMotionPhase.FixtureClamp or AutonomousCellMotionPhase.FixtureRelease
			? progress * 8.0
			: 0;
		scale96.LoadMotorCurrentA = loadActive ? 2.5 + progress * 6.0 + cell.LoadJointTorqueNm * 0.02 : 0.2;
		scale96.LoadMotorCurrentB = scale96.LoadMotorCurrentA * (0.97 + noise.NextDouble() * 0.04);
		scale96.TransferMotorCurrentA = transferActive ? 1.8 + progress * 5.5 : 0.15;
		scale96.LaneOccupancyIndex = Math.Min(3, cell.CompletedParts % 4);
		scale96.ConveyorSpeedMmPerS = outputActive ? 80 + progress * 40 : 0;
		if (cell.ReplenishmentEvents > 0 && cell.MotionPhase == AutonomousCellMotionPhase.HiddenInboundDelivery)
		{
			scale96.ScaleGrossKg = 46 + cell.PalletQuantityRemaining * 0.8;
		}

		scale96.VacuumPumpCurrentA = transferActive ? 1.2 + progress * 2.0 : 0.05;
		scale96.ClampApproachVelocityMmPerS = cell.MotionPhase == AutonomousCellMotionPhase.ProcessApproach ? 15 + progress * 25 : 0;
		scale96.PartEdgeGradient = visionActive ? Math.Abs(cell.VisionDimensionOffsetMm) * 12 + 0.5 : 0;
		scale96.ForcePeakFilterState += (cell.ProcessForceKn - scale96.ForcePeakFilterState) * Math.Min(1.0, dt * 2.0);
		scale96.ForcePeakFilteredKn = scale96.ForcePeakFilterState;
		scale96.ClampPressureSecondary = cell.FixtureClampForceN * 0.00045 + noise.NextDouble() * 0.02;
		scale96.TransferEncoderMm = cell.TransferAxisPositionMm + (noise.NextDouble() * 0.05 - 0.025);
		scale96.DimensionOffsetDuplicateMm = cell.VisionDimensionOffsetMm + (noise.NextDouble() * 0.01 - 0.005);
		scale96.LineFrequencyHz = 49.9 + (scale96.LineVoltageRms - 230.0) * 0.002 + noise.NextDouble() * 0.02;
		scale96.ContainerFillEma += (cell.ContainerFillLevel - scale96.ContainerFillEma) * Math.Min(1.0, dt * 0.5);
		scale96.ContainerFillSmoothed = scale96.ContainerFillEma;
		scale96.BeltTensionN = transferActive ? 130 + progress * 25 : 118 + noise.NextDouble() * 2;
		scale96.CompressedAirBar += (6.2 - scale96.CompressedAirBar) * dt * 0.01 + noise.NextDouble() * 0.001;
		scale96.CoolantFlowLpm += (4.5 - scale96.CoolantFlowLpm) * dt * 0.008 + noise.NextDouble() * 0.002;
		scale96.LineVoltageRms += (230.0 - scale96.LineVoltageRms) * dt * 0.005 + noise.NextDouble() * 0.05 - 0.025;
		scale96.BrakeReleaseBar = loadActive ? 4.0 + progress * 2.0 : 0;
		scale96.FilterDiffPressureBar += 0.00001 * dt + noise.NextDouble() * 0.0005;
		scale96.DieTemperatureC += (32 + cell.ProcessToolWearIndex * 4 - scale96.DieTemperatureC) * dt * 0.0008;
		scale96.GuideWearIndex = Math.Min(1.0, scale96.GuideWearIndex + dt * 0.00002);
		scale96.LensTemperatureC += (24.5 - scale96.LensTemperatureC) * dt * 0.0006;
		scale96.OilTemperatureC += (31 + cell.ProcessServoTempC * 0.05 - scale96.OilTemperatureC) * dt * 0.0005;
		scale96.ServoBusUtilizationPercent = processActive
			? 35 + progress * 40 + noise.NextDouble() * 4 - 2
			: 5 + noise.NextDouble() * 2;
		scale96.FollowingErrorMm = loadActive ? noise.NextDouble() * 0.08 - 0.04 : noise.NextDouble() * 0.02 - 0.01;
		scale96.SurfaceReflectanceIndex = visionActive
			? cell.VisionSurfaceScore * 0.01 + noise.NextDouble() * 0.05
			: 0.45 + noise.NextDouble() * 0.02;

		TickIrrelevantIndependent(scale96, dt, seed);
		TickSparseAvailability(scale96, cell, seed);
		TickIntermittent(scale96, cell, seed);
		TickContextual(scale96, cell);
		TickDerived(cell, scale96, dt);

		if (cell.CompletedParts > scale96.LastCompletedPartsObserved)
		{
			scale96.LastCompletedPartsObserved = cell.CompletedParts;
			if (cell.CompletedParts % 5 == 0)
			{
				scale96.DiverterActuationCount++;
			}
		}
	}

	public static void Apply(PhysicalMachineRuntime runtime, AutonomousCellKinematicsState cell, AutonomousCellScale96Runtime scale96, int seed)
	{
		Set(runtime, "Process.HydraulicPressureSupply", scale96.HydraulicSupplyBar);
		Set(runtime, "Process.HydraulicPressureReturn", scale96.HydraulicReturnBar);
		Set(runtime, "Fixture.AlignmentPinPosition", scale96.AlignmentPinMm);
		Set(runtime, "LoadRobot.MotorCurrentPhaseA", scale96.LoadMotorCurrentA);
		Set(runtime, "LoadRobot.MotorCurrentPhaseB", scale96.LoadMotorCurrentB);
		Set(runtime, "TransferRobot.MotorCurrentPhaseA", scale96.TransferMotorCurrentA);
		Set(runtime, "Sorting.LaneOccupancyIndex", scale96.LaneOccupancyIndex);
		Set(runtime, "Output.ConveyorSpeedActual", scale96.ConveyorSpeedMmPerS);
		Set(runtime, "Inbound.ScaleGrossWeight", scale96.ScaleGrossKg);
		Set(runtime, "TransferRobot.VacuumPumpCurrent", scale96.VacuumPumpCurrentA);
		Set(runtime, "Process.ClampApproachVelocity", scale96.ClampApproachVelocityMmPerS);
		Set(runtime, "Vision.PartEdgeGradient", scale96.PartEdgeGradient);
		Set(runtime, "Process.ForcePeakFiltered", scale96.ForcePeakFilteredKn);
		Set(runtime, "Fixture.ClampPressureSecondary", scale96.ClampPressureSecondary);
		Set(runtime, "TransferRobot.AxisPositionEncoder", scale96.TransferEncoderMm);
		Set(runtime, "Vision.DimensionOffsetDuplicate", scale96.DimensionOffsetDuplicateMm);
		Set(runtime, "Cell.LineFrequencyHz", scale96.LineFrequencyHz);
		Set(runtime, "Output.ContainerFillLevelSmoothed", scale96.ContainerFillSmoothed);
		Set(runtime, "TransferRobot.BeltTensionActual", scale96.BeltTensionN);
		Set(runtime, "Cell.CompressedAirPressure", scale96.CompressedAirBar);
		Set(runtime, "Cell.CoolantFlowRate", scale96.CoolantFlowLpm);
		Set(runtime, "Cell.LineVoltageRms", scale96.LineVoltageRms);
		Set(runtime, "LoadRobot.BrakeReleasePressure", scale96.BrakeReleaseBar);
		Set(runtime, "Process.FilterDifferentialPressure", scale96.FilterDiffPressureBar);
		Set(runtime, "Process.DieTemperatureZoneA", scale96.DieTemperatureC);
		Set(runtime, "Fixture.GuideWearIndicator", scale96.GuideWearIndex);
		Set(runtime, "Vision.LensTemperature", scale96.LensTemperatureC);
		Set(runtime, "Process.OilTemperatureSump", scale96.OilTemperatureC);
		if (scale96.FocusDriveAvailable)
		{
			Set(runtime, "Vision.FocusDrivePosition", scale96.FocusDriveMm);
		}

		Set(runtime, "Sorting.DiverterActuationCount", scale96.DiverterActuationCount);
		if (scale96.GroundLeakageAvailable)
		{
			Set(runtime, "Cell.GroundLeakageMilliamp", scale96.GroundLeakageMa);
		}

		if (scale96.LabelApplicatorPulse)
		{
			SetBool(runtime, "Output.LabelApplicatorReady", scale96.LabelApplicatorReady);
		}

		Set(runtime, "Cell.PowerFactorInstantaneous", scale96.PowerFactor + Noise(seed, 81) * 0.03);
		Set(runtime, "Process.ServoBusUtilization", scale96.ServoBusUtilizationPercent);
		Set(runtime, "LoadRobot.FollowingError", scale96.FollowingErrorMm);
		Set(runtime, "Vision.SurfaceReflectanceIndex", scale96.SurfaceReflectanceIndex);
		Set(runtime, "Auxiliary.PlantChilledWaterSupplyTemp", scale96.ChilledWaterSupplyC);
		Set(runtime, "Auxiliary.NeighborPressVibration", scale96.NeighborPressVibration);
		Set(runtime, "Auxiliary.BuildingHvacDamperPosition", scale96.HvacDamperPercent);
		Set(runtime, "Auxiliary.UnrelatedStackLightState", scale96.StackLightState);
		if (scale96.BarcodeConfidenceAvailable)
		{
			Set(runtime, "Vision.BarcodeReadConfidence", scale96.BarcodeConfidence);
		}

		if (scale96.RejectChuteAvailable)
		{
			Set(runtime, "Output.RejectChutePosition", scale96.RejectChuteMm);
		}

		if (scale96.BarcodeTriggerPulse)
		{
			SetBool(runtime, "Inbound.BarcodeScannerTrigger", scale96.BarcodeScannerTrigger);
		}

		Set(runtime, "Vision.BacklightIntensity", scale96.BacklightPercent);
		Set(runtime, "Cell.DoorInterlockState", scale96.DoorInterlockState);
		SetBool(runtime, "Cell.ShiftMaintenanceModeActive", scale96.ShiftMaintenanceModeActive);
		Set(runtime, "Process.CycleTimeSlidingAverage", scale96.CycleTimeSlidingAverageSeconds);
		Set(runtime, "Process.PeakForceLastStroke", scale96.PeakForceLastStrokeKn);
	}

	private static void TickDerived(AutonomousCellKinematicsState cell, AutonomousCellScale96Runtime scale96, double dt)
	{
		if (cell.MotionPhase == AutonomousCellMotionPhase.ProcessPressFit)
		{
			scale96.LastStrokePeakForceKn = Math.Max(scale96.LastStrokePeakForceKn, cell.ProcessForceKn);
		}
		else if (cell.MotionPhase == AutonomousCellMotionPhase.ProcessRetract && cell.PhaseElapsedSeconds < dt * 1.5)
		{
			scale96.PeakForceLastStrokeKn = scale96.LastStrokePeakForceKn;
			scale96.LastStrokePeakForceKn = 0;
		}

		double phaseDuration = Math.Max(1.0, cell.PhaseDurationSeconds);
		scale96.CycleTimeSlidingAverageSeconds += (phaseDuration - scale96.CycleTimeSlidingAverageSeconds) * Math.Min(1.0, dt * 0.02);
		scale96.PowerFactor = 0.88 + Math.Min(0.1, cell.ProcessForceKn * 0.002);
	}

	private static void TickContextual(AutonomousCellScale96Runtime scale96, AutonomousCellKinematicsState cell)
	{
		scale96.BacklightPercent = cell.CurrentVariant switch
		{
			'B' => 72,
			'C' => 78,
			_ => 65
		};
		scale96.DoorInterlockState = cell.MotionPhase == AutonomousCellMotionPhase.Complete ? 0 : 1;
		scale96.ShiftMaintenanceModeActive = false;
	}

	private static void TickIntermittent(AutonomousCellScale96Runtime scale96, AutonomousCellKinematicsState cell, int seed)
	{
		Random intermittent = new(Machine3SeedArchitecture.DropoutAvailabilitySeed ^ seed ^ cell.CompletedParts);
		scale96.BarcodeConfidenceAvailable = cell.MotionPhase == AutonomousCellMotionPhase.VisionInspect && intermittent.NextDouble() > 0.2;
		scale96.BarcodeConfidence = scale96.BarcodeConfidenceAvailable ? 0.7 + intermittent.NextDouble() * 0.25 : 0;
		scale96.RejectChuteAvailable = cell.QualityClassificationGt == "rework" && cell.MotionPhase == AutonomousCellMotionPhase.SortOutput;
		scale96.RejectChuteMm = scale96.RejectChuteAvailable ? 35 + intermittent.NextDouble() * 5 : 0;
		scale96.BarcodeTriggerPulse = cell.MotionPhase == AutonomousCellMotionPhase.HiddenInboundDelivery && cell.PhaseElapsedSeconds < 0.5;
		scale96.BarcodeScannerTrigger = scale96.BarcodeTriggerPulse;
	}

	private static void TickSparseAvailability(AutonomousCellScale96Runtime scale96, AutonomousCellKinematicsState cell, int seed)
	{
		Random sparse = new(Machine3SeedArchitecture.SparseSignalsSeed ^ seed);
		int sparseWindow = (int)(scale96.SimulationElapsedSeconds / 120.0);
		scale96.FocusDriveAvailable = sparseWindow % 4 == 0 && cell.PartIndex % 3 == 0;
		if (scale96.FocusDriveAvailable)
		{
			scale96.FocusDriveMm = 10 + sparse.NextDouble() * 4;
		}

		scale96.GroundLeakageAvailable = sparseWindow % 5 == 1;
		if (scale96.GroundLeakageAvailable)
		{
			scale96.GroundLeakageMa = 0.7 + sparse.NextDouble() * 0.4;
		}

		scale96.LabelApplicatorPulse = cell.ContainerExchangeRequested || cell.MotionPhase == AutonomousCellMotionPhase.ContainerFill;
		scale96.LabelApplicatorReady = scale96.LabelApplicatorPulse && sparse.NextDouble() > 0.3;
	}

	private static void TickIrrelevantIndependent(AutonomousCellScale96Runtime scale96, double dt, int seed)
	{
		Random r0 = new(Machine3SeedArchitecture.IrrelevantSlotSeed(2) ^ seed);
		Random r1 = new(Machine3SeedArchitecture.IrrelevantSlotSeed(3) ^ seed);
		Random r2 = new(Machine3SeedArchitecture.IrrelevantSlotSeed(4) ^ seed);
		Random r3 = new(Machine3SeedArchitecture.IrrelevantSlotSeed(5) ^ seed);
		if (r0.NextDouble() < dt * 0.08)
		{
			scale96.ChilledWaterSupplyC += r0.NextDouble() * 0.04 - 0.02;
		}

		if (r1.NextDouble() < dt * 0.12)
		{
			scale96.NeighborPressVibration += r1.NextDouble() * 0.05 - 0.025;
		}

		if (r2.NextDouble() < dt * 0.05)
		{
			scale96.HvacDamperPercent += r2.NextDouble() * 0.6 - 0.3;
		}

		if (r3.NextDouble() < dt * 0.03)
		{
			scale96.StackLightState = (scale96.StackLightState + r3.Next(0, 3)) % 4;
		}
	}

	private static double Noise(int seed, int channel) =>
		new Random(Machine3SeedArchitecture.SignalNoiseSeed ^ seed ^ channel).NextDouble() * 2 - 1;

	private static bool IsProcessMotion(AutonomousCellMotionPhase phase) =>
		phase is AutonomousCellMotionPhase.ProcessApproach
			or AutonomousCellMotionPhase.ProcessPressFit
			or AutonomousCellMotionPhase.ProcessRetract;

	private static bool IsLoadMotion(AutonomousCellMotionPhase phase) =>
		phase is AutonomousCellMotionPhase.LoadPick or AutonomousCellMotionPhase.LoadTransfer;

	private static bool IsTransferMotion(AutonomousCellMotionPhase phase) =>
		phase is AutonomousCellMotionPhase.TransferPickup or AutonomousCellMotionPhase.TransferToVision;

	private static void Set(PhysicalMachineRuntime runtime, string signalId, double value)
	{
		SignalRuntimeState? signal = runtime.Signals.FirstOrDefault(s => string.Equals(s.SignalId, signalId, StringComparison.OrdinalIgnoreCase));
		if (signal != null)
		{
			signal.CurrentValue = value;
		}
	}

	private static void Set(PhysicalMachineRuntime runtime, string signalId, int value) => Set(runtime, signalId, (double)value);

	private static void SetBool(PhysicalMachineRuntime runtime, string signalId, bool value) => Set(runtime, signalId, value ? 1.0 : 0.0);
}
