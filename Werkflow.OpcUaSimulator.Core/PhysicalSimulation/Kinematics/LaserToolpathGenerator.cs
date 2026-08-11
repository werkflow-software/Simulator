using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class LaserToolpathGenerator
{
	public static LaserToolpathPlan CreatePartPlan(FixedProductionJobDefinition job, int seed, int partIndex)
	{
		Random random = new Random(seed ^ (partIndex * 4177) ^ job.CatalogIndex);
		double offsetX = VirtualMachineKinematicsConfig.SafeXMin + 100.0 + (partIndex * 127.0 % 2200.0);
		double offsetY = VirtualMachineKinematicsConfig.SafeYMin + 90.0 + (partIndex * 97.0 % 1100.0);
		offsetX = Clamp(offsetX, VirtualMachineKinematicsConfig.SafeXMin, VirtualMachineKinematicsConfig.SafeXMax - 350.0);
		offsetY = Clamp(offsetY, VirtualMachineKinematicsConfig.SafeYMin, VirtualMachineKinematicsConfig.SafeYMax - 250.0);

		double width = 160.0 + job.MaterialThicknessMm * 22.0 + random.Next(50, 180);
		double height = 110.0 + job.MaterialThicknessMm * 16.0 + random.Next(40, 140);
		width = Math.Min(width, VirtualMachineKinematicsConfig.SafeXMax - offsetX);
		height = Math.Min(height, VirtualMachineKinematicsConfig.SafeYMax - offsetY);

		double cutSpeed = Math.Max(4.0, 18.0 * job.FeedRateFactor);
		double pierceSeconds = 1.2 + job.MaterialThicknessMm * 0.4 + random.NextDouble() * 1.0;
		List<LaserToolpathSegment> segments = new List<LaserToolpathSegment>
		{
			new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.RapidMove, TargetX = offsetX, TargetY = offsetY },
			new LaserToolpathSegment
			{
				Kind = LaserToolpathSegmentKind.Pierce,
				TargetX = offsetX,
				TargetY = offsetY,
				PierceDurationSeconds = pierceSeconds
			},
			new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX + width, TargetY = offsetY, CutSpeedMmPerS = cutSpeed },
			new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX + width, TargetY = offsetY + height, CutSpeedMmPerS = cutSpeed * 0.82, IsCornerEntry = true },
			new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX, TargetY = offsetY + height, CutSpeedMmPerS = cutSpeed * 0.9, IsCornerEntry = true },
			new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX, TargetY = offsetY, CutSpeedMmPerS = cutSpeed, IsCornerEntry = true }
		};

		if (width > 220 && height > 160)
		{
			double diagX = offsetX + width * 0.55;
			double diagY = offsetY + height * 0.45;
			segments.Insert(3, new LaserToolpathSegment
			{
				Kind = LaserToolpathSegmentKind.CutLine,
				TargetX = diagX,
				TargetY = diagY,
				CutSpeedMmPerS = cutSpeed * 0.78
			});
		}

		return new LaserToolpathPlan
		{
			Segments = segments,
			RequiresNozzleChange = RequiresNozzleChange(job)
		};
	}

	public static LaserToolpathPlan CreatePlan(FixedProductionJobDefinition job, int seed)
	{
		Random random = new Random(seed ^ (job.CatalogIndex * 7919) ^ job.JobName.GetHashCode());
		int contourCount = 2 + (job.CatalogIndex % 4);
		List<LaserToolpathSegment> segments = new List<LaserToolpathSegment>();
		double baseX = VirtualMachineKinematicsConfig.SafeXMin + 120.0 + (job.CatalogIndex * 37.0 % 400.0);
		double baseY = VirtualMachineKinematicsConfig.SafeYMin + 80.0 + (job.CatalogIndex * 53.0 % 300.0);

		for (int c = 0; c < contourCount; c++)
		{
			double offsetX = baseX + c * (180.0 + random.Next(40, 160));
			double offsetY = baseY + c * (140.0 + random.Next(30, 120));
			offsetX = Clamp(offsetX, VirtualMachineKinematicsConfig.SafeXMin, VirtualMachineKinematicsConfig.SafeXMax - 400.0);
			offsetY = Clamp(offsetY, VirtualMachineKinematicsConfig.SafeYMin, VirtualMachineKinematicsConfig.SafeYMax - 300.0);

			double width = 180.0 + job.MaterialThicknessMm * 25.0 + random.Next(60, 220);
			double height = 120.0 + job.MaterialThicknessMm * 18.0 + random.Next(40, 160);
			width = Math.Min(width, VirtualMachineKinematicsConfig.SafeXMax - offsetX);
			height = Math.Min(height, VirtualMachineKinematicsConfig.SafeYMax - offsetY);

			double cutSpeed = Math.Max(4.0, 18.0 * job.FeedRateFactor);
			double pierceSeconds = 1.5 + job.MaterialThicknessMm * 0.35 + random.NextDouble() * 1.2;

			segments.Add(new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.RapidMove, TargetX = offsetX, TargetY = offsetY });
			segments.Add(new LaserToolpathSegment
			{
				Kind = LaserToolpathSegmentKind.Pierce,
				TargetX = offsetX,
				TargetY = offsetY,
				PierceDurationSeconds = pierceSeconds
			});
			segments.Add(new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX + width, TargetY = offsetY, CutSpeedMmPerS = cutSpeed });
			segments.Add(new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX + width, TargetY = offsetY + height, CutSpeedMmPerS = cutSpeed * 0.85 });
			segments.Add(new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX, TargetY = offsetY + height, CutSpeedMmPerS = cutSpeed * 0.9 });
			segments.Add(new LaserToolpathSegment { Kind = LaserToolpathSegmentKind.CutLine, TargetX = offsetX, TargetY = offsetY, CutSpeedMmPerS = cutSpeed });
		}

		return new LaserToolpathPlan
		{
			Segments = segments,
			RequiresNozzleChange = RequiresNozzleChange(job)
		};
	}

	public static bool RequiresNozzleChange(FixedProductionJobDefinition job) =>
		job.CatalogIndex % 3 == 0
		|| job.MaterialThicknessMm >= 6.0
		|| job.MaterialName.Contains("Cu", StringComparison.OrdinalIgnoreCase);

	private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
}
