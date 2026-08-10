using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class GroundTruthRunValidationResult
{
	public required string RunId { get; init; }

	public required string RunType { get; init; }

	public bool Passed { get; init; }

	public List<string> FailedCriteria { get; init; } = [];

	public bool ChronologyPassed { get; init; }

	public bool DuplicateLifecycleEventsPassed { get; init; }
}

public static class GroundTruthRunValidator
{
	private static readonly GroundTruthEventType[] UniqueLifecycleTypes =
	[
		GroundTruthEventType.ScenarioStarted,
		GroundTruthEventType.DegradationBecameDetectable,
		GroundTruthEventType.ThresholdFirstReached,
		GroundTruthEventType.ThresholdConfirmed,
		GroundTruthEventType.MachineFaulted,
		GroundTruthEventType.RecoveryStarted,
		GroundTruthEventType.RecoveryCompleted,
		GroundTruthEventType.ScenarioStopped
	];

	public static GroundTruthRunValidationResult ValidateRun(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> events)
	{
		var runEvents = events
			.Where(e => e.RunId.Equals(run.RunId, StringComparison.OrdinalIgnoreCase))
			.OrderBy(e => e.ExperimentSimulationTimestamp)
			.ToList();

		var failed = new List<string>();
		bool chronology = ValidateChronology(run, runEvents, failed);
		bool duplicates = ValidateNoDuplicateLifecycle(runEvents, failed);
		bool requirements = ValidateRequirements(run, runEvents, failed);

		return new GroundTruthRunValidationResult
		{
			RunId = run.RunId,
			RunType = run.RunType,
			Passed = chronology && duplicates && requirements,
			FailedCriteria = failed,
			ChronologyPassed = chronology,
			DuplicateLifecycleEventsPassed = duplicates
		};
	}

	public static bool ValidateChronology(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		List<string> failed)
	{
		if (run.RunType == "Normal")
		{
			if (runEvents.Any(e => e.EventType is GroundTruthEventType.ScenarioStarted
				or GroundTruthEventType.MachineFaulted
				or GroundTruthEventType.RecoveryCompleted))
			{
				failed.Add($"{run.RunId}:normal-has-scenario-or-fault-events");
				return false;
			}
			return true;
		}

		if (run.RunType == "Control")
		{
			if (runEvents.Any(e => e.EventType == GroundTruthEventType.MachineFaulted))
			{
				failed.Add($"{run.RunId}:control-has-machine-faulted");
				return false;
			}
			if (runEvents.Any(e => e.ScenarioPhase?.Equals("Faulted", StringComparison.OrdinalIgnoreCase) == true))
			{
				failed.Add($"{run.RunId}:control-has-faulted-phase");
				return false;
			}
			return true;
		}

		if (run.RunType != "Fault")
		{
			return true;
		}

		var scenarioStart = FirstTime(runEvents, GroundTruthEventType.ScenarioStarted);
		var detectable = FirstTime(runEvents, GroundTruthEventType.DegradationBecameDetectable);
		var threshold = FirstTime(runEvents, GroundTruthEventType.ThresholdFirstReached);
		var fault = FirstTime(runEvents, GroundTruthEventType.MachineFaulted);
		var recovery = FirstTime(runEvents, GroundTruthEventType.RecoveryCompleted);

		if (scenarioStart == null)
		{
			failed.Add($"{run.RunId}:missing-scenario-started");
			return false;
		}

		if (detectable != null && detectable < scenarioStart)
		{
			failed.Add($"{run.RunId}:detectable-before-scenario-start");
			return false;
		}

		if (threshold != null && detectable != null && threshold < detectable)
		{
			failed.Add($"{run.RunId}:threshold-before-detectable");
			return false;
		}

		if (fault != null && threshold != null && fault < threshold)
		{
			failed.Add($"{run.RunId}:fault-before-threshold");
			return false;
		}

		if (recovery != null && fault != null && recovery < fault)
		{
			failed.Add($"{run.RunId}:recovery-before-fault");
			return false;
		}

		return true;
	}

