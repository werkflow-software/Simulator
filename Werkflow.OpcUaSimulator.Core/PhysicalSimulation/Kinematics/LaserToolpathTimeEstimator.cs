using System;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class LaserToolpathTimeEstimator
{
	public static double EstimatePlanPartSeconds(
		CuttingPlan plan,
		int layoutPartIndex,
		FixedProductionJobDefinition job,
		int seed)
	{
		if (plan.PartCount == 0)
		{
			return 0.0;
		}

		int index = layoutPartIndex % plan.PartCount;
		LaserToolpathPlan toolpath = CuttingPlanToolpathBuilder.BuildToolpath(
			plan.Parts[index],
			index,
			job,
			seed ^ (index * 1337));
		return EstimateToolpathSeconds(toolpath, 0, 0.0, null, null);
	}

	public static double EstimateRemainingPartSeconds(LaserKinematicsState kinematics, FixedProductionJobDefinition job, int seed)
	{
		if (kinematics.CurrentPlan == null || kinematics.CurrentPlan.Segments.Count == 0)
		{
			return 0.0;
		}

		return EstimateToolpathSeconds(
			kinematics.CurrentPlan,
			kinematics.SegmentIndex,
			kinematics.DistanceAlongSegmentMm,
			kinematics,
			job);
	}

	public static double EstimateRemainingJobSeconds(
		PhysicalSimulationContext context,
		FixedProductionJobDefinition job,
		int seed)
	{
		if (context.Job.TargetQuantity <= 0 || context.Kinematics.ActiveCuttingPlan == null)
		{
			return 0.0;
		}

		double remaining = EstimateRemainingPartSeconds(context.Kinematics, job, seed);
		int produced = context.Kinematics.PartIndex;
		int remainingParts = Math.Max(0, context.Job.TargetQuantity - produced);
		if (remainingParts <= 1)
		{
			return remaining;
		}

		CuttingPlan plan = context.Kinematics.ActiveCuttingPlan;
		for (int i = 1; i < remainingParts; i++)
		{
			int layoutIndex = (produced + i) % plan.PartCount;
			remaining += EstimatePlanPartSeconds(plan, layoutIndex, job, seed ^ ((produced + i) * 1337));
		}

		return remaining;
	}

	public static double EstimateFullJobSeconds(
		PhysicalSimulationContext context,
		FixedProductionJobDefinition job,
		int seed)
	{
		if (context.Job.TargetQuantity <= 0 || context.Kinematics.ActiveCuttingPlan == null)
		{
			return 0.0;
		}

		double total = 0.0;
		CuttingPlan plan = context.Kinematics.ActiveCuttingPlan;
		for (int i = 0; i < context.Job.TargetQuantity; i++)
		{
			int layoutIndex = i % plan.PartCount;
			total += EstimatePlanPartSeconds(plan, layoutIndex, job, seed ^ (i * 1337));
		}

		return total;
	}

	public static double EstimateSetupRemainingSeconds(PhysicalSimulationContext context)
	{
		if (!context.IsJobChangePauseActive)
		{
			return 0.0;
		}

		double remaining = (context.JobChangePauseUntil - context.SimulationTime).TotalSeconds;
		return Math.Max(0.0, remaining);
	}

	private static double EstimateToolpathSeconds(
		LaserToolpathPlan plan,
		int startSegmentIndex,
		double distanceAlongCurrentMm,
		LaserKinematicsState? kinematics,
		FixedProductionJobDefinition? job)
	{
		double total = 0.0;
		for (int i = startSegmentIndex; i < plan.Segments.Count; i++)
		{
			LaserToolpathSegment segment = plan.Segments[i];
			double segmentSeconds = EstimateSegmentSeconds(segment, kinematics, job, i == startSegmentIndex, distanceAlongCurrentMm);
			total += segmentSeconds;
		}

		return total;
	}

	private static double EstimateSegmentSeconds(
		LaserToolpathSegment segment,
		LaserKinematicsState? kinematics,
		FixedProductionJobDefinition? job,
		bool isCurrent,
		double distanceAlongCurrentMm)
	{
		switch (segment.Kind)
		{
		case LaserToolpathSegmentKind.Pierce:
			if (isCurrent && kinematics != null)
			{
				return Math.Max(0.0, segment.PierceDurationSeconds - kinematics.PierceElapsedSeconds);
			}

			return segment.PierceDurationSeconds;
		case LaserToolpathSegmentKind.RapidMove:
			double rapidDistance = SegmentLength(kinematics, segment, isCurrent);
			if (isCurrent && kinematics != null)
			{
				double dx = segment.TargetX - kinematics.X;
				double dy = segment.TargetY - kinematics.Y;
				rapidDistance = Math.Sqrt(dx * dx + dy * dy);
			}

			return rapidDistance / VirtualMachineKinematicsConfig.RapidSpeedMmPerS;
		case LaserToolpathSegmentKind.CutLine:
			double cutDistance = SegmentLength(kinematics, segment, isCurrent);
			if (isCurrent && kinematics != null)
			{
				double dx = segment.TargetX - kinematics.X;
				double dy = segment.TargetY - kinematics.Y;
				cutDistance = Math.Sqrt(dx * dx + dy * dy);
			}

			double speed = segment.CutSpeedMmPerS;
			if (job != null)
			{
				speed = Math.Max(3.0, speed);
			}

			return cutDistance / Math.Max(3.0, speed);
		}

		return 0.0;
	}

	private static double SegmentLength(LaserKinematicsState? kinematics, LaserToolpathSegment segment, bool isCurrent)
	{
		if (isCurrent && kinematics != null)
		{
			double dx = segment.TargetX - kinematics.SegmentStartX;
			double dy = segment.TargetY - kinematics.SegmentStartY;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		return 100.0;
	}
}
