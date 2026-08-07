namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalJobSegmentSnapshot
{
	public int JobId { get; init; }

	public string JobName { get; init; } = string.Empty;

	public string PartName { get; init; } = string.Empty;

	public int TargetCounter { get; init; }

	public int ActualCounterAtStart { get; init; }
}
