using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.CuttingPlans;

public enum CuttingPartState
{
	NotStarted,
	InProgress,
	Completed
}

public enum CuttingContourState
{
	Unprocessed,
	Active,
	Completed
}

public sealed class CuttingPlanPoint
{
	public double X { get; init; }

	public double Y { get; init; }
}

public sealed class CuttingPlanContour
{
	public required int ContourIndex { get; init; }

	public bool IsInnerContour { get; init; }

	public required List<CuttingPlanPoint> Vertices { get; init; }

	public required CuttingPlanPoint PiercePoint { get; init; }

	public CuttingContourState State { get; set; } = CuttingContourState.Unprocessed;
}

public sealed class CuttingPlanPart
{
	public required int PartIndex { get; init; }

	public required string Label { get; init; }

	public required List<CuttingPlanContour> Contours { get; init; }

	public CuttingPartState State { get; set; } = CuttingPartState.NotStarted;
}

public sealed class CuttingPlan
{
	public required string PlanId { get; init; }

	public required int JobCatalogIndex { get; init; }

	public required string JobId { get; init; }

	public double SheetWidth { get; init; } = VirtualMachineKinematicsConfig.WorkingAreaXMax;

	public double SheetHeight { get; init; } = VirtualMachineKinematicsConfig.WorkingAreaYMax;

	public required List<CuttingPlanPart> Parts { get; init; }

	public int PartCount => Parts.Count;
}
