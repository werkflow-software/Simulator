namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public enum LaserToolpathSegmentKind
{
	RapidMove,
	Pierce,
	CutLine
}

public sealed class LaserToolpathSegment
{
	public required LaserToolpathSegmentKind Kind { get; init; }

	public required double TargetX { get; init; }

	public required double TargetY { get; init; }

	public double PierceDurationSeconds { get; init; } = 2.5;

	public double CutSpeedMmPerS { get; init; } = 12.0;

	/// <summary>First portion of segment uses reduced speed after direction change.</summary>
	public bool IsCornerEntry { get; init; }
}

public sealed class LaserToolpathPlan
{
	public required List<LaserToolpathSegment> Segments { get; init; }

	public required bool RequiresNozzleChange { get; init; }
}
