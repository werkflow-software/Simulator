using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R2FaultScenarioTests
{
    [Fact]
    public void AP4R2_TimelineValidator_RejectedWhenThresholdConfirmedWithoutFirstReached()
    {
        var report = new Ap4R2FaultRecoveryCase
        {
            ThresholdConfirmedAtUtc = DateTime.UtcNow,
            ThresholdFirstReachedAtUtc = null
        };
        var eval = Ap4R2TimelineValidator.ValidateFaultRecoveryCase(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("threshold-first-reached"));
    }

    [Fact]
    public void AP4R2_TimelineValidator_RequiresErrorActiveDuringFault()
    {
        var report = new Ap4R2FaultRecoveryCase
        {
            ThresholdFirstReachedAtUtc = DateTime.UtcNow.AddSeconds(-30),
            ThresholdConfirmedAtUtc = DateTime.UtcNow.AddSeconds(-20),
            MachineFaultedAtUtc = DateTime.UtcNow.AddSeconds(-20),
            RecoveryStartedAtUtc = DateTime.UtcNow.AddSeconds(-10),
            RecoveryCompletedAtUtc = DateTime.UtcNow,
            Timeline = [
                new Ap4R2TimelineSample { ErrorActive = false, MachineState = nameof(MachineState.Running), ScenarioId = "test" }
            ]
        };
        var eval = Ap4R2TimelineValidator.ValidateFaultRecoveryCase(report);
        Assert.False(eval.Passed);
    }

    [Fact]
    public async Task AP4R2_FaultRecovery_LaserAndHydraulicPass()
    {
        var report = await PhysicalAp4R2VerificationHarness.RunFaultRecoveryVerificationAsync(42, 35.0);
        Assert.True(report.Passed, string.Join("; ", report.Laser.FailedCriteria.Concat(report.Bending.FailedCriteria)));
        Assert.NotNull(report.Laser.ThresholdFirstReachedAtUtc);
        Assert.NotNull(report.Laser.RecoveryCompletedAtUtc);
        Assert.NotNull(report.Bending.ThresholdFirstReachedAtUtc);
        Assert.NotNull(report.Bending.RecoveryCompletedAtUtc);
    }

    [Fact]
    public async Task AP4R2_ComplexScenarios_PassDirectedChecks()
    {
        var report = await PhysicalAp4R2VerificationHarness.RunComplexScenarioVerificationAsync(42);
        Assert.True(report.Passed,
            $"imbalance={report.Imbalance.Passed} periodic={report.Imbalance.PeriodicBehavior} " +
            $"drift={report.SensorDrift.Passed} coolant={report.CoolantLoss.Passed} " +
            $"hydraulic={report.HydraulicLeak.Passed} intermittent={report.Intermittent.Passed} " +
            string.Join("; ", report.CoolantLoss.FailedCriteria));
        Assert.True(report.Imbalance.PeriodicBehavior);
        Assert.True(report.SensorDrift.HiddenStableWhileSignalMoves);
        Assert.True(report.Intermittent.EpisodeCount >= 3);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task AP4R2_FinalEndToEnd_ShortRun()
    {
        Environment.SetEnvironmentVariable("AP4R2_E2E_SECONDS", "120");
        var report = await PhysicalAp4R2VerificationHarness.RunFinalEndToEndAsync(PhysicalAp4R2VerificationHarness.CreateVerificationRunId());
        Assert.True(report.TotalOpcUaUpdates > 0);
        Assert.True(report.Passed, string.Join("; ", report.FailedCriteria));
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task AP4R2_EvidenceExport_WhenRequested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("AP4R2_VERIFY_EXPORT"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var runId = PhysicalAp4R2VerificationHarness.CreateVerificationRunId();
        var manifest = await PhysicalAp4R2VerificationHarness.BuildScenarioManifestAsync();
        var faultRecovery = await PhysicalAp4R2VerificationHarness.RunFaultRecoveryVerificationAsync();
        var complex = await PhysicalAp4R2VerificationHarness.RunComplexScenarioVerificationAsync();
        Environment.SetEnvironmentVariable("AP4R2_E2E_SECONDS", "120");
        var endToEnd = await PhysicalAp4R2VerificationHarness.RunFinalEndToEndAsync(runId);

        await PhysicalAp4R2VerificationHarness.ExportEvidenceAsync(runId, manifest, faultRecovery, complex, endToEnd);

        Assert.True(faultRecovery.Passed);
        Assert.True(complex.Passed);
        Assert.True(endToEnd.Passed);
        Assert.True(File.Exists(Path.Combine(PhysicalAp4R2VerificationHarness.EvidenceDirectory, "AP-04-R2-fault-recovery-verification.json")));
    }
}
