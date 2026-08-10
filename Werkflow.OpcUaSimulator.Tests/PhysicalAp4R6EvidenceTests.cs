using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R6EvidenceTests
{
    [Fact]
    public async Task AP4R6_ClosureVerification_Passes()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var report = await PhysicalAp4R6VerificationHarness.RunClosureVerificationAsync(44, 25.0, cts.Token);
        Assert.True(report.Ap4R6Passed, string.Join(",", report.FailedCriteria));
        Assert.True(report.Ap4OverallPassed);
        Assert.True(report.Hydraulic.Passed, string.Join(",", report.Hydraulic.FailedCriteria));
        Assert.True(report.LaserRegression.Passed, string.Join(",", report.LaserRegression.FailedCriteria));
        Assert.True(report.SensorDriftRegression.Passed, string.Join(",", report.SensorDriftRegression.FailedCriteria));

        foreach (var check in report.Hydraulic.RecoveryDirectionChecks.Where(c => c.Required))
        {
            Assert.True(check.TowardNormalPassed, check.SignalId);
            Assert.True(check.Passed, check.SignalId);
        }

        Assert.True(report.Hydraulic.DistanceToNormal.RecoveryImproved);
    }

    [Fact]
    public void AP4R6_SelfConsistency_Passes()
    {
        var report = PhysicalAp4R6VerificationHarness.RunClosureVerificationAsync(44, 25.0).GetAwaiter().GetResult();
        var failed = PhysicalAp4R6VerificationHarness.ValidateSelfConsistency(report);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task AP4R6_ExportEvidence_PassesSelfConsistency()
    {
        var runId = PhysicalAp4R6VerificationHarness.CreateVerificationRunId();
        var report = await PhysicalAp4R6VerificationHarness.RunClosureVerificationAsync(44, 25.0);
        await PhysicalAp4R6VerificationHarness.ExportEvidenceAsync(runId, report);
        var failed = PhysicalAp4R6VerificationHarness.ValidateSelfConsistency(report);
        Assert.Empty(failed);
        Assert.True(File.Exists(Path.Combine(
            PhysicalAp4R6VerificationHarness.EvidenceDirectory,
            "AP-04-R6-final-closure-verification.json")));
    }

    [Fact]
    public void AP4R6_NegativeTests_AllPass()
    {
        var tests = PhysicalAp4R6VerificationHarness.RunClosureVerificationAsync(44, 25.0).GetAwaiter().GetResult().NegativeValidatorTests;
        Assert.True(tests.All(t => t.Passed), string.Join(",", tests.Where(t => !t.Passed).Select(t => t.Name)));
    }
}
