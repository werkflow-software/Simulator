namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalCorrelationEvaluationRequest
{
	public double Pearson { get; init; }

	public double Spearman { get; init; }

	public int StrongestLag { get; init; }

	public double StrongestCrossCorrelation { get; init; }

	public int SampleCount { get; init; }

	public string ExpectedDirection { get; init; } = "positive";

	public double MinPearson { get; init; }

	public double MaxPearson { get; init; }

	public int ExpectedLagSeconds { get; init; }
}
