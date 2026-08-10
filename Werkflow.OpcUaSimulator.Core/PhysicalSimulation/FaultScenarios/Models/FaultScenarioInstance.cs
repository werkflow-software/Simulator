using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioInstance
{
	public Guid InstanceId { get; init; } = Guid.NewGuid();

	public required Guid MachineId { get; init; }

	public required string ScenarioId { get; init; }

	public required FaultScenarioDefinition Definition { get; init; }

	public FaultScenarioLifecycleState LifecycleState { get; set; } = FaultScenarioLifecycleState.Created;

	public FaultScenarioPhase CurrentPhase { get; set; } = FaultScenarioPhase.Dormant;

	public FaultScenarioRunMode RunMode { get; set; } = FaultScenarioRunMode.Normal;

	public double Intensity { get; set; } = 1.0;

	public double TimeFactor { get; set; } = 1.0;

	public int Seed { get; set; }

	public bool AutoThresholdFaultEnabled { get; set; } = true;

	public bool AutoScenarioEndEnabled { get; set; } = true;

	public DateTimeOffset StartedAt { get; set; }

	public DateTimeOffset? PausedAt { get; set; }

	public TimeSpan PausedAccumulated { get; set; }

	public TimeSpan ScenarioElapsedTime { get; set; }

	public TimeSpan RecoveryElapsedTime { get; set; }

	public double RecoveryProgress { get; set; }

	public bool ThresholdFaultTriggered { get; set; }

	public string? ActiveFaultCode { get; set; }

	public Dictionary<string, double> EffectAccumulators { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> PreviousSourceValues { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> FrozenSignalValues { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> NoiseModifiers { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> HiddenStateOffsets { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> SignalOffsets { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public TimeSpan RecoveryStableElapsedTime { get; set; }

	public TimeSpan LastScenarioDeltaTime { get; set; }

	public double OscillationPhase { get; set; }

	public double IntermittentPhase { get; set; }

	public int IntermittentEpisodeCount { get; set; }

	public bool IsIntermittentPulseActive { get; set; }

	public DateTimeOffset? NextPhaseChangeAt { get; set; }

	public DateTimeOffset? ThresholdConditionStartedAt { get; set; }

	public TimeSpan? ThresholdConditionStartedSimulationTime { get; set; }

	public DateTimeOffset? ThresholdFirstReachedAtUtc { get; set; }

	public DateTimeOffset? ThresholdConfirmedAtUtc { get; set; }

	public DateTimeOffset? MachineFaultedAtUtc { get; set; }

	public DateTimeOffset? RecoveryStartedAtUtc { get; set; }

	public DateTimeOffset? RecoveryCompletedAtUtc { get; set; }

	public double? ThresholdValueAtFirstReached { get; set; }

	public double? ThresholdValueAtConfirmed { get; set; }

	public string? ActiveThresholdRuleId { get; set; }
}
