using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class SignalDependencyDefinition
{
	public string DependencyId { get; init; } = string.Empty;

	public string SourceStateId { get; init; } = string.Empty;

	public string TargetSignalId { get; init; } = string.Empty;

	public DependencyType DependencyType { get; init; } = DependencyType.Linear;

	public double Weight { get; init; } = 1.0;

	public double Offset { get; init; }

	public TimeSpan ResponseDelay { get; init; } = TimeSpan.Zero;

	public double ResponseInertia { get; init; }

	public double? MinimumEffect { get; init; }

	public double? MaximumEffect { get; init; }

	public double ThresholdValue { get; init; }

	public bool IsEnabled { get; init; } = true;
}
