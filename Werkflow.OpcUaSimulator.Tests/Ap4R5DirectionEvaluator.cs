using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Tests;

/// <summary>
/// Central AP-4-R5 direction evaluation — Passed must match computed Start/End/Delta values.
/// </summary>
internal static class Ap4R5DirectionEvaluator
{
    public const double StableTolerance = 0.001;
    public const double MinimumImprovementNormalized = 0.02;

    public static double GetMinimumMeaningfulDelta(string signalId, double referenceMagnitude)
    {
        if (signalId.Contains("MotorTemperature", StringComparison.OrdinalIgnoreCase))
        {
            return 0.5;
        }

        if (signalId.Contains("MotorCurrent", StringComparison.OrdinalIgnoreCase)
            || signalId.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase))
        {
            return 0.1;
        }

        if (signalId.Contains("Speed", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(2.0, referenceMagnitude * 0.003);
        }

        if (signalId.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        if (signalId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase))
        {
            return 0.05;
        }

        return Math.Max(0.01, referenceMagnitude * 0.01);
    }

    public static double GetNormalRange(string signalId)
    {
        if (signalId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        if (signalId.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase))
        {
            return 50.0;
        }

        if (signalId.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase))
        {
            return 5.0;
        }

        if (signalId.Contains("MotorCurrent", StringComparison.OrdinalIgnoreCase))
        {
            return 3.0;
        }

        if (signalId.Contains("Speed", StringComparison.OrdinalIgnoreCase))
        {
            return 200.0;
        }

        if (signalId.Contains("MotorTemperature", StringComparison.OrdinalIgnoreCase))
        {
            return 30.0;
        }

