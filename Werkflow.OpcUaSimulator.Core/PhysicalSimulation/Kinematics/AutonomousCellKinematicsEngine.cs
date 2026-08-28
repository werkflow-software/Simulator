using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class AutonomousCellKinematicsEngine
{
	private static readonly Dictionary<AutonomousCellMotionPhase, double> NominalPhaseDurations = new()
	{
		[AutonomousCellMotionPhase.HiddenInboundDelivery] = 2.0,
		[AutonomousCellMotionPhase.LoadPick] = 8.0,
		[AutonomousCellMotionPhase.LoadTransfer] = 6.0,
		[AutonomousCellMotionPhase.FixtureClamp] = 4.0,
		[AutonomousCellMotionPhase.ProcessApproach] = 3.0,
		[AutonomousCellMotionPhase.ProcessPressFit] = 12.0,
		[AutonomousCellMotionPhase.ProcessRetract] = 3.0,
		[AutonomousCellMotionPhase.FixtureRelease] = 2.0,
		[AutonomousCellMotionPhase.TransferPickup] = 5.0,
		[AutonomousCellMotionPhase.TransferToVision] = 5.0,
		[AutonomousCellMotionPhase.VisionInspect] = 3.0,
		[AutonomousCellMotionPhase.SortOutput] = 2.0,
		[AutonomousCellMotionPhase.ContainerFill] = 1.0,
		[AutonomousCellMotionPhase.WaitReplenishment] = 30.0,
		[AutonomousCellMotionPhase.WaitContainerExchange] = 35.0,
		[AutonomousCellMotionPhase.HiddenOutboundExchange] = 3.0
	};

	public static bool ShouldEnable(Guid machineId) =>
		VirtualAutonomousCellMachineRegistry.IsVirtualAutonomousCellMachine(machineId);

	public static int ConsumePendingPartCompletions(PhysicalSimulationContext context)
	{
		if (!context.AutonomousCell.IsEnabled)
		{
			return 0;
		}

		int pending = context.AutonomousCell.PendingPartCompletions;
		context.AutonomousCell.PendingPartCompletions = 0;
		return pending;
	}

	public static void StopAndResetProduction(PhysicalSimulationContext context, int seed)
	{
		if (!context.AutonomousCell.IsEnabled)
		{
			return;
		}

		Initialize(context, seed, VirtualAutonomousProductionCellContract.MachineId);
		context.IsProductionMotionActive = false;
	}

	public static double GetPhaseRemainingSeconds(AutonomousCellKinematicsState cell) =>
		Math.Max(0.0, cell.PhaseDurationSeconds - cell.PhaseElapsedSeconds);

	public static void Initialize(PhysicalSimulationContext context, int seed, Guid machineId)
	{
		if (!ShouldEnable(machineId))
		{
			context.AutonomousCell.IsEnabled = false;
			return;
		}

		AutonomousCellKinematicsState cell = context.AutonomousCell;
		cell.IsEnabled = true;
		cell.MotionPhase = AutonomousCellMotionPhase.WaitRawMaterial;
		cell.PartIndex = 0;
		cell.CompletedParts = 0;
		cell.TargetParts = VirtualAutonomousCellRunProfile.TotalParts;
		cell.PalletQuantityRemaining = 0;
		cell.RawMaterialPresent = false;
		cell.ContainerFillLevel = 0;
		cell.ContainerParts = 0;
		cell.ContainerExchangeRequested = false;
		cell.EmptyContainerPresent = true;
		cell.ReplenishmentEvents = 0;
		cell.ContainerExchangeEvents = 0;
		cell.UnattendedBaselineEnabled = VirtualAutonomousCellRunProfile.UnattendedBaselineEnabled;
		cell.ProcessServoTempC = 32.0;
		cell.ProcessToolWearIndex = 0.0;
		cell.AuxiliaryConveyorEncoder = 0;
		ResetIrrelevantStreams(cell);
		AutonomousCellExposedSignalSemantics.ApplyTokens(cell);
		AdvancePhase(cell, AutonomousCellMotionPhase.HiddenInboundDelivery, seed, ResolveDuration(seed, AutonomousCellMotionPhase.HiddenInboundDelivery, Machine3SeedArchitecture.LogisticsAmrSeed));
	}

	public static void OnJobApplied(PhysicalSimulationContext context, int seed)
	{
		if (!context.AutonomousCell.IsEnabled)
		{
			return;
		}

		Initialize(context, seed, VirtualAutonomousProductionCellContract.MachineId);
		context.IsProductionMotionActive = true;
	}

	public static void Tick(
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime,
		PhysicalSimulationContext context,
		TimeSpan deltaTime,
		int seed,
		Guid machineId,
		IAutonomousCellGroundTruthRecorder? groundTruth = null)
	{
		if (!context.AutonomousCell.IsEnabled)
		{
			return;
		}

		double dt = deltaTime.TotalSeconds;
		if (dt <= 0.0)
		{
			return;
		}

		AutonomousCellKinematicsState cell = context.AutonomousCell;
		if (!context.IsProductionMotionActive)
		{
			ApplySignals(runtime, cell);
			return;
		}

		cell.PhaseElapsedSeconds += dt;
		UpdatePhysicsForPhase(cell, dt, seed);
		UpdateThermal(cell, dt);
		TickIrrelevantIndependent(cell, dt, seed);

		if (cell.PhaseElapsedSeconds >= cell.PhaseDurationSeconds)
		{
			AdvanceFromPhase(context, cell, seed, groundTruth, machineId);
		}

		ApplySignals(runtime, cell);
		ApplyExpandedSignals(runtime, cell, seed);
		ApplyBankSignals(runtime, profile, cell, seed);
	}

	private static void AdvanceFromPhase(
		PhysicalSimulationContext context,
		AutonomousCellKinematicsState cell,
		int seed,
		IAutonomousCellGroundTruthRecorder? groundTruth,
		Guid machineId)
	{
		switch (cell.MotionPhase)
		{
		case AutonomousCellMotionPhase.HiddenInboundDelivery:
			cell.RawMaterialPresent = true;
			cell.PalletQuantityRemaining = VirtualAutonomousCellRunProfile.PalletCapacity;
			cell.HiddenAmrTaskState = "idle";
			cell.CurrentVariant = VirtualAutonomousCellRunProfile.GetVariantForPartIndex(cell.CompletedParts);
			RecordGt(groundTruth, machineId, cell, "inbound_delivery_complete", "HiddenAmr");
			if (cell.ReplenishmentEvents > 0)
			{
				RecordGt(groundTruth, machineId, cell, "replenishment_complete", "HiddenAmr");
			}

			AdvancePhase(cell, AutonomousCellMotionPhase.LoadPick, seed, ResolveDuration(seed, AutonomousCellMotionPhase.LoadPick, Machine3SeedArchitecture.RobotVariabilitySeed));
			break;
		case AutonomousCellMotionPhase.LoadPick:
			cell.PalletQuantityRemaining = Math.Max(0, cell.PalletQuantityRemaining - 1);
			AdvancePhase(cell, AutonomousCellMotionPhase.LoadTransfer, seed, ResolveDuration(seed, AutonomousCellMotionPhase.LoadTransfer, Machine3SeedArchitecture.RobotVariabilitySeed));
			break;
		case AutonomousCellMotionPhase.LoadTransfer:
			AdvancePhase(cell, AutonomousCellMotionPhase.FixtureClamp, seed, ResolveDuration(seed, AutonomousCellMotionPhase.FixtureClamp, Machine3SeedArchitecture.PhysicalProcessSeed));
			break;
		case AutonomousCellMotionPhase.FixtureClamp:
			AdvancePhase(cell, AutonomousCellMotionPhase.ProcessApproach, seed, ResolveDuration(seed, AutonomousCellMotionPhase.ProcessApproach, Machine3SeedArchitecture.PhysicalProcessSeed));
			break;
		case AutonomousCellMotionPhase.ProcessApproach:
			AdvancePhase(cell, AutonomousCellMotionPhase.ProcessPressFit, seed, ResolveDuration(seed, AutonomousCellMotionPhase.ProcessPressFit, Machine3SeedArchitecture.PhysicalProcessSeed));
			RecordGt(groundTruth, machineId, cell, "process_press_start", "Process");
			break;
		case AutonomousCellMotionPhase.ProcessPressFit:
			AdvancePhase(cell, AutonomousCellMotionPhase.ProcessRetract, seed, ResolveDuration(seed, AutonomousCellMotionPhase.ProcessRetract, Machine3SeedArchitecture.PhysicalProcessSeed));
			RecordGt(groundTruth, machineId, cell, "process_press_end", "Process");
			break;
		case AutonomousCellMotionPhase.ProcessRetract:
			AdvancePhase(cell, AutonomousCellMotionPhase.FixtureRelease, seed, ResolveDuration(seed, AutonomousCellMotionPhase.FixtureRelease, Machine3SeedArchitecture.PhysicalProcessSeed));
			break;
		case AutonomousCellMotionPhase.FixtureRelease:
			AdvancePhase(cell, AutonomousCellMotionPhase.TransferPickup, seed, ResolveDuration(seed, AutonomousCellMotionPhase.TransferPickup, Machine3SeedArchitecture.RobotVariabilitySeed));
			break;
		case AutonomousCellMotionPhase.TransferPickup:
			AdvancePhase(cell, AutonomousCellMotionPhase.TransferToVision, seed, ResolveDuration(seed, AutonomousCellMotionPhase.TransferToVision, Machine3SeedArchitecture.RobotVariabilitySeed));
			break;
		case AutonomousCellMotionPhase.TransferToVision:
			AdvancePhase(cell, AutonomousCellMotionPhase.VisionInspect, seed, ResolveDuration(seed, AutonomousCellMotionPhase.VisionInspect, Machine3SeedArchitecture.VisionSeed));
			break;
		case AutonomousCellMotionPhase.VisionInspect:
			ComputeVisionFeatures(cell, seed);
			RecordGt(groundTruth, machineId, cell, "vision_inspect_complete", "Vision", cell.QualityClassificationGt);
			AdvancePhase(cell, AutonomousCellMotionPhase.SortOutput, seed, ResolveDuration(seed, AutonomousCellMotionPhase.SortOutput, Machine3SeedArchitecture.PhysicalProcessSeed));
			break;
		case AutonomousCellMotionPhase.SortOutput:
			cell.SortingPositionIndex = 1;
			AdvancePhase(cell, AutonomousCellMotionPhase.ContainerFill, seed, ResolveDuration(seed, AutonomousCellMotionPhase.ContainerFill, Machine3SeedArchitecture.PhysicalProcessSeed));
			break;
		case AutonomousCellMotionPhase.ContainerFill:
			CompletePart(cell, groundTruth, machineId);
			if (cell.CompletedParts >= cell.TargetParts)
			{
				cell.MotionPhase = AutonomousCellMotionPhase.Complete;
				context.IsProductionMotionActive = false;
				RecordGt(groundTruth, machineId, cell, "baseline_complete", "Cell");
				return;
			}

			if (cell.PalletQuantityRemaining <= 0)
			{
				cell.RawMaterialPresent = false;
				cell.ReplenishmentEvents++;
				RecordGt(groundTruth, machineId, cell, "replenishment_wait_start", "HiddenAmr");
				AdvancePhase(cell, AutonomousCellMotionPhase.WaitReplenishment, seed, ResolveDuration(seed, AutonomousCellMotionPhase.WaitReplenishment, Machine3SeedArchitecture.LogisticsAmrSeed));
			}
			else if (cell.ContainerExchangeRequested)
			{
				RecordGt(groundTruth, machineId, cell, "container_exchange_wait_start", "HiddenAmr");
				AdvancePhase(cell, AutonomousCellMotionPhase.WaitContainerExchange, seed, ResolveDuration(seed, AutonomousCellMotionPhase.WaitContainerExchange, Machine3SeedArchitecture.LogisticsAmrSeed));
			}
			else
			{
				StartNextPart(cell, seed);
			}

			break;
		case AutonomousCellMotionPhase.WaitReplenishment:
			cell.HiddenAmrTaskState = "inbound_delivery";
			AdvancePhase(cell, AutonomousCellMotionPhase.HiddenInboundDelivery, seed, ResolveDuration(seed, AutonomousCellMotionPhase.HiddenInboundDelivery, Machine3SeedArchitecture.LogisticsAmrSeed));
			break;
		case AutonomousCellMotionPhase.WaitContainerExchange:
			cell.HiddenAmrTaskState = "outbound_exchange";
			AdvancePhase(cell, AutonomousCellMotionPhase.HiddenOutboundExchange, seed, ResolveDuration(seed, AutonomousCellMotionPhase.HiddenOutboundExchange, Machine3SeedArchitecture.LogisticsAmrSeed));
			break;
		case AutonomousCellMotionPhase.HiddenOutboundExchange:
			cell.ContainerFillLevel = 0;
			cell.ContainerParts = 0;
			cell.ContainerExchangeRequested = false;
			cell.EmptyContainerPresent = true;
			cell.ContainerExchangeEvents++;
			cell.HiddenAmrTaskState = "idle";
			RecordGt(groundTruth, machineId, cell, "container_exchange_complete", "HiddenAmr");
			StartNextPart(cell, seed);
			break;
		default:
			StartNextPart(cell, seed);
			break;
		}
	}

	private static void CompletePart(AutonomousCellKinematicsState cell, IAutonomousCellGroundTruthRecorder? groundTruth, Guid machineId)
	{
		cell.CompletedParts++;
		cell.PendingPartCompletions++;
		cell.ContainerParts++;
		cell.ContainerFillLevel = Math.Min(1.0, cell.ContainerParts / (double)VirtualAutonomousCellRunProfile.ContainerCapacity);
		cell.OutputContainerFillPercent = cell.ContainerFillLevel * 100.0;
		cell.ProcessToolWearIndex = Math.Min(1.0, cell.ProcessToolWearIndex + 0.002);
		cell.FixtureMaintenanceCounter++;
		RecordGt(groundTruth, machineId, cell, "part_complete", "Cell");

		if (VirtualAutonomousCellRunProfile.RequiresExchangeAfterPart(cell.CompletedParts)
		    && cell.ContainerParts >= VirtualAutonomousCellRunProfile.ContainerCapacity)
		{
			cell.ContainerExchangeRequested = true;
			cell.EmptyContainerPresent = false;
		}
	}

	private static void StartNextPart(AutonomousCellKinematicsState cell, int seed)
	{
		cell.CurrentVariant = VirtualAutonomousCellRunProfile.GetVariantForPartIndex(cell.CompletedParts);
		cell.InboundMaterialWidthMm = cell.CurrentVariant switch
		{
			'B' => 48.5,
			'C' => 55.0,
			_ => 42.0
		};
		AdvancePhase(cell, AutonomousCellMotionPhase.LoadPick, seed, ResolveDuration(seed, AutonomousCellMotionPhase.LoadPick, Machine3SeedArchitecture.RobotVariabilitySeed));
	}

	private static void AdvancePhase(AutonomousCellKinematicsState cell, AutonomousCellMotionPhase phase, int seed, double durationSeconds)
	{
		cell.MotionPhase = phase;
		cell.PhaseElapsedSeconds = 0.0;
		cell.PhaseDurationSeconds = Math.Max(0.2, durationSeconds);
		AutonomousCellExposedSignalSemantics.ApplyTokens(cell);
	}

	private static double ResolveDuration(int seed, AutonomousCellMotionPhase phase, int streamSeed)
	{
		if (!NominalPhaseDurations.TryGetValue(phase, out double nominal))
		{
			nominal = 2.0;
		}

		Random random = new Random(streamSeed ^ (int)phase ^ seed);
		double factor = 0.92 + random.NextDouble() * 0.16;
		return nominal * factor;
	}

	private static void UpdatePhysicsForPhase(AutonomousCellKinematicsState cell, double dt, int seed)
	{
		double progress = cell.PhaseDurationSeconds <= 0 ? 1.0 : Math.Min(1.0, cell.PhaseElapsedSeconds / cell.PhaseDurationSeconds);
		Random processRandom = new Random(Machine3SeedArchitecture.PhysicalProcessSeed ^ seed ^ (int)cell.MotionPhase);

		switch (cell.MotionPhase)
		{
		case AutonomousCellMotionPhase.LoadPick or AutonomousCellMotionPhase.LoadTransfer:
			cell.LoadAxisPositionMm = 200 + progress * 600;
			cell.LoadAxisSecondaryMm = cell.LoadAxisPositionMm;
			cell.LoadVelocityMmPerS = 80 * (1.0 - Math.Abs(progress - 0.5));
			cell.LoadGripperPressureBar = 2.0 + progress * 4.0;
			cell.LoadJointTorqueNm = 20 + progress * 40;
			break;
		case AutonomousCellMotionPhase.FixtureClamp or AutonomousCellMotionPhase.FixtureRelease:
			cell.FixtureClampForceN = cell.MotionPhase == AutonomousCellMotionPhase.FixtureClamp ? progress * 2200 : (1 - progress) * 2200;
			cell.FixturePartSeatForceN = progress * 450;
			cell.FixtureVibrationRms = 0.2 + progress * 1.5;
			break;
		case AutonomousCellMotionPhase.ProcessApproach or AutonomousCellMotionPhase.ProcessPressFit or AutonomousCellMotionPhase.ProcessRetract:
			cell.ProcessStrokeMm = cell.MotionPhase == AutonomousCellMotionPhase.ProcessPressFit ? progress * 45 : progress * 20;
			cell.ProcessForceKn = cell.MotionPhase == AutonomousCellMotionPhase.ProcessPressFit ? progress * ResolveTargetForce(cell) : cell.ProcessForceKn * (1 - progress * 0.5);
			cell.ProcessRamVelocityMmPerS = cell.MotionPhase == AutonomousCellMotionPhase.ProcessPressFit ? 12 : 25;
			cell.ProcessEnergyIntegralKj += cell.ProcessForceKn * cell.ProcessRamVelocityMmPerS * dt / 1000.0;
			cell.ProcessForceMirrorKn = cell.ProcessForceKn;
			cell.ProcessForceNoiseKn = cell.ProcessForceKn + processRandom.NextDouble() * 0.4 - 0.2;
			break;
		case AutonomousCellMotionPhase.TransferPickup or AutonomousCellMotionPhase.TransferToVision:
			cell.TransferAxisPositionMm = 150 + progress * 700;
			cell.TransferPathProgress = progress;
			cell.TransferGripperVacuumKpa = -40 - progress * 35;
			break;
		case AutonomousCellMotionPhase.VisionInspect:
			break;
		}
	}

	private static double ResolveTargetForce(AutonomousCellKinematicsState cell) =>
		cell.CurrentVariant switch
		{
			'B' => 32.0,
			'C' => 38.0,
			_ => 28.0
		};

	private static void ComputeVisionFeatures(AutonomousCellKinematicsState cell, int seed)
	{
		Random visionRandom = new Random(Machine3SeedArchitecture.VisionSeed ^ seed ^ cell.PartIndex);
		double baseline = cell.CurrentVariant switch
		{
			'B' => 0.08,
			'C' => 0.12,
			_ => 0.05
		};
		cell.VisionDimensionOffsetMm = baseline + visionRandom.NextDouble() * 0.04 - 0.02;
		cell.VisionSurfaceScore = 88 + visionRandom.NextDouble() * 8;
		cell.VisionAlignmentDeviationMm = visionRandom.NextDouble() * 0.15;
		cell.VisionEdgeDeviationMm = cell.VisionDimensionOffsetMm * 0.8;
		cell.VisionDimensionOffsetFilteredMm = cell.VisionDimensionOffsetMm * 0.95;
		cell.VisionRawPixelContrast = cell.VisionSurfaceScore + visionRandom.NextDouble() * 2 - 1;
		cell.VisionCameraExposureIndex = 95 + cell.PartIndex % 5;
		cell.VisionCalibrationPulse = cell.PartIndex % 7 == 0;
		cell.VisionAlignmentIntermittentMm = cell.VisionAlignmentDeviationMm;
		cell.QualityClassificationGt = cell.VisionDimensionOffsetMm < 0.25 ? "accept" : "rework";
	}

	private static void UpdateThermal(AutonomousCellKinematicsState cell, double dt)
	{
		Random thermal = new Random(Machine3SeedArchitecture.ThermalSeed);
		double target = 32 + cell.ProcessToolWearIndex * 8 + thermal.NextDouble() * 0.01;
		cell.ProcessServoTempC += (target - cell.ProcessServoTempC) * dt * 0.02;
		cell.CellEnclosureTemperatureC += (23.5 - cell.CellEnclosureTemperatureC) * dt * 0.001;
		cell.CellAmbientHumidity += (45.0 - cell.CellAmbientHumidity) * dt * 0.0005;
	}

	private static void ResetIrrelevantStreams(AutonomousCellKinematicsState cell)
	{
		Random r1 = new Random(Machine3SeedArchitecture.IrrelevantSlotSeed(0));
		Random r2 = new Random(Machine3SeedArchitecture.IrrelevantSlotSeed(1));
		cell.AuxiliaryPowerRippleV = 228 + r1.NextDouble() * 4;
		cell.AuxiliaryConveyorEncoder = r2.Next(1000, 5000);
	}

	private static void TickIrrelevantIndependent(AutonomousCellKinematicsState cell, double dt, int seed)
	{
		Random r1 = new Random(Machine3SeedArchitecture.IrrelevantSlotSeed(0) ^ unchecked((int)(cell.AuxiliaryConveyorEncoder & 0xFFFF)));
		Random r2 = new Random(Machine3SeedArchitecture.IrrelevantSlotSeed(1) ^ seed);
		if (r1.NextDouble() < dt * 0.5)
		{
			cell.AuxiliaryPowerRippleV += r1.NextDouble() * 0.2 - 0.1;
		}

		if (r2.NextDouble() < dt * 0.3)
		{
			cell.AuxiliaryConveyorEncoder += r2.Next(-3, 4);
		}
	}

	private static void ApplySignals(PhysicalMachineRuntime runtime, AutonomousCellKinematicsState cell)
	{
		Set(runtime, "Cell.OperationalState", cell.CellOperationalStateCode);
		SetString(runtime, "Cell.CurrentProductId", cell.CurrentVariant.ToString());
		SetInt64(runtime, "Cell.CompletedPartCount", cell.CompletedParts);
		SetBool(runtime, "Inbound.RawMaterialPresent", cell.RawMaterialPresent);
		Set(runtime, "Inbound.PalletQuantityRemaining", cell.PalletQuantityRemaining);
		SetString(runtime, "LoadRobot.ActivityState", cell.LoadActivityToken);
		Set(runtime, "LoadRobot.AxisPosition", cell.LoadAxisPositionMm);
		Set(runtime, "LoadRobot.GripperPressure", cell.LoadGripperPressureBar);
		Set(runtime, "LoadRobot.VelocityActual", cell.LoadVelocityMmPerS);
		Set(runtime, "Fixture.ClampForce", cell.FixtureClampForceN);
		SetString(runtime, "Fixture.ClampState", cell.FixtureClampToken);
		SetString(runtime, "Process.ActivityState", cell.ProcessActivityToken);
		Set(runtime, "Process.ForceActual", cell.ProcessForceKn);
		Set(runtime, "Process.StrokePosition", cell.ProcessStrokeMm);
		Set(runtime, "Process.ServoDriveTemperature", cell.ProcessServoTempC);
		SetString(runtime, "TransferRobot.ActivityState", cell.TransferActivityToken);
		Set(runtime, "TransferRobot.AxisPosition", cell.TransferAxisPositionMm);
		Set(runtime, "TransferRobot.GripperVacuum", cell.TransferGripperVacuumKpa);
		Set(runtime, "Vision.DimensionOffset", cell.VisionDimensionOffsetMm);
		Set(runtime, "Vision.SurfaceScore", cell.VisionSurfaceScore);
		Set(runtime, "Vision.AlignmentDeviation", cell.VisionAlignmentDeviationMm);
		Set(runtime, "Sorting.PositionIndex", cell.SortingPositionIndex);
		Set(runtime, "Output.ContainerFillLevel", cell.ContainerFillLevel);
		SetBool(runtime, "Output.ContainerExchangeRequested", cell.ContainerExchangeRequested);
	}

	private static void ApplyExpandedSignals(PhysicalMachineRuntime runtime, AutonomousCellKinematicsState cell, int seed)
	{
		Set(runtime, "Process.EnergyIntegral", cell.ProcessEnergyIntegralKj);
		Set(runtime, "Vision.EdgeDeviation", cell.VisionEdgeDeviationMm);
		Set(runtime, "Fixture.PartSeatForce", cell.FixturePartSeatForceN);
		Set(runtime, "LoadRobot.JointTorqueActual", cell.LoadJointTorqueNm);
		Set(runtime, "TransferRobot.PathProgress", cell.TransferPathProgress);
		SetBool(runtime, "Output.EmptyContainerPresent", cell.EmptyContainerPresent);
		Set(runtime, "Inbound.MaterialWidthRaw", cell.InboundMaterialWidthMm);
		Set(runtime, "Process.RamVelocityActual", cell.ProcessRamVelocityMmPerS);
		Set(runtime, "Process.ForceActualMirror", cell.ProcessForceMirrorKn);
		Set(runtime, "LoadRobot.AxisPositionSecondary", cell.LoadAxisSecondaryMm);
		Set(runtime, "Vision.DimensionOffsetFiltered", cell.VisionDimensionOffsetFilteredMm);
		Set(runtime, "Output.ContainerFillPercent", cell.OutputContainerFillPercent);
		Set(runtime, "Cell.AmbientHumidity", cell.CellAmbientHumidity);
		Set(runtime, "Fixture.VibrationRms", cell.FixtureVibrationRms);
		Set(runtime, "Vision.CameraExposureIndex", cell.VisionCameraExposureIndex);
		Set(runtime, "Process.ToolWearIndex", cell.ProcessToolWearIndex);
		Set(runtime, "Cell.EnclosureTemperature", cell.CellEnclosureTemperatureC);
		SetBool(runtime, "Vision.CalibrationPulse", cell.VisionCalibrationPulse);
		SetInt64(runtime, "Fixture.MaintenanceCounter", cell.FixtureMaintenanceCounter);
		Set(runtime, "Process.ForceSensorNoiseChannel", cell.ProcessForceNoiseKn);
		Set(runtime, "Vision.RawPixelContrast", cell.VisionRawPixelContrast);
		Set(runtime, "Auxiliary.FacilityPowerRipple", cell.AuxiliaryPowerRippleV);
		SetInt64(runtime, "Auxiliary.UnrelatedConveyorEncoder", cell.AuxiliaryConveyorEncoder);

		Random dropout = new Random(Machine3SeedArchitecture.DropoutAvailabilitySeed ^ seed);
		cell.VisionAlignmentIntermittentAvailable = dropout.NextDouble() > 0.15;
		if (cell.VisionAlignmentIntermittentAvailable)
		{
			Set(runtime, "Vision.AlignmentDeviationIntermittent", cell.VisionAlignmentIntermittentMm);
		}
	}

	private static void ApplyBankSignals(PhysicalMachineRuntime runtime, PhysicalMachineProfile profile, AutonomousCellKinematicsState cell, int seed)
	{
		foreach (SignalDefinition signal in profile.Signals.Where(s => s.IsEnabled && s.SignalId.StartsWith("Bank.", StringComparison.Ordinal)))
		{
			int slot = int.Parse(signal.SignalId.Split('.')[1].Replace("Slot", string.Empty));
			Random random = new Random(Machine3SeedArchitecture.ProfileTierSeed("bank") ^ slot ^ seed);
			double value = random.NextDouble() * 100.0;
			Set(runtime, signal.SignalId, value);
		}
	}

	private static void RecordGt(
		IAutonomousCellGroundTruthRecorder? recorder,
		Guid machineId,
		AutonomousCellKinematicsState cell,
		string eventType,
		string source,
		string? quality = null)
	{
		recorder?.Record(new AutonomousCellGroundTruthEvent
		{
			TimestampUtc = DateTimeOffset.UtcNow,
			MachineId = machineId,
			EventType = eventType,
			PartOrdinal = cell.PartIndex,
			ProductVariant = cell.CurrentVariant.ToString(),
			CellPhase = cell.MotionPhase.ToString(),
			StationState = cell.ProcessActivityToken,
			AmrTaskState = cell.HiddenAmrTaskState,
			QualityClassification = quality ?? cell.QualityClassificationGt,
			Source = source
		});
	}

	private static void Set(PhysicalMachineRuntime runtime, string signalId, double value)
	{
		SignalRuntimeState? signal = runtime.Signals.FirstOrDefault(s => string.Equals(s.SignalId, signalId, StringComparison.OrdinalIgnoreCase));
		if (signal != null)
		{
			signal.CurrentValue = value;
		}
	}

	private static void Set(PhysicalMachineRuntime runtime, string signalId, int value) => Set(runtime, signalId, (double)value);

	private static void SetInt64(PhysicalMachineRuntime runtime, string signalId, long value)
	{
		SignalRuntimeState? signal = runtime.Signals.FirstOrDefault(s => string.Equals(s.SignalId, signalId, StringComparison.OrdinalIgnoreCase));
		if (signal != null)
		{
			signal.CurrentValue = value;
		}
	}

	private static void SetBool(PhysicalMachineRuntime runtime, string signalId, bool value) => Set(runtime, signalId, value ? 1.0 : 0.0);

	private static void SetString(PhysicalMachineRuntime runtime, string signalId, string value)
	{
		SignalRuntimeState? signal = runtime.Signals.FirstOrDefault(s => string.Equals(s.SignalId, signalId, StringComparison.OrdinalIgnoreCase));
		if (signal != null)
		{
			signal.CurrentStringValue = value;
		}
	}
}
