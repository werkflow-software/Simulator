using System;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class SimulationLogEntry
{
	public DateTime Timestamp { get; set; } = DateTime.Now;

	public string MachineName { get; set; } = "—";

	public LogCategory Category { get; set; }

	public string Message { get; set; } = string.Empty;

	public string? PreviousValue { get; set; }

	public string? NewValue { get; set; }
}
