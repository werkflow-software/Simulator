using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public sealed class ExperimentDefinition
{
	public required string ExperimentId { get; init; }

	public string DisplayName { get; init; } = "";

	public string ExperimentVersion { get; init; } = "1.0";

	public required string MachineProfileId { get; init; }

	public required string ScenarioId { get; init; }

	public ExperimentType ExperimentType { get; init; } = ExperimentType.FaultLearningSeries;

	public TimeSpan WarmupDuration { get; init; } = TimeSpan.FromMinutes(1);

	public TimeSpan NormalLearningDuration { get; init; } = TimeSpan.FromMinutes(5);

	public int FaultRunCount { get; init; } = 3;

	public int ControlRunCount { get; init; } = 1;

	public TimeSpan RecoveryDuration { get; init; } = TimeSpan.FromMinutes(4);

	public TimeSpan CooldownDuration { get; init; } = TimeSpan.FromMinutes(1);

	public double TimeFactor { get; init; } = 25.0;

	public int BaseSeed { get; init; } = 42;

	public ExperimentVariationDefinition Variation { get; init; } = new();

	public bool StopOnFault { get; init; } = true;

	public bool ResetBetweenRuns { get; init; } = true;

	public bool ShuffleRuns { get; init; } = false;

	public VigilMode VigilMode { get; init; } = VigilMode.GroundTruthOnly;

	public string[] ControlScenarioIds { get; init; } = [];

	public string[] AdditionalFaultScenarioIds { get; init; } = [];
}