	public static bool ValidateNoDuplicateLifecycle(IReadOnlyList<GroundTruthEvent> runEvents, List<string> failed)
	{
		foreach (var type in UniqueLifecycleTypes)
		{
			int count = runEvents.Count(e => e.EventType == type);
			if (count > 1)
			{
				failed.Add($"duplicate-{type}");
				return false;
			}
		}
		return true;
	}

	public static bool ValidateRequirements(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		List<string> failed)
	{
		switch (run.RunType)
		{
		case "Fault":
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.ScenarioStarted))
			{
				failed.Add($"{run.RunId}:missing-scenario-started");
			}
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.DegradationBecameDetectable))
			{
				failed.Add($"{run.RunId}:missing-detectable");
			}
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.ThresholdFirstReached))
			{
				failed.Add($"{run.RunId}:missing-threshold");
			}
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.MachineFaulted))
			{
				failed.Add($"{run.RunId}:missing-machine-faulted");
			}
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.RecoveryCompleted))
			{
				failed.Add($"{run.RunId}:missing-recovery-completed");
			}
			break;
		case "Control":
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.ScenarioStarted))
			{
				failed.Add($"{run.RunId}:missing-scenario-started");
			}
			if (!runEvents.Any(e => e.EventType is GroundTruthEventType.ScenarioPhaseChanged
				or GroundTruthEventType.DegradationBecameDetectable))
			{
				failed.Add($"{run.RunId}:missing-measurable-deviation");
			}
			if (!runEvents.Any(e => e.EventType is GroundTruthEventType.ScenarioStopped
				or GroundTruthEventType.RecoveryCompleted))
			{
				failed.Add($"{run.RunId}:missing-completed");
			}
			break;
		case "Normal":
			if (runEvents.Any(e => e.EventType is GroundTruthEventType.ScenarioStarted
				or GroundTruthEventType.MachineFaulted))
			{
				failed.Add($"{run.RunId}:normal-has-fault-events");
			}
			break;
		}

		return failed.Count == 0;
	}

	public static string DeriveOutcome(RunManifestEntry run, IReadOnlyList<GroundTruthEvent> runEvents)
	{
		bool faulted = runEvents.Any(e => e.EventType == GroundTruthEventType.MachineFaulted);
		bool recovered = runEvents.Any(e => e.EventType == GroundTruthEventType.RecoveryCompleted);

		if (run.RunType == "Normal" || run.RunType == "Control")
		{
			return faulted ? "FaultUnexpected" : "NoFault";
		}

		if (faulted && recovered)
		{
			return "FaultRecovered";
		}
		if (faulted)
		{
			return "FaultIncomplete";
		}
		return "NoFaultTriggered";
	}

	public static void PopulateManifestFromEvents(RunManifestEntry run, IReadOnlyList<GroundTruthEvent> runEvents)
	{
		if (run.RunType == "Normal")
		{
			run.Outcome = DeriveOutcome(run, runEvents);
			run.ScenarioStart ??= FirstTime(runEvents, GroundTruthEventType.NormalObservationStarted);
			if (run.RecoveryCompletedAt == null && run.ScenarioStart.HasValue && runEvents.Count > 0)
			{
				var last = runEvents.Max(e => e.ExperimentSimulationTimestamp);
				if (last > run.ScenarioStart.Value)
				{
					run.RecoveryCompletedAt = last;
				}
			}
			return;
		}

		run.ScenarioStart = FirstTime(runEvents, GroundTruthEventType.ScenarioStarted);
		run.DetectableAt = FirstTime(runEvents, GroundTruthEventType.DegradationBecameDetectable);
		run.ThresholdAt = FirstTime(runEvents, GroundTruthEventType.ThresholdFirstReached);
		run.FaultAt = FirstTime(runEvents, GroundTruthEventType.MachineFaulted);
		run.RecoveryCompletedAt = FirstTime(runEvents, GroundTruthEventType.RecoveryCompleted);
		run.Outcome = DeriveOutcome(run, runEvents);
	}

	private static TimeSpan? FirstTime(IReadOnlyList<GroundTruthEvent> events, GroundTruthEventType type)
	{
		var match = events
			.Where(e => e.EventType == type)
			.Select(e => e.ExperimentSimulationTimestamp)
			.ToList();
		return match.Count > 0 ? match[0] : null;
	}
}
