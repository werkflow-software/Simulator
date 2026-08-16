using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class LaserKinematicsEngine
{
	private const double PositionTolerance = 0.5;

	public static bool ShouldEnable(Guid machineId) => VirtualLaserMachineRegistry.IsVirtualLaserMachine(machineId);

	public static void Initialize(PhysicalSimulationContext context, int seed, Guid machineId)
	{
		if (!ShouldEnable(machineId))
		{
			context.Kinematics.IsEnabled = false;
			return;
		}

		LaserKinematicsState kinematics = context.Kinematics;
		kinematics.IsEnabled = true;
		kinematics.X = VirtualMachineKinematicsConfig.HomeX;
		kinematics.Y = VirtualMachineKinematicsConfig.HomeY;
		kinematics.Z = VirtualMachineKinematicsConfig.ZService;
		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.MotionPhase = LaserMotionPhase.Idle;
		kinematics.PartIndex = 0;
		kinematics.SegmentIndex = 0;
		kinematics.PierceElapsedSeconds = 0.0;
		kinematics.PendingPartCompletions = 0;
		kinematics.NozzleChangeRequired = false;
		kinematics.NozzleChangeActive = false;
		kinematics.NozzleChangeElapsedSeconds = 0.0;
		kinematics.MovingToService = false;
		kinematics.Vz = 0.0;
		kinematics.PathSpeedMmPerS = 0.0;
		kinematics.LaserPowerKw = 0.0;
		kinematics.DistanceAlongSegmentMm = 0.0;
		kinematics.NextActionHint = "Bereit";
		kinematics.MinX = kinematics.X;
		kinematics.MaxX = kinematics.X;
		kinematics.MinY = kinematics.Y;
		kinematics.MaxY = kinematics.Y;
		LoadCuttingPlanForJob(context);
		LoadPartPlan(context, seed);
	}

	public static void OnProductionPaused(PhysicalSimulationContext context)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		LaserKinematicsState kinematics = context.Kinematics;
		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.Vz = 0.0;
		kinematics.CutFeedMmPerMin = 0.0;
		kinematics.PathSpeedMmPerS = 0.0;
		kinematics.NextActionHint = "Pause";
	}

	public static void OnProductionResumed(PhysicalSimulationContext context)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		context.Kinematics.NextActionHint = "Fortsetzen";
	}

	public static void StopAndResetProduction(PhysicalSimulationContext context, int seed)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		context.IsProductionPaused = false;
		context.ProductionRunStartedAtUtc = null;
		context.FrozenPartRemainingSeconds = 0.0;
		context.FrozenJobRemainingSeconds = 0.0;
		context.IsProductionMotionActive = false;
		context.Job.ProducedQuantity = 0;

		LaserKinematicsState kinematics = context.Kinematics;
		kinematics.PartIndex = 0;
		kinematics.SegmentIndex = 0;
		kinematics.PierceElapsedSeconds = 0.0;
		kinematics.PendingPartCompletions = 0;
		kinematics.DistanceAlongSegmentMm = 0.0;
		kinematics.MovingToService = false;
		kinematics.NozzleChangeActive = false;
		kinematics.NozzleChangeElapsedSeconds = 0.0;
		kinematics.NozzleChangeRequired = false;
		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.Vz = 0.0;
		kinematics.CutFeedMmPerMin = 0.0;
		kinematics.PathSpeedMmPerS = 0.0;
		kinematics.LaserPowerKw = 0.15;
		kinematics.MotionPhase = LaserMotionPhase.Idle;
		kinematics.NextActionHint = "Gestoppt";
		LoadCuttingPlanForJob(context);
		if (context.Kinematics.ActiveCuttingPlan != null)
		{
			CuttingPlanGeometry.ResetRuntimeStates(context.Kinematics.ActiveCuttingPlan);
		}

		LoadPartPlan(context, seed);
	}

	public static void AbortProductionForJobChange(PhysicalSimulationContext context, FixedProductionJobDefinition nextJob)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		context.IsProductionPaused = false;
		context.Kinematics.PendingPartCompletions = 0;
		context.IsProductionMotionActive = false;
		OnJobChangeBegin(context, nextJob);
	}

	public static void OnJobChangeBegin(PhysicalSimulationContext context, FixedProductionJobDefinition nextJob)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		context.Kinematics.DisplayCuttingPlan = context.Kinematics.ActiveCuttingPlan;
		context.Kinematics.MovingToService = true;
		context.Kinematics.MotionPhase = LaserMotionPhase.JobChange;
		context.Kinematics.NozzleChangeRequired = RequiresNozzleChange(context.Job.MaterialName, context.Job.MaterialThicknessMm, nextJob);
		context.Kinematics.NozzleChangeActive = false;
		context.Kinematics.NozzleChangeElapsedSeconds = 0.0;
		context.Kinematics.Vx = 0.0;
		context.Kinematics.Vy = 0.0;
	}

	public static void OnJobApplied(PhysicalSimulationContext context, int seed)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		LaserKinematicsState kinematics = context.Kinematics;
		kinematics.PartIndex = 0;
		kinematics.SegmentIndex = 0;
		kinematics.PierceElapsedSeconds = 0.0;
		kinematics.MovingToService = false;
		kinematics.NozzleChangeActive = false;
		kinematics.NozzleChangeElapsedSeconds = 0.0;
		LoadCuttingPlanForJob(context);
		LoadPartPlan(context, seed);
		kinematics.MotionPhase = LaserMotionPhase.RapidPositioning;
	}

	public static int ConsumePendingPartCompletions(PhysicalSimulationContext context)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return 0;
		}

		int pending = context.Kinematics.PendingPartCompletions;
		context.Kinematics.PendingPartCompletions = 0;
		return pending;
	}

	public static double GetMotionDemand(LaserMotionPhase phase)
	{
		if (1 == 0)
		{
		}
		double result = phase switch
		{
			LaserMotionPhase.Idle => 0.15,
			LaserMotionPhase.Setup => 0.25,
			LaserMotionPhase.JobChange => 0.22,
			LaserMotionPhase.NozzleChange => 0.2,
			LaserMotionPhase.RapidPositioning => 0.42,
			LaserMotionPhase.Repositioning => 0.4,
			LaserMotionPhase.Piercing => 0.78,
			LaserMotionPhase.Cutting => 0.88,
			LaserMotionPhase.Recovery => 0.3,
			_ => 0.3
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static ProcessPhase MapToProcessPhase(LaserMotionPhase phase)
	{
		if (1 == 0)
		{
		}
		ProcessPhase result = phase switch
		{
			LaserMotionPhase.Idle => ProcessPhase.Idle,
			LaserMotionPhase.Setup => ProcessPhase.Setup,
			LaserMotionPhase.JobChange => ProcessPhase.Setup,
			LaserMotionPhase.NozzleChange => ProcessPhase.Setup,
			LaserMotionPhase.RapidPositioning => ProcessPhase.RampUp,
			LaserMotionPhase.Repositioning => ProcessPhase.RampUp,
			LaserMotionPhase.Piercing => ProcessPhase.RampUp,
			LaserMotionPhase.Cutting => ProcessPhase.Processing,
			LaserMotionPhase.Recovery => ProcessPhase.RampDown,
			_ => ProcessPhase.Idle
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static void Tick(
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime,
		PhysicalSimulationContext context,
		TimeSpan deltaTime,
		int seed)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

		double dt = deltaTime.TotalSeconds;
		if (dt <= 0.0)
		{
			return;
		}

		LaserKinematicsState kinematics = context.Kinematics;
		if (context.IsProductionPaused)
		{
			TickPausedHold(kinematics);
			context.CurrentPhase = MapToProcessPhase(kinematics.MotionPhase);
			kinematics.TrackPosition();
			ApplySignals(runtime, kinematics);
			ApplyFrictionTarget(runtime, kinematics);
			return;
		}

		if (!context.IsProductionMotionActive && !context.IsJobChangePauseActive && !kinematics.MovingToService)
		{
			TickIdleHold(kinematics);
			context.CurrentPhase = MapToProcessPhase(kinematics.MotionPhase);
			kinematics.TrackPosition();
			ApplySignals(runtime, kinematics);
			ApplyFrictionTarget(runtime, kinematics);
			return;
		}

		if (context.IsJobChangePauseActive || kinematics.MovingToService)
		{
			TickJobChange(context, kinematics, dt);
		}
		else if (kinematics.PartIndex >= context.Job.TargetQuantity && context.Job.TargetQuantity > 0)
		{
			TickIdleHold(kinematics);
			kinematics.MotionPhase = LaserMotionPhase.Idle;
		}
		else
		{
			TickProduction(context, kinematics, seed, dt);
		}

		context.CurrentPhase = MapToProcessPhase(kinematics.MotionPhase);
		kinematics.TrackPosition();
		ApplySignals(runtime, kinematics);
		ApplyFrictionTarget(runtime, kinematics);
	}

	private static void TickJobChange(PhysicalSimulationContext context, LaserKinematicsState kinematics, double dt)
	{
		if (kinematics.MovingToService)
		{
			kinematics.NextActionHint = "Fahrt zur Serviceposition";
			MoveTowardPointWithZ(
				kinematics,
				VirtualMachineKinematicsConfig.NozzleServiceX,
				VirtualMachineKinematicsConfig.NozzleServiceY,
				VirtualMachineKinematicsConfig.ZService,
				VirtualMachineKinematicsConfig.RapidSpeedMmPerS,
				dt);
			kinematics.MotionPhase = LaserMotionPhase.JobChange;
			if (AtPoint(kinematics, VirtualMachineKinematicsConfig.NozzleServiceX, VirtualMachineKinematicsConfig.NozzleServiceY))
			{
				kinematics.MovingToService = false;
				if (kinematics.NozzleChangeRequired)
				{
					kinematics.NozzleChangeActive = true;
					kinematics.NozzleChangeElapsedSeconds = 0.0;
					kinematics.MotionPhase = LaserMotionPhase.NozzleChange;
				}
				else
				{
					kinematics.MotionPhase = LaserMotionPhase.Setup;
				}
			}

			return;
		}

		if (kinematics.NozzleChangeActive)
		{
			kinematics.NozzleChangeElapsedSeconds += dt;
			kinematics.Vx = 0.0;
			kinematics.Vy = 0.0;
			kinematics.Vz = 0.0;
			kinematics.NextActionHint = "Düsenwechsel";
			kinematics.MotionPhase = LaserMotionPhase.NozzleChange;
			MoveZ(kinematics, VirtualMachineKinematicsConfig.ZService, dt);
			if (kinematics.NozzleChangeElapsedSeconds >= VirtualMachineKinematicsConfig.NozzleChangeDurationSeconds)
			{
				kinematics.NozzleChangeActive = false;
				kinematics.MotionPhase = LaserMotionPhase.Setup;
			}

			return;
		}

		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.Vz = 0.0;
		kinematics.NextActionHint = "Einrichten / Jobwechsel";
		kinematics.MotionPhase = LaserMotionPhase.Setup;
	}

	private static double GetEffectiveCutSpeed(LaserKinematicsState kinematics, LaserToolpathSegment segment)
	{
		double speed = segment.CutSpeedMmPerS;
		if (segment.IsCornerEntry)
		{
			double cornerRamp = Math.Clamp(kinematics.DistanceAlongSegmentMm / 45.0, 0.0, 1.0);
			speed *= 0.45 + cornerRamp * 0.55;
		}

		double segDx = segment.TargetX - kinematics.SegmentStartX;
		double segDy = segment.TargetY - kinematics.SegmentStartY;
		double segLen = Math.Sqrt(segDx * segDx + segDy * segDy);
		if (segLen < 120.0)
		{
			speed *= 0.72 + segLen / 120.0 * 0.28;
		}

		return Math.Max(3.0, speed);
	}

	private static bool AtPoint(LaserKinematicsState kinematics, double targetX, double targetY)
	{
		double dx = targetX - kinematics.X;
		double dy = targetY - kinematics.Y;
		return Math.Sqrt(dx * dx + dy * dy) <= PositionTolerance;
	}

	private static void MoveTowardPointWithZ(
		LaserKinematicsState kinematics,
		double targetX,
		double targetY,
		double targetZ,
		double maxSpeed,
		double dt)
	{
		MoveTowardPoint(kinematics, targetX, targetY, maxSpeed, dt);
		MoveZ(kinematics, targetZ, dt);
	}

	private static void MoveZ(LaserKinematicsState kinematics, double targetZ, double dt)
	{
		double delta = targetZ - kinematics.Z;
		if (Math.Abs(delta) <= 0.02)
		{
			kinematics.Z = targetZ;
			kinematics.Vz = 0.0;
			return;
		}

		double maxVz = 35.0;
		double desiredVz = Math.Sign(delta) * Math.Min(maxVz, Math.Abs(delta) / dt);
		kinematics.Vz = ApproachScalar(kinematics.Vz, desiredVz, 120.0, dt);
		double step = kinematics.Vz * dt;
		if (Math.Abs(step) >= Math.Abs(delta))
		{
			kinematics.Z = targetZ;
			kinematics.Vz = 0.0;
		}
		else
		{
			kinematics.Z += step;
		}
	}

	private static void TickProduction(PhysicalSimulationContext context, LaserKinematicsState kinematics, int seed, double dt)
	{
		if (kinematics.CurrentPlan == null || kinematics.CurrentPlan.Segments.Count == 0)
		{
			LoadPartPlan(context, seed);
		}

		if (kinematics.CurrentPlan == null)
		{
			TickIdleHold(kinematics);
			return;
		}

		if (kinematics.SegmentIndex >= kinematics.CurrentPlan.Segments.Count)
		{
			CompletePart(context, kinematics, seed);
			return;
		}

		LaserToolpathSegment segment = kinematics.CurrentPlan.Segments[kinematics.SegmentIndex];
		if (kinematics.DistanceAlongSegmentMm <= 0.0)
		{
			kinematics.SegmentStartX = kinematics.X;
			kinematics.SegmentStartY = kinematics.Y;
			UpdateContourTracking(kinematics, segment, starting: true);
		}

		switch (segment.Kind)
		{
		case LaserToolpathSegmentKind.RapidMove:
			kinematics.MotionPhase = LaserMotionPhase.RapidPositioning;
			kinematics.NextActionHint = "Rapid zur Startposition";
			MoveTowardPointWithZ(
				kinematics,
				segment.TargetX,
				segment.TargetY,
				VirtualMachineKinematicsConfig.ZRapid,
				VirtualMachineKinematicsConfig.RapidSpeedMmPerS,
				dt);
			if (AtPoint(kinematics, segment.TargetX, segment.TargetY))
			{
				AdvanceSegment(kinematics);
			}
			break;
		case LaserToolpathSegmentKind.Pierce:
			kinematics.MotionPhase = LaserMotionPhase.Piercing;
			kinematics.NextActionHint = "Einstechen";
			kinematics.Vx = 0.0;
			kinematics.Vy = 0.0;
			MoveZ(kinematics, GetPierceZ(context.Job.MaterialThicknessMm), dt);
			kinematics.PierceElapsedSeconds += dt;
			if (kinematics.PierceElapsedSeconds >= segment.PierceDurationSeconds)
			{
				kinematics.PierceElapsedSeconds = 0.0;
				AdvanceSegment(kinematics);
			}
			break;
		case LaserToolpathSegmentKind.CutLine:
			kinematics.MotionPhase = LaserMotionPhase.Cutting;
			kinematics.NextActionHint = $"Kontur / Teil {kinematics.PartIndex + 1}";
			MoveZ(kinematics, GetCutZ(context.Job.MaterialThicknessMm), dt);
			double cutSpeed = GetEffectiveCutSpeed(kinematics, segment);
			kinematics.CutFeedMmPerMin = cutSpeed * 60.0;
			if (MoveTowardPoint(kinematics, segment.TargetX, segment.TargetY, cutSpeed, dt))
			{
				AdvanceSegment(kinematics);
			}
			break;
		}
	}

	private static void AdvanceSegment(LaserKinematicsState kinematics)
	{
		if (kinematics.CurrentPlan != null && kinematics.SegmentIndex < kinematics.CurrentPlan.Segments.Count)
		{
			LaserToolpathSegment completed = kinematics.CurrentPlan.Segments[kinematics.SegmentIndex];
			UpdateContourTracking(kinematics, completed, starting: false);
		}

		kinematics.SegmentIndex++;
		kinematics.DistanceAlongSegmentMm = 0.0;
	}

	private static void UpdateContourTracking(LaserKinematicsState kinematics, LaserToolpathSegment segment, bool starting)
	{
		if (kinematics.ActiveCuttingPlan == null)
		{
			return;
		}

		kinematics.SheetPartIndex = segment.SheetPartIndex;
		kinematics.CurrentContourIndex = segment.ContourIndex;
		if (segment.SheetPartIndex < 0 || segment.SheetPartIndex >= kinematics.ActiveCuttingPlan.Parts.Count)
		{
			return;
		}

		CuttingPlanPart part = kinematics.ActiveCuttingPlan.Parts[segment.SheetPartIndex];
		if (starting && segment.Kind == LaserToolpathSegmentKind.Pierce)
		{
			part.State = CuttingPartState.InProgress;
			foreach (CuttingPlanContour contour in part.Contours)
			{
				if (contour.ContourIndex == segment.ContourIndex)
				{
					contour.State = CuttingContourState.Active;
				}
			}
		}

		if (!starting && segment.Kind == LaserToolpathSegmentKind.CutLine)
		{
			int nextIndex = kinematics.SegmentIndex + 1;
			bool contourComplete = kinematics.CurrentPlan == null
				|| nextIndex >= kinematics.CurrentPlan.Segments.Count
				|| kinematics.CurrentPlan.Segments[nextIndex].ContourIndex != segment.ContourIndex
				|| kinematics.CurrentPlan.Segments[nextIndex].SheetPartIndex != segment.SheetPartIndex
				|| kinematics.CurrentPlan.Segments[nextIndex].Kind != LaserToolpathSegmentKind.CutLine;

			if (contourComplete)
			{
				foreach (CuttingPlanContour contour in part.Contours)
				{
					if (contour.ContourIndex == segment.ContourIndex)
					{
						contour.State = CuttingContourState.Completed;
					}
				}
			}
		}
	}

	private static void CompletePart(PhysicalSimulationContext context, LaserKinematicsState kinematics, int seed)
	{
		if (kinematics.ActiveCuttingPlan != null)
		{
			int layoutIndex = kinematics.SheetPartIndex;
			if (layoutIndex >= 0 && layoutIndex < kinematics.ActiveCuttingPlan.Parts.Count)
			{
				kinematics.ActiveCuttingPlan.Parts[layoutIndex].State = CuttingPartState.Completed;
				foreach (CuttingPlanContour contour in kinematics.ActiveCuttingPlan.Parts[layoutIndex].Contours)
				{
					contour.State = CuttingContourState.Completed;
				}
			}
		}

		kinematics.PendingPartCompletions++;
		kinematics.PartIndex++;
		kinematics.SegmentIndex = 0;
		kinematics.PierceElapsedSeconds = 0.0;
		kinematics.DistanceAlongSegmentMm = 0.0;
		if (kinematics.PartIndex >= context.Job.TargetQuantity)
		{
			kinematics.MotionPhase = LaserMotionPhase.Idle;
			kinematics.Vx = 0.0;
			kinematics.Vy = 0.0;
			kinematics.CurrentPlan = null;
			return;
		}

		LoadPartPlan(context, seed);
		kinematics.MotionPhase = LaserMotionPhase.Repositioning;
	}

	private static void TickPausedHold(LaserKinematicsState kinematics)
	{
		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.Vz = 0.0;
		kinematics.CutFeedMmPerMin = 0.0;
		kinematics.PathSpeedMmPerS = 0.0;
		kinematics.LaserPowerKw = 0.15;
		kinematics.NextActionHint = "Pause";
	}

	private static void TickIdleHold(LaserKinematicsState kinematics)
	{
		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.Vz = 0.0;
		kinematics.CutFeedMmPerMin = 0.0;
		kinematics.PathSpeedMmPerS = 0.0;
		kinematics.LaserPowerKw = 0.15;
		kinematics.NextActionHint = "Leerlauf";
	}

	private static void LoadCuttingPlanForJob(PhysicalSimulationContext context)
	{
		FixedProductionJobDefinition definition = new FixedProductionJobDefinition
		{
			CatalogIndex = context.Job.CatalogIndex,
			JobName = context.Job.JobName,
			PartName = context.Job.PartName,
			TargetQuantity = context.Job.TargetQuantity,
			MaterialName = context.Job.MaterialName,
			MaterialThicknessMm = context.Job.MaterialThicknessMm,
			RecipeName = context.Job.RecipeName,
			ProgramName = context.Job.ProgramName
		};
		CuttingPlan plan = CuttingPlanCatalog.GetForJob(definition);
		CuttingPlanGeometry.ResetRuntimeStates(plan);
		context.Kinematics.ActiveCuttingPlan = plan;
		context.Kinematics.DisplayCuttingPlan = plan;
	}

	private static void LoadPartPlan(PhysicalSimulationContext context, int seed)
	{
		FixedProductionJobDefinition definition = new FixedProductionJobDefinition
		{
			CatalogIndex = context.Job.CatalogIndex,
			JobName = context.Job.JobName,
			PartName = context.Job.PartName,
			TargetQuantity = context.Job.TargetQuantity,
			MaterialName = context.Job.MaterialName,
			MaterialThicknessMm = context.Job.MaterialThicknessMm,
			RecipeName = context.Job.RecipeName,
			ProgramName = context.Job.ProgramName
		};

		if (context.Kinematics.ActiveCuttingPlan == null)
		{
			LoadCuttingPlanForJob(context);
		}

		CuttingPlan plan = context.Kinematics.ActiveCuttingPlan!;
		int layoutIndex = plan.PartCount == 0 ? 0 : context.Kinematics.PartIndex % plan.PartCount;
		if (layoutIndex == 0 && context.Kinematics.PartIndex > 0)
		{
			CuttingPlanGeometry.ResetRuntimeStates(plan);
		}

		PrepareSheetVisualState(plan, layoutIndex, context.Kinematics.PartIndex);
		CuttingPlanPart sheetPart = plan.Parts[layoutIndex];
		int partSeed = seed ^ (context.Kinematics.PartIndex * 1337);
		context.Kinematics.SheetPartIndex = layoutIndex;
		context.Kinematics.CurrentContourIndex = 0;
		context.Kinematics.CurrentPlan = CuttingPlanToolpathBuilder.BuildToolpath(sheetPart, layoutIndex, definition, partSeed);
		context.Kinematics.SegmentIndex = 0;
		context.Kinematics.PierceElapsedSeconds = 0.0;
		context.Kinematics.DistanceAlongSegmentMm = 0.0;
	}

	private static void PrepareSheetVisualState(CuttingPlan plan, int layoutIndex, int productionPartIndex)
	{
		for (int i = 0; i < layoutIndex; i++)
		{
			plan.Parts[i].State = CuttingPartState.Completed;
		}

		for (int i = layoutIndex + 1; i < plan.Parts.Count; i++)
		{
			plan.Parts[i].State = CuttingPartState.NotStarted;
			foreach (CuttingPlanContour contour in plan.Parts[i].Contours)
			{
				contour.State = CuttingContourState.Unprocessed;
			}
		}

		CuttingPlanPart current = plan.Parts[layoutIndex];
		current.State = CuttingPartState.NotStarted;
		foreach (CuttingPlanContour contour in current.Contours)
		{
			contour.State = CuttingContourState.Unprocessed;
		}
	}

	private static bool MoveTowardPoint(LaserKinematicsState kinematics, double targetX, double targetY, double maxSpeed, double dt)
	{
		double dx = targetX - kinematics.X;
		double dy = targetY - kinematics.Y;
		double distance = Math.Sqrt(dx * dx + dy * dy);
		if (distance <= PositionTolerance)
		{
			kinematics.X = targetX;
			kinematics.Y = targetY;
			kinematics.Vx = 0.0;
			kinematics.Vy = 0.0;
			return true;
		}

		double desiredSpeed = Math.Min(maxSpeed, distance / dt);
		double currentSpeed = Math.Sqrt(kinematics.Vx * kinematics.Vx + kinematics.Vy * kinematics.Vy);
		double newSpeed = ApproachScalar(currentSpeed, desiredSpeed, VirtualMachineKinematicsConfig.MaxAccelMmPerS2, dt);
		double invDist = 1.0 / distance;
		kinematics.Vx = dx * invDist * newSpeed;
		kinematics.Vy = dy * invDist * newSpeed;
		double step = newSpeed * dt;
		if (step >= distance)
		{
			kinematics.X = targetX;
			kinematics.Y = targetY;
			kinematics.Vx = 0.0;
			kinematics.Vy = 0.0;
			return true;
		}

		kinematics.X += kinematics.Vx * dt;
		kinematics.Y += kinematics.Vy * dt;
		kinematics.DistanceAlongSegmentMm += newSpeed * dt;
		kinematics.PathSpeedMmPerS = newSpeed;
		return false;
	}

	private static double GetPierceZ(double thicknessMm) => GetCutZ(thicknessMm) - 0.6;

	private static double ApproachScalar(double current, double target, double maxAccel, double dt)
	{
		double delta = target - current;
		double maxDelta = maxAccel * dt;
		if (Math.Abs(delta) <= maxDelta)
		{
			return target;
		}

		return current + Math.Sign(delta) * maxDelta;
	}

	private static double GetCutZ(double thicknessMm) => VirtualMachineKinematicsConfig.ZCutBase + thicknessMm * 0.12;

	private static bool RequiresNozzleChange(string currentMaterial, double currentThickness, FixedProductionJobDefinition nextJob)
	{
		return !string.Equals(currentMaterial, nextJob.MaterialName, StringComparison.OrdinalIgnoreCase)
			|| Math.Abs(currentThickness - nextJob.MaterialThicknessMm) >= 1.5
			|| nextJob.CatalogIndex % 3 == 0;
	}

	private static void ApplySignals(PhysicalMachineRuntime runtime, LaserKinematicsState kinematics)
	{
		double pathSpeed = Math.Sqrt(kinematics.Vx * kinematics.Vx + kinematics.Vy * kinematics.Vy);
		kinematics.PathSpeedMmPerS = pathSpeed;
		bool motionActive = pathSpeed > 0.5
			|| Math.Abs(kinematics.Vz) > 0.2
			|| kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting;
		bool cutting = kinematics.MotionPhase == LaserMotionPhase.Cutting;
		bool positioning = kinematics.MotionPhase is LaserMotionPhase.RapidPositioning
			or LaserMotionPhase.Repositioning or LaserMotionPhase.JobChange;
		bool laserActive = kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting;
		double feed = cutting ? kinematics.CutFeedMmPerMin : 0.0;
		double focus = kinematics.Z - VirtualMachineKinematicsConfig.ZCutBase;
		double basePower = 2.5 + kinematics.CutFeedMmPerMin / 600.0;
		kinematics.LaserPowerKw = laserActive
			? (kinematics.MotionPhase == LaserMotionPhase.Piercing ? basePower * 1.15 : basePower * 0.92)
			: (positioning ? 0.8 : 0.15);

		foreach (SignalRuntimeState signal in runtime.Signals)
		{
			switch (signal.SignalId)
			{
			case "Axis01.Position":
				signal.CurrentValue = kinematics.X;
				signal.TargetValue = kinematics.X;
				break;
			case "Axis02.Position":
				signal.CurrentValue = kinematics.Y;
				signal.TargetValue = kinematics.Y;
				break;
			case "Axis03.Position":
				signal.CurrentValue = kinematics.Z;
				signal.TargetValue = kinematics.Z;
				break;
			case "Axis01.TargetPosition":
				signal.CurrentValue = kinematics.X;
				signal.TargetValue = kinematics.X;
				break;
			case "Axis02.TargetPosition":
				signal.CurrentValue = kinematics.Y;
				signal.TargetValue = kinematics.Y;
				break;
			case "Axis03.TargetPosition":
				signal.CurrentValue = kinematics.Z;
				signal.TargetValue = kinematics.Z;
				break;
			case "Axis01.Speed":
			case "Axis01.TargetSpeed":
				signal.CurrentValue = Math.Abs(kinematics.Vx);
				signal.TargetValue = Math.Abs(kinematics.Vx);
				break;
			case "Axis02.Speed":
			case "Axis02.TargetSpeed":
				signal.CurrentValue = Math.Abs(kinematics.Vy);
				signal.TargetValue = Math.Abs(kinematics.Vy);
				break;
			case "Axis03.Speed":
			case "Axis03.TargetSpeed":
				signal.CurrentValue = Math.Abs(kinematics.Vz);
				signal.TargetValue = Math.Abs(kinematics.Vz);
				break;
			case "Axis01.MotionActive":
			case "Axis02.MotionActive":
			case "Axis03.MotionActive":
				signal.CurrentValue = motionActive ? 1.0 : 0.0;
				signal.TargetValue = signal.CurrentValue;
				break;
			case "Process.FeedRate":
				signal.CurrentValue = feed;
				signal.TargetValue = feed;
				break;
			case "Process.CuttingSpeed":
				signal.CurrentValue = feed;
				signal.TargetValue = feed;
				break;
			case "Process.FocusPosition":
				signal.CurrentValue = focus;
				signal.TargetValue = focus;
				break;
			case "Process.PierceTime":
				if (kinematics.MotionPhase == LaserMotionPhase.Piercing)
				{
					signal.CurrentValue = Math.Max(signal.CurrentValue, kinematics.PierceElapsedSeconds);
				}
				break;
			case "Process.LaserPowerActual":
			case "Process.LaserPowerSetpoint":
				signal.CurrentValue = kinematics.LaserPowerKw;
				signal.TargetValue = kinematics.LaserPowerKw;
				break;
			case "Process.PowerDemand":
				signal.CurrentValue = kinematics.LaserPowerKw + pathSpeed * 0.004 + (motionActive ? 1.2 : 0.4);
				signal.TargetValue = signal.CurrentValue;
				break;
			}

			signal.LastUpdatedAt = DateTimeOffset.UtcNow;
		}
	}

	private static void ApplyFrictionTarget(PhysicalMachineRuntime runtime, LaserKinematicsState kinematics)
	{
		double speed = kinematics.PathSpeedMmPerS;
		double ratio = Math.Clamp(speed / VirtualMachineKinematicsConfig.RapidSpeedMmPerS, 0.0, 1.0);
		double friction = 0.12 + ratio * 0.45;
		if (kinematics.MotionPhase == LaserMotionPhase.Cutting)
		{
			friction += 0.08;
		}

		foreach (HiddenProcessRuntimeState state in runtime.HiddenProcessStates)
		{
			if (state.StateId.Equals("Friction", StringComparison.OrdinalIgnoreCase))
			{
				state.TargetValue = Math.Clamp(friction, 0.1, 0.6);
			}
		}
	}
}
