using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioDefinition
{
	public string ScenarioId { get; set; } = string.Empty;

	public string ScenarioVersion { get; set; } = "1.0";

	public string DisplayName { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public List<string> MachineProfileIds { get; set; } = new List<string>();

	public FaultScenarioCategory Category { get; set; }

	public FaultScenarioSeverity Severity { get; set; }

	public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5.0);

	public TimeSpan MinimumDuration { get; set; } = TimeSpan.FromSeconds(30.0);

	public TimeSpan MaximumDuration { get; set; } = TimeSpan.FromMinutes(30.0);

	public double DefaultIntensity { get; set; } = 1.0;

	public double MinimumIntensity { get; set; } = 0.25;

	public double MaximumIntensity { get; set; } = 1.5;

	public List<FaultScenarioPhaseTiming> Phases { get; set; } = new List<FaultScenarioPhaseTiming>();

	public List<FaultEffectDefinition> Effects { get; set; } = new List<FaultEffectDefinition>();

	public List<FaultThresholdRule> ThresholdRules { get; set; } = new List<FaultThresholdRule>();

	public FaultRecoveryDefinition Recovery { get; set; } = new FaultRecoveryDefinition();

	public bool CanRunInParallel { get; set; } = true;

	public List<string> MutuallyExclusiveScenarioIds { get; set; } = new List<string>();

	public string? RequiredMachinePhase { get; set; }

	public List<string> AllowedMachinePhases { get; set; } = new List<string>();

	public bool IsEnabled { get; set; } = true;

	public List<string> Tags { get; set; } = new List<string>();

	public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

	public bool SupportsNonFaultingControlRun { get; set; }

	public int Priority { get; set; } = 5;
}
