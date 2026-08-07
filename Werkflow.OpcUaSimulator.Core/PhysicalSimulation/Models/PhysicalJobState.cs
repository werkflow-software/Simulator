using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalJobState
{
	public int JobIndex { get; set; }

	public string JobName { get; set; } = "JOB-001";

	public string PartName { get; set; } = "PART-A";

	public int TargetQuantity { get; set; } = 25;

	public int ProducedQuantity { get; set; }

	public DateTimeOffset JobStartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
