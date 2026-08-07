namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhaseStatisticsSnapshot
{
	public long Samples { get; init; }

	public double Minimum { get; init; }

	public double Maximum { get; init; }

	public double Mean { get; init; }

	public double PercentInExpectedRange { get; init; }
}
