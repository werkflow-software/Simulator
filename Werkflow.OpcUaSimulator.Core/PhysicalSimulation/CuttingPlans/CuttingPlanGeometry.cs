using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;

public static class CuttingPlanGeometry
{
	public static CuttingPlanContour RectangleContour(
		int contourIndex,
		double x,
		double y,
		double width,
		double height,
		bool isInner)
	{
		var vertices = new List<CuttingPlanPoint>
		{
			new() { X = x, Y = y },
			new() { X = x + width, Y = y },
			new() { X = x + width, Y = y + height },
			new() { X = x, Y = y + height }
		};
		return new CuttingPlanContour
		{
			ContourIndex = contourIndex,
			IsInnerContour = isInner,
			Vertices = vertices,
			PiercePoint = new CuttingPlanPoint { X = x + 2.0, Y = y + 2.0 }
		};
	}

	public static CuttingPlanContour LShapeContour(int contourIndex, double x, double y, double w, double h, double leg, bool isInner)
	{
		var vertices = new List<CuttingPlanPoint>
		{
			new() { X = x, Y = y },
			new() { X = x + w, Y = y },
			new() { X = x + w, Y = y + leg },
			new() { X = x + leg, Y = y + leg },
			new() { X = x + leg, Y = y + h },
			new() { X = x, Y = y + h }
		};
		return new CuttingPlanContour
		{
			ContourIndex = contourIndex,
			IsInnerContour = isInner,
			Vertices = vertices,
			PiercePoint = new CuttingPlanPoint { X = x + 2.0, Y = y + 2.0 }
		};
	}

	public static CuttingPlanContour FrameContour(
		int contourIndex,
		double x,
		double y,
		double outerW,
		double outerH,
		double margin)
	{
		double ix = x + margin;
		double iy = y + margin;
		double iw = outerW - 2 * margin;
		double ih = outerH - 2 * margin;
		var vertices = new List<CuttingPlanPoint>
		{
			new() { X = x, Y = y },
			new() { X = x + outerW, Y = y },
			new() { X = x + outerW, Y = y + outerH },
			new() { X = x, Y = y + outerH },
			new() { X = x, Y = iy + ih },
			new() { X = ix, Y = iy + ih },
			new() { X = ix, Y = iy },
			new() { X = ix + iw, Y = iy },
			new() { X = ix + iw, Y = iy + ih },
			new() { X = x, Y = iy + ih },
			new() { X = x, Y = y }
		};
		return new CuttingPlanContour
		{
			ContourIndex = contourIndex,
			IsInnerContour = false,
			Vertices = vertices,
			PiercePoint = new CuttingPlanPoint { X = x + 2.0, Y = y + 2.0 }
		};
	}

	public static CuttingPlanPart BuildPart(int partIndex, string label, double x, double y, double w, double h, int innerHoles = 0)
	{
		var contours = new List<CuttingPlanContour>();
		int ci = 0;
		for (int i = 0; i < innerHoles; i++)
		{
			double holeW = w * 0.22;
			double holeH = h * 0.22;
			double hx = x + w * 0.25 + i * (holeW + 12);
			double hy = y + h * 0.35;
			contours.Add(RectangleContour(ci++, hx, hy, holeW, holeH, isInner: true));
		}

		contours.Add(RectangleContour(ci, x, y, w, h, isInner: false));
		return new CuttingPlanPart
		{
			PartIndex = partIndex,
			Label = label,
			Contours = contours
		};
	}

	public static CuttingPlanPart BuildFramePart(int partIndex, string label, double x, double y, double w, double h, double margin)
	{
		return new CuttingPlanPart
		{
			PartIndex = partIndex,
			Label = label,
			Contours = [FrameContour(0, x, y, w, h, margin)]
		};
	}

	public static CuttingPlanPart BuildLPart(int partIndex, string label, double x, double y, double w, double h, double leg)
	{
		return new CuttingPlanPart
		{
			PartIndex = partIndex,
			Label = label,
			Contours = [LShapeContour(0, x, y, w, h, leg, isInner: false)]
		};
	}

	public static void ResetRuntimeStates(CuttingPlan plan)
	{
		foreach (CuttingPlanPart part in plan.Parts)
		{
			part.State = CuttingPartState.NotStarted;
			foreach (CuttingPlanContour contour in part.Contours)
			{
				contour.State = CuttingContourState.Unprocessed;
			}
		}
	}

	public static void ClampToSafeArea(CuttingPlanPart part)
	{
		foreach (CuttingPlanContour contour in part.Contours)
		{
			foreach (CuttingPlanPoint p in contour.Vertices)
			{
				// vertices are absolute; validation only at build time
			}
		}
	}
}
