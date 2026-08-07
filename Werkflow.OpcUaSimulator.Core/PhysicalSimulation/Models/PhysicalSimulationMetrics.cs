using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalSimulationMetrics
{
	public double EngineTicksPerSecond { get; set; }

	public double SignalCalculationsPerSecond { get; set; }

	public double AverageCalculationDurationMs { get; set; }

	public double MaxCalculationDurationMs { get; set; }

	public int PlausibilityViolations { get; set; }

	public int HardLimitPrevented { get; set; }

	public int HarmlessOutliersTriggered { get; set; }

	public int RecoveryCyclesCompleted { get; set; }

	public string? LastPlausibilityError { get; set; }

	public DateTimeOffset? LastEngineTickAt { get; set; }

	public long TotalEngineTicks { get; set; }

	public int PhaseChanges { get; set; }

	public int JobChanges { get; set; }
}
