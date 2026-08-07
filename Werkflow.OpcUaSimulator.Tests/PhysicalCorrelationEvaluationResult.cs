namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalCorrelationEvaluationResult
{
	public double Pearson { get; set; }

	public double Spearman { get; set; }

	public int StrongestLag { get; set; }

	public double StrongestCrossCorrelation { get; set; }

	public int SampleCount { get; set; }

	public double MinPearson { get; set; }

	public double MaxPearson { get; set; }

	public string ExpectedDirection { get; set; } = string.Empty;

	public int ExpectedLagSeconds { get; set; }

	public bool DirectionCorrect { get; set; }

	public bool MinStrengthMet { get; set; }

	public bool MaxStrengthMet { get; set; }

	public bool LagPlausible { get; set; }

	public string Result { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;
}
