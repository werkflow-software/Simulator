using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class VigilLabLaserReducedProfileFactory
{
	public const string ProfileId = "vigil-lab-laser-reduced";

	public const string ProfileVersion = "1.0.0";

	public static readonly IReadOnlySet<string> EnabledPhysicalSignalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"Axis01.Position",
		"Axis02.Position",
		"Axis03.Position",
		"Process.CuttingSpeed",
		"Process.FocusPosition",
		"Process.LaserPowerActual",
		"Thermal.OpticsHousingTemp"
	};

	public static PhysicalMachineProfile Create()
	{
		PhysicalMachineProfile fullProfile = LaserProcessingMachine300ProfileFactory.Create();
		List<SignalDefinition> signals = fullProfile.Signals
			.Select(signal => CloneSignal(signal, EnabledPhysicalSignalIds.Contains(signal.SignalId)))
			.ToList();

		return new PhysicalMachineProfile
		{
			ProfileId = ProfileId,
			ProfileVersion = ProfileVersion,
			DisplayName = "VIGIL LAB Laser Reduced",
			Description = "Reduced physical profile for VIGIL Run 001 (7 enabled physical signals).",
			MachineType = fullProfile.MachineType,
			Manufacturer = fullProfile.Manufacturer,
			DefaultUpdateInterval = fullProfile.DefaultUpdateInterval,
			Metadata = new Dictionary<string, string>(fullProfile.Metadata)
			{
				["purpose"] = "vigil-lab-run-001",
				["profileKind"] = "physical-simulation-reduced",
				["signalCount"] = signals.Count.ToString(),
				["enabledSignalCount"] = signals.Count(s => s.IsEnabled).ToString()
			},
			Signals = signals,
			HiddenProcessStates = fullProfile.HiddenProcessStates,
			Dependencies = fullProfile.Dependencies,
			HiddenStateDependencies = fullProfile.HiddenStateDependencies
		};
	}

	private static SignalDefinition CloneSignal(SignalDefinition source, bool isEnabled) =>
		new()
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
			IsEnabled = isEnabled,
			IsWritable = source.IsWritable,
			TechnicalBehavior = source.TechnicalBehavior,
			CounterStepSize = source.CounterStepSize,
			InitialStringValue = source.InitialStringValue,
			InitialDateTimeUtc = source.InitialDateTimeUtc,
			AllowedValues = source.AllowedValues,
			HiddenProcessInputs = source.HiddenProcessInputs
		};
}
