using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class ProfileDistribution
{
	public int TotalSignals { get; init; }

	public int EnabledSignals { get; init; }

	public required IReadOnlyDictionary<PhysicalSignalDataType, int> ByDataType { get; init; }

	public required IReadOnlyDictionary<TechnicalSignalBehavior, int> ByBehavior { get; init; }

	public required IReadOnlyDictionary<SignalCategory, int> ByCategory { get; init; }

	public required IReadOnlyDictionary<string, int> ByIntervalGroup { get; init; }
}
