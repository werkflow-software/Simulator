using System;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class PhysicalProfileStatistics
{
	public static ProfileDistribution Analyze(PhysicalMachineProfile profile)
	{
		return new ProfileDistribution
		{
			TotalSignals = profile.Signals.Count,
			EnabledSignals = profile.Signals.Count((SignalDefinition s) => s.IsEnabled),
			ByDataType = (from s in profile.Signals
				group s by s.DataType).ToDictionary((IGrouping<PhysicalSignalDataType, SignalDefinition> g) => g.Key, (IGrouping<PhysicalSignalDataType, SignalDefinition> g) => g.Count()),
			ByBehavior = (from s in profile.Signals
				group s by s.TechnicalBehavior).ToDictionary((IGrouping<TechnicalSignalBehavior, SignalDefinition> g) => g.Key, (IGrouping<TechnicalSignalBehavior, SignalDefinition> g) => g.Count()),
			ByCategory = (from s in profile.Signals
				group s by s.Category).ToDictionary((IGrouping<SignalCategory, SignalDefinition> g) => g.Key, (IGrouping<SignalCategory, SignalDefinition> g) => g.Count()),
			ByIntervalGroup = (from s in profile.Signals
				group s by GetIntervalGroup(s.UpdateInterval)).ToDictionary((IGrouping<string, SignalDefinition> g) => g.Key, (IGrouping<string, SignalDefinition> g) => g.Count())
		};
	}

	private static string GetIntervalGroup(TimeSpan interval)
	{
		double totalSeconds = interval.TotalSeconds;
		if (1 == 0)
		{
		}
		string result = ((totalSeconds <= 1.0) ? "0-1s" : ((totalSeconds <= 5.0) ? "1-5s" : ((totalSeconds <= 30.0) ? "5-30s" : ((!(totalSeconds <= 300.0)) ? ">5m" : "30s-5m"))));
		if (1 == 0)
		{
		}
		return result;
	}
}
