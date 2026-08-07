using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Tests;

internal static class Ap4R2TimelineValidator
{
    public static Ap4R2PassEvaluation ValidateFaultRecoveryCase(Ap4R2FaultRecoveryCase report)
    {
        var failed = new List<string>();

        if (report.ThresholdFirstReachedAtUtc == null)
        {
            failed.Add("threshold-first-reached-missing");
        }

        if (report.ThresholdConfirmedAtUtc == null)
        {
            failed.Add("threshold-confirmed-missing");
        }

        if (report.MachineFaultedAtUtc == null)
        {
            failed.Add("machine-faulted-missing");
        }

        if (report.RecoveryStartedAtUtc == null)
        {
            failed.Add("recovery-started-missing");
        }

        if (report.RecoveryCompletedAtUtc == null)
        {
            failed.Add("recovery-completed-missing");
        }

        if (report.ThresholdFirstReachedAtUtc != null
            && report.ThresholdConfirmedAtUtc != null
            && report.ThresholdConfirmedAtUtc < report.ThresholdFirstReachedAtUtc)
        {
            failed.Add("threshold-confirmed-before-first-reached");
        }

        if (report.ThresholdConfirmedAtUtc != null
            && report.MachineFaultedAtUtc != null
            && report.MachineFaultedAtUtc < report.ThresholdConfirmedAtUtc)
        {
            failed.Add("machine-faulted-before-threshold-confirmed");
        }

        if (report.MachineFaultedAtUtc != null
            && report.RecoveryStartedAtUtc != null
            && report.RecoveryStartedAtUtc <= report.MachineFaultedAtUtc)
        {
            failed.Add("recovery-started-before-fault");
        }

        if (report.RecoveryStartedAtUtc != null
            && report.RecoveryCompletedAtUtc != null
            && report.RecoveryCompletedAtUtc <= report.RecoveryStartedAtUtc)
        {
            failed.Add("recovery-completed-before-started");
        }

        if (!report.Timeline.Any(t => !t.ErrorActive && t.MachineState != nameof(MachineState.Error)))
        {
            failed.Add("no-pre-fault-sample");
        }

        if (!report.Timeline.Any(t => t.ErrorActive && t.MachineState == nameof(MachineState.Error)))
        {
            failed.Add("no-faulted-error-active-sample");
        }

        if (!report.Timeline.Any(t => t.ErrorActive && !string.IsNullOrEmpty(t.ErrorMessage)))
        {
            failed.Add("no-error-message-sample");
        }

        if (report.Timeline.Any(t => t.ErrorActive && !t.ServerReachable))
        {
            failed.Add("server-offline-during-fault");
        }

        if (report.Timeline.Any(t => t.ErrorActive && t.ProductionRunning))
        {
            failed.Add("production-running-during-fault");
        }

        if (!report.Timeline.Any(t => !t.ErrorActive && t.ProductionRunning) && report.ExpectProductionResume)
        {
            failed.Add("production-not-resumed");
        }

        if (report.Timeline.Any(t => string.IsNullOrEmpty(t.ScenarioId)))
        {
            failed.Add("empty-scenario-id-in-timeline");
        }

        return new Ap4R2PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    public static Ap4R2PassEvaluation ValidateEndToEnd(Ap4R2FinalEndToEndReport report)
    {
        var failed = new List<string>();

        if (!report.ThresholdTimelinePassed)
        {
            failed.Add("threshold-timeline");
        }

        if (!report.FaultNodesPassed)
        {
            failed.Add("fault-nodes");
        }

        if (!report.ProductionStopPassed)
        {
            failed.Add("production-stop");
        }

        if (!report.PhysicalServerOnlinePassed)
        {
            failed.Add("physical-server-online");
        }

        if (!report.RecoveryPassed)
        {
            failed.Add("recovery");
        }

        if (!report.LifecyclePassed)
        {
            failed.Add("lifecycle");
        }

        if (report.TotalOpcUaUpdates <= 0)
        {
            failed.Add("no-opcua-updates");
        }

        if (report.Exceptions.Count > 0)
        {
            failed.Add("exceptions");
        }

        failed.AddRange(report.FailedCriteria);

        return new Ap4R2PassEvaluation
        {
            Passed = failed.Count == 0,
            FailedCriteria = failed.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    public static int CountPeaks(IReadOnlyList<double> samples, double minAmplitude = 0.01)
    {
        if (samples.Count < 3)
        {
            return 0;
        }

        var peaks = 0;
        for (var i = 1; i < samples.Count - 1; i++)
        {
            if (samples[i] > samples[i - 1] && samples[i] > samples[i + 1]
                && samples[i] - samples.Min() >= minAmplitude)
            {
                peaks++;
            }
        }

        return peaks;
    }
}

internal sealed class Ap4R2PassEvaluation
{
    public bool Passed { get; init; }
    public List<string> FailedCriteria { get; init; } = [];
}
