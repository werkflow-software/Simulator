using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class LaserProcessingMachine300ProfileFactory
{
	public const string ProfileId = "laser-processing-machine-300";

	public const string ProfileVersion = "1.1.0";

	public static PhysicalMachineProfile Create()
	{
		PhysicalMachineProfile physicalMachineProfile = TechnicalLearningMachine300ProfileFactory.Create();
		List<SignalDefinition> list = physicalMachineProfile.Signals.ToList();
		list.Add(CreateNumeric("Mechanical.VibrationRms", "Mechanical Vibration RMS", SignalCategory.Vibration, "mm/s", 0.8, 1.4, 1.1, 0.0, 12.0, 0.5));
		List<SignalDependencyDefinition> list2 = PhysicalProfileDependencyBuilder.CreateLaserSignalDependencies();
		PhysicalProfileDependencyBuilder.ApplyHiddenInputs(list, list2);
		return new PhysicalMachineProfile
		{
			ProfileId = "laser-processing-machine-300",
			ProfileVersion = "1.1.0",
			DisplayName = "Laser Processing Machine 300",
			Description = "Physikalisches Laser-/Bearbeitungsmaschinenprofil mit Hidden States und gekoppelten Signalverläufen (AP 3).",
			MachineType = "LaserProcessingCell",
			Manufacturer = "Werkflow",
			DefaultUpdateInterval = TimeSpan.FromSeconds(1.0),
			Metadata = new Dictionary<string, string>
			{
				["ap"] = "3",
				["purpose"] = "physical-simulation",
				["profileKind"] = "physical-simulation",
				["signalCount"] = list.Count.ToString(),
				["hiddenStateCount"] = "12",
				["dependencyCount"] = list2.Count.ToString()
			},
			Signals = list,
			HiddenProcessStates = PhysicalProfileDependencyBuilder.CreateLaserHiddenStates(),
			Dependencies = list2,
			HiddenStateDependencies = PhysicalProfileDependencyBuilder.CreateLaserHiddenDependencies()
		};
	}

	private static SignalDefinition CreateNumeric(string signalId, string displayName, SignalCategory category, string unit, double normalMin, double normalMax, double nominal, double hardMin, double hardMax, double intervalSeconds)
	{
		return new SignalDefinition
		{
			SignalId = signalId,
			NodeId = signalId,
			BrowseName = signalId.Split('.')[^1],
			DisplayName = displayName,
			Description = displayName,
			Category = category,
			DataType = PhysicalSignalDataType.Double,
			EngineeringUnit = unit,
			NormalMinimum = normalMin,
			NormalMaximum = normalMax,
			NominalValue = nominal,
			HardMinimum = hardMin,
			HardMaximum = hardMax,
			NoiseModel = NoiseModel.Gaussian,
			NoiseAmplitude = Math.Max(0.01, (normalMax - normalMin) * 0.02),
			UpdateInterval = TimeSpan.FromSeconds(intervalSeconds),
			DecimalPlaces = 2,
			ResponseInertia = 0.2,
			InitialValue = nominal,
			IsEnabled = true,
			TechnicalBehavior = TechnicalSignalBehavior.Continuous
		};
	}
}
