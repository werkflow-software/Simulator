using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalJobState
{
	public int CatalogIndex { get; set; }

	public int JobIndex { get; set; }

	public string JobName { get; set; } = "JOB-001";

	public string PartName { get; set; } = "PART-A";

	public int TargetQuantity { get; set; } = 25;

	public int ProducedQuantity { get; set; }

	public DateTimeOffset JobStartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public string MaterialName { get; set; } = "S235JR";

	public double MaterialThicknessMm { get; set; } = 2.0;

	public string RecipeName { get; set; } = "LaserCut-Standard-A";

	public string ProgramName { get; set; } = "PRG-12045";

	public double ProcessLoadFactor { get; set; } = 1.0;

	public double FeedRateFactor { get; set; } = 1.0;
}
