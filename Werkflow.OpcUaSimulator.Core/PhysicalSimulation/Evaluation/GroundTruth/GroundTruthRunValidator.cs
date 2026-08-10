using System;
using System.Collections.Generic;
using System.Globalization;
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

	public bool RunSemanticsPassed { get; init; }

	public bool MinimumDurationSatisfied { get; init; }

	public bool ThresholdContinuityPassed { get; init; }
}

public static class GroundTruthRunValidator
{
	private static readonly TimeSpan TickTolerance = TimeSpan.FromMilliseconds(50);

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
		IReadOnlyList<GroundTruthEvent> events,
		bool strictFaultLearningSeries = false)
	{
		var runEvents = events
			.Where(e => e.RunId.Equals(run.RunId, StringComparison.OrdinalIgnoreCase))
			.OrderBy(e => e.ExperimentSimulationTimestamp)
			.ToList();

		var failed = new List<string>();
		bool chronology = ValidateChronology(run, runEvents, failed, strictFaultLearningSeries);
		bool duplicates = ValidateNoDuplicateLifecycle(runEvents, failed);
		bool semantics = ValidateRunSemantics(run, runEvents, failed);
		bool minimumDuration = ValidateMinimumDuration(run, runEvents, failed);
		bool continuity = ValidateThresholdContinuity(run, runEvents, failed);
		bool requirements = ValidateRequirements(run, runEvents, failed, strictFaultLearningSeries);

		return new GroundTruthRunValidationResult
		{
			RunId = run.RunId,
			RunType = run.RunType,
			Passed = chronology && duplicates && semantics && minimumDuration && continuity && requirements,
			FailedCriteria = failed,
			ChronologyPassed = chronology,
			DuplicateLifecycleEventsPassed = duplicates,
			RunSemanticsPassed = semantics && minimumDuration && continuity,
			MinimumDurationSatisfied = minimumDuration,
			ThresholdContinuityPassed = continuity
		};
	}

	public static bool ValidateChronology(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		List<string> failed,
		bool strictFaultLearningSeries = false)
	{
		if (run.RunType == "Normal")
		{
			if (runEvents.Any(e => e.EventType is GroundTruthEventType.ScenarioStarted
				or GroundTruthEventType.MachineFaulted
				or GroundTruthEventType.RecoveryCompleted
				or GroundTruthEventType.RecoveryStarted
				or GroundTruthEventType.ThresholdFirstReached
				or GroundTruthEventType.ThresholdConfirmed))
			{
				failed.Add($"{run.RunId}:normal-has-scenario-or-fault-events");
				return false;
			}
			if (runEvents.Any(e => e.EventType == GroundTruthEventType.ScenarioPhaseChanged))
			{
				failed.Add($"{run.RunId}:normal-has-scenario-phase-events");
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
			if (runEvents.Any(e => e.EventType == GroundTruthEventType.ThresholdConfirmed))
			{
				failed.Add($"{run.RunId}:control-has-threshold-confirmed");
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

		var runStarted = run.RunStartedAt ?? FirstTime(runEvents, GroundTruthEventType.ScenarioStarted);
		var scenarioStart = FirstTime(runEvents, GroundTruthEventType.ScenarioStarted);
		var detectable = FirstTime(runEvents, GroundTruthEventType.DegradationBecameDetectable);
		var thresholdApproaching = FirstTime(runEvents, GroundTruthEventType.ThresholdApproaching);
		var thresholdFirst = FirstTime(runEvents, GroundTruthEventType.ThresholdFirstReached);
		var thresholdConfirmed = FirstTime(runEvents, GroundTruthEventType.ThresholdConfirmed);
		var fault = FirstTime(runEvents, GroundTruthEventType.MachineFaulted);
		var firstFaultedPhase = FirstFaultedPhaseTime(runEvents);
		var recoveryStarted = FirstTime(runEvents, GroundTruthEventType.RecoveryStarted);
		var recovery = FirstTime(runEvents, GroundTruthEventType.RecoveryCompleted);
		var runCompleted = run.RunCompletedAt ?? LastTime(runEvents);

		if (scenarioStart == null)
		{
			failed.Add($"{run.RunId}:missing-scenario-started");
			return false;
		}

		if (runStarted != null && scenarioStart < runStarted - TickTolerance)
		{
			failed.Add($"{run.RunId}:scenario-before-run-started");
			return false;
		}

		if (detectable != null && detectable <= scenarioStart)
		{
			failed.Add($"{run.RunId}:detectable-not-after-scenario-start");
			return false;
		}

		if (strictFaultLearningSeries && detectable == null)
		{
			failed.Add($"{run.RunId}:missing-detectable");
			return false;
		}

		if (thresholdApproaching != null && detectable != null && thresholdApproaching < detectable - TickTolerance)
		{
			failed.Add($"{run.RunId}:threshold-approaching-before-detectable");
			return false;
		}

		if (thresholdFirst != null && detectable != null && thresholdFirst <= detectable)
		{
			failed.Add($"{run.RunId}:threshold-first-not-after-detectable");
			return false;
		}

		if (thresholdConfirmed != null && thresholdFirst != null && thresholdConfirmed <= thresholdFirst)
		{
			failed.Add($"{run.RunId}:threshold-confirmed-not-after-first-reached");
			return false;
		}

		if (fault != null && thresholdConfirmed != null && fault < thresholdConfirmed - TickTolerance)
		{
			failed.Add($"{run.RunId}:fault-before-threshold-confirmed");
			return false;
		}

		if (firstFaultedPhase != null && fault != null && firstFaultedPhase < fault - TickTolerance)
		{
			failed.Add($"{run.RunId}:faulted-phase-before-machine-faulted");
			return false;
		}

		if (recoveryStarted != null && fault != null && recoveryStarted <= fault)
		{
			failed.Add($"{run.RunId}:recovery-started-not-after-fault");
			return false;
		}

		if (recovery != null && recoveryStarted != null && recovery < recoveryStarted - TickTolerance)
		{
			failed.Add($"{run.RunId}:recovery-completed-before-recovery-started");
			return false;
		}

		if (runCompleted != null && recovery != null && runCompleted < recovery - TickTolerance)
		{
			failed.Add($"{run.RunId}:run-completed-before-recovery");
			return false;
		}

		if (strictFaultLearningSeries && detectable != null && fault != null)
		{
			if (detectable >= fault - TickTolerance)
			{
				failed.Add($"{run.RunId}:detectable-not-before-fault");
				return false;
			}
		}

		return true;
	}

	public static bool ValidateMinimumDuration(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		List<string> failed)
	{
		if (run.RunType != "Fault")
		{
			return true;
		}

		var thresholdConfirmed = FirstTime(runEvents, GroundTruthEventType.ThresholdConfirmed);
		if (thresholdConfirmed == null)
		{
			return true;
		}

		var confirmedEvent = runEvents.First(e => e.EventType == GroundTruthEventType.ThresholdConfirmed);
		TimeSpan minimumDuration = run.ThresholdMinimumDuration ?? ParseMinimumDuration(confirmedEvent);
		TimeSpan? streakStart = run.ConfirmedThresholdStreakStartedAt ?? DeriveConfirmedStreakStart(runEvents);
		if (minimumDuration <= TimeSpan.Zero || streakStart == null)
		{
			return true;
		}

		TimeSpan observed = thresholdConfirmed.Value - streakStart.Value;
		if (observed + TickTolerance < minimumDuration)
		{
			failed.Add($"{run.RunId}:minimum-duration-not-satisfied");
			return false;
		}

		return true;
	}

	public static bool ValidateThresholdContinuity(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		List<string> failed)
	{
		if (run.RunType != "Fault")
		{
			return true;
		}

		var thresholdFirst = FirstTime(runEvents, GroundTruthEventType.ThresholdFirstReached);
		var thresholdConfirmed = FirstTime(runEvents, GroundTruthEventType.ThresholdConfirmed);
		var fault = FirstTime(runEvents, GroundTruthEventType.MachineFaulted);
		var confirmedEvent = runEvents.FirstOrDefault(e => e.EventType == GroundTruthEventType.ThresholdConfirmed);
		TimeSpan? streakStart = run.ConfirmedThresholdStreakStartedAt ?? DeriveConfirmedStreakStart(runEvents);

		if (thresholdConfirmed == null || streakStart == null)
		{
			return true;
		}

		if (thresholdFirst != null && streakStart < thresholdFirst - TickTolerance)
		{
			failed.Add($"{run.RunId}:confirmed-streak-before-first-reached");
			return false;
		}

		if (streakStart >= thresholdConfirmed)
		{
			failed.Add($"{run.RunId}:confirmed-streak-not-before-confirmed");
			return false;
		}

		if (fault != null && fault < thresholdConfirmed - TickTolerance)
		{
			failed.Add($"{run.RunId}:fault-before-threshold-confirmed");
			return false;
		}

		var exitInsideStreak = runEvents
			.Where(e => e.EventType == GroundTruthEventType.ThresholdExited)
			.Where(e => e.ExperimentSimulationTimestamp > streakStart.Value - TickTolerance
				&& e.ExperimentSimulationTimestamp < thresholdConfirmed.Value + TickTolerance)
			.ToList();
		if (exitInsideStreak.Count > 0)
		{
			failed.Add($"{run.RunId}:threshold-exit-inside-confirmed-streak");
			return false;
		}

		return true;
	}

	public static bool ValidateRunSemantics(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		List<string> failed)
	{
		if (run.RunType == "Normal")
		{
			if (run.ScenarioStartedAt != null || run.ScenarioStart != null)
			{
				failed.Add($"{run.RunId}:normal-scenario-started-not-null");
				return false;
			}
			if (run.DetectableAt != null || run.ThresholdFirstReachedAt != null || run.ThresholdConfirmedAt != null
				|| run.ThresholdAt != null || run.FaultAt != null || run.RecoveryStartedAt != null
				|| run.RecoveryCompletedAt != null)
			{
				failed.Add($"{run.RunId}:normal-fault-fields-not-null");
				return false;
			}
			return true;
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
		List<string> failed,
		bool strictFaultLearningSeries = false)
	{
		int before = failed.Count;
		switch (run.RunType)
		{
		case "Fault":
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.ScenarioStarted))
			{
				failed.Add($"{run.RunId}:missing-scenario-started");
			}
			if (strictFaultLearningSeries && !runEvents.Any(e => e.EventType == GroundTruthEventType.DegradationBecameDetectable))
			{
				failed.Add($"{run.RunId}:missing-detectable");
			}
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.ThresholdFirstReached))
			{
				failed.Add($"{run.RunId}:missing-threshold-first-reached");
			}
			if (!runEvents.Any(e => e.EventType == GroundTruthEventType.ThresholdConfirmed))
			{
				failed.Add($"{run.RunId}:missing-threshold-confirmed");
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
			if (runEvents.Any(e => e.EventType == GroundTruthEventType.ScenarioPhaseChanged))
			{
				failed.Add($"{run.RunId}:normal-has-scenario-phase-events");
			}
			break;
		}

		return failed.Count == before;
	}

	public static string DeriveOutcome(RunManifestEntry run, IReadOnlyList<GroundTruthEvent> runEvents)
	{
		bool faulted = runEvents.Any(e => e.EventType == GroundTruthEventType.MachineFaulted);
		bool confirmed = runEvents.Any(e => e.EventType == GroundTruthEventType.ThresholdConfirmed);
		bool recovered = runEvents.Any(e => e.EventType == GroundTruthEventType.RecoveryCompleted);

		if (run.RunType == "Normal" || run.RunType == "Control")
		{
			return faulted ? "FaultUnexpected" : "NoFault";
		}

		if (faulted && recovered && confirmed)
		{
			return "FaultRecovered";
		}
		if (faulted && !recovered)
		{
			return "FaultNotRecovered";
		}
		return "NoFaultTriggered";
	}

	public static void PopulateManifestFromEvents(RunManifestEntry run, IReadOnlyList<GroundTruthEvent> runEvents)
	{
		var ordered = runEvents.OrderBy(e => e.ExperimentSimulationTimestamp).ToList();
		if (ordered.Count == 0)
		{
			run.Outcome = DeriveOutcome(run, runEvents);
			return;
		}

		run.RunStartedAt ??= ordered[0].ExperimentSimulationTimestamp;
		run.RunCompletedAt ??= ordered[^1].ExperimentSimulationTimestamp;

		if (run.RunType == "Normal")
		{
			run.ScenarioStartedAt = null;
			run.ScenarioStart = null;
			run.DetectableAt = null;
			run.ThresholdApproachingAt = null;
			run.ThresholdFirstReachedAt = null;
			run.ThresholdConfirmedAt = null;
			run.ThresholdAt = null;
			run.FaultAt = null;
			run.RecoveryStartedAt = null;
			run.RecoveryCompletedAt = null;
			run.RunStartedAt = FirstTime(ordered, GroundTruthEventType.NormalObservationStarted) ?? run.RunStartedAt;
			run.Outcome = DeriveOutcome(run, runEvents);
			return;
		}

		run.ScenarioStartedAt = FirstTime(ordered, GroundTruthEventType.ScenarioStarted);
		run.ScenarioStart = run.ScenarioStartedAt;
		run.DetectableAt = FirstTime(ordered, GroundTruthEventType.DegradationBecameDetectable);
		run.ThresholdApproachingAt = FirstTime(ordered, GroundTruthEventType.ThresholdApproaching);
		run.ThresholdFirstReachedAt = FirstTime(ordered, GroundTruthEventType.ThresholdFirstReached);
		run.ThresholdConfirmedAt = FirstTime(ordered, GroundTruthEventType.ThresholdConfirmed);
		run.ThresholdAt = run.ThresholdFirstReachedAt;
		run.ConfirmedThresholdStreakStartedAt = DeriveConfirmedStreakStart(ordered);
		run.ThresholdEnterCount = ordered.Count(e => e.EventType == GroundTruthEventType.ThresholdEntered);
		run.ThresholdExitCount = ordered.Count(e => e.EventType == GroundTruthEventType.ThresholdExited);
		run.FaultAt = FirstTime(ordered, GroundTruthEventType.MachineFaulted);
		run.RecoveryStartedAt = FirstTime(ordered, GroundTruthEventType.RecoveryStarted);
		run.RecoveryCompletedAt = FirstTime(ordered, GroundTruthEventType.RecoveryCompleted);
		run.FirstFaultedPhaseAt = FirstFaultedPhaseTime(ordered);
		run.ThresholdMinimumDuration = ParseMinimumDuration(
			ordered.FirstOrDefault(e => e.EventType == GroundTruthEventType.ThresholdConfirmed));
		run.Outcome = DeriveOutcome(run, runEvents);
	}

	private static TimeSpan? FirstTime(IReadOnlyList<GroundTruthEvent> events, GroundTruthEventType type)
	{
		var match = events.Where(e => e.EventType == type).Select(e => e.ExperimentSimulationTimestamp).ToList();
		return match.Count > 0 ? match[0] : null;
	}

	private static TimeSpan? LastTime(IReadOnlyList<GroundTruthEvent> events)
	{
		return events.Count > 0 ? events[^1].ExperimentSimulationTimestamp : null;
	}

	private static TimeSpan? FirstFaultedPhaseTime(IReadOnlyList<GroundTruthEvent> events)
	{
		var match = events
			.Where(e => e.EventType == GroundTruthEventType.ScenarioPhaseChanged
				&& e.ScenarioPhase?.Equals("Faulted", StringComparison.OrdinalIgnoreCase) == true)
			.Select(e => e.ExperimentSimulationTimestamp)
			.ToList();
		return match.Count > 0 ? match[0] : null;
	}

	private static TimeSpan ParseMinimumDuration(GroundTruthEvent? confirmedEvent)
	{
		if (confirmedEvent == null)
		{
			return TimeSpan.Zero;
		}

		string? detail = confirmedEvent.Metadata.TryGetValue("detail", out var metaDetail) ? metaDetail : null;
		if (detail != null && detail.Contains('|', StringComparison.Ordinal))
		{
			detail = detail.Split('|')[0];
		}

		if (detail != null && TimeSpan.TryParse(detail, CultureInfo.InvariantCulture, out var parsed))
		{
			return parsed;
		}

		return TimeSpan.Zero;
	}

	private static TimeSpan? DeriveConfirmedStreakStart(IReadOnlyList<GroundTruthEvent> events)
	{
		var confirmed = events.FirstOrDefault(e => e.EventType == GroundTruthEventType.ThresholdConfirmed);
		if (confirmed == null)
		{
			return null;
		}

		TimeSpan confirmedTime = confirmed.ExperimentSimulationTimestamp;
		var enters = events
			.Where(e => e.EventType == GroundTruthEventType.ThresholdEntered
				&& e.ExperimentSimulationTimestamp < confirmedTime)
			.OrderBy(e => e.ExperimentSimulationTimestamp)
			.ToList();
		if (enters.Count == 0)
		{
			return ParseConfirmedStreakStart(confirmed);
		}

		var exits = events
			.Where(e => e.EventType == GroundTruthEventType.ThresholdExited
				&& e.ExperimentSimulationTimestamp < confirmedTime)
			.OrderBy(e => e.ExperimentSimulationTimestamp)
			.ToList();
		TimeSpan? lastExit = exits.Count > 0 ? exits[^1].ExperimentSimulationTimestamp : null;
		var streakEnter = enters.LastOrDefault(e => lastExit == null || e.ExperimentSimulationTimestamp > lastExit);
		return streakEnter?.ExperimentSimulationTimestamp ?? ParseConfirmedStreakStart(confirmed);
	}

	private static TimeSpan? ParseConfirmedStreakStart(GroundTruthEvent? confirmedEvent)
	{
		if (confirmedEvent == null)
		{
			return null;
		}

		if (confirmedEvent.Metadata.TryGetValue("detail", out var detail)
			&& detail.Contains('|', StringComparison.Ordinal))
		{
			string streakPart = detail.Split('|')[1];
			if (TimeSpan.TryParse(streakPart, CultureInfo.InvariantCulture, out var scenarioStreak))
			{
				var scenarioStart = confirmedEvent.ExperimentSimulationTimestamp;
				return scenarioStart - scenarioStreak > TimeSpan.Zero
					? confirmedEvent.ExperimentSimulationTimestamp - (confirmedEvent.ExperimentSimulationTimestamp - scenarioStart)
					: null;
			}
		}

		return null;
	}
}