        return 1.0;
    }

    public static bool EvaluateDirection(
        string direction,
        double delta,
        double minimumMeaningfulDelta,
        double distanceToNormalStart,
        double distanceToNormalEnd)
    {
        return direction switch
        {
            "increase" => delta > minimumMeaningfulDelta,
            "decrease" => delta < -minimumMeaningfulDelta,
            "toward-normal" => distanceToNormalEnd < distanceToNormalStart,
            "stable" => Math.Abs(delta) <= StableTolerance,
            "change" => Math.Abs(delta) > minimumMeaningfulDelta,
            _ => Math.Abs(delta) > minimumMeaningfulDelta
        };
    }

    public static List<Ap4R4DirectionCheck> BuildFaultDirectionChecks(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, string> expectedDirections,
        int windowSize = 5)
    {
        int recoveryIndex = FindIndex(timeline, s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        var faultPeriod = recoveryIndex > 0 ? timeline.Take(recoveryIndex).ToList() : timeline.ToList();
        if (faultPeriod.Count < Math.Max(2, windowSize))
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

        var preFaultSamples = faultPeriod.Where(s => s.LifecycleStage == "PreFault").ToList();
        var peakFaultSamples = faultPeriod
            .Where(s => s.ScenarioPhase is nameof(FaultScenarioPhase.Critical)
                or nameof(FaultScenarioPhase.Faulted)
                || (s.ErrorActive && s.MachineState == nameof(MachineState.Error)))
            .ToList();

        return expectedDirections.Select(kv =>
        {
            bool useHidden = kv.Key.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase);
            var startWindow = preFaultSamples.Count >= windowSize
                ? TakeWindow(preFaultSamples, windowSize, fromEnd: false)
                : TakeWindow(faultPeriod.Take(Math.Min(3, faultPeriod.Count)).ToList(), Math.Min(3, faultPeriod.Count), fromEnd: false);
            var endWindow = peakFaultSamples.Count >= windowSize
                ? TakeWindow(peakFaultSamples, windowSize, fromEnd: true)
                : TakeWindow(faultPeriod, windowSize, fromEnd: true);
            if (kv.Key.Contains("Speed", StringComparison.OrdinalIgnoreCase))
            {
                var errorSamples = faultPeriod
                    .Where(s => s.ErrorActive && s.MachineState == nameof(MachineState.Error))
                    .ToList();
                if (errorSamples.Count >= windowSize * 2)
                {
                    startWindow = TakeWindow(errorSamples, windowSize, fromEnd: false);
                    endWindow = TakeWindow(errorSamples, windowSize, fromEnd: true);
                }
            }

            return BuildCheck(kv.Key, kv.Value, "Fault", startWindow, endWindow, useHidden, required: true);
        }).ToList();
    }

    public static List<Ap4R4DirectionCheck> BuildRecoveryDirectionChecks(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, string> expectedDirections,
        IReadOnlyDictionary<string, double> normalTargets,
        int windowSize = 5)
    {
        int recoveryIndex = FindIndex(timeline, s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        if (recoveryIndex < 0)
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

        var recoverySamples = timeline.Skip(recoveryIndex).ToList();
        var recoveringOnly = recoverySamples
            .Where(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering))
            .ToList();
        var postRecoverySamples = timeline.Where(s => s.LifecycleStage == "PostRecovery").ToList();

        return expectedDirections.Select(kv =>
        {
            bool useHidden = kv.Key.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase);
            double? normalTarget = normalTargets.GetValueOrDefault(kv.Key);
            var earlyRecoveryWindow = recoveringOnly.Count >= windowSize
                ? TakeWindow(recoveringOnly, windowSize, fromEnd: false)
                : TakeWindow(recoverySamples, windowSize, fromEnd: false);
            if (kv.Key.Contains("Speed", StringComparison.OrdinalIgnoreCase) && kv.Value == "increase" && recoveryIndex > windowSize)
            {
                var preRecoveryFault = timeline.Take(recoveryIndex).TakeLast(windowSize).ToList();
                if (preRecoveryFault.Count >= windowSize)
                {
                    earlyRecoveryWindow = preRecoveryFault;
                }
            }
            IReadOnlyList<Ap4R4RecoverySample> lateRecoveryWindow;
            if (kv.Key.Contains("MotorTemperature", StringComparison.OrdinalIgnoreCase))
            {
                lateRecoveryWindow = recoveringOnly.Count >= windowSize
                    ? TakeWindow(recoveringOnly, windowSize, fromEnd: true)
                    : TakeWindow(recoverySamples, windowSize, fromEnd: true);
            }
            else if (postRecoverySamples.Count >= windowSize)
            {
                lateRecoveryWindow = TakeWindow(postRecoverySamples, windowSize, fromEnd: true);
            }
            else if (recoveringOnly.Count >= windowSize)
            {
                lateRecoveryWindow = TakeWindow(recoveringOnly, windowSize, fromEnd: true);
            }
            else
            {
                lateRecoveryWindow = TakeWindow(recoverySamples, windowSize, fromEnd: true);
            }

            return BuildCheck(kv.Key, kv.Value, "Recovery", earlyRecoveryWindow, lateRecoveryWindow, useHidden, required: true, normalTarget);
        }).ToList();
    }

    public static Ap4R4DistanceToNormal ComputeDistanceToNormal(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, double> normalTargets,
        int windowSize = 5)
    {
        int recoveryIndex = FindIndex(timeline, s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        var recoverySamples = recoveryIndex >= 0 ? timeline.Skip(recoveryIndex).ToList() : timeline.ToList();
        var early = TakeWindow(
            recoverySamples.Where(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering)).ToList(),
            windowSize,
            fromEnd: false);
        var late = TakeWindow(
            recoverySamples.Where(s => s.LifecycleStage is "PostRecovery" or "RecoveryCompleted").ToList(),
            windowSize,
            fromEnd: true);

        if (early.Count == 0 || late.Count == 0)
        {
            return new Ap4R4DistanceToNormal { RecoveryImproved = false };
        }

        double startDistance = 0;
        double endDistance = 0;
        foreach (var (signalId, normal) in normalTargets)
        {
            bool useHidden = signalId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase);
            double range = GetNormalRange(signalId);
            double start = AverageSampleValue(early, signalId, useHidden);
            double end = AverageSampleValue(late, signalId, useHidden);
            startDistance += NormalizedDistance(start, normal, range);
            endDistance += NormalizedDistance(end, normal, range);
        }

        return new Ap4R4DistanceToNormal
        {
            DistanceToNormalStart = startDistance,
            DistanceToNormalEnd = endDistance,
            RecoveryImproved = endDistance < startDistance - MinimumImprovementNormalized
        };
    }

    public static bool ComputeScenarioPassed(
        bool validatorPassed,
        IEnumerable<Ap4R4DirectionCheck> faultChecks,
        IEnumerable<Ap4R4DirectionCheck> recoveryChecks,
        IEnumerable<Ap4R4SafetyCheck> safetyChecks)
    {
        if (!validatorPassed)
        {
            return false;
        }

        foreach (var check in faultChecks.Concat(recoveryChecks))
        {
            if (check.Required && !check.Passed)
            {
                return false;
            }
        }

        foreach (var check in safetyChecks)
        {
            if (!check.Passed)
            {
                return false;
            }
        }

        return true;
    }

    public static List<string> ValidateSelfConsistency(IReadOnlyList<Ap4R4DirectionCheck> checks)
    {
        var failed = new List<string>();
        foreach (var check in checks)
        {
            double minDelta = check.MinimumMeaningfulDelta > 0
                ? check.MinimumMeaningfulDelta
                : GetMinimumMeaningfulDelta(check.SignalId, Math.Max(check.StartValue, check.EndValue));

            if (check.Direction == "increase" && check.Passed && check.Delta <= minDelta)
            {
                failed.Add($"increase-inconsistent:{check.SignalId}:{check.Delta:F4}");
            }

            if (check.Direction == "decrease" && check.Passed && check.Delta >= -minDelta)
            {
                failed.Add($"decrease-inconsistent:{check.SignalId}:{check.Delta:F4}");
            }

            if (check.Direction == "toward-normal" && check.Passed
                && check.DistanceToNormalStart.HasValue && check.DistanceToNormalEnd.HasValue
                && check.DistanceToNormalEnd.Value >= check.DistanceToNormalStart.Value)
            {
                failed.Add($"toward-normal-inconsistent:{check.SignalId}");
            }
        }

        return failed;
    }

    private static int FindIndex(IReadOnlyList<Ap4R4RecoverySample> list, Func<Ap4R4RecoverySample, bool> predicate)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static Ap4R4DirectionCheck BuildCheck(
        string signalId,
        string direction,
        string phase,
        IReadOnlyList<Ap4R4RecoverySample> startWindow,
        IReadOnlyList<Ap4R4RecoverySample> endWindow,
        bool useHidden,
        bool required,
        double? normalTarget = null)
    {
        if (startWindow.Count == 0 || endWindow.Count == 0)
        {
            return new Ap4R4DirectionCheck
            {
                SignalId = signalId,
                Direction = direction,
                Phase = phase,
                Required = required,
                WindowStartSampleCount = startWindow.Count,
                WindowEndSampleCount = endWindow.Count,
                Passed = false
            };
        }

        double start = AverageSampleValue(startWindow, signalId, useHidden);
        double end = AverageSampleValue(endWindow, signalId, useHidden);
        double delta = end - start;
        double minDelta = GetMinimumMeaningfulDelta(signalId, Math.Max(Math.Abs(start), Math.Abs(end)));
        double range = GetNormalRange(signalId);
        double distanceStart = normalTarget.HasValue
            ? NormalizedDistance(start, normalTarget.Value, range)
            : 0;
        double distanceEnd = normalTarget.HasValue
            ? NormalizedDistance(end, normalTarget.Value, range)
            : 0;

        bool passed = EvaluateDirection(direction, delta, minDelta, distanceStart, distanceEnd);

        return new Ap4R4DirectionCheck
        {
            SignalId = signalId,
            Direction = direction,
            Phase = phase,
            Required = required,
            WindowStartSampleCount = startWindow.Count,
            WindowEndSampleCount = endWindow.Count,
            StartValue = start,
            EndValue = end,
            Delta = delta,
            MinimumMeaningfulDelta = minDelta,
            DistanceToNormalStart = normalTarget.HasValue ? distanceStart : null,
            DistanceToNormalEnd = normalTarget.HasValue ? distanceEnd : null,
            Passed = passed
        };
    }

    private static int FindFaultIndex(IReadOnlyList<Ap4R4RecoverySample> timeline)
    {
        for (var i = 0; i < timeline.Count; i++)
        {
            if (timeline[i].ErrorActive && timeline[i].MachineState == nameof(MachineState.Error))
            {
                return i;
            }
        }

        return -1;
    }

    private static List<Ap4R4RecoverySample> TakeWindow(
        IReadOnlyList<Ap4R4RecoverySample> samples,
        int windowSize,
        bool fromEnd)
    {
        if (samples.Count == 0)
        {
            return [];
        }

        int size = Math.Min(windowSize, samples.Count);
        return fromEnd
            ? samples.TakeLast(size).ToList()
            : samples.Take(size).ToList();
    }

    private static double AverageSampleValue(
        IReadOnlyList<Ap4R4RecoverySample> samples,
        string signalId,
        bool useHidden)
    {
        var values = samples.Select(s => ReadValue(s, signalId, useHidden)).ToList();
        return values.Count == 0 ? 0 : values.Average();
    }

    private static double ReadValue(Ap4R4RecoverySample sample, string signalId, bool useHidden)
    {
        if (useHidden && sample.HiddenStates.TryGetValue(signalId, out double hidden))
        {
            return hidden;
        }

        return sample.Signals.GetValueOrDefault(signalId);
    }

    private static double NormalizedDistance(double value, double target, double normalRange)
    {
        if (normalRange <= 0)
        {
            return Math.Abs(value - target);
        }

        return Math.Abs(value - target) / normalRange;
    }
}
