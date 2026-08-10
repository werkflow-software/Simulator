using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R5EvidenceTests
{
    [Fact]
    public async Task AP4R5_TruthVerification_CompletesUnderTwoMinutes()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var report = await PhysicalAp4R5VerificationHarness.RunTruthVerificationAsync(44, 25.0, cts.Token);
        Assert.True(report.Ap4R5Passed, string.Join(",", report.FailedCriteria));
        Assert.True(report.Ap4OverallPassed);
        Assert.True(report.Laser.Passed, string.Join(",", report.Laser.FailedCriteria));
        Assert.True(report.Hydraulic.Passed, string.Join(",", report.Hydraulic.FailedCriteria));
        Assert.True(report.SensorDriftRegression.Passed, string.Join(",", report.SensorDriftRegression.FailedCriteria));
    }

    [Fact]
    public void AP4R5_SelfConsistency_ValidatesDirectionChecks()
    {
        var report = new Ap4R5CompletenessReport
        {
            Laser = new Ap4R4RecoveryCaseResult
            {
                FaultDirectionChecks = [
                    new Ap4R4DirectionCheck
                    {
                        SignalId = "Axis01.MotorCurrent",
                        Direction = "increase",
                        Passed = true,
                        Delta = 1.2,
                        MinimumMeaningfulDelta = 0.15
                    }
                ]
            },
            Hydraulic = new Ap4R4RecoveryCaseResult
            {
                RecoveryDirectionChecks = [
                    new Ap4R4DirectionCheck
                    {
                        SignalId = "Hydraulic.SupplyPressure",
                        Direction = "toward-normal",
                        Passed = true,
                        DistanceToNormalStart = 0.5,
                        DistanceToNormalEnd = 0.2
                    }
                ],
                DistanceToNormal = new Ap4R4DistanceToNormal
                {
                    DistanceToNormalStart = 0.5,
                    DistanceToNormalEnd = 0.2,
                    RecoveryImproved = true
                }
            }
        };

        var failed = PhysicalAp4R5VerificationHarness.ValidateExportedSelfConsistency(report);
        Assert.Empty(failed);
    }

    [Fact]
    public void AP4R5_SelfConsistency_DetectsIncreaseInconsistency()
    {
        var failed = Ap4R5DirectionEvaluator.ValidateSelfConsistency([
            new Ap4R4DirectionCheck
            {
                SignalId = "Axis01.MotorCurrent",
                Direction = "increase",
                Passed = true,
                Delta = 0,
                MinimumMeaningfulDelta = 0.15
            }
        ]);
        Assert.Contains(failed, f => f.StartsWith("increase-inconsistent"));
    }

    [Fact]
    public async Task AP4R5_ExportEvidence_PassesSelfConsistency()
    {
        var runId = PhysicalAp4R5VerificationHarness.CreateVerificationRunId();
        var report = await PhysicalAp4R5VerificationHarness.RunTruthVerificationAsync(44, 25.0);
        await PhysicalAp4R5VerificationHarness.ExportEvidenceAsync(runId, report);
        var failed = PhysicalAp4R5VerificationHarness.ValidateExportedSelfConsistency(report);
        Assert.Empty(failed);
        Assert.True(File.Exists(Path.Combine(
            PhysicalAp4R5VerificationHarness.EvidenceDirectory,
            "AP-04-R5-validator-truth-verification.json")));
    }

    [Fact]
    public void AP4R5_NegativeTests_AllPass()
    {
        var report = PhysicalAp4R5VerificationHarness.RunTruthVerificationAsync(44, 25.0).GetAwaiter().GetResult();
        Assert.True(report.NegativeValidatorTests.All(t => t.Passed),
            string.Join(",", report.NegativeValidatorTests.Where(t => !t.Passed).Select(t => t.Name)));
    }
}
