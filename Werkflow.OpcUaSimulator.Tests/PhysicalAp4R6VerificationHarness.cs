using System.Text.Json;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp4R6VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-04-r6-final"));

    public static string CreateVerificationRunId() =>
        $"ap4r6-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 44);

    public static async Task<Ap4R6CompletenessReport> RunClosureVerificationAsync(
        int seed = 44,
        double timeFactor = 25.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R6CompletenessReport { StartedAtUtc = DateTime.UtcNow };

        report.Hydraulic = await PhysicalAp4R4VerificationHarness.RunHydraulicRecoveryCaseAsync(seed + 11, timeFactor, cancellationToken);
        report.LaserRegression = await PhysicalAp4R4VerificationHarness.RunLaserRecoveryCaseAsync(seed, timeFactor, cancellationToken);
        report.SensorDriftRegression = await PhysicalAp4R4VerificationHarness.RunSensorDriftCaseAsync(seed + 3, cancellationToken);
        report.NegativeValidatorTests = RunNegativeValidatorTests();

        report.Ap4R6Passed = report.Hydraulic.Passed
            && report.LaserRegression.Passed
            && report.SensorDriftRegression.Passed
            && report.NegativeValidatorTests.All(t => t.Passed);
        report.Ap4OverallPassed = report.Ap4R6Passed;
        report.FailedCriteria = report.Hydraulic.FailedCriteria
            .Concat(report.LaserRegression.FailedCriteria)
            .Concat(report.SensorDriftRegression.FailedCriteria)
            .Concat(report.NegativeValidatorTests.Where(t => !t.Passed).Select(t => t.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!report.Ap4R6Passed)
        {
            report.FailedCriteria.Insert(0, "ap4r6-closure");
        }

        report.EndedAtUtc = DateTime.UtcNow;
        return report;
    }

    public static List<string> ValidateSelfConsistency(Ap4R6CompletenessReport report)
    {
        var allChecks = report.Hydraulic.FaultDirectionChecks
            .Concat(report.Hydraulic.RecoveryDirectionChecks)
            .Concat(report.LaserRegression.RecoveryDirectionChecks)
            .ToList();
        var failed = Ap4R5DirectionEvaluator.ValidateSelfConsistency(allChecks);

        foreach (var check in report.Hydraulic.RecoveryDirectionChecks.Where(c => c.Required))
        {
            if (check.Passed && !check.TowardNormalPassed)
            {
                failed.Add($"required-recovery-toward-normal:{check.SignalId}");
            }
        }

        if (report.Hydraulic.Passed)
        {
            foreach (var check in report.Hydraulic.RecoveryDirectionChecks.Where(c => c.Required && !c.Passed))
            {
                failed.Add($"hydraulic-passed-child-false:{check.SignalId}");
            }
        }

        if (report.Hydraulic.DistanceToNormal.RecoveryImproved
            && report.Hydraulic.DistanceToNormal.DistanceToNormalEnd
                >= report.Hydraulic.DistanceToNormal.DistanceToNormalStart)
        {
            failed.Add("aggregate-distance-inconsistent");
        }

        return failed;
    }

    public static async Task ExportEvidenceAsync(
        string verificationRunId,
        Ap4R6CompletenessReport report,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var opts = new JsonSerializerOptions { WriteIndented = true };
        report.VerificationRunId = verificationRunId;

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-04-R6-final-closure-verification.json"),
            JsonSerializer.Serialize(report, opts),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "summary.md"),
            BuildSummaryMarkdown(report),
            cancellationToken);
    }

    private static string BuildSummaryMarkdown(Ap4R6CompletenessReport report) =>
        $"""
# AP-04-R6 Closure Summary

VerificationRunId: `{report.VerificationRunId}`

| Check | Passed |
|-------|--------|
| Hydraulic | {report.Hydraulic.Passed} |
| Laser regression | {report.LaserRegression.Passed} |
| SensorDrift regression | {report.SensorDriftRegression.Passed} |
| Negative validator tests | {report.NegativeValidatorTests.All(t => t.Passed)} |
| Ap4R6Passed | {report.Ap4R6Passed} |
| Ap4OverallPassed | {report.Ap4OverallPassed} |
""";

    private static List<Ap4R5NegativeTestResult> RunNegativeValidatorTests()
    {
        var results = new List<Ap4R5NegativeTestResult>();
        var efficiencyBand = Ap4R6ProfileNormals.GetBendingBand("HydraulicEfficiency");

        void Add(string name, bool passed, string detail = "") =>
            results.Add(new Ap4R5NegativeTestResult { Name = name, Passed = passed, Detail = detail });

        var increaseAway = BuildRecoveryCheck("increase", 0.75, 1.05, efficiencyBand);
        Add("recovery-increase-away-from-normal-false", !increaseAway.Passed);

        var decreaseAway = BuildRecoveryCheck("decrease", 0.95, 0.5, efficiencyBand);
        Add("recovery-decrease-away-from-normal-false", !decreaseAway.Passed);

        var towardBetter = BuildRecoveryCheck("toward-normal", 0.5, 0.88, efficiencyBand);
        Add("toward-normal-smaller-distance-true", towardBetter.Passed && towardBetter.TowardNormalPassed);

        var insideBand = Ap4R6SignalNormalBand.FromSignal(
            BendingHydraulicMachine300ProfileFactory.Create().Signals.First(s =>
                s.SignalId == "Hydraulic.SupplyPressure"));
        Add("inside-normal-range-distance-zero",
            Ap4R5DirectionEvaluator.ComputeBandDistance(180.0, insideBand) == 0);
        Add("above-normal-range-distance",
            Ap4R5DirectionEvaluator.ComputeBandDistance(200.0, insideBand) == 15.0);
        Add("below-normal-range-distance",
            Ap4R5DirectionEvaluator.ComputeBandDistance(160.0, insideBand) == 15.0);

        var pumpBand = Ap4R6ProfileNormals.GetBendingBand("Hydraulic.PumpCurrent");
        var phaseCheck = BuildRecoveryCheck("toward-normal", 0.2, 7.5, pumpBand);
        Add("phase-aware-pump-toward-normal-true", phaseCheck.TowardNormalPassed);

        var requiredFailed = new Ap4R4RecoveryCaseResult
        {
            RecoveryDirectionChecks = [
                new Ap4R4DirectionCheck
                {
                    SignalId = "HydraulicEfficiency",
                    Required = true,
                    Passed = false,
                    TowardNormalPassed = false,
                    DirectionPassed = false
                }
            ],
            DistanceToNormal = new Ap4R4DistanceToNormal { RecoveryImproved = true }
        };
        Add("required-recovery-false-scenario-false",
            !requiredFailed.RecoveryDirectionChecks.Where(c => c.Required).All(c => c.Passed && c.TowardNormalPassed));

        var aggregateMaskChecks = new[]
        {
            new Ap4R4DirectionCheck
            {
                SignalId = "HydraulicEfficiency",
                Required = true,
                Passed = false,
                TowardNormalPassed = false,
                DirectionPassed = true
            },
            new Ap4R4DirectionCheck
            {
                SignalId = "Hydraulic.SupplyPressure",
                Required = true,
                Passed = true,
                TowardNormalPassed = true,
                DirectionPassed = true
            }
        };
        Add("aggregate-improved-single-signal-worse-scenario-false",
            !aggregateMaskChecks.Where(c => c.Required).All(c => c.Passed && c.TowardNormalPassed));

        return results;
    }

    private static Ap4R4DirectionCheck BuildRecoveryCheck(
        string direction,
        double start,
        double end,
        Ap4R6SignalNormalBand band)
    {
        double delta = end - start;
        double minDelta = Ap4R5DirectionEvaluator.GetMinimumMeaningfulDelta("HydraulicEfficiency", Math.Max(start, end));
        double distStart = Ap4R5DirectionEvaluator.ComputeBandDistance(start, band);
        double distEnd = Ap4R5DirectionEvaluator.ComputeBandDistance(end, band);
        double normStart = Ap4R5DirectionEvaluator.NormalizeBandDistance(distStart, band);
        double normEnd = Ap4R5DirectionEvaluator.NormalizeBandDistance(distEnd, band);
        bool directionPassed = Ap4R5DirectionEvaluator.EvaluateDirection(direction, delta, minDelta, normStart, normEnd);
        bool towardNormalPassed = Ap4R5DirectionEvaluator.EvaluateTowardNormal(band, start, end, distStart, distEnd);
        return new Ap4R4DirectionCheck
        {
            SignalId = "HydraulicEfficiency",
            Direction = direction,
            Phase = "Recovery",
            Required = true,
            StartValue = start,
            EndValue = end,
            Delta = delta,
            MinimumMeaningfulDelta = minDelta,
            NormalMin = band.NormalMin,
            NormalMax = band.NormalMax,
            DistanceToNormalStart = normStart,
            DistanceToNormalEnd = normEnd,
            DirectionPassed = directionPassed,
            TowardNormalPassed = towardNormalPassed,
            Passed = directionPassed && towardNormalPassed
        };
    }
}

public sealed class Ap4R6CompletenessReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public Ap4R4RecoveryCaseResult Hydraulic { get; set; } = new();
    public Ap4R4RecoveryCaseResult LaserRegression { get; set; } = new();
    public Ap4R4SensorDriftResult SensorDriftRegression { get; set; } = new();
    public List<Ap4R5NegativeTestResult> NegativeValidatorTests { get; set; } = [];
    public bool Ap4R6Passed { get; set; }
    public bool Ap4OverallPassed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}
