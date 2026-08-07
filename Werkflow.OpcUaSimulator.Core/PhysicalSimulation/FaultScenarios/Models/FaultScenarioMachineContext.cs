using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioMachineContext
{
	public int MaxParallelScenarios { get; set; } = 3;

	public Dictionary<Guid, FaultScenarioInstance> ActiveInstances { get; } = new Dictionary<Guid, FaultScenarioInstance>();

	public Dictionary<string, Guid> ScenarioIdToInstance { get; } = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

	public HashSet<string> ActiveFaultCodes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public string? HighestPriorityFaultMessage { get; set; }

	public int HighestPriority { get; set; } = int.MaxValue;
}
