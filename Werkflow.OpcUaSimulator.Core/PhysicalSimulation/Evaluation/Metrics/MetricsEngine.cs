using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;

public sealed class MetricsEngine
{
	public TimeSpan MaximumPredictionHorizon { get; init; } = TimeSpan.FromMinutes(30);

	public EvaluationMetrics Compute(
		IReadOnlyList<GroundTruthEvent> groundTruthEvents,
		IReadOnlyList<VigilEvent> vigilEvents,
		IReadOnlyList<RunManifestEntry> runs,
		bool vigilConnected,
		EvidenceType evidenceType)
	{
		if (!vigilConnected || vigilEvents.Count == 0)
		{
			return new EvaluationMetrics
			{
				VigilEvaluationAvailable = false,
				EvidenceType = EvidenceType.NotAvailable,
				RealVigilLearningEvaluation = "NotExecuted",
				FaultCount = runs.Count(r => r.RunType == "Fault"),
				ControlRunCount = runs.Count(r => r.RunType == "Control"),
				NormalDuration = ComputeNormalDuration(groundTruthEvents, runs)
			};
		}

		var warnings = vigilEvents
			.Where(e => e.EventType is VigilEventType.Warning or VigilEventType.Anomaly or VigilEventType.Prediction)
			.OrderBy(e => e.SimulationTimestamp)
			.ToList();

		var faultRuns = runs.Where(r => r.RunType == "Fault").ToList();
		var normalRuns = runs.Where(r => r.RunType == "Normal").ToList();
		var controlRuns = runs.Where(r => r.RunType == "Control").ToList();

		var matchedWarnings = new HashSet<string>();
		var truePositives = new List<(RunManifestEntry run, VigilEvent warning, TimeSpan lead)>();
		var missed = new List<RunManifestEntry>();

		foreach (var run in faultRuns)
		{
			var faultAt = run.FaultAt ?? FindEventTime(groundTruthEvents, run.RunId, GroundTruthEventType.MachineFaulted);
			if (faultAt == null)
			{
				continue;
			}

			var runWarnings = warnings
				.Where(w => w.RunId.Equals(run.RunId, StringComparison.OrdinalIgnoreCase)
					|| string.IsNullOrEmpty(w.RunId))
				.Where(w => w.SimulationTimestamp <= faultAt.Value)
				.Where(w => faultAt.Value - w.SimulationTimestamp <= MaximumPredictionHorizon)
				.OrderBy(w => w.SimulationTimestamp)
				.ToList();

			var firstValid = runWarnings.FirstOrDefault(w => !matchedWarnings.Contains(w.EventId));
			if (firstValid != null)
			{
				matchedWarnings.Add(firstValid.EventId);
				truePositives.Add((run, firstValid, faultAt.Value - firstValid.SimulationTimestamp));
			}
			else
			{
				missed.Add(run);
			}
		}

		var falsePositives = warnings
			.Where(w => !matchedWarnings.Contains(w.EventId))
			.Where(w => IsFalsePositive(w, groundTruthEvents, runs, MaximumPredictionHorizon))
			.ToList();

		var leadTimes = truePositives.Select(tp => tp.lead).ToList();
		var perRep = faultRuns
			.GroupBy(r => r.RepetitionIndex)
			.Select(g =>
			{
				var match = truePositives.FirstOrDefault(tp => tp.run.RepetitionIndex == g.Key);
				return new RepetitionLeadTimeMetric
				{
					RepetitionIndex = g.Key,
					LeadTime = match.warning != null ? match.lead : null,
					Confidence = match.warning?.Confidence,
					WarningCount = warnings.Count(w => w.RunId == g.First().RunId),
					Detected = match.warning != null
				};
			})
			.OrderBy(r => r.RepetitionIndex)
			.ToList();

		TimeSpan normalDuration = TimeSpan.FromTicks(
			normalRuns.Sum(r => (r.RecoveryCompletedAt ?? r.FaultAt ?? TimeSpan.Zero).Ticks));

		return new EvaluationMetrics
		{
			VigilEvaluationAvailable = true,
			EvidenceType = evidenceType,
			RealVigilLearningEvaluation = evidenceType == EvidenceType.RealVigilEvidence ? "Executed" : "NotExecuted",
			FaultCount = faultRuns.Count,
			DetectedFaultCount = truePositives.Count,
			MissedFaultCount = missed.Count,
			DetectionRate = faultRuns.Count > 0 ? (double)truePositives.Count / faultRuns.Count : null,
			WarningCount = warnings.Count,
			TruePositiveWarnings = truePositives.Count,
			FalsePositiveWarnings = falsePositives.Count,
			FalsePositiveRate = warnings.Count > 0 ? (double)falsePositives.Count / warnings.Count : null,
			MeanLeadTime = AverageLead(leadTimes),
			MedianLeadTime = MedianLead(leadTimes),
			MinLeadTime = leadTimes.Count > 0 ? leadTimes.Min() : null,
			MaxLeadTime = leadTimes.Count > 0 ? leadTimes.Max() : null,
			ControlRunCount = controlRuns.Count,
			ControlRunsWithWarning = controlRuns.Count(c =>
				warnings.Any(w => w.RunId.Equals(c.RunId, StringComparison.OrdinalIgnoreCase))),
			NormalDuration = normalDuration,
			WarningsPerNormalHour = normalDuration.TotalHours > 0
				? falsePositives.Count(w => normalRuns.Any(n => n.RunId.Equals(w.RunId, StringComparison.OrdinalIgnoreCase)))
					/ normalDuration.TotalHours
				: null,
			PerRepetitionLeadTimes = perRep
		};
	}

