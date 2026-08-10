using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;

public enum EvaluationWindowType
{
	NormalWindow,
	PreDetectableWindow,
	DetectablePreFaultWindow,
	PreThresholdWindow,
	FaultWindow,
	RecoveryWindow
}

public sealed class EvaluationMetrics
{
	public bool VigilEvaluationAvailable { get; init; }

	public EvidenceType EvidenceType { get; init; } = EvidenceType.NotAvailable;

	public string RealVigilLearningEvaluation { get; init; } = "NotExecuted";

	public int FaultCount { get; init; }

	public int DetectedFaultCount { get; init; }

	public int MissedFaultCount { get; init; }

	public double? DetectionRate { get; init; }

	public int WarningCount { get; init; }

	public int TruePositiveWarnings { get; init; }

	public int FalsePositiveWarnings { get; init; }

	public double? FalsePositiveRate { get; init; }

	public TimeSpan? MeanLeadTime { get; init; }

	public TimeSpan? MedianLeadTime { get; init; }

	public TimeSpan? MinLeadTime { get; init; }

	public TimeSpan? MaxLeadTime { get; init; }

	public int ControlRunCount { get; init; }

	public int ControlRunsWithWarning { get; init; }

	public TimeSpan NormalDuration { get; init; }

	public double? WarningsPerNormalHour { get; init; }

	public List<RepetitionLeadTimeMetric> PerRepetitionLeadTimes { get; init; } = [];
}

public sealed class RepetitionLeadTimeMetric
{
	public int RepetitionIndex { get; init; }

	public TimeSpan? LeadTime { get; init; }

	public double? Confidence { get; init; }

	public int WarningCount { get; init; }

	public bool Detected { get; init; }
}
