using System;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class PressBrakeKinematicsEngine
{
	public static bool ShouldEnable(Guid machineId) =>
		VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId);

	public static void Initialize(PhysicalSimulationContext context, int seed, Guid machineId)
	{
		if (!ShouldEnable(machineId))
		{
			context.PressBrake.IsEnabled = false;
			return;
		}

		PressBrakeKinematicsState pressBrake = context.PressBrake;
		pressBrake.IsEnabled = true;
		pressBrake.MotionPhase = PressBrakeMotionPhase.Idle;
		pressBrake.RamPositionMm = VirtualPressBrakeKinematicsConfig.RamOpenPositionMm;
		pressBrake.RamVelocityMmPerS = 0.0;
		pressBrake.BackgaugePositionMm = 400.0;
		pressBrake.TargetBackgaugeMm = 400.0;
		pressBrake.BendAngleDeg = 0.0;
		pressBrake.TargetBendAngleDeg = 0.0;
		pressBrake.FormingForceKn = 0.0;
		pressBrake.HydraulicOilTempC = VirtualPressBrakeKinematicsConfig.BaseHydraulicOilTempC;
		pressBrake.ProducedParts = 0;
		pressBrake.PendingPartCompletions = 0;
		pressBrake.PhaseElapsedSeconds = 0.0;
		pressBrake.InterruptRequested = false;
		pressBrake.ToolChangeRequired = false;
		pressBrake.NextActionHint = "Bereit";
		SetOpaqueTokens(pressBrake, PressBrakeMotionPhase.Idle);
		LoadProgram(context, seed, 0);
	}

	public static void OnProductionPaused(PhysicalSimulationContext context)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		context.PressBrake.RamVelocityMmPerS = 0.0;
		context.PressBrake.NextActionHint = "Pause";
	}

	public static void OnProductionResumed(PhysicalSimulationContext context)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		context.PressBrake.NextActionHint = "Fortsetzen";
	}

	public static void StopAndResetProduction(PhysicalSimulationContext context, int seed)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		context.IsProductionPaused = false;
		context.ProductionRunStartedAtUtc = null;
		context.IsProductionMotionActive = false;
		context.Job.ProducedQuantity = 0;
		Initialize(context, seed, VirtualPressBrakeContract.MachineId);
	}

	public static void AbortProductionForJobChange(PhysicalSimulationContext context, FixedProductionJobDefinition nextJob)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		context.IsProductionMotionActive = false;
		context.PressBrake.PendingPartCompletions = 0;
		context.PressBrake.ToolChangeRequired = nextJob.CatalogIndex % 2 == 0;
		context.PressBrake.MotionPhase = PressBrakeMotionPhase.ProgramTransition;
		context.PressBrake.PhaseElapsedSeconds = 0.0;
	}

	public static void OnJobChangeBegin(PhysicalSimulationContext context, FixedProductionJobDefinition nextJob)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		context.PressBrake.ToolChangeRequired = nextJob.CatalogIndex % 3 == 0;
		context.PressBrake.MotionPhase = PressBrakeMotionPhase.ProgramTransition;
		context.PressBrake.PhaseElapsedSeconds = 0.0;
	}

	public static void OnJobApplied(PhysicalSimulationContext context, int seed)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		int programIndex = Math.Max(0, context.Job.CatalogIndex) % PressBrakeProgramCatalog.ProgramCount;
		LoadProgram(context, seed, programIndex);
		context.PressBrake.ProducedParts = 0;
		context.PressBrake.PartIndex = 0;
		context.PressBrake.BendStepIndex = 0;
		context.PressBrake.PhaseElapsedSeconds = 0.0;
		context.PressBrake.MotionPhase = PressBrakeMotionPhase.Setup;
		context.PressBrake.TargetParts = Math.Max(1, context.Job.TargetQuantity);
	}

	public static int ConsumePendingPartCompletions(PhysicalSimulationContext context)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return 0;
		}

		int pending = context.PressBrake.PendingPartCompletions;
		context.PressBrake.PendingPartCompletions = 0;
		return pending;
	}

	public static double GetMotionDemand(PressBrakeMotionPhase phase) =>
		phase switch
		{
			PressBrakeMotionPhase.Idle => 0.12,
			PressBrakeMotionPhase.Setup => 0.22,
			PressBrakeMotionPhase.OperatorWait => 0.08,
			PressBrakeMotionPhase.ToolChange => 0.18,
			PressBrakeMotionPhase.ProgramTransition => 0.2,
			PressBrakeMotionPhase.BackgaugeMove => 0.35,
			PressBrakeMotionPhase.RamApproach => 0.55,
			PressBrakeMotionPhase.Forming => 0.92,
			PressBrakeMotionPhase.Hold => 0.85,
			PressBrakeMotionPhase.RamReturn => 0.48,
			PressBrakeMotionPhase.InterStepWait => 0.15,
			PressBrakeMotionPhase.InterPartWait => 0.1,
			PressBrakeMotionPhase.InterruptRecovery => 0.14,
			_ => 0.2
		};

	public static ProcessPhase MapToProcessPhase(PressBrakeMotionPhase phase) =>
		phase switch
		{
			PressBrakeMotionPhase.Idle => ProcessPhase.Idle,
			PressBrakeMotionPhase.Setup or PressBrakeMotionPhase.OperatorWait
				or PressBrakeMotionPhase.ToolChange or PressBrakeMotionPhase.ProgramTransition => ProcessPhase.Setup,
			PressBrakeMotionPhase.BackgaugeMove or PressBrakeMotionPhase.RamApproach => ProcessPhase.RampUp,
			PressBrakeMotionPhase.Forming or PressBrakeMotionPhase.Hold => ProcessPhase.Processing,
			PressBrakeMotionPhase.RamReturn => ProcessPhase.RampDown,
			PressBrakeMotionPhase.InterStepWait or PressBrakeMotionPhase.InterPartWait => ProcessPhase.Waiting,
			PressBrakeMotionPhase.InterruptRecovery => ProcessPhase.Cooling,
			_ => ProcessPhase.Idle
		};

	public static void Tick(
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime,
		PhysicalSimulationContext context,
		TimeSpan deltaTime,
		int seed,
		Guid machineId,
		IPressBrakeGroundTruthRecorder? groundTruth = null)
	{
		if (!context.PressBrake.IsEnabled)
		{
			return;
		}

		double dt = deltaTime.TotalSeconds;
		if (dt <= 0.0)
		{
			return;
		}

		PressBrakeKinematicsState pressBrake = context.PressBrake;
		if (context.IsProductionPaused)
		{
			pressBrake.RamVelocityMmPerS = 0.0;
			context.CurrentPhase = MapToProcessPhase(pressBrake.MotionPhase);
			ApplySignals(runtime, pressBrake);
			UpdateThermal(pressBrake, dt, forming: false);
			return;
		}

		if (!context.IsProductionMotionActive && !context.IsJobChangePauseActive)
		{
			TickIdleHold(pressBrake, dt);
			context.CurrentPhase = MapToProcessPhase(pressBrake.MotionPhase);
			ApplySignals(runtime, pressBrake);
			UpdateThermal(pressBrake, dt, forming: false);
			return;
		}

		if (context.IsJobChangePauseActive)
		{
			TickProgramTransition(context, pressBrake, dt, groundTruth, machineId);
		}
		else if (pressBrake.ProducedParts >= pressBrake.TargetParts && pressBrake.TargetParts > 0)
		{
			pressBrake.MotionPhase = PressBrakeMotionPhase.Idle;
			pressBrake.RamVelocityMmPerS = 0.0;
		}
		else
		{
			TickProduction(context, pressBrake, seed, dt, groundTruth, machineId);
		}

		context.CurrentPhase = MapToProcessPhase(pressBrake.MotionPhase);
		ApplySignals(runtime, pressBrake);
		UpdateThermal(pressBrake, dt, pressBrake.MotionPhase is PressBrakeMotionPhase.Forming or PressBrakeMotionPhase.Hold);
	}

	private static void TickProduction(
		PhysicalSimulationContext context,
		PressBrakeKinematicsState pressBrake,
		int seed,
		double dt,
		IPressBrakeGroundTruthRecorder? groundTruth,
		Guid machineId)
	{
		if (pressBrake.ActiveProgram == null)
		{
			LoadProgram(context, seed, 0);
		}

		PressBrakePartDefinition? part = GetCurrentPart(pressBrake);
		PressBrakeBendStepDefinition? step = GetCurrentStep(pressBrake, part);
		double phaseDuration = GetPhaseDuration(pressBrake, part, step, seed);

		if (pressBrake.InterruptRequested && pressBrake.MotionPhase is not PressBrakeMotionPhase.InterruptRecovery)
		{
			pressBrake.MotionPhase = PressBrakeMotionPhase.InterruptRecovery;
			pressBrake.PhaseElapsedSeconds = 0.0;
			RecordGt(groundTruth, machineId, pressBrake, "interruption_start", "PressBrakeKinematicsEngine");
		}

		switch (pressBrake.MotionPhase)
		{
		case PressBrakeMotionPhase.Setup:
			TickTimedPhase(pressBrake, dt, pressBrake.ActiveProgram!.SetupDurationSeconds, PressBrakeMotionPhase.BackgaugeMove);
			pressBrake.NextActionHint = "Einrichten";
			RecordGtOnce(groundTruth, machineId, pressBrake, "setup_start", "PressBrakeKinematicsEngine", pressBrake.PhaseElapsedSeconds <= dt);
			if (pressBrake.MotionPhase == PressBrakeMotionPhase.BackgaugeMove)
			{
				RecordGt(groundTruth, machineId, pressBrake, "setup_end", "PressBrakeKinematicsEngine");
			}
			break;
		case PressBrakeMotionPhase.OperatorWait:
			TickTimedPhase(pressBrake, dt, 8.0 + (seed % 7), PressBrakeMotionPhase.BackgaugeMove);
			pressBrake.NextActionHint = "Bedienervorgang";
			RecordGtOnce(groundTruth, machineId, pressBrake, "operator_wait_start", "PressBrakeKinematicsEngine", pressBrake.PhaseElapsedSeconds <= dt);
			break;
		case PressBrakeMotionPhase.ToolChange:
			TickTimedPhase(pressBrake, dt, pressBrake.ActiveProgram!.ToolChangeDurationSeconds, PressBrakeMotionPhase.Setup);
			pressBrake.NextActionHint = "Werkzeugwechsel";
			RecordGtOnce(groundTruth, machineId, pressBrake, "tooling_change_start", "PressBrakeKinematicsEngine", pressBrake.PhaseElapsedSeconds <= dt);
			break;
		case PressBrakeMotionPhase.BackgaugeMove:
			TickBackgaugeMove(pressBrake, step, dt);
			if (pressBrake.PhaseElapsedSeconds >= GetBackgaugeDuration(pressBrake, step))
			{
				AdvancePhase(pressBrake, PressBrakeMotionPhase.RamApproach);
				RecordGt(groundTruth, machineId, pressBrake, "bend_step_start", "PressBrakeKinematicsEngine", step?.StepIndex);
			}
			break;
		case PressBrakeMotionPhase.RamApproach:
			TickRamApproach(pressBrake, step, dt);
			if (pressBrake.PhaseElapsedSeconds >= step?.ApproachDurationSeconds)
			{
				AdvancePhase(pressBrake, PressBrakeMotionPhase.Forming);
				RecordGt(groundTruth, machineId, pressBrake, "approach_end", "PressBrakeKinematicsEngine", step?.StepIndex);
				RecordGt(groundTruth, machineId, pressBrake, "forming_start", "PressBrakeKinematicsEngine", step?.StepIndex);
			}
			break;
		case PressBrakeMotionPhase.Forming:
			TickForming(pressBrake, step, dt);
			if (pressBrake.PhaseElapsedSeconds >= step?.FormingDurationSeconds)
			{
				AdvancePhase(pressBrake, PressBrakeMotionPhase.Hold);
				RecordGt(groundTruth, machineId, pressBrake, "forming_end", "PressBrakeKinematicsEngine", step?.StepIndex);
			}
			break;
		case PressBrakeMotionPhase.Hold:
			TickHold(pressBrake, step, dt);
			if (pressBrake.PhaseElapsedSeconds >= step?.HoldDurationSeconds)
			{
				AdvancePhase(pressBrake, PressBrakeMotionPhase.RamReturn);
				RecordGt(groundTruth, machineId, pressBrake, "return_start", "PressBrakeKinematicsEngine", step?.StepIndex);
			}
			break;
		case PressBrakeMotionPhase.RamReturn:
			TickRamReturn(pressBrake, dt);
			if (pressBrake.PhaseElapsedSeconds >= step?.ReturnDurationSeconds)
			{
				RecordGt(groundTruth, machineId, pressBrake, "return_end", "PressBrakeKinematicsEngine", step?.StepIndex);
				RecordGt(groundTruth, machineId, pressBrake, "bend_step_end", "PressBrakeKinematicsEngine", step?.StepIndex);
				CompleteBendStep(context, pressBrake, part, seed, groundTruth, machineId);
			}
			break;
		case PressBrakeMotionPhase.InterStepWait:
			TickTimedPhase(pressBrake, dt, step?.InterStepWaitSeconds ?? 1.0, PressBrakeMotionPhase.BackgaugeMove);
			pressBrake.NextActionHint = "Zwischenschritt";
			break;
		case PressBrakeMotionPhase.InterPartWait:
			TickTimedPhase(pressBrake, dt, part?.InterPartWaitSeconds ?? 4.0, PressBrakeMotionPhase.BackgaugeMove);
			pressBrake.NextActionHint = "Teilepause";
			RecordGtOnce(groundTruth, machineId, pressBrake, "inter_part_wait", "PressBrakeKinematicsEngine", pressBrake.PhaseElapsedSeconds <= dt);
			if (pressBrake.MotionPhase == PressBrakeMotionPhase.BackgaugeMove)
			{
				RecordGt(groundTruth, machineId, pressBrake, "part_start", "PressBrakeKinematicsEngine");
			}
			break;
		case PressBrakeMotionPhase.InterruptRecovery:
			TickTimedPhase(pressBrake, dt, 2.5, PressBrakeMotionPhase.BackgaugeMove);
			pressBrake.InterruptRequested = false;
			pressBrake.NextActionHint = "Unterbrechung";
			RecordGtOnce(groundTruth, machineId, pressBrake, "interruption_end", "PressBrakeKinematicsEngine", pressBrake.PhaseElapsedSeconds <= dt);
			break;
		default:
			AdvancePhase(pressBrake, PressBrakeMotionPhase.Setup);
			break;
		}

		SetOpaqueTokens(pressBrake, pressBrake.MotionPhase);
	}

	private static void CompleteBendStep(
		PhysicalSimulationContext context,
		PressBrakeKinematicsState pressBrake,
		PressBrakePartDefinition? part,
		int seed,
		IPressBrakeGroundTruthRecorder? groundTruth,
		Guid machineId)
	{
		if (part == null)
		{
			return;
		}

		pressBrake.BendStepIndex++;
		if (pressBrake.BendStepIndex < part.BendSteps.Count)
		{
			AdvancePhase(pressBrake, PressBrakeMotionPhase.InterStepWait);
			return;
		}

		RecordGt(groundTruth, machineId, pressBrake, "part_end", "PressBrakeKinematicsEngine");
		pressBrake.ProducedParts++;
		pressBrake.PendingPartCompletions++;
		context.Job.ProducedQuantity = pressBrake.ProducedParts;
		RecordGt(groundTruth, machineId, pressBrake, "cycle_completion", "PressBrakeKinematicsEngine");

		if (pressBrake.ProducedParts >= pressBrake.TargetParts)
		{
			pressBrake.MotionPhase = PressBrakeMotionPhase.Idle;
			pressBrake.PhaseElapsedSeconds = 0.0;
			return;
		}

		pressBrake.PartIndex = (pressBrake.PartIndex + 1) % pressBrake.ActiveProgram!.Parts.Count;
		pressBrake.BendStepIndex = 0;
		LoadCurrentPart(pressBrake);
		if (ShouldOperatorWait(part, seed, pressBrake.ProducedParts))
		{
			AdvancePhase(pressBrake, PressBrakeMotionPhase.OperatorWait);
			return;
		}

		AdvancePhase(pressBrake, PressBrakeMotionPhase.InterPartWait);
	}

	private static void TickProgramTransition(
		PhysicalSimulationContext context,
		PressBrakeKinematicsState pressBrake,
		double dt,
		IPressBrakeGroundTruthRecorder? groundTruth,
		Guid machineId)
	{
		double duration = pressBrake.ToolChangeRequired
			? pressBrake.ActiveProgram?.ToolChangeDurationSeconds ?? 40.0
			: pressBrake.ActiveProgram?.ProgramTransitionSeconds ?? 18.0;
		PressBrakeMotionPhase next = pressBrake.ToolChangeRequired
			? PressBrakeMotionPhase.ToolChange
			: PressBrakeMotionPhase.Setup;
		if (pressBrake.MotionPhase != PressBrakeMotionPhase.ToolChange && pressBrake.MotionPhase != PressBrakeMotionPhase.ProgramTransition)
		{
			pressBrake.MotionPhase = PressBrakeMotionPhase.ProgramTransition;
			pressBrake.PhaseElapsedSeconds = 0.0;
			RecordGt(groundTruth, machineId, pressBrake, "program_transition_start", "PressBrakeKinematicsEngine");
		}

		TickTimedPhase(pressBrake, dt, duration, next);
		pressBrake.NextActionHint = "Programmwechsel";
		if (pressBrake.MotionPhase == next)
		{
			RecordGt(groundTruth, machineId, pressBrake, "program_transition_end", "PressBrakeKinematicsEngine");
			context.IsJobChangePauseActive = false;
			context.IsProductionMotionActive = true;
		}
	}

	private static void TickBackgaugeMove(PressBrakeKinematicsState pressBrake, PressBrakeBendStepDefinition? step, double dt)
	{
		if (step == null)
		{
			return;
		}

		pressBrake.TargetBackgaugeMm = step.BackgaugePositionMm;
		double delta = pressBrake.TargetBackgaugeMm - pressBrake.BackgaugePositionMm;
		double maxMove = VirtualPressBrakeKinematicsConfig.BackgaugeSpeedMmPerS * dt;
		if (Math.Abs(delta) <= maxMove)
		{
			pressBrake.BackgaugePositionMm = pressBrake.TargetBackgaugeMm;
		}
		else
		{
			pressBrake.BackgaugePositionMm += Math.Sign(delta) * maxMove;
		}

		pressBrake.PhaseElapsedSeconds += dt;
		pressBrake.RamVelocityMmPerS = 0.0;
		pressBrake.NextActionHint = "Rückanschlag";
	}

	private static void TickRamApproach(PressBrakeKinematicsState pressBrake, PressBrakeBendStepDefinition? step, double dt)
	{
		double target = GetFormingRamPosition(step);
		MoveRam(pressBrake, target, VirtualPressBrakeKinematicsConfig.RamApproachSpeedMmPerS, dt);
		pressBrake.PhaseElapsedSeconds += dt;
		pressBrake.BendAngleDeg = Math.Max(0.0, pressBrake.BendAngleDeg - 12.0 * dt);
		pressBrake.FormingForceKn = Math.Max(0.0, pressBrake.FormingForceKn - 30.0 * dt);
		pressBrake.NextActionHint = "Anfahren";
	}

	private static void TickForming(PressBrakeKinematicsState pressBrake, PressBrakeBendStepDefinition? step, double dt)
	{
		if (step == null)
		{
			return;
		}

		double target = GetFormingRamPosition(step);
		MoveRam(pressBrake, target, VirtualPressBrakeKinematicsConfig.RamFormingSpeedMmPerS, dt);
		pressBrake.PhaseElapsedSeconds += dt;
		double progress = Math.Clamp(pressBrake.PhaseElapsedSeconds / Math.Max(0.1, step.FormingDurationSeconds), 0.0, 1.0);
		pressBrake.BendAngleDeg = step.TargetAngleDeg * progress;
		pressBrake.FormingForceKn = step.PeakForceKn * (0.2 + 0.8 * progress);
		pressBrake.NextActionHint = "Umformen";
	}

	private static void TickHold(PressBrakeKinematicsState pressBrake, PressBrakeBendStepDefinition? step, double dt)
	{
		pressBrake.RamVelocityMmPerS = 0.0;
		pressBrake.PhaseElapsedSeconds += dt;
		if (step != null)
		{
			pressBrake.BendAngleDeg = step.TargetAngleDeg;
			pressBrake.FormingForceKn = step.PeakForceKn * 0.95;
		}

		pressBrake.NextActionHint = "Halten";
	}

	private static void TickRamReturn(PressBrakeKinematicsState pressBrake, double dt)
	{
		MoveRam(pressBrake, VirtualPressBrakeKinematicsConfig.RamOpenPositionMm, VirtualPressBrakeKinematicsConfig.RamReturnSpeedMmPerS, dt);
		pressBrake.PhaseElapsedSeconds += dt;
		pressBrake.BendAngleDeg = Math.Max(0.0, pressBrake.BendAngleDeg - 18.0 * dt);
		pressBrake.FormingForceKn = Math.Max(0.0, pressBrake.FormingForceKn - 45.0 * dt);
		pressBrake.NextActionHint = "Rückhub";
	}

	private static void TickIdleHold(PressBrakeKinematicsState pressBrake, double dt)
	{
		pressBrake.MotionPhase = PressBrakeMotionPhase.Idle;
		pressBrake.RamVelocityMmPerS = 0.0;
		pressBrake.FormingForceKn = Math.Max(0.0, pressBrake.FormingForceKn - 20.0 * dt);
		pressBrake.BendAngleDeg = Math.Max(0.0, pressBrake.BendAngleDeg - 8.0 * dt);
	}

	private static void TickTimedPhase(PressBrakeKinematicsState pressBrake, double dt, double durationSeconds, PressBrakeMotionPhase nextPhase)
	{
		pressBrake.PhaseElapsedSeconds += dt;
		pressBrake.RamVelocityMmPerS = 0.0;
		if (pressBrake.PhaseElapsedSeconds >= durationSeconds)
		{
			AdvancePhase(pressBrake, nextPhase);
		}
	}

	private static void AdvancePhase(PressBrakeKinematicsState pressBrake, PressBrakeMotionPhase nextPhase)
	{
		pressBrake.MotionPhase = nextPhase;
		pressBrake.PhaseElapsedSeconds = 0.0;
	}

	private static void MoveRam(PressBrakeKinematicsState pressBrake, double targetMm, double speedMmPerS, double dt)
	{
		double delta = targetMm - pressBrake.RamPositionMm;
		double maxMove = speedMmPerS * dt;
		if (Math.Abs(delta) <= maxMove)
		{
			pressBrake.RamVelocityMmPerS = Math.Abs(delta) / Math.Max(dt, 0.001);
			pressBrake.RamPositionMm = targetMm;
		}
		else
		{
			pressBrake.RamVelocityMmPerS = Math.Sign(delta) * speedMmPerS;
			pressBrake.RamPositionMm += Math.Sign(delta) * maxMove;
		}
	}

	private static double GetFormingRamPosition(PressBrakeBendStepDefinition? step)
	{
		if (step == null)
		{
			return VirtualPressBrakeKinematicsConfig.RamOpenPositionMm * 0.55;
		}

		double ratio = Math.Clamp(step.TargetAngleDeg / 120.0, 0.15, 0.95);
		return VirtualPressBrakeKinematicsConfig.RamOpenPositionMm * (1.0 - ratio * 0.72);
	}

	private static double GetBackgaugeDuration(PressBrakeKinematicsState pressBrake, PressBrakeBendStepDefinition? step)
	{
		if (step == null)
		{
			return 1.5;
		}

		double distance = Math.Abs(step.BackgaugePositionMm - pressBrake.BackgaugePositionMm);
		return Math.Max(1.2, distance / VirtualPressBrakeKinematicsConfig.BackgaugeSpeedMmPerS + 0.4);
	}

	private static double GetPhaseDuration(PressBrakeKinematicsState pressBrake, PressBrakePartDefinition? part, PressBrakeBendStepDefinition? step, int seed) =>
		pressBrake.MotionPhase switch
		{
			PressBrakeMotionPhase.Setup => pressBrake.ActiveProgram?.SetupDurationSeconds ?? 28.0,
			PressBrakeMotionPhase.ToolChange => pressBrake.ActiveProgram?.ToolChangeDurationSeconds ?? 40.0,
			PressBrakeMotionPhase.BackgaugeMove => GetBackgaugeDuration(pressBrake, step),
			PressBrakeMotionPhase.RamApproach => step?.ApproachDurationSeconds ?? 2.8,
			PressBrakeMotionPhase.Forming => step?.FormingDurationSeconds ?? 1.6,
			PressBrakeMotionPhase.Hold => step?.HoldDurationSeconds ?? 0.7,
			PressBrakeMotionPhase.RamReturn => step?.ReturnDurationSeconds ?? 2.2,
			PressBrakeMotionPhase.InterStepWait => step?.InterStepWaitSeconds ?? 1.1,
			PressBrakeMotionPhase.InterPartWait => part?.InterPartWaitSeconds ?? 4.5,
			_ => 1.0
		};

	private static void UpdateThermal(PressBrakeKinematicsState pressBrake, double dt, bool forming)
	{
		if (forming)
		{
			pressBrake.HydraulicOilTempC += VirtualPressBrakeKinematicsConfig.OilTempRisePerFormingSecond * dt;
		}
		else
		{
			pressBrake.HydraulicOilTempC -= VirtualPressBrakeKinematicsConfig.OilTempCoolPerIdleSecond * dt;
		}

		pressBrake.HydraulicOilTempC = Math.Clamp(
			pressBrake.HydraulicOilTempC,
			VirtualPressBrakeKinematicsConfig.BaseHydraulicOilTempC - 2.0,
			VirtualPressBrakeKinematicsConfig.BaseHydraulicOilTempC + 18.0);
	}

	private static void LoadProgram(PhysicalSimulationContext context, int seed, int programIndex)
	{
		PressBrakeKinematicsState pressBrake = context.PressBrake;
		pressBrake.ProgramIndex = programIndex;
		pressBrake.ActiveProgram = PressBrakeProgramCatalog.GetProgram(programIndex);
		pressBrake.PartIndex = 0;
		pressBrake.BendStepIndex = 0;
		pressBrake.ProgramId = pressBrake.ActiveProgram.ProgramId;
		LoadCurrentPart(pressBrake);
	}

	private static void LoadCurrentPart(PressBrakeKinematicsState pressBrake)
	{
		PressBrakePartDefinition? part = GetCurrentPart(pressBrake);
		pressBrake.PartId = part?.PartId ?? "—";
		if (part != null && part.BendSteps.Count > 0)
		{
			pressBrake.TargetBackgaugeMm = part.BendSteps[0].BackgaugePositionMm;
			pressBrake.TargetBendAngleDeg = part.BendSteps[0].TargetAngleDeg;
		}
	}

	private static PressBrakePartDefinition? GetCurrentPart(PressBrakeKinematicsState pressBrake) =>
		pressBrake.ActiveProgram != null && pressBrake.ActiveProgram.Parts.Count > 0
			? pressBrake.ActiveProgram.Parts[pressBrake.PartIndex % pressBrake.ActiveProgram.Parts.Count]
			: null;

	private static PressBrakeBendStepDefinition? GetCurrentStep(PressBrakeKinematicsState pressBrake, PressBrakePartDefinition? part) =>
		part != null && pressBrake.BendStepIndex < part.BendSteps.Count
			? part.BendSteps[pressBrake.BendStepIndex]
			: null;

	private static bool ShouldOperatorWait(PressBrakePartDefinition part, int seed, int producedParts) =>
		(seed + producedParts) % 7 == 0 && part.OperatorWaitChance > 0.1;

	private static void SetOpaqueTokens(PressBrakeKinematicsState pressBrake, PressBrakeMotionPhase phase)
	{
		int machineIndex = (int)phase % VirtualPressBrakeKinematicsConfig.MachineStateTokens.Length;
		int activityIndex = ((int)phase * 2 + pressBrake.BendStepIndex) % VirtualPressBrakeKinematicsConfig.ActivityStateTokens.Length;
		int toolIndex = (pressBrake.ProgramIndex + pressBrake.PartIndex) % VirtualPressBrakeKinematicsConfig.ToolStationTokens.Length;
		pressBrake.MachineStateToken = VirtualPressBrakeKinematicsConfig.MachineStateTokens[machineIndex];
		pressBrake.ActivityStateToken = VirtualPressBrakeKinematicsConfig.ActivityStateTokens[activityIndex];
		pressBrake.ToolStationToken = VirtualPressBrakeKinematicsConfig.ToolStationTokens[toolIndex];
	}

	private static void ApplySignals(PhysicalMachineRuntime runtime, PressBrakeKinematicsState pressBrake)
	{
		foreach (SignalRuntimeState signal in runtime.Signals)
		{
			switch (signal.SignalId)
			{
			case "Machine.MachineState":
				signal.CurrentStringValue = pressBrake.MachineStateToken;
				break;
			case "Machine.ProgramId":
				signal.CurrentStringValue = pressBrake.ProgramId;
				break;
			case "Machine.PartId":
				signal.CurrentStringValue = pressBrake.PartId;
				break;
			case "Machine.ActualCounter":
				signal.CurrentValue = pressBrake.ProducedParts;
				signal.TargetValue = pressBrake.ProducedParts;
				break;
			case "Machine.TargetCounter":
				signal.CurrentValue = pressBrake.TargetParts;
				signal.TargetValue = pressBrake.TargetParts;
				break;
			case "Machine.LastProductionChange":
				signal.CurrentDateTimeUtc = DateTime.UtcNow;
				break;
			case "Ram.Position":
				signal.CurrentValue = pressBrake.RamPositionMm;
				signal.TargetValue = pressBrake.RamPositionMm;
				break;
			case "Ram.Velocity":
				signal.CurrentValue = pressBrake.RamVelocityMmPerS;
				signal.TargetValue = pressBrake.RamVelocityMmPerS;
				break;
			case "Backgauge.Position":
				signal.CurrentValue = pressBrake.BackgaugePositionMm;
				signal.TargetValue = pressBrake.BackgaugePositionMm;
				break;
			case "Process.BendAngle":
				signal.CurrentValue = pressBrake.BendAngleDeg;
				signal.TargetValue = pressBrake.BendAngleDeg;
				break;
			case "Process.FormingForce":
				signal.CurrentValue = pressBrake.FormingForceKn;
				signal.TargetValue = pressBrake.FormingForceKn;
				break;
			case "Tool.StationState":
				signal.CurrentStringValue = pressBrake.ToolStationToken;
				break;
			case "Thermal.HydraulicOilTemp":
				signal.CurrentValue = pressBrake.HydraulicOilTempC;
				signal.TargetValue = pressBrake.HydraulicOilTempC;
				break;
			case "Cycle.ActivityState":
				signal.CurrentStringValue = pressBrake.ActivityStateToken;
				break;
			}
		}
	}

	private static void RecordGt(
		IPressBrakeGroundTruthRecorder? recorder,
		Guid machineId,
		PressBrakeKinematicsState pressBrake,
		string eventType,
		string source,
		int? bendStepIndex = null)
	{
		recorder?.Record(new PressBrakeGroundTruthEvent
		{
			TimestampUtc = DateTimeOffset.UtcNow,
			MachineId = machineId,
			EventType = eventType,
			ProgramReference = pressBrake.ProgramId,
			PartReference = pressBrake.PartId,
			BendStepReference = bendStepIndex ?? pressBrake.BendStepIndex,
			PhysicalPhase = pressBrake.MotionPhase.ToString(),
			Source = source
		});
	}

	private static void RecordGtOnce(
		IPressBrakeGroundTruthRecorder? recorder,
		Guid machineId,
		PressBrakeKinematicsState pressBrake,
		string eventType,
		string source,
		bool shouldRecord,
		int? bendStepIndex = null)
	{
		if (shouldRecord)
		{
			RecordGt(recorder, machineId, pressBrake, eventType, source, bendStepIndex);
		}
	}
}
