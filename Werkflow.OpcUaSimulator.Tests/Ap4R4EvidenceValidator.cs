using System.Globalization;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Tests;

internal static class Ap4R4EvidenceValidator
{
    public const int MinimumPostRecoverySamples = 5;
    public const int MinimumSensorDriftSamples = 40;
    public const int MinimumSensorDistinctValues = 10;
    public const double MinimumSensorBiasDelta = 0.5;
    public const double SensorDriftHiddenMaxDelta = 0.25;

    public static Ap4R4PassEvaluation ValidateLaserRecovery(Ap4R4RecoveryCaseResult report)
    {
        var failed = new List<string>();
        failed.AddRange(ValidateRecoveryTimeline(report).FailedCriteria);
        failed.AddRange(ValidateRequiredDirectionChecks(report.FaultDirectionChecks, "fault-direction").FailedCriteria);
        failed.AddRange(ValidateRequiredDirectionChecks(report.RecoveryDirectionChecks, "recovery-direction").FailedCriteria);
        failed.AddRange(ValidateRecoverySafety(report).FailedCriteria);
        failed.AddRange(ValidatePostRecoverySafety(report).FailedCriteria);

        return new Ap4R4PassEvaluation
        {
            Passed = failed.Count == 0,
            FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    public static Ap4R4PassEvaluation ValidateHydraulicRecovery(Ap4R4RecoveryCaseResult report)
    {
        var failed = new List<string>();
        failed.AddRange(ValidateRecoveryTimeline(report).FailedCriteria);
        failed.AddRange(ValidateRequiredDirectionChecks(report.FaultDirectionChecks, "fault-direction").FailedCriteria);
        failed.AddRange(ValidateRequiredDirectionChecks(report.RecoveryDirectionChecks, "recovery-direction").FailedCriteria);
        failed.AddRange(ValidateRecoverySafety(report).FailedCriteria);
        failed.AddRange(ValidatePostRecoverySafety(report).FailedCriteria);

        var keyRecoveryChecks = report.RecoveryDirectionChecks
            .Where(c => c.Required && (c.SignalId == "HydraulicEfficiency" || c.SignalId == "Hydraulic.SupplyPressure"))
            .ToList();
        if (!report.DistanceToNormal.RecoveryImproved
            && (keyRecoveryChecks.Count == 0 || keyRecoveryChecks.Any(c => !c.Passed)))
        {
            failed.Add("distance-to-normal-not-improved");
        }

        return new Ap4R4PassEvaluation
        {
            Passed = failed.Count == 0,
            FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    public static Ap4R4PassEvaluation ValidateSensorDrift(Ap4R4SensorDriftResult report)
    {
        var failed = new List<string>();

        if (report.SensorSampleCount < MinimumSensorDriftSamples)
        {
            failed.Add($"sensor-drift-sample-count:{report.SensorSampleCount}");
        }

        if (report.DistinctValues < MinimumSensorDistinctValues)
        {
            failed.Add($"sensor-drift-distinct-values:{report.DistinctValues}");
        }

        if (Math.Abs(report.BiasDelta) < MinimumSensorBiasDelta)
        {
            failed.Add($"sensor-drift-bias-delta:{report.BiasDelta:F3}");
        }

        if (report.HiddenDelta == null)
        {
            failed.Add("sensor-drift-hidden-not-stable");
        }
        else if (Math.Abs(report.BiasDelta) > MinimumSensorBiasDelta
            && Math.Abs(report.HiddenDelta.Value) >= Math.Abs(report.BiasDelta) * 0.45)
        {
            failed.Add("sensor-drift-hidden-not-stable");
        }
        else if (Math.Abs(report.HiddenDelta.Value) > SensorDriftHiddenMaxDelta
            && Math.Abs(report.BiasDelta) <= MinimumSensorBiasDelta)
        {
            failed.Add("sensor-drift-hidden-not-stable");
        }

        if (report.RedundantDelta != null
            && Math.Abs(report.BiasDelta) > 0.5
            && Math.Abs(report.RedundantDelta.Value - report.BiasDelta) < 0.15)
        {
            failed.Add("redundant-follows-artificial-drift");
        }

        return new Ap4R4PassEvaluation
        {
            Passed = failed.Count == 0,
            FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    public static Ap4R4PassEvaluation ValidateSignalFreezeDistinct(Ap4R4SensorDriftResult report)
    {
        var failed = new List<string>();
        if (report.DistinctValues != 1)
        {
            failed.Add($"signal-freeze-distinct-values:{report.DistinctValues}");
        }

        if (Math.Abs(report.BiasDelta) > 0.01)
        {
            failed.Add($"signal-freeze-bias-delta:{report.BiasDelta:F3}");
        }

        return new Ap4R4PassEvaluation
        {
            Passed = failed.Count == 0,
            FailedCriteria = failed
        };
    }

    public static Ap4R4PassEvaluation ValidateRecoveryTimeline(Ap4R4RecoveryCaseResult report)
    {
        var failed = new List<string>();

        if (report.Timeline.Count < Ap4R3EvidenceValidator.MinimumRecoveryTimelineSamples)
        {
            failed.Add($"recovery-timeline-too-short:{report.Timeline.Count}");
        }

        if (!report.Timeline.Any(s => !s.ErrorActive && s.MachineState != nameof(MachineState.Error)))
        {
            failed.Add("no-pre-fault-sample");
        }

        if (!report.Timeline.Any(s => s.ErrorActive && s.MachineState == nameof(MachineState.Error)))
        {
            failed.Add("no-faulted-sample");
        }

        if (!report.Timeline.Any(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering)))
        {
            failed.Add("no-recovery-start-sample");
        }

        var recovering = report.Timeline.Where(s =>
            s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering)
            || s.LifecycleStage.StartsWith("Recovery", StringComparison.OrdinalIgnoreCase)).ToList();
        if (recovering.Count < 2)
        {
            failed.Add("no-recovery-mid-sample");
        }

        if (report.RecoveryCompletedAtUtc == null)
        {
            failed.Add("recovery-completed-missing");
        }

        if (!report.Timeline.Any(s => s.LifecycleStage == "PostRecovery") && report.ExpectProductionResume)
        {
            failed.Add("no-post-recovery-sample");
        }

        return new Ap4R4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    public static Ap4R4PassEvaluation ValidateRecoverySafety(Ap4R4RecoveryCaseResult report)
    {
        var failed = new List<string>();
        if (!report.SafeRecoveryThreshold.HasValue)
        {
            return new Ap4R4PassEvaluation { Passed = true, FailedCriteria = failed };
        }

        var completedSample = report.Timeline.FirstOrDefault(s => s.LifecycleStage == "RecoveryCompleted")
            ?? report.Timeline.LastOrDefault(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        if (completedSample != null)
        {
            var value = completedSample.Signals.GetValueOrDefault(report.SafeRecoverySourceId);
            if (!IsSafeValue(value, report.SafeRecoveryThreshold.Value, report.SafeRecoveryComparison, report.SafeRecoveryTolerance))
            {
                failed.Add($"recovery-completed-above-safe-threshold:{value:F2}");
            }
        }

        return new Ap4R4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    public static Ap4R4PassEvaluation ValidatePostRecoverySafety(Ap4R4RecoveryCaseResult report)
    {
        var failed = new List<string>();
        var postSamples = report.Timeline.Where(s => s.LifecycleStage == "PostRecovery").ToList();
        if (postSamples.Count < MinimumPostRecoverySamples)
        {
            failed.Add($"post-recovery-samples:{postSamples.Count}");
            return new Ap4R4PassEvaluation { Passed = false, FailedCriteria = failed };
        }

        foreach (var sample in postSamples)
        {
            var value = sample.Signals.GetValueOrDefault(report.SafeRecoverySourceId);
            if (report.FaultThreshold.HasValue && IsFaultThresholdViolated(value, report.FaultThreshold.Value, report.FaultThresholdComparison))
            {
                failed.Add($"post-recovery-fault-threshold:{value:F2}");
                break;
            }

            if (report.SafeRecoveryThreshold.HasValue
                && !IsSafeValue(value, report.SafeRecoveryThreshold.Value, report.SafeRecoveryComparison, report.SafeRecoveryTolerance))
            {
                failed.Add($"post-recovery-safe-threshold:{value:F2}");
                break;
            }
        }

        return new Ap4R4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    public static Ap4R4PassEvaluation ValidateRequiredDirectionChecks(
        IReadOnlyList<Ap4R4DirectionCheck> checks,
        string prefix)
    {
        var failed = new List<string>();
        foreach (var check in checks.Where(c => c.Required))
        {
            if (!check.Passed)
            {
                failed.Add($"{prefix}:{check.SignalId}:{check.Direction}");
            }
        }

        return new Ap4R4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    public static bool ComputeRecursivePassed(bool selfPassed, IEnumerable<bool> childPassed)
    {
        return selfPassed && childPassed.All(p => p);
    }

    public static List<Ap4R4DirectionCheck> ComputeFaultDirectionChecks(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, string> expectedDirections,
        int windowCount = 5)
    {
        int faultIndex = -1;
        for (var i = 0; i < timeline.Count; i++)
        {
            if (timeline[i].ErrorActive && timeline[i].MachineState == nameof(MachineState.Error))
            {
                faultIndex = i;
                break;
            }
        }

        var preFaultSamples = faultIndex > 0 ? timeline.Take(faultIndex).ToList() : [];
        var faultSamples = faultIndex >= 0
            ? timeline.Skip(faultIndex).Where(s =>
                s.ErrorActive
                && s.ScenarioPhase != nameof(FaultScenarioPhase.Recovering)
                && s.LifecycleStage != "PostRecovery").ToList()
            : [];
        if (faultSamples.Count < 2 || preFaultSamples.Count < 2)
        {
            return expectedDirections.Select(kv => new Ap4R4DirectionCheck
            {
                SignalId = kv.Key,
                Direction = kv.Value,
                Phase = "Fault",
                Required = true,
                Passed = false
            }).ToList();
        }

        var startWindow = Math.Min(windowCount, preFaultSamples.Count);
        var endWindow = Math.Min(windowCount, faultSamples.Count);

        return expectedDirections.Select(kv =>
        {
            var start = AverageSignal(preFaultSamples.TakeLast(startWindow).ToList(), kv.Key, kv.Key == "HydraulicEfficiency");
            var end = AverageSignal(faultSamples.TakeLast(endWindow).ToList(), kv.Key, kv.Key == "HydraulicEfficiency");
            var delta = end - start;
            return new Ap4R4DirectionCheck
            {
                SignalId = kv.Key,
                Direction = kv.Value,
                Phase = "Fault",
                Required = true,
                StartValue = start,
                EndValue = end,
                Delta = delta,
                Passed = EvaluateDirection(kv.Value, delta)
            };
        }).ToList();
    }

    public static List<Ap4R4DirectionCheck> ComputeRecoveryDirectionChecks(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, string> expectedDirections,
        IReadOnlyDictionary<string, double> normalTargets,
        int windowCount = 5)
    {
        var earlyRecovery = timeline.Where(s => s.LifecycleStage is "RecoveryStart" or "RecoveryMid").Take(windowCount * 2).ToList();
        var lateRecovery = timeline.Where(s => s.LifecycleStage is "RecoveryMid" or "RecoveryCompleted" or "PostRecovery").TakeLast(windowCount * 2).ToList();
        if (earlyRecovery.Count < 2 || lateRecovery.Count < 2)
        {
            return expectedDirections.Select(kv => new Ap4R4DirectionCheck
            {
                SignalId = kv.Key,
                Direction = kv.Value,
                Phase = "Recovery",
                Required = true,
                Passed = false
            }).ToList();
        }

        var startWindow = Math.Min(windowCount, earlyRecovery.Count);
        var endWindow = Math.Min(windowCount, lateRecovery.Count);

        return expectedDirections.Select(kv =>
        {
            var start = AverageSignal(earlyRecovery.Take(startWindow).ToList(), kv.Key, kv.Key == "HydraulicEfficiency");
            var end = AverageSignal(lateRecovery.TakeLast(endWindow).ToList(), kv.Key, kv.Key == "HydraulicEfficiency");
            var delta = end - start;
            var normal = normalTargets.GetValueOrDefault(kv.Key, end);
            var distanceStart = Math.Abs(start - normal);
            var distanceEnd = Math.Abs(end - normal);
            var towardNormal = distanceEnd <= distanceStart;
            var passed = kv.Value == "toward-normal"
                ? towardNormal
                : EvaluateDirection(kv.Value, delta);
            return new Ap4R4DirectionCheck
            {
                SignalId = kv.Key,
                Direction = kv.Value,
                Phase = "Recovery",
                Required = true,
                StartValue = start,
                EndValue = end,
                Delta = delta,
                DistanceToNormalStart = distanceStart,
                DistanceToNormalEnd = distanceEnd,
                Passed = passed
            };
        }).ToList();
    }

    public static Ap4R4DistanceToNormal ComputeDistanceToNormal(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, double> normalTargets,
        int windowCount = 5)
    {
        var early = timeline.Where(s => s.LifecycleStage is "RecoveryStart" or "RecoveryMid").Take(windowCount).ToList();
        var late = timeline.Where(s => s.LifecycleStage is "PostRecovery" or "RecoveryCompleted").TakeLast(windowCount).ToList();
        if (early.Count == 0 || late.Count == 0)
        {
            return new Ap4R4DistanceToNormal { RecoveryImproved = false };
        }

        double startDistance = 0;
        double endDistance = 0;
        foreach (var (signalId, normal) in normalTargets)
        {
            var start = AverageSignal(early, signalId, signalId == "HydraulicEfficiency");
            var end = AverageSignal(late, signalId, signalId == "HydraulicEfficiency");
            startDistance += Math.Abs(start - normal);
            endDistance += Math.Abs(end - normal);
        }

        return new Ap4R4DistanceToNormal
        {
            DistanceToNormalStart = startDistance,
            DistanceToNormalEnd = endDistance,
            RecoveryImproved = endDistance <= startDistance
        };
    }

    private static bool EvaluateDirection(string direction, double delta)
    {
        return direction switch
        {
            "increase" => delta > 0.01,
            "decrease" => delta < -0.01,
            "change" => Math.Abs(delta) > 0.01,
            _ => Math.Abs(delta) > 0.01
        };
    }

    private static bool IsFaultThresholdViolated(
        double value,
        double threshold,
        FaultThresholdComparison? comparison)
    {
        return comparison switch
        {
            FaultThresholdComparison.LessThan => value < threshold,
            FaultThresholdComparison.LessThanOrEqual => value <= threshold,
            FaultThresholdComparison.GreaterThan => value > threshold,
            FaultThresholdComparison.GreaterThanOrEqual => value >= threshold,
            _ => value >= threshold
        };
    }

    private static bool IsSafeValue(
        double value,
        double threshold,
        FaultThresholdComparison? comparison,
        double tolerance)
    {
        return comparison switch
        {
            FaultThresholdComparison.LessThan => value < threshold + tolerance,
            FaultThresholdComparison.LessThanOrEqual => value <= threshold + tolerance,
            FaultThresholdComparison.GreaterThan => value > threshold - tolerance,
            FaultThresholdComparison.GreaterThanOrEqual => value >= threshold - tolerance,
            _ => Math.Abs(value - threshold) <= tolerance
        };
    }

    private static double AverageSignal(
        IReadOnlyList<Ap4R4RecoverySample> samples,
        string signalId,
        bool useHidden)
    {
        var values = samples.Select(s =>
        {
            if (useHidden && s.HiddenStates.TryGetValue(signalId, out var hidden))
            {
                return hidden;
            }

            return s.Signals.GetValueOrDefault(signalId);
        }).ToList();
        return values.Count == 0 ? 0 : values.Average();
    }
}

internal sealed class Ap4R4PassEvaluation
{
    public bool Passed { get; init; }
    public List<string> FailedCriteria { get; init; } = [];
}

public sealed class Ap4R4CompletenessReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public Ap4R4RecoveryCaseResult LaserRecovery { get; set; } = new();
    public Ap4R4RecoveryCaseResult HydraulicRecovery { get; set; } = new();
    public Ap4R4SensorDriftResult SensorDrift { get; set; } = new();
    public List<Ap4R4ValidatorCheck> ValidatorChecks { get; set; } = [];
    public bool Ap4R4Passed { get; set; }
    public bool Ap4OverallPassed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R4RecoveryCaseResult
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public bool ExpectProductionResume { get; set; } = true;
    public double? FaultThreshold { get; set; }

    public FaultThresholdComparison? FaultThresholdComparison { get; set; }
    public double? SafeRecoveryThreshold { get; set; }
    public string SafeRecoverySourceId { get; set; } = "Axis01.MotorTemperature";
    public FaultThresholdComparison? SafeRecoveryComparison { get; set; }
    public double SafeRecoveryTolerance { get; set; } = 1.0;
    public TimeSpan MinimumStableDuration { get; set; }
    public DateTime? ScenarioStartedAtUtc { get; set; }
    public DateTime? ThresholdFirstReachedAtUtc { get; set; }
    public DateTime? ThresholdConfirmedAtUtc { get; set; }
    public DateTime? MachineFaultedAtUtc { get; set; }
    public DateTime? RecoveryStartedAtUtc { get; set; }
    public DateTime? RecoveryCompletedAtUtc { get; set; }
    public List<Ap4R4RecoverySample> Timeline { get; set; } = [];
    public List<Ap4R4RecoverySample> FaultSamples => Timeline.Where(t => t.LifecycleStage is "Faulted" or "RecoveryStart").ToList();
    public List<Ap4R4RecoverySample> RecoverySamples => Timeline.Where(t => s_isRecoveryStage(t.LifecycleStage)).ToList();
    public List<Ap4R4RecoverySample> PostRecoverySamples => Timeline.Where(t => t.LifecycleStage == "PostRecovery").ToList();
    public Dictionary<string, List<double>> SignalSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<double>> HiddenSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Ap4R4DirectionCheck> FaultDirectionChecks { get; set; } = [];
    public List<Ap4R4DirectionCheck> RecoveryDirectionChecks { get; set; } = [];
    public List<Ap4R4SafetyCheck> SafetyChecks { get; set; } = [];
    public Ap4R4DistanceToNormal DistanceToNormal { get; set; } = new();
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];

    private static bool s_isRecoveryStage(string stage) =>
        stage is "RecoveryStart" or "RecoveryMid" or "RecoveryCompleted";
}

public sealed class Ap4R4RecoverySample
{
    public DateTime TimestampUtc { get; set; }
    public string ScenarioId { get; set; } = "";
    public string ScenarioPhase { get; set; } = "";
    public string LifecycleStage { get; set; } = "";
    public bool ErrorActive { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string MachineState { get; set; } = "";
    public bool ProductionRunning { get; set; }
    public bool ServerReachable { get; set; }
    public Dictionary<string, double> Signals { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> HiddenStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class Ap4R4SensorDriftResult
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public List<double> SensorSamples { get; set; } = [];
    public List<double> HiddenSamples { get; set; } = [];
    public List<double> RedundantSamples { get; set; } = [];
    public int SensorSampleCount => SensorSamples.Count;
    public int DistinctValues => SensorSamples.Select(v => Math.Round(v, 2)).Distinct().Count();
    public double SensorBiasStart { get; set; }
    public double SensorBiasEnd { get; set; }
    public double BiasDelta => SensorBiasEnd - SensorBiasStart;
    public double? HiddenDelta { get; set; }
    public double? RedundantDelta { get; set; }
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R4DirectionCheck
{
    public string SignalId { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Phase { get; set; } = "";
    public bool Required { get; set; } = true;
    public double StartValue { get; set; }
    public double EndValue { get; set; }
    public double Delta { get; set; }
    public double? DistanceToNormalStart { get; set; }
    public double? DistanceToNormalEnd { get; set; }
    public bool Passed { get; set; }
}

public sealed class Ap4R4SafetyCheck
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public string Detail { get; set; } = "";
}

public sealed class Ap4R4DistanceToNormal
{
    public double DistanceToNormalStart { get; set; }
    public double DistanceToNormalEnd { get; set; }
    public bool RecoveryImproved { get; set; }
}

public sealed class Ap4R4ValidatorCheck
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}
