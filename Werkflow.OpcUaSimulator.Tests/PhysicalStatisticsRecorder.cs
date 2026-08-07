using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalStatisticsRecorder
{
	private readonly Dictionary<string, SignalStatistics> _signals = new Dictionary<string, SignalStatistics>(StringComparer.OrdinalIgnoreCase);

	public void Record(string signalId, double value, SignalDefinition definition, DateTimeOffset timestamp, ProcessPhase? phase = null)
	{
		if (!_signals.TryGetValue(signalId, out SignalStatistics value2))
		{
			value2 = new SignalStatistics(signalId, definition);
			_signals[signalId] = value2;
		}
		value2.Add(value, timestamp, phase);
	}

	public IReadOnlyList<SignalStatisticsSnapshot> BuildSnapshots()
	{
		return _signals.Values.Select((SignalStatistics s) => s.ToSnapshot()).OrderBy<SignalStatisticsSnapshot, string>((SignalStatisticsSnapshot s) => s.SignalId, StringComparer.Ordinal).ToList();
	}
}
