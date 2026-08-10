using System;
using System.Collections.Generic;
using System.Linq;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public static class ExperimentCatalog
{
	public static IReadOnlyList<ExperimentDefinition> GetAll() => new[]
	{
		CreateExp001Short(),
		CreateExp002Short(),
		CreateExp003(),
		CreateExp004(),
		CreateExp005()
	};

	public static ExperimentDefinition? GetById(string experimentId) =>
		GetAll().FirstOrDefault(e => e.ExperimentId.Equals(experimentId, StringComparison.OrdinalIgnoreCase));

	public static ExperimentDefinition CreateExp001Short() => new()
	{
		ExperimentId = "EXP-001",
		DisplayName = "Laser Overheating Learning (Short)",
		MachineProfileId = "laser-processing-machine-300",
		ScenarioId = "laser-overheating-axis-drive",
		ExperimentType = ExperimentType.FaultLearningSeries,
		WarmupDuration = TimeSpan.FromSeconds(10),
		NormalLearningDuration = TimeSpan.FromSeconds(30),
		FaultRunCount = 3,
		ControlRunCount = 1,
		RecoveryDuration = TimeSpan.FromMinutes(4),
		CooldownDuration = TimeSpan.FromSeconds(15),
		TimeFactor = 50.0,
		BaseSeed = 101,
		ControlScenarioIds = ["laser-overheating-axis-drive"]
	};

	public static ExperimentDefinition CreateExp002Short() => new()
	{
		ExperimentId = "EXP-002",
		DisplayName = "Bending Hydraulic Leak Learning (Short)",
		MachineProfileId = "bending-hydraulic-machine-300",
		ScenarioId = "hydraulic-leak",
		ExperimentType = ExperimentType.FaultLearningSeries,
		WarmupDuration = TimeSpan.FromSeconds(10),
		NormalLearningDuration = TimeSpan.FromSeconds(30),
		FaultRunCount = 2,
		ControlRunCount = 1,
		CooldownDuration = TimeSpan.FromSeconds(15),
		TimeFactor = 50.0,
		BaseSeed = 202,
		ControlScenarioIds = ["hydraulic-leak"]
	};

	public static ExperimentDefinition CreateExp003() => new()
	{
		ExperimentId = "EXP-003",
		DisplayName = "Laser Sensor Drift",
		MachineProfileId = "laser-processing-machine-300",
		ScenarioId = "sensor-drift",
		ExperimentType = ExperimentType.MixedSeries,
		FaultRunCount = 2,
		ControlRunCount = 1,
		BaseSeed = 303,
		AdditionalFaultScenarioIds = ["sensor-freeze"]
	};

	public static ExperimentDefinition CreateExp004() => new()
	{
		ExperimentId = "EXP-004",
		DisplayName = "Mixed Laser Faults",
		MachineProfileId = "laser-processing-machine-300",
		ScenarioId = "laser-overheating-axis-drive",
		ExperimentType = ExperimentType.MixedSeries,
		FaultRunCount = 3,
		AdditionalFaultScenarioIds = ["coolant-loss", "sensor-drift"],
		BaseSeed = 404
	};

	public static ExperimentDefinition CreateExp005() => new()
	{
		ExperimentId = "EXP-005",
		DisplayName = "Mixed Bending Faults",
		MachineProfileId = "bending-hydraulic-machine-300",
		ScenarioId = "hydraulic-leak",
		ExperimentType = ExperimentType.MixedSeries,
		FaultRunCount = 3,
		AdditionalFaultScenarioIds = ["oil-aging", "press-force-drop"],
		BaseSeed = 505
	};
}
