using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class SignalStatisticsSnapshot
{
	public string ProfileId { get; set; } = string.Empty;

	public string SignalId { get; init; } = string.Empty;

	public string Unit { get; init; } = string.Empty;

	public double NormalMinimum { get; init; }

	public double NormalMaximum { get; init; }

	public double HardMinimum { get; init; }

	public double HardMaximum { get; init; }

	public long Samples { get; init; }

	public double Minimum { get; init; }

	public double Maximum { get; init; }

	public double Mean { get; init; }

	public double Median { get; init; }

	public double StandardDeviation { get; init; }

	public double PercentWithinNormal { get; init; }

	public double PercentBelowNormal { get; init; }

	public double PercentAboveNormal { get; init; }

	public double PercentAtHardMinimum { get; init; }

	public double PercentAtHardMaximum { get; init; }

	public long ChangeCount { get; init; }

	public double AverageChangeRate { get; init; }

	public double MaxChangeRate { get; init; }

	public DateTimeOffset? FirstTimestampUtc { get; init; }

	public DateTimeOffset? LastTimestampUtc { get; init; }

	public Dictionary<string, double> MeanByPhase { get; init; } = new Dictionary<string, double>(StringComparer.Ordinal);

	public Dictionary<string, PhaseStatisticsSnapshot> PhaseStatistics { get; init; } = new Dictionary<string, PhaseStatisticsSnapshot>(StringComparer.Ordinal);

	public bool PhaseEvaluationPassed { get; init; }

	public string PhaseEvaluationNotes { get; init; } = string.Empty;
}
