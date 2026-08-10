using System.Globalization;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Tests;

internal static class Ap4R3EvidenceValidator
{
    public const int MinimumComplexSampleCount = 20;
    public const int PreferredComplexSampleCount = 30;
    public const int MinimumRecoveryTimelineSamples = 5;

    public static Ap4R3PassEvaluation ValidateRecovery(Ap4R3RecoveryCaseResult report)
    {
        var failed = new List<string>();

        if (report.Timeline.Count < MinimumRecoveryTimelineSamples)
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

        if (!report.Timeline.Any(s => !s.ErrorActive && s.ProductionRunning) && report.ExpectProductionResume)
        {
            failed.Add("no-post-recovery-sample");
        }

        if (report.Timeline.Any(t => t.ErrorActive && !t.ServerReachable))
        {
            failed.Add("server-offline-during-fault");
        }

        if (!report.DirectionChecks.Any(d => d.Passed))
        {
            failed.Add("no-direction-toward-normal");
        }

        failed.AddRange(ValidateSampleMaps(report.SignalSamples, report.RequiredSignalIds, "signal"));
        failed.AddRange(ValidateSampleMaps(report.HiddenSamples, report.RequiredHiddenIds, "hidden"));

        return new Ap4R3PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    public static Ap4R3PassEvaluation ValidateSensorDrift(Ap4R3ComplexCaseResult report)
    {
        var failed = new List<string>();
        failed.AddRange(Ap4R3EvidenceValidator.ValidateComplexCase(report, report.RequiredSignalIds, report.RequiredHiddenIds, MinimumComplexSampleCount).FailedCriteria);

        if (report.SampleCount < MinimumComplexSampleCount)
        {
            failed.Add($"sensor-drift-sample-count:{report.SampleCount}");
        }

        if (report.SensorDelta == null)
        {
            failed.Add("sensor-drift-missing-sensor-delta");
        }
        else
        {
            var sensorFrozen = Math.Abs(report.SensorDelta.Value) < 0.08;
            var sensorDrifted = Math.Abs(report.SensorDelta.Value) > 0.03;
            if (!sensorFrozen && !sensorDrifted)
            {
                failed.Add("sensor-drift-insufficient-sensor-movement");
            }
        }

        if (report.HiddenDelta == null || Math.Abs(report.HiddenDelta.Value) > 0.25)
        {
            failed.Add("sensor-drift-hidden-not-stable");
        }

        if (report.RedundantDelta != null && report.SensorDelta != null
            && Math.Abs(report.RedundantDelta.Value - report.SensorDelta.Value) < 0.02
            && Math.Abs(report.SensorDelta.Value) > 0.05)
        {
            failed.Add("redundant-follows-artificial-drift");
        }

        return new Ap4R3PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList() };
    }

