using System;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class ScenarioDefinition
{
	public string Id { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public Guid? TargetMachineId { get; set; }

	public int? DurationMs { get; set; }

	public bool IsRunning { get; set; }
}
