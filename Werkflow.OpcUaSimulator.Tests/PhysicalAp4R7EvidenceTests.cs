using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R7EvidenceTests
{
    [Fact]
    public async Task AP4R7_HydraulicEfficiencyRecovery_Passes()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var report = await PhysicalAp4R7VerificationHarness.RunHydraulicRecoveryVerificationAsync(cts.Token);

        Assert.NotNull(report.EfficiencyRecovery);
        Assert.True(report.EfficiencyRecovery.FaultEndValue < report.EfficiencyRecovery.NormalMin,
            $"faultPeak={report.EfficiencyRecovery.FaultEndValue}");
        Assert.True(
            report.EfficiencyRecovery.DistanceToNormalEnd <= report.EfficiencyRecovery.DistanceToNormalStart,
            $"start={report.EfficiencyRecovery.DistanceToNormalStart} end={report.EfficiencyRecovery.DistanceToNormalEnd}");
        Assert.True(report.EfficiencyRecovery.InNormalRangeAtCompletion);
        Assert.True(report.EfficiencyRecovery.TowardNormalPassed);
        Assert.True(report.EfficiencyRecovery.PostRecoveryStable);
        Assert.True(report.EfficiencyRecovery.RecoveryEndValue <= report.EfficiencyRecovery.NormalMax);
        Assert.True(report.SupplyPressureRegression);
        Assert.True(report.PumpCurrentRegression);
        Assert.True(report.ValidatorRegression);
        Assert.True(report.Ap4R7Passed, string.Join(",", report.FailedCriteria));
        Assert.True(report.Ap4OverallPassed);
    }

    [Fact]
    public async Task AP4R7_SupplyPressureFaultDirection_Passes()
    {
        var hydraulic = await PhysicalAp4R4VerificationHarness.RunHydraulicRecoveryCaseAsync(44, 25, CancellationToken.None);
        var check = hydraulic.FaultDirectionChecks.First(c => c.SignalId == "Hydraulic.SupplyPressure");
        Assert.True(check.Passed, $"start={check.StartValue} end={check.EndValue} delta={check.Delta}");
    }

    [Fact]
    public void AP4R7_NegativeTests_AllPass()
    {
        var results = PhysicalAp4R7VerificationHarness.RunNegativeTests();
        Assert.True(results.All(r => r.Passed), string.Join(",", results.Where(r => !r.Passed).Select(r => r.Name)));
    }

    [Fact]
    public void AP4R7_R6OvershootCase_RemainsFalse()
    {
        var overshoot = PhysicalAp4R7VerificationHarness.RunNegativeTests()
            .First(r => r.Name == "r6-case-0.477-to-1.2-still-false");
        Assert.True(overshoot.Passed);
    }

    [Fact]
    public async Task AP4R7_ExportEvidence_Succeeds()
    {
        var runId = PhysicalAp4R7VerificationHarness.CreateVerificationRunId();
        var report = await PhysicalAp4R7VerificationHarness.RunHydraulicRecoveryVerificationAsync();
        await PhysicalAp4R7VerificationHarness.ExportEvidenceAsync(runId, report);
        Assert.True(File.Exists(Path.Combine(
            PhysicalAp4R7VerificationHarness.EvidenceDirectory,
            "AP-04-R7-final-hydraulic-recovery-verification.json")));
    }
}
