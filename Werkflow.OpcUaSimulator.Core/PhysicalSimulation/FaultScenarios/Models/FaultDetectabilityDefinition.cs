using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

/// <summary>
/// Declarative detectability threshold for ground-truth recording (not exposed via OPC UA).
/// </summary>
public sealed class FaultDetectabilityDefinition
{
	public double MinimumProgress { get; set; } = 0.35;

	public double MinimumEffectMagnitude { get; set; } = 0.2;

	public TimeSpan MinimumDuration { get; set; } = TimeSpan.FromSeconds(30);

	public int MinimumOutOfBandSignalCount { get; set; } = 2;
}
