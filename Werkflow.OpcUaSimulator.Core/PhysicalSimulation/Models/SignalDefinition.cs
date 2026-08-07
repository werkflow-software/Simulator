using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class SignalDefinition
{
	public string SignalId { get; init; } = string.Empty;

	public string NodeId { get; init; } = string.Empty;

	public string BrowseName { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public SignalCategory Category { get; init; }

	public PhysicalSignalDataType DataType { get; init; } = PhysicalSignalDataType.Double;

	public string EngineeringUnit { get; init; } = string.Empty;

	public double NormalMinimum { get; init; }

	public double NormalMaximum { get; init; }

	public double NominalValue { get; init; }

	public double HardMinimum { get; init; }

	public double HardMaximum { get; init; }

	public NoiseModel NoiseModel { get; init; } = NoiseModel.None;

	public double NoiseAmplitude { get; init; }

	public TimeSpan UpdateInterval { get; init; } = TimeSpan.FromSeconds(1.0);

	public int DecimalPlaces { get; init; } = 2;

	public double ResponseInertia { get; init; }

	public double InitialValue { get; init; }

	public bool IsEnabled { get; init; } = true;

	public bool IsWritable { get; init; }

	public TechnicalSignalBehavior TechnicalBehavior { get; init; } = TechnicalSignalBehavior.Continuous;

	public int CounterStepSize { get; init; } = 1;

	public string InitialStringValue { get; init; } = string.Empty;

	public DateTime? InitialDateTimeUtc { get; init; }

	public IReadOnlyList<string> AllowedValues { get; init; } = Array.Empty<string>();

	public IReadOnlyList<string> HiddenProcessInputs { get; init; } = Array.Empty<string>();
}
