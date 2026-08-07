using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class EventTypeSettings
{
	public SimulationEventType EventType { get; set; }

	public bool IsEnabled { get; set; } = true;

	public double ProbabilityPercent { get; set; } = 5.0;

	public int MinDurationMs { get; set; } = 1000;

	public int MaxDurationMs { get; set; } = 10000;

	public int MinCooldownMs { get; set; } = 5000;

	public int MaxCooldownMs { get; set; } = 30000;

	public List<Guid> AffectedMachineIds { get; set; } = new List<Guid>();
}
