using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

internal sealed class MutableSignalDefinition
{
	public string SignalId { get; set; } = string.Empty;

	public string NodeId { get; set; } = string.Empty;

	public string BrowseName { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public SignalCategory Category { get; set; }

	public PhysicalSignalDataType DataType { get; set; } = PhysicalSignalDataType.Double;

	public string EngineeringUnit { get; set; } = string.Empty;

	public double NormalMinimum { get; set; }

	public double NormalMaximum { get; set; }

	public double NominalValue { get; set; }

	public double HardMinimum { get; set; }

	public double HardMaximum { get; set; }

	public NoiseModel NoiseModel { get; set; } = NoiseModel.None;

	public double NoiseAmplitude { get; set; }

	public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(1.0);

	public int DecimalPlaces { get; set; } = 2;

	public double ResponseInertia { get; set; }

	public double InitialValue { get; set; }

	public bool IsEnabled { get; set; } = true;

	public bool IsWritable { get; set; }

	public TechnicalSignalBehavior TechnicalBehavior { get; set; } = TechnicalSignalBehavior.Continuous;

	public int CounterStepSize { get; set; } = 1;

	public string InitialStringValue { get; set; } = string.Empty;

	public DateTime? InitialDateTimeUtc { get; set; }

	public List<string> AllowedValues { get; set; } = new List<string>();

	public List<string> HiddenProcessInputs { get; set; } = new List<string>();

	public static MutableSignalDefinition From(SignalDefinition source)
	{
		return new MutableSignalDefinition
		{
			SignalId = source.SignalId,
			NodeId = source.NodeId,
			BrowseName = source.BrowseName,
			DisplayName = source.DisplayName,
			Description = source.Description,
			Category = source.Category,
			DataType = source.DataType,
			EngineeringUnit = source.EngineeringUnit,
			NormalMinimum = source.NormalMinimum,
			NormalMaximum = source.NormalMaximum,
			NominalValue = source.NominalValue,
			HardMinimum = source.HardMinimum,
			HardMaximum = source.HardMaximum,
			NoiseModel = source.NoiseModel,
			NoiseAmplitude = source.NoiseAmplitude,
			UpdateInterval = source.UpdateInterval,
			DecimalPlaces = source.DecimalPlaces,
			ResponseInertia = source.ResponseInertia,
			InitialValue = source.InitialValue,
			IsEnabled = source.IsEnabled,
			IsWritable = source.IsWritable,
			TechnicalBehavior = source.TechnicalBehavior,
			CounterStepSize = source.CounterStepSize,
			InitialStringValue = source.InitialStringValue,
			InitialDateTimeUtc = source.InitialDateTimeUtc,
			AllowedValues = source.AllowedValues.ToList(),
			HiddenProcessInputs = source.HiddenProcessInputs.ToList()
		};
	}

	public SignalDefinition ToDefinition()
	{
		return new SignalDefinition
		{
			SignalId = SignalId,
			NodeId = NodeId,
			BrowseName = BrowseName,
			DisplayName = DisplayName,
			Description = Description,
			Category = Category,
			DataType = DataType,
			EngineeringUnit = EngineeringUnit,
			NormalMinimum = NormalMinimum,
			NormalMaximum = NormalMaximum,
			NominalValue = NominalValue,
			HardMinimum = HardMinimum,
			HardMaximum = HardMaximum,
			NoiseModel = NoiseModel,
			NoiseAmplitude = NoiseAmplitude,
			UpdateInterval = UpdateInterval,
			DecimalPlaces = DecimalPlaces,
			ResponseInertia = ResponseInertia,
			InitialValue = InitialValue,
			IsEnabled = IsEnabled,
			IsWritable = IsWritable,
			TechnicalBehavior = TechnicalBehavior,
			CounterStepSize = CounterStepSize,
			InitialStringValue = InitialStringValue,
			InitialDateTimeUtc = InitialDateTimeUtc,
			AllowedValues = AllowedValues,
			HiddenProcessInputs = HiddenProcessInputs
		};
	}
}