	private static bool IsFalsePositive(
		VigilEvent warning,
		IReadOnlyList<GroundTruthEvent> groundTruth,
		IReadOnlyList<RunManifestEntry> runs,
		TimeSpan horizon)
	{
		var run = runs.FirstOrDefault(r => r.RunId.Equals(warning.RunId, StringComparison.OrdinalIgnoreCase));
		if (run == null)
		{
			return true;
		}

		if (run.RunType == "Fault")
		{
			var faultAt = run.FaultAt ?? FindEventTime(groundTruth, run.RunId, GroundTruthEventType.MachineFaulted);
			if (faultAt.HasValue && warning.SimulationTimestamp <= faultAt.Value
				&& faultAt.Value - warning.SimulationTimestamp <= horizon)
			{
				return false;
			}
		}

		return run.RunType is "Normal" or "Control";
	}

	private static TimeSpan? FindEventTime(
		IReadOnlyList<GroundTruthEvent> events,
		string runId,
		GroundTruthEventType type)
	{
		return events
			.Where(e => e.RunId.Equals(runId, StringComparison.OrdinalIgnoreCase) && e.EventType == type)
			.Select(e => e.SimulationTimestamp)
			.FirstOrDefault();
	}

	private static TimeSpan? AverageLead(List<TimeSpan> leads) =>
		leads.Count == 0 ? null : TimeSpan.FromTicks((long)leads.Average(l => l.Ticks));

	private static TimeSpan? MedianLead(List<TimeSpan> leads)
	{
		if (leads.Count == 0)
		{
			return null;
		}
		var sorted = leads.OrderBy(l => l).ToList();
		return sorted[sorted.Count / 2];
	}

	private static TimeSpan ComputeNormalDuration(
		IReadOnlyList<GroundTruthEvent> groundTruthEvents,
		IReadOnlyList<RunManifestEntry> runs)
	{
		var normalRun = runs.FirstOrDefault(r => r.RunType == "Normal");
		if (normalRun == null)
		{
			return TimeSpan.Zero;
		}

		if (normalRun.RecoveryCompletedAt.HasValue && normalRun.ScenarioStart.HasValue)
		{
			return normalRun.RecoveryCompletedAt.Value - normalRun.ScenarioStart.Value;
		}

		var runEvents = groundTruthEvents
			.Where(e => e.RunId.Equals(normalRun.RunId, StringComparison.OrdinalIgnoreCase))
			.ToList();
		var start = runEvents
			.Where(e => e.EventType == GroundTruthEventType.NormalObservationStarted)
			.Select(e => e.ExperimentSimulationTimestamp)
			.FirstOrDefault();
		if (start <= TimeSpan.Zero)
		{
			return TimeSpan.Zero;
		}

		var last = runEvents.Max(e => e.ExperimentSimulationTimestamp);
		return last > start ? last - start : TimeSpan.Zero;
	}
}

public sealed class RunManifestEntry
{
	public required string RunId { get; init; }

	public required string RunType { get; init; }

	public int RunSeed { get; init; }

	public int RepetitionIndex { get; init; }

	public double Intensity { get; init; }

	public TimeSpan? ScenarioStart { get; set; }

	public TimeSpan? DetectableAt { get; set; }

	public TimeSpan? ThresholdAt { get; set; }

	public TimeSpan? FaultAt { get; set; }

	public TimeSpan? RecoveryCompletedAt { get; set; }

	public string Outcome { get; set; } = "";
}
