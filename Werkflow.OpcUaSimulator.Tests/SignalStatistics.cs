using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class SignalStatistics
{
	private sealed class PhaseBucket
	{
		private long _count;

		private double _mean;

		private double _min = double.PositiveInfinity;

		private double _max = double.NegativeInfinity;

		private long _inExpected;

		public double Mean => _mean;

		public double Min => (_count == 0L) ? 0.0 : _min;

		public double Max => (_count == 0L) ? 0.0 : _max;

		public long Count => _count;

		public long InExpectedRange => _inExpected;

		public void Add(double value, string signalId, ProcessPhase phase)
		{
			_count++;
			_min = Math.Min(_min, value);
			_max = Math.Max(_max, value);
			double num = value - _mean;
			_mean += num / (double)_count;
			if (PhysicalPhaseRangeExpectations.TryGetExpectedRange(signalId, phase, out var min, out var max) && value >= min && value <= max)
			{
				_inExpected++;
			}
		}
	}

	private long _count;

	private double _min = double.PositiveInfinity;

	private double _max = double.NegativeInfinity;

	private double _mean;

	private double _m2;

	private readonly List<double> _recent = new List<double>(2048);

	private long _changes;

	private double? _previous;

	private double _sumChangeRate;

	private double _maxChangeRate;

	private DateTimeOffset? _first;

	private DateTimeOffset? _last;

	private long _inNormal;

	private long _belowNormal;

	private long _aboveNormal;

	private long _atHardMin;

	private long _atHardMax;

	private readonly Dictionary<ProcessPhase, PhaseBucket> _phaseBuckets = new Dictionary<ProcessPhase, PhaseBucket>();

	public string SignalId { get; }

	public SignalDefinition Definition { get; }

	public SignalStatistics(string signalId, SignalDefinition definition)
	{
		SignalId = signalId;
		Definition = definition;
	}

	public void Add(double value, DateTimeOffset timestamp, ProcessPhase? phase = null)
	{
		_count++;
		_min = Math.Min(_min, value);
		_max = Math.Max(_max, value);
		DateTimeOffset valueOrDefault = _first.GetValueOrDefault();
		if (!_first.HasValue)
		{
			valueOrDefault = timestamp;
			_first = valueOrDefault;
		}
		_last = timestamp;
		double num = ((_count == 1) ? 0.0 : (value - _mean));
		_mean += num / (double)_count;
		double num2 = value - _mean;
		_m2 += num * num2;
		if (_recent.Count == _recent.Capacity)
		{
			_recent.RemoveAt(0);
		}
		_recent.Add(value);
		if (value >= Definition.NormalMinimum && value <= Definition.NormalMaximum)
		{
			_inNormal++;
		}
		else if (value < Definition.NormalMinimum)
		{
			_belowNormal++;
		}
		else
		{
			_aboveNormal++;
		}
		if (Math.Abs(value - Definition.HardMinimum) < 1E-06)
		{
			_atHardMin++;
		}
		if (Math.Abs(value - Definition.HardMaximum) < 1E-06)
		{
			_atHardMax++;
		}
		if (_previous.HasValue && Math.Abs(value - _previous.Value) > 1E-09)
		{
			_changes++;
			double num3 = Math.Abs(value - _previous.Value);
			_sumChangeRate += num3;
			_maxChangeRate = Math.Max(_maxChangeRate, num3);
		}
		_previous = value;
		if (phase.HasValue)
		{
			if (!_phaseBuckets.TryGetValue(phase.Value, out PhaseBucket value2))
			{
				value2 = new PhaseBucket();
				_phaseBuckets[phase.Value] = value2;
			}
			value2.Add(value, SignalId, phase.Value);
		}
	}

	public SignalStatisticsSnapshot ToSnapshot()
	{
		double d = ((_count > 1) ? (_m2 / (double)(_count - 1)) : 0.0);
		double median = 0.0;
		if (_recent.Count > 0)
		{
			double[] array = _recent.OrderBy((double v) => v).ToArray();
			median = ((array.Length % 2 == 0) ? ((array[array.Length / 2 - 1] + array[array.Length / 2]) / 2.0) : array[array.Length / 2]);
		}
		Dictionary<string, PhaseStatisticsSnapshot> dictionary = _phaseBuckets.ToDictionary<KeyValuePair<ProcessPhase, PhaseBucket>, string, PhaseStatisticsSnapshot>((KeyValuePair<ProcessPhase, PhaseBucket> kvp) => kvp.Key.ToString(), (KeyValuePair<ProcessPhase, PhaseBucket> kvp) => new PhaseStatisticsSnapshot
		{
			Samples = kvp.Value.Count,
			Minimum = kvp.Value.Min,
			Maximum = kvp.Value.Max,
			Mean = kvp.Value.Mean,
			PercentInExpectedRange = ((kvp.Value.Count == 0L) ? 0.0 : (100.0 * (double)kvp.Value.InExpectedRange / (double)kvp.Value.Count))
		}, StringComparer.Ordinal);
		(bool, string) tuple = EvaluateProcessingPhases(dictionary);
		SignalStatisticsSnapshot obj = new SignalStatisticsSnapshot
		{
			SignalId = SignalId,
			Unit = Definition.EngineeringUnit,
			NormalMinimum = Definition.NormalMinimum,
			NormalMaximum = Definition.NormalMaximum,
			HardMinimum = Definition.HardMinimum,
			HardMaximum = Definition.HardMaximum,
			Samples = _count,
			Minimum = ((_count == 0L) ? 0.0 : _min),
			Maximum = ((_count == 0L) ? 0.0 : _max),
			Mean = _mean,
			Median = median,
			StandardDeviation = Math.Sqrt(d),
			PercentWithinNormal = ((_count == 0L) ? 0.0 : (100.0 * (double)_inNormal / (double)_count)),
			PercentBelowNormal = ((_count == 0L) ? 0.0 : (100.0 * (double)_belowNormal / (double)_count)),
			PercentAboveNormal = ((_count == 0L) ? 0.0 : (100.0 * (double)_aboveNormal / (double)_count)),
			PercentAtHardMinimum = ((_count == 0L) ? 0.0 : (100.0 * (double)_atHardMin / (double)_count)),
			PercentAtHardMaximum = ((_count == 0L) ? 0.0 : (100.0 * (double)_atHardMax / (double)_count)),
			ChangeCount = _changes,
			AverageChangeRate = ((_changes == 0L) ? 0.0 : (_sumChangeRate / (double)_changes)),
			MaxChangeRate = _maxChangeRate,
			FirstTimestampUtc = _first,
			LastTimestampUtc = _last,
			MeanByPhase = _phaseBuckets.ToDictionary<KeyValuePair<ProcessPhase, PhaseBucket>, string, double>((KeyValuePair<ProcessPhase, PhaseBucket> kvp) => kvp.Key.ToString(), (KeyValuePair<ProcessPhase, PhaseBucket> kvp) => kvp.Value.Mean, StringComparer.Ordinal),
			PhaseStatistics = dictionary,
			PhaseEvaluationPassed = tuple.Item1,
			PhaseEvaluationNotes = tuple.Item2
		};
		return obj;
	}

	private (bool Passed, string Notes) EvaluateProcessingPhases(Dictionary<string, PhaseStatisticsSnapshot> phaseStats)
	{
		if (!PhysicalPhaseRangeExpectations.IsProcessingCritical(SignalId))
		{
			return (Passed: true, Notes: string.Empty);
		}
		if (!phaseStats.TryGetValue(ProcessPhase.Processing.ToString(), out PhaseStatisticsSnapshot value) || value.Samples < 3)
		{
			return (Passed: false, Notes: "Insufficient Processing samples.");
		}
		if (!PhysicalPhaseRangeExpectations.TryGetExpectedRange(SignalId, ProcessPhase.Processing, out var min, out var max))
		{
			return (Passed: true, Notes: string.Empty);
		}
		if (value.Mean < min || value.Mean > max)
		{
			return (Passed: false, Notes: $"Processing mean {value.Mean:F2} outside [{min}, {max}].");
		}
		if (value.PercentInExpectedRange < 50.0)
		{
			return (Passed: false, Notes: $"Processing in-range {value.PercentInExpectedRange:F1}% below 60%.");
		}
		if (SignalId.Contains("QualityIndex", StringComparison.OrdinalIgnoreCase) && _max - _min < 0.15 && _count > 20)
		{
			return (Passed: false, Notes: "Quality index lacks variation (possible saturation).");
		}
		return (Passed: true, Notes: string.Empty);
	}
}
