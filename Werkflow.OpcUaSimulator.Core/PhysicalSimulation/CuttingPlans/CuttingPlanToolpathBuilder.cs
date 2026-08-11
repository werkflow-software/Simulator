using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;

public static class CuttingPlanToolpathBuilder
{
	public static LaserToolpathPlan BuildToolpath(
		CuttingPlanPart sheetPart,
		int sheetPartIndex,
		FixedProductionJobDefinition job,
		int seed)
	{
		Random random = new Random(seed ^ (sheetPartIndex * 4177) ^ job.CatalogIndex);
		double cutSpeed = Math.Max(4.0, 18.0 * job.FeedRateFactor);
		List<LaserToolpathSegment> segments = new List<LaserToolpathSegment>();

		IEnumerable<CuttingPlanContour> orderedContours = sheetPart.Contours
			.OrderBy(c => c.IsInnerContour ? 0 : 1)
			.ThenBy(c => c.ContourIndex);

		foreach (CuttingPlanContour contour in orderedContours)
		{
			double pierceSeconds = 1.2 + job.MaterialThicknessMm * 0.4 + random.NextDouble() * 1.0;
			segments.Add(new LaserToolpathSegment
			{
				Kind = LaserToolpathSegmentKind.RapidMove,
				TargetX = contour.PiercePoint.X,
				TargetY = contour.PiercePoint.Y,
				SheetPartIndex = sheetPartIndex,
				ContourIndex = contour.ContourIndex,
				IsInnerContour = contour.IsInnerContour
			});
			segments.Add(new LaserToolpathSegment
			{
				Kind = LaserToolpathSegmentKind.Pierce,
				TargetX = contour.PiercePoint.X,
				TargetY = contour.PiercePoint.Y,
				PierceDurationSeconds = pierceSeconds,
				SheetPartIndex = sheetPartIndex,
				ContourIndex = contour.ContourIndex,
				IsInnerContour = contour.IsInnerContour
			});

			int vertexCount = contour.Vertices.Count;
			for (int v = 0; v < vertexCount; v++)
			{
				CuttingPlanPoint end = contour.Vertices[v];
				segments.Add(new LaserToolpathSegment
				{
					Kind = LaserToolpathSegmentKind.CutLine,
					TargetX = end.X,
					TargetY = end.Y,
					CutSpeedMmPerS = cutSpeed * (v == 0 ? 1.0 : 0.88),
					IsCornerEntry = v > 0,
					SheetPartIndex = sheetPartIndex,
					ContourIndex = contour.ContourIndex,
					IsInnerContour = contour.IsInnerContour
				});
			}
		}

		return new LaserToolpathPlan
		{
			Segments = segments,
			RequiresNozzleChange = LaserToolpathGenerator.RequiresNozzleChange(job)
		};
	}
}
