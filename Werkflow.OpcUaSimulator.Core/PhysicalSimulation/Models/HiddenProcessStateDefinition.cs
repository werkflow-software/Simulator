using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class HiddenProcessStateDefinition
{
	public string StateId { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public double NormalMinimum { get; init; }

	public double NormalMaximum { get; init; }

	public double NominalValue { get; init; }

	public double HardMinimum { get; init; }

	public double HardMaximum { get; init; }

	public double InitialValue { get; init; }

	public double ResponseInertia { get; init; }

	public double NaturalDrift { get; init; }

	public double NoiseAmplitude { get; init; }

	public double RecoveryRate { get; init; } = 0.05;

	public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(1.0);
}