    public static Ap4R3PassEvaluation ValidateCoolantLoss(Ap4R3ComplexCaseResult report)
    {
        var failed = new List<string>();
        failed.AddRange(Ap4R3EvidenceValidator.ValidateComplexCase(report, report.RequiredSignalIds, report.RequiredHiddenIds, MinimumComplexSampleCount).FailedCriteria);

        if (!report.DirectionChecks.Any(d => d.SignalId.Contains("Flow", StringComparison.OrdinalIgnoreCase) && d.Direction == "decrease" && d.Passed))
        {
            failed.Add("coolant-flow-not-decreasing");
        }

        if (!report.DirectionChecks.Any(d => d.SignalId.Contains("Pressure", StringComparison.OrdinalIgnoreCase) && d.Direction == "decrease" && d.Passed))
        {
            failed.Add("coolant-pressure-not-decreasing");
        }

        if (!report.DirectionChecks.Any(d => d.SignalId.Contains("Temperature", StringComparison.OrdinalIgnoreCase) && d.Direction == "increase" && d.Passed))
        {
            failed.Add("coolant-temperature-not-increasing");
        }

        if (!report.DirectionChecks.Any(d => d.SignalId.Equals("CoolingEfficiency", StringComparison.OrdinalIgnoreCase) && d.Direction == "decrease" && d.Passed))
        {
            failed.Add("cooling-efficiency-not-decreasing");
        }

        if (!report.TimingChecks.Any(t => t.Passed))
        {
            failed.Add("coolant-timing-check-failed");
        }

        return new Ap4R3PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList() };
    }

    public static Ap4R3PassEvaluation ValidateHydraulicLeak(Ap4R3ComplexCaseResult report)
    {
        var failed = new List<string>();
        failed.AddRange(Ap4R3EvidenceValidator.ValidateComplexCase(report, report.RequiredSignalIds, report.RequiredHiddenIds, MinimumComplexSampleCount).FailedCriteria);

        if (!report.DirectionChecks.Any(d => d.SignalId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase) && d.Direction == "decrease" && d.Passed))
        {
            failed.Add("hydraulic-efficiency-not-decreasing");
        }

        if (!report.DirectionChecks.Any(d => d.SignalId.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase) && d.Direction == "decrease" && d.Passed))
        {
            failed.Add("supply-pressure-not-decreasing");
        }

        if (!report.DirectionChecks.Any(d => d.SignalId.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase) && d.Direction == "increase" && d.Passed))
        {
            failed.Add("pump-current-not-increasing");
        }

        var secondaryPassed = report.DirectionChecks.Any(d =>
            (d.SignalId.Contains("OilTemperature", StringComparison.OrdinalIgnoreCase) && d.Direction == "increase" && d.Passed)
            || (d.SignalId.Contains("CycleTime", StringComparison.OrdinalIgnoreCase) && d.Direction == "increase" && d.Passed)
            || (d.SignalId.Contains("PressForce", StringComparison.OrdinalIgnoreCase) && d.Passed));

        if (!secondaryPassed)
        {
            failed.Add("hydraulic-secondary-effect-missing");
        }

        return new Ap4R3PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList() };
    }

    public static Ap4R3PassEvaluation ValidateComplexCase(
        Ap4R3ComplexCaseResult report,
        IReadOnlyList<string> requiredSignals,
        IReadOnlyList<string> requiredHidden,
        int minimumSampleCount)
    {
        var failed = new List<string>();

        if (report.SampleCount < minimumSampleCount)
        {
            failed.Add($"sample-count:{report.SampleCount}");
        }

        failed.AddRange(ValidateSampleMaps(report.SignalSamples, requiredSignals, "signal"));
        failed.AddRange(ValidateSampleMaps(report.HiddenSamples, requiredHidden, "hidden"));

        foreach (var series in report.SignalSamples.Values.Concat(report.HiddenSamples.Values))
        {
            if (series.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            {
                failed.Add("nan-or-infinity");
                break;
            }
        }

        return new Ap4R3PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList() };
    }

    public static List<string> ValidateSampleMaps(
        Dictionary<string, List<double>> samples,
        IReadOnlyList<string> requiredIds,
        string kind)
    {
        var failed = new List<string>();

        if (samples.Count == 0)
        {
            failed.Add($"{kind}-samples-empty");
            return failed;
        }

        foreach (var id in requiredIds)
        {
            if (!samples.TryGetValue(id, out var series) || series.Count == 0)
            {
                failed.Add($"missing-{kind}:{id}");
            }
        }

        return failed;
    }

    public static List<Ap4R3DirectionCheck> ComputeEndpointDirectionChecks(
        Dictionary<string, List<double>> signalSamples,
        Dictionary<string, List<double>> hiddenSamples,
        IReadOnlyDictionary<string, string> expectedDirections,
        int endpointCount = 5)
    {
        var checks = new List<Ap4R3DirectionCheck>();
        foreach (var (id, direction) in expectedDirections)
        {
            List<double>? series = signalSamples.TryGetValue(id, out var s) ? s : hiddenSamples.GetValueOrDefault(id);
            if (series == null || series.Count < endpointCount * 2)
            {
                checks.Add(new Ap4R3DirectionCheck { SignalId = id, Direction = direction, Passed = false });
                continue;
            }

            var start = series.Take(endpointCount).Average();
            var end = series.TakeLast(endpointCount).Average();
            if (id.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase) && direction == "increase")
            {
                start = series.Take(endpointCount).Min();
                end = series.TakeLast(endpointCount).Max();
            }
            else if (id.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase) && direction == "decrease")
            {
                start = series.Take(endpointCount).Max();
                end = series.TakeLast(endpointCount).Min();
            }

            var delta = end - start;
            var passed = direction switch
            {
                "increase" => delta > 0.01,
                "decrease" => delta < -0.01,
                "change" => Math.Abs(delta) > 0.01,
                _ => Math.Abs(delta) > 0.01
            };
            checks.Add(new Ap4R3DirectionCheck
            {
                SignalId = id,
                Direction = direction,
                StartValue = start,
                EndValue = end,
                Delta = delta,
                Passed = passed
            });
        }

        return checks;
    }

    public static List<Ap4R3DirectionCheck> ComputeDirectionChecks(
        Dictionary<string, List<double>> signalSamples,
        Dictionary<string, List<double>> hiddenSamples,
        IReadOnlyDictionary<string, string> expectedDirections)
    {
        var checks = new List<Ap4R3DirectionCheck>();
        foreach (var (id, direction) in expectedDirections)
        {
            List<double>? series = signalSamples.TryGetValue(id, out var s) ? s : hiddenSamples.GetValueOrDefault(id);
            if (series == null || series.Count < 4)
            {
                checks.Add(new Ap4R3DirectionCheck { SignalId = id, Direction = direction, Passed = false, Delta = 0 });
                continue;
            }

            var start = AverageSlice(series, 0, Math.Max(1, series.Count / 4));
            var end = AverageSlice(series, series.Count - Math.Max(1, series.Count / 4), series.Count);
            if (id.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase) && direction == "increase")
            {
                start = series.Take(Math.Max(1, series.Count / 4)).Min();
                end = series.TakeLast(Math.Max(1, series.Count / 4)).Max();
            }
            else if (id.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase) && direction == "decrease")
            {
                start = series.Take(Math.Max(1, series.Count / 4)).Max();
                end = series.TakeLast(Math.Max(1, series.Count / 4)).Min();
            }

            var delta = end - start;
            var passed = direction switch
            {
                "increase" => delta > 0.01,
                "decrease" => delta < -0.01,
                "change" => Math.Abs(delta) > 0.01,
                _ => Math.Abs(delta) > 0.01
            };
            checks.Add(new Ap4R3DirectionCheck
            {
                SignalId = id,
                Direction = direction,
                StartValue = start,
                EndValue = end,
                Delta = delta,
                Passed = passed
            });
        }

        return checks;
    }

    public static List<Ap4R3TimingCheck> ComputeCoolantTimingChecks(Dictionary<string, List<double>> signals)
    {
        var checks = new List<Ap4R3TimingCheck>();
        var flow = signals.GetValueOrDefault("Cooling.PrimaryCircuit.Flow");
        var pressure = signals.GetValueOrDefault("Cooling.PrimaryCircuit.Pressure");
        var temp = signals.GetValueOrDefault("Cooling.PrimaryCircuit.Temperature");
        if (flow == null || pressure == null || temp == null || temp.Count < 8)
        {
            checks.Add(new Ap4R3TimingCheck { Name = "flow-pressure-before-temp", Passed = false });
            return checks;
        }

        var flowDropIndex = FirstSignificantChangeIndex(flow, -0.05);
        var pressureDropIndex = FirstSignificantChangeIndex(pressure, -0.05);
        var tempRiseIndex = FirstSignificantChangeIndex(temp, 0.03);
        var primaryDrop = Math.Min(flowDropIndex, pressureDropIndex);
        var passed = tempRiseIndex >= primaryDrop;
        checks.Add(new Ap4R3TimingCheck
        {
            Name = "flow-pressure-before-temp",
            PrimaryChangeIndex = primaryDrop,
            SecondaryChangeIndex = tempRiseIndex,
            Passed = passed
        });
        return checks;
    }

    public static List<Ap4R3DirectionCheck> ComputeLaserRecoveryDirections(List<Ap4R3RecoverySample> timeline)
    {
        var checks = new List<Ap4R3DirectionCheck>();
        var faulted = timeline.Where(t => t.ErrorActive).ToList();
        var post = timeline.Where(t => !t.ErrorActive).ToList();
        if (faulted.Count == 0 || post.Count == 0)
        {
            return checks;
        }

        AddRecoveryDirection(checks, faulted, post, "Axis01.MotorCurrent", "decrease");
        AddRecoveryDirection(checks, faulted, post, "Axis01.Speed", "increase");
        var currentCheck = checks.FirstOrDefault(c => c.SignalId == "Axis01.MotorCurrent");
        var tempCheck = AddRecoveryDirection(checks, faulted, post, "Axis01.MotorTemperature", "decrease");
        if (currentCheck != null && tempCheck != null && currentCheck.Passed)
        {
            checks.Add(new Ap4R3DirectionCheck
            {
                SignalId = "MotorTemperatureVsCurrentLag",
                Direction = "temperature-slower-than-current",
                Delta = tempCheck.Delta - currentCheck.Delta,
                Passed = Math.Abs(tempCheck.Delta) < Math.Abs(currentCheck.Delta) * 0.85
            });
        }

        return checks;
    }

    private static Ap4R3DirectionCheck AddRecoveryDirection(
        List<Ap4R3DirectionCheck> checks,
        List<Ap4R3RecoverySample> faulted,
        List<Ap4R3RecoverySample> post,
        string signalId,
        string direction)
    {
        var faultAvg = AverageSignal(faulted, signalId);
        var postAvg = AverageSignal(post, signalId);
        var delta = postAvg - faultAvg;
        var passed = direction == "increase" ? delta > 0.01 : delta < -0.01;
        var check = new Ap4R3DirectionCheck
        {
            SignalId = signalId,
            Direction = direction,
            StartValue = faultAvg,
            EndValue = postAvg,
            Delta = delta,
            Passed = passed
        };
        checks.Add(check);
        return check;
    }

    private static double AverageSignal(IReadOnlyList<Ap4R3RecoverySample> samples, string signalId)
    {
        var values = samples.Where(s => s.Signals.ContainsKey(signalId)).Select(s => s.Signals[signalId]).ToList();
        return values.Count == 0 ? 0 : values.Average();
    }

    private static double AverageSlice(List<double> values, int start, int end)
    {
        if (end <= start)
        {
            return values[start];
        }

        return values.Skip(start).Take(end - start).Average();
    }

    private static int FirstSignificantChangeIndex(List<double> series, double threshold)
    {
        var baseline = AverageSlice(series, 0, Math.Max(1, series.Count / 5));
        for (var i = 0; i < series.Count; i++)
        {
            if (threshold < 0 && series[i] - baseline <= threshold)
            {
                return i;
            }

            if (threshold > 0 && series[i] - baseline >= threshold)
            {
                return i;
            }
        }

        return series.Count - 1;
    }
}

