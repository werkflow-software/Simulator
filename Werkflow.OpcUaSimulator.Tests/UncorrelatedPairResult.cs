namespace Werkflow.OpcUaSimulator.Tests;

public sealed class UncorrelatedPairResult
{
	public string PairId { get; init; } = string.Empty;

	public string SignalA { get; init; } = string.Empty;

	public string SignalB { get; init; } = string.Empty;

	public double Pearson { get; init; }

	public int StrongestLag { get; init; }

	public double StrongestCrossCorrelation { get; init; }

	public string Assessment { get; init; } = string.Empty;
}
