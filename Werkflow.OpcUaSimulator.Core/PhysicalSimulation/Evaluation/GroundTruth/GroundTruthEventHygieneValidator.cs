using System;
using System.Collections.Generic;
using System.Linq;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class ThresholdEventHygieneResult
{
	public required string RunId { get; init; }

	public int ThresholdApproachingCount { get; init; }

	public int ThresholdEnteredCount { get; init; }

	public int ThresholdExitedCount { get; init; }

	public int ThresholdConfirmedCount { get; init; }

	public int DuplicateApproachingCount { get; init; }

	public int InvalidTransitionCount { get; init; }

	public bool EventStateReconstructionPassed { get; init; }

	public bool Passed { get; init; }
}

public static class GroundTruthEventHygieneValidator
{
	private enum ReplayState
	{
		Normal,
		Approaching,
		Satisfied,
		Confirmed
	}

	private static readonly TimeSpan ApproachingDuplicateGap = TimeSpan.FromSeconds(2);

	public static ThresholdEventHygieneResult ValidateRun(string runId, IReadOnlyList<GroundTruthEvent> events)
	{
		var runEvents = events
			.Where(e => e.RunId.Equals(runId, StringComparison.OrdinalIgnoreCase))
			.OrderBy(e => e.ExperimentSimulationTimestamp)
			.ToList();

		int approachingCount = runEvents.Count(e => e.EventType == GroundTruthEventType.ThresholdApproaching);
		int enteredCount = runEvents.Count(e => e.EventType == GroundTruthEventType.ThresholdEntered);
		int exitedCount = runEvents.Count(e => e.EventType == GroundTruthEventType.ThresholdExited);
		int confirmedCount = runEvents.Count(e => e.EventType == GroundTruthEventType.ThresholdConfirmed);
		int firstReachedCount = runEvents.Count(e => e.EventType == GroundTruthEventType.ThresholdFirstReached);

		var ruleIds = runEvents
			.Where(e => e.EventType is GroundTruthEventType.ThresholdApproaching
				or GroundTruthEventType.ThresholdEntered
				or GroundTruthEventType.ThresholdExited
				or GroundTruthEventType.ThresholdConfirmed)
			.Select(ResolveRuleId)
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		int duplicateApproaching = 0;
		int invalidTransitions = 0;

		if (firstReachedCount > 1)
		{
			invalidTransitions++;
		}

		if (ruleIds.Count == 0)
		{
			(duplicateApproaching, invalidTransitions) = ReplayThresholdEvents(
				runEvents.Where(e => IsThresholdReplayEvent(e.EventType)).ToList(),
				duplicateApproaching,
				invalidTransitions);
		}
		else
		{
			foreach (string ruleId in ruleIds)
			{
				var ruleEvents = runEvents
					.Where(e => IsThresholdReplayEvent(e.EventType) && RuleIdMatches(e, ruleId))
					.ToList();
				(duplicateApproaching, invalidTransitions) = ReplayThresholdEvents(
					ruleEvents,
					duplicateApproaching,
					invalidTransitions);
			}
		}

		bool reconstructionPassed = duplicateApproaching == 0 && invalidTransitions == 0;
		bool passed = reconstructionPassed && firstReachedCount <= 1;

		return new ThresholdEventHygieneResult
		{
			RunId = runId,
			ThresholdApproachingCount = approachingCount,
			ThresholdEnteredCount = enteredCount,
			ThresholdExitedCount = exitedCount,
			ThresholdConfirmedCount = confirmedCount,
			DuplicateApproachingCount = duplicateApproaching,
			InvalidTransitionCount = invalidTransitions,
			EventStateReconstructionPassed = reconstructionPassed,
			Passed = passed
		};
	}

	private static (int DuplicateApproaching, int InvalidTransitions) ReplayThresholdEvents(
		IReadOnlyList<GroundTruthEvent> thresholdEvents,
		int duplicateApproaching,
		int invalidTransitions)
	{
		ReplayState state = ReplayState.Normal;
		TimeSpan? lastApproachingAt = null;

		foreach (GroundTruthEvent evt in thresholdEvents)
		{
			switch (evt.EventType)
			{
			case GroundTruthEventType.ThresholdApproaching:
				if (lastApproachingAt != null
					&& evt.ExperimentSimulationTimestamp - lastApproachingAt < ApproachingDuplicateGap)
				{
					duplicateApproaching++;
				}
				lastApproachingAt = evt.ExperimentSimulationTimestamp;

				if (state is ReplayState.Satisfied or ReplayState.Confirmed)
				{
					invalidTransitions++;
				}
				state = ReplayState.Approaching;
				break;

			case GroundTruthEventType.ThresholdEntered:
				if (state is ReplayState.Satisfied or ReplayState.Confirmed)
				{
					invalidTransitions++;
				}
				state = ReplayState.Satisfied;
				break;

			case GroundTruthEventType.ThresholdExited:
				if (state != ReplayState.Satisfied)
				{
					invalidTransitions++;
				}
				state = ReplayState.Normal;
				break;

			case GroundTruthEventType.ThresholdConfirmed:
				if (state != ReplayState.Satisfied)
				{
					invalidTransitions++;
				}
				state = ReplayState.Confirmed;
				break;
			}
		}

		return (duplicateApproaching, invalidTransitions);
	}

	private static bool IsThresholdReplayEvent(GroundTruthEventType type) => type is
		GroundTruthEventType.ThresholdApproaching
		or GroundTruthEventType.ThresholdEntered
		or GroundTruthEventType.ThresholdExited
		or GroundTruthEventType.ThresholdConfirmed
		or GroundTruthEventType.ThresholdFirstReached;

	private static string? ResolveRuleId(GroundTruthEvent evt)
	{
		if (evt.Metadata.TryGetValue("detail", out var detail) && !string.IsNullOrWhiteSpace(detail))
		{
			return detail;
		}

		return null;
	}

	private static bool RuleIdMatches(GroundTruthEvent evt, string ruleId)
	{
		string? eventRuleId = ResolveRuleId(evt);
		return eventRuleId != null && eventRuleId.Equals(ruleId, StringComparison.OrdinalIgnoreCase);
	}
}