internal sealed class Ap4R3PassEvaluation
{
    public bool Passed { get; init; }
    public List<string> FailedCriteria { get; init; } = [];
}

public sealed class Ap4R3CompletenessReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public Ap4R3RecoveryCaseResult LaserRecovery { get; set; } = new();
    public Ap4R3RecoveryCaseResult HydraulicRecovery { get; set; } = new();
    public Ap4R3ComplexCaseResult SensorDrift { get; set; } = new();
    public Ap4R3ComplexCaseResult CoolantLoss { get; set; } = new();
    public Ap4R3ComplexCaseResult HydraulicLeak { get; set; } = new();
    public bool Ap4R3Passed { get; set; }
    public bool Ap4OverallPassed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R3RecoveryCaseResult
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public bool ExpectProductionResume { get; set; } = true;
    public int SampleCount => Timeline.Count;
    public DateTime? ScenarioStartedAtUtc { get; set; }
    public DateTime? ThresholdFirstReachedAtUtc { get; set; }
    public DateTime? ThresholdConfirmedAtUtc { get; set; }
    public DateTime? MachineFaultedAtUtc { get; set; }
    public DateTime? RecoveryStartedAtUtc { get; set; }
    public DateTime? RecoveryCompletedAtUtc { get; set; }
    public List<Ap4R3RecoverySample> Timeline { get; set; } = [];
    public Dictionary<string, List<double>> SignalSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<double>> HiddenSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> RequiredSignalIds { get; set; } = [];
    public List<string> RequiredHiddenIds { get; set; } = [];
    public List<Ap4R3DirectionCheck> DirectionChecks { get; set; } = [];
    public List<Ap4R3TimingCheck> TimingChecks { get; set; } = [];
    public List<string> RequiredEvidenceChecks { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R3RecoverySample
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

public sealed class Ap4R3ComplexCaseResult
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public int SampleCount { get; set; }
    public Dictionary<string, List<double>> SignalSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<double>> HiddenSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> RequiredSignalIds { get; set; } = [];
    public List<string> RequiredHiddenIds { get; set; } = [];
    public double? SensorStart { get; set; }
    public double? SensorEnd { get; set; }
    public double? SensorDelta { get; set; }
    public double? HiddenStart { get; set; }
    public double? HiddenEnd { get; set; }
    public double? HiddenDelta { get; set; }
    public double? RedundantStart { get; set; }
    public double? RedundantEnd { get; set; }
    public double? RedundantDelta { get; set; }
    public List<Ap4R3DirectionCheck> DirectionChecks { get; set; } = [];
    public List<Ap4R3TimingCheck> TimingChecks { get; set; } = [];
    public List<string> RequiredEvidenceChecks { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R3DirectionCheck
{
    public string SignalId { get; set; } = "";
    public string Direction { get; set; } = "";
    public double StartValue { get; set; }
    public double EndValue { get; set; }
    public double Delta { get; set; }
    public bool Passed { get; set; }
}

public sealed class Ap4R3TimingCheck
{
    public string Name { get; set; } = "";
    public int PrimaryChangeIndex { get; set; }
    public int SecondaryChangeIndex { get; set; }
    public bool Passed { get; set; }
}
