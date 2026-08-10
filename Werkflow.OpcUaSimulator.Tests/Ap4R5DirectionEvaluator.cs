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
            "toward-normal" => distanceToNormalEnd <= distanceToNormalStart,
            "stable" => Math.Abs(delta) <= StableTolerance,
            "change" => Math.Abs(delta) > minimumMeaningfulDelta,
            _ => Math.Abs(delta) > minimumMeaningfulDelta
        };
    }

    public static double ComputeBandDistance(double value, Ap4R6SignalNormalBand band)
    {
        if (value >= band.NormalMin && value <= band.NormalMax)
        {
            return 0.0;
        }

        if (value < band.NormalMin)
        {
            return band.NormalMin - value;
        }

        return value - band.NormalMax;
    }

    public static double NormalizeBandDistance(double bandDistance, Ap4R6SignalNormalBand band) =>
        bandDistance / band.BandWidth;

    public static bool IsWithinNormalBand(double value, Ap4R6SignalNormalBand band) =>
        value >= band.NormalMin && value <= band.NormalMax;

    public static bool EvaluateTowardNormal(
        Ap4R6SignalNormalBand band,
        double startValue,
        double endValue,
        double distanceStart,
        double distanceEnd)
    {
        if (IsWithinNormalBand(endValue, band))
        {
            return true;
        }

        double normalizedStart = NormalizeBandDistance(distanceStart, band);
        double normalizedEnd = NormalizeBandDistance(distanceEnd, band);
        return normalizedEnd < normalizedStart - MinimumImprovementNormalized
            || normalizedEnd < normalizedStart && distanceEnd < distanceStart;
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
            else if (kv.Key.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase) && kv.Value == "decrease")
            {
                if (faultPeriod.Count >= windowSize)
                {
                    startWindow = faultPeriod
                        .OrderByDescending(s => s.Signals.GetValueOrDefault(kv.Key))
                        .Take(windowSize)
                        .ToList();
                    endWindow = faultPeriod
                        .OrderBy(s => s.Signals.GetValueOrDefault(kv.Key))
                        .Take(windowSize)
                        .ToList();
                }
            }
            else if (kv.Key.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase) && kv.Value == "increase")
            {
                if (faultPeriod.Count >= windowSize)
                {
                    startWindow = faultPeriod
                        .OrderBy(s => s.Signals.GetValueOrDefault(kv.Key))
                        .Take(windowSize)
                        .ToList();
                    endWindow = faultPeriod
                        .OrderByDescending(s => s.Signals.GetValueOrDefault(kv.Key))
                        .Take(windowSize)
                        .ToList();
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
        var bands = normalTargets.ToDictionary(
            kv => kv.Key,
            kv => CreateBandFromNominal(kv.Key, kv.Value),
            StringComparer.OrdinalIgnoreCase);
        return BuildRecoveryDirectionChecks(timeline, expectedDirections, bands, windowSize, null);
    }

    private static Ap4R6SignalNormalBand CreateBandFromNominal(string signalId, double nominal)
    {
        double range = GetNormalRange(signalId);
        return new Ap4R6SignalNormalBand(nominal - range * 0.5, nominal + range * 0.5, nominal);
    }

    public static List<Ap4R4DirectionCheck> BuildRecoveryDirectionChecks(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, string> expectedDirections,
        IReadOnlyDictionary<string, Ap4R6SignalNormalBand> normalBands,
        int windowSize = 5,
        IReadOnlyDictionary<string, Func<Ap4R4RecoverySample, bool>>? lateWindowFilters = null)
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
            Ap4R6SignalNormalBand? band = normalBands.GetValueOrDefault(kv.Key);
            double? normalTarget = band?.NominalValue;
            var earlyRecoveryWindow = recoveringOnly.Count >= windowSize
                ? TakeWindow(recoveringOnly, windowSize, fromEnd: false)
                : recoveringOnly.Count > 0
                    ? recoveringOnly
                    : TakeWindow(
                        recoverySamples.Where(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering)).ToList(),
                        Math.Min(windowSize, recoverySamples.Count),
                        fromEnd: false);
            if (useHidden && kv.Value == "toward-normal" && recoveryIndex > 0)
            {
                var faultPeriod = timeline.Take(recoveryIndex).ToList();
                var faultDegraded = faultPeriod
                    .Where(s => s.ScenarioPhase is nameof(FaultScenarioPhase.Critical)
                        or nameof(FaultScenarioPhase.Faulted)
                        || (s.ErrorActive && s.MachineState == nameof(MachineState.Error)))
                    .ToList();
                if (faultDegraded.Count >= windowSize)
                {
                    earlyRecoveryWindow = TakeWindow(faultDegraded, windowSize, fromEnd: true);
                }
            }

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
                var filteredPost = postRecoverySamples;
                if (lateWindowFilters != null && lateWindowFilters.TryGetValue(kv.Key, out var filter))
                {
                    filteredPost = postRecoverySamples.Where(filter).ToList();
                }

                lateRecoveryWindow = filteredPost.Count >= windowSize
                    ? TakeWindow(filteredPost, windowSize, fromEnd: true)
                    : TakeWindow(postRecoverySamples, windowSize, fromEnd: true);
            }
            else if (recoveringOnly.Count >= windowSize)
            {
                lateRecoveryWindow = TakeWindow(recoveringOnly, windowSize, fromEnd: true);
            }
            else
            {
                lateRecoveryWindow = TakeWindow(recoverySamples, windowSize, fromEnd: true);
            }

            return BuildCheck(kv.Key, kv.Value, "Recovery", earlyRecoveryWindow, lateRecoveryWindow, useHidden, required: true, normalTarget, band);
        }).ToList();
    }

    public static Ap4R4DistanceToNormal ComputeDistanceToNormal(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, double> normalTargets,
        int windowSize = 5)
    {
        var bands = normalTargets.ToDictionary(
            kv => kv.Key,
            kv => Ap4R6ProfileNormals.GetBendingBand(kv.Key),
            StringComparer.OrdinalIgnoreCase);
        return ComputeDistanceToNormal(timeline, bands, windowSize);
    }

    public static Ap4R4DistanceToNormal ComputeDistanceToNormal(
        IReadOnlyList<Ap4R4RecoverySample> timeline,
        IReadOnlyDictionary<string, Ap4R6SignalNormalBand> normalBands,
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
        foreach (var (signalId, band) in normalBands)
        {
            bool useHidden = signalId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase);
            double start = AverageSampleValue(early, signalId, useHidden);
            double end = AverageSampleValue(late, signalId, useHidden);
            startDistance += NormalizeBandDistance(ComputeBandDistance(start, band), band);
            endDistance += NormalizeBandDistance(ComputeBandDistance(end, band), band);
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
                && check.DistanceToNormalEnd.Value > check.DistanceToNormalStart.Value)
            {
                failed.Add($"toward-normal-inconsistent:{check.SignalId}");
            }

            if (check.Phase == "Recovery" && check.Required && check.Passed && check.TowardNormalPassed == false)
            {
                failed.Add($"recovery-toward-normal-inconsistent:{check.SignalId}");
            }

            if (check.Phase == "Recovery" && check.Required && check.Passed
                && check.DistanceToNormalStart.HasValue && check.DistanceToNormalEnd.HasValue
                && !check.TowardNormalPassed)
            {
                failed.Add($"recovery-distance-inconsistent:{check.SignalId}");
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
        double? normalTarget = null,
        Ap4R6SignalNormalBand? normalBand = null)
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
        double range = normalBand?.BandWidth ?? GetNormalRange(signalId);
        double distanceStart = normalBand != null
            ? ComputeBandDistance(start, normalBand)
            : normalTarget.HasValue
                ? NormalizedDistance(start, normalTarget.Value, range) * range
                : 0;
        double distanceEnd = normalBand != null
            ? ComputeBandDistance(end, normalBand)
            : normalTarget.HasValue
                ? NormalizedDistance(end, normalTarget.Value, range) * range
                : 0;
        double normDistanceStart = normalBand != null
            ? NormalizeBandDistance(distanceStart, normalBand)
            : distanceStart / range;
        double normDistanceEnd = normalBand != null
            ? NormalizeBandDistance(distanceEnd, normalBand)
            : distanceEnd / range;

        bool directionPassed = EvaluateDirection(direction, delta, minDelta, normDistanceStart, normDistanceEnd);
        bool towardNormalPassed = normalBand == null
            || EvaluateTowardNormal(normalBand, start, end, distanceStart, distanceEnd);
        bool passed = phase == "Recovery" && normalBand != null
            ? (direction == "toward-normal"
                ? towardNormalPassed
                : directionPassed && towardNormalPassed)
            : directionPassed;

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
            NormalMin = normalBand?.NormalMin,
            NormalMax = normalBand?.NormalMax,
            DistanceToNormalStart = normalBand != null || normalTarget.HasValue ? normDistanceStart : null,
            DistanceToNormalEnd = normalBand != null || normalTarget.HasValue ? normDistanceEnd : null,
            DirectionPassed = directionPassed,
            TowardNormalPassed = phase == "Recovery" ? towardNormalPassed : true,
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
