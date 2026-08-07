using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultEffectDefinition
{
	public string EffectId { get; set; } = string.Empty;

	public FaultEffectTargetType TargetType { get; set; }

	public string TargetId { get; set; } = string.Empty;

	public FaultEffectType EffectType { get; set; }

	public FaultScenarioPhase StartPhase { get; set; }

	public FaultScenarioPhase EndPhase { get; set; } = FaultScenarioPhase.Faulted;

	public FaultEffectDirection Direction { get; set; }

	public double Magnitude { get; set; }

	public double RatePerSimulationMinute { get; set; }

	public TimeSpan Delay { get; set; }

	public double Inertia { get; set; } = 1.0;

	public double MinimumEffect { get; set; }

	public double MaximumEffect { get; set; } = 1.0;

	public double NoiseModifier { get; set; } = 1.0;

	public bool IsEnabled { get; set; } = true;

	public double OscillationFrequencyHz { get; set; } = 0.5;

	public double PulseIntervalSeconds { get; set; } = 10.0;

	public double PulseDurationSeconds { get; set; } = 2.0;
}
