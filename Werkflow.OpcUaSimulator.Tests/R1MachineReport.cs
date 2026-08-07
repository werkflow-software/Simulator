using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class R1MachineReport
{
	public Guid MachineId { get; set; }

	public string MachineName { get; set; } = string.Empty;

	public string Endpoint { get; set; } = string.Empty;

	public string ProfileId { get; set; } = string.Empty;

	public string ProfileVersion { get; set; } = string.Empty;

	public int SignalCount { get; set; }

	public int HiddenStateCount { get; set; }

	public int SignalDependencyCount { get; set; }

	public int HiddenStateDependencyCount { get; set; }

	public long EngineTicks { get; set; }

	public long OpcUaUpdates { get; set; }

	public double AverageCalculationDurationMs { get; set; }

	public double MaxCalculationDurationMs { get; set; }

	public double AveragePublishDurationMs { get; set; }

	public double MaxPublishDurationMs { get; set; }

	public int PlausibilityViolations { get; set; }

	public int HardLimitViolations { get; set; }

	public string CurrentPhase { get; set; } = string.Empty;

	public List<string> MonitoredSignals { get; set; } = new List<string>();
}
