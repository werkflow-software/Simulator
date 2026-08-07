namespace Werkflow.OpcUaSimulator.Tests;

public sealed class CorrelationGroupResult
{
	public string PairId { get; init; } = string.Empty;

	public string ProfileId { get; init; } = string.Empty;

	public string? HiddenStateId { get; init; }

	public string TargetSignalId { get; init; } = string.Empty;

	public string ExpectedDirection { get; init; } = string.Empty;

	public string ExpectedDependencyType { get; init; } = string.Empty;

	public int ExpectedLagSeconds { get; init; }

	public int SampleCount { get; init; }

	public double Pearson { get; init; }

	public double Spearman { get; init; }

	public int StrongestCrossCorrelationLag { get; init; }

	public double StrongestCrossCorrelation { get; init; }

	public string Assessment { get; init; } = string.Empty;
}
