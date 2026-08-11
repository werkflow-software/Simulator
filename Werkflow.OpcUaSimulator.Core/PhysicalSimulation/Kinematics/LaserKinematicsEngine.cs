using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class LaserKinematicsEngine
{
	private const double PositionTolerance = 0.5;

	public static bool ShouldEnable(Guid machineId) => machineId == VirtualMachineContract.MachineId;

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
		kinematics.MinX = kinematics.X;
		kinematics.MaxX = kinematics.X;
		kinematics.MinY = kinematics.Y;
		kinematics.MaxY = kinematics.Y;
		LoadPartPlan(context, seed);
	}

	public static void OnJobChangeBegin(PhysicalSimulationContext context, FixedProductionJobDefinition nextJob)
	{
		if (!context.Kinematics.IsEnabled)
		{
			return;
		}

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
			bool arrived = MoveTowardPoint(
				kinematics,
				VirtualMachineKinematicsConfig.NozzleServiceX,
				VirtualMachineKinematicsConfig.NozzleServiceY,
				VirtualMachineKinematicsConfig.RapidSpeedMmPerS,
				dt);
			kinematics.Z = ApproachZ(kinematics.Z, VirtualMachineKinematicsConfig.ZService, dt);
			kinematics.MotionPhase = LaserMotionPhase.JobChange;
			if (arrived)
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
			kinematics.MotionPhase = LaserMotionPhase.NozzleChange;
			if (kinematics.NozzleChangeElapsedSeconds >= VirtualMachineKinematicsConfig.NozzleChangeDurationSeconds)
			{
				kinematics.NozzleChangeActive = false;
				kinematics.MotionPhase = LaserMotionPhase.Setup;
			}

			return;
		}

		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.MotionPhase = LaserMotionPhase.Setup;
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
		switch (segment.Kind)
		{
		case LaserToolpathSegmentKind.RapidMove:
			kinematics.MotionPhase = LaserMotionPhase.RapidPositioning;
			kinematics.Z = ApproachZ(kinematics.Z, VirtualMachineKinematicsConfig.ZRapid, dt);
			if (MoveTowardPoint(kinematics, segment.TargetX, segment.TargetY, VirtualMachineKinematicsConfig.RapidSpeedMmPerS, dt))
			{
				kinematics.SegmentIndex++;
			}
			break;
		case LaserToolpathSegmentKind.Pierce:
			kinematics.MotionPhase = LaserMotionPhase.Piercing;
			kinematics.Vx = 0.0;
			kinematics.Vy = 0.0;
			kinematics.Z = ApproachZ(kinematics.Z, GetCutZ(context.Job.MaterialThicknessMm) - 0.8, dt);
			kinematics.PierceElapsedSeconds += dt;
			if (kinematics.PierceElapsedSeconds >= segment.PierceDurationSeconds)
			{
				kinematics.PierceElapsedSeconds = 0.0;
				kinematics.SegmentIndex++;
			}
			break;
		case LaserToolpathSegmentKind.CutLine:
			kinematics.MotionPhase = LaserMotionPhase.Cutting;
			kinematics.Z = ApproachZ(kinematics.Z, GetCutZ(context.Job.MaterialThicknessMm), dt);
			double cutSpeed = segment.CutSpeedMmPerS;
			kinematics.CutFeedMmPerMin = cutSpeed * 60.0;
			if (MoveTowardPoint(kinematics, segment.TargetX, segment.TargetY, cutSpeed, dt))
			{
				kinematics.SegmentIndex++;
			}
			break;
		}
	}

	private static void CompletePart(PhysicalSimulationContext context, LaserKinematicsState kinematics, int seed)
	{
		kinematics.PendingPartCompletions++;
		kinematics.PartIndex++;
		kinematics.SegmentIndex = 0;
		kinematics.PierceElapsedSeconds = 0.0;
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

	private static void TickIdleHold(LaserKinematicsState kinematics)
	{
		kinematics.Vx = 0.0;
		kinematics.Vy = 0.0;
		kinematics.CutFeedMmPerMin = 0.0;
	}

	private static void LoadPartPlan(PhysicalSimulationContext context, int seed)
	{
		FixedProductionJobDefinition definition = new FixedProductionJobDefinition
		{
			CatalogIndex = context.Job.JobIndex - 1,
			JobName = context.Job.JobName,
			PartName = context.Job.PartName,
			TargetQuantity = context.Job.TargetQuantity,
			MaterialName = context.Job.MaterialName,
			MaterialThicknessMm = context.Job.MaterialThicknessMm,
			RecipeName = context.Job.RecipeName,
			ProgramName = context.Job.ProgramName
		};
		int partSeed = seed ^ (context.Kinematics.PartIndex * 1337);
		context.Kinematics.CurrentPlan = LaserToolpathGenerator.CreatePartPlan(definition, partSeed, context.Kinematics.PartIndex);
		context.Kinematics.SegmentIndex = 0;
		context.Kinematics.PierceElapsedSeconds = 0.0;
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
		return false;
	}

	private static double ApproachZ(double current, double target, double dt)
	{
		return current + (target - current) * Math.Min(1.0, dt / 0.35);
	}

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
		double speed = Math.Sqrt(kinematics.Vx * kinematics.Vx + kinematics.Vy * kinematics.Vy);
		bool motionActive = speed > 0.5
			|| kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting;
		double feed = kinematics.MotionPhase == LaserMotionPhase.Cutting ? kinematics.CutFeedMmPerMin : 0.0;
		double focus = kinematics.Z - VirtualMachineKinematicsConfig.ZCutBase;

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
				signal.CurrentValue = 0.0;
				signal.TargetValue = 0.0;
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
			}

			signal.LastUpdatedAt = DateTimeOffset.UtcNow;
		}
	}

	private static void ApplyFrictionTarget(PhysicalMachineRuntime runtime, LaserKinematicsState kinematics)
	{
		double speed = Math.Sqrt(kinematics.Vx * kinematics.Vx + kinematics.Vy * kinematics.Vy);
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
