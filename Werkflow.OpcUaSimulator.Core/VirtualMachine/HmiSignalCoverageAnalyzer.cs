using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

public sealed class HmiSignalCoverageReport
{
	public int PhysicalSignalsInProfile { get; init; }

	public int HmiSignalsMapped { get; init; }

	public IReadOnlyList<string> UnmappedSignals { get; init; } = [];

	public bool Passed => UnmappedSignals.Count == 0;
}

public static class HmiSignalCoverageAnalyzer
{
	public static HmiSignalCoverageReport Analyze(PhysicalMachineProfile profile)
	{
		var enabled = profile.Signals.Where(s => s.IsEnabled).ToList();
		var mappedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var tab in HmiSignalCatalog.TabDefinitions)
		{
			foreach (var signal in enabled.Where(s => tab.Categories.Contains(s.Category)))
			{
				mappedIds.Add(signal.SignalId);
			}
		}

		var unmapped = enabled
			.Where(s => !mappedIds.Contains(s.SignalId))
			.Select(s => s.SignalId)
			.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
			.ToList();

		return new HmiSignalCoverageReport
		{
			PhysicalSignalsInProfile = enabled.Count,
			HmiSignalsMapped = mappedIds.Count,
			UnmappedSignals = unmapped
		};
	}
}
