using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalCorrelationRecorder
{
	private readonly Dictionary<string, CorrelationSeries> _series = new Dictionary<string, CorrelationSeries>(StringComparer.OrdinalIgnoreCase);

	private readonly int _capacity;

	public PhysicalCorrelationRecorder(int capacity = 4096)
	{
		_capacity = capacity;
	}

	public void RecordPair(string pairId, double x, double y)
	{
		if (!_series.TryGetValue(pairId, out CorrelationSeries value))
		{
			value = new CorrelationSeries(pairId, _capacity);
			_series[pairId] = value;
		}
		value.Add(x, y);
	}

	public CorrelationGroupResult Analyze(string pairId, string profileId, string? hiddenStateId, string targetSignalId, string expectedDirection, string expectedDependencyType, int expectedLagSeconds, bool useFirstDifferences = false)
	{
		if (!_series.TryGetValue(pairId, out CorrelationSeries value) || value.Count < 20)
		{
			return new CorrelationGroupResult
			{
				PairId = pairId,
				ProfileId = profileId,
				HiddenStateId = hiddenStateId,
				TargetSignalId = targetSignalId,
				ExpectedDirection = expectedDirection,
				ExpectedDependencyType = expectedDependencyType,
				ExpectedLagSeconds = expectedLagSeconds,
				SampleCount = (value?.Count ?? 0),
				Assessment = "insufficient-samples"
			};
		}
		double num = (useFirstDifferences ? value.PearsonFirstDifferences() : value.Pearson());
		double spearman = value.Spearman();
		var (num2, strongestCrossCorrelation) = value.StrongestCrossCorrelation(60);
		if (1 == 0)
		{
		}
		bool flag = ((expectedDirection == "positive") ? (num > 0.15) : ((!(expectedDirection == "negative")) ? (Math.Abs(num) > 0.1) : (num < -0.15)));
		if (1 == 0)
		{
		}
		bool flag2 = flag;
		bool flag3 = Math.Abs(num2 - expectedLagSeconds) <= Math.Max(5, expectedLagSeconds / 2 + 2);
		string assessment = ((!flag2 || !(Math.Abs(num) < 0.98)) ? ((Math.Abs(num) < 0.1) ? "weak-or-uncorrelated" : "review") : (flag3 ? "pass" : "pass-direction-lag-deviation"));
		return new CorrelationGroupResult
		{
			PairId = pairId,
			ProfileId = profileId,
			HiddenStateId = hiddenStateId,
			TargetSignalId = targetSignalId,
			ExpectedDirection = expectedDirection,
			ExpectedDependencyType = expectedDependencyType,
			ExpectedLagSeconds = expectedLagSeconds,
			SampleCount = value.Count,
			Pearson = num,
			Spearman = spearman,
			StrongestCrossCorrelationLag = num2,
			StrongestCrossCorrelation = strongestCrossCorrelation,
			Assessment = assessment
		};
	}
}
