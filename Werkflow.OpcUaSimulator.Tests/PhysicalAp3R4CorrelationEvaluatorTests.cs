using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalAp3R4CorrelationEvaluatorTests
{
    [Fact]
    public void PositiveCorrelation_WithinMinMax_Passed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = 0.55, Spearman = 0.5, SampleCount = 100,
            ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88, ExpectedLagSeconds = 0, StrongestLag = 0
        });
        Assert.Equal("Passed", result.Result);
    }

    [Fact]
    public void NegativeCorrelation_WithinMinMax_Passed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = -0.55, Spearman = -0.5, SampleCount = 100,
            ExpectedDirection = "negative", MinPearson = 0.30, MaxPearson = 0.85, ExpectedLagSeconds = 0, StrongestLag = 0
        });
        Assert.Equal("Passed", result.Result);
    }

    [Fact]
    public void PositiveCorrelation_BelowMinimum_Failed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = 0.2, SampleCount = 100, ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88
        });
        Assert.Equal("Failed", result.Result);
    }

    [Fact]
    public void NegativeCorrelation_BelowMinimum_Failed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = -0.1, SampleCount = 100, ExpectedDirection = "negative", MinPearson = 0.30, MaxPearson = 0.85
        });
        Assert.Equal("Failed", result.Result);
    }

    [Fact]
    public void PositiveCorrelation_AboveMaximum_Failed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = 0.97, SampleCount = 100, ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88
        });
        Assert.Equal("Failed", result.Result);
    }

    [Fact]
    public void NegativeCorrelation_AboveMaximum_Failed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = -0.97, SampleCount = 100, ExpectedDirection = "negative", MinPearson = 0.30, MaxPearson = 0.85
        });
        Assert.Equal("Failed", result.Result);
    }

    [Fact]
    public void WrongDirection_Failed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = -0.6, SampleCount = 100, ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88
        });
        Assert.Equal("Failed", result.Result);
    }

    [Fact]
    public void ImplausibleLag_Failed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = 0.6, SampleCount = 100, ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88,
            ExpectedLagSeconds = 0, StrongestLag = 40
        });
        Assert.Equal("Failed", result.Result);
    }

    [Fact]
    public void FailedMandatoryCorrelation_SetsOverallStatusFalse()
    {
        var report = BuildPassingReport();
        report.Correlations = [new R4CorrelationEvaluation { Result = "Failed" }];
        Assert.False(PhysicalPhysicsR4VerificationHarness.EvaluateEndToEndPassForTests(report));
    }

    [Fact]
    public void Review_DoesNotAutoPass()
    {
        var report = BuildPassingReport();
        report.Correlations = [new R4CorrelationEvaluation { Result = "Review", SampleCount = 100 }];
        Assert.False(PhysicalPhysicsR4VerificationHarness.EvaluateEndToEndPassForTests(report));
    }

    [Fact]
    public void ExactMinimum_IsAllowed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = 0.35, SampleCount = 50, ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88
        });
        Assert.Equal("Passed", result.Result);
    }

    [Fact]
    public void ExactMaximum_IsAllowed()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = 0.88, SampleCount = 50, ExpectedDirection = "positive", MinPearson = 0.35, MaxPearson = 0.88
        });
        Assert.Equal("Passed", result.Result);
    }

    [Fact]
    public void NegativeRelationship_UsesAbsolutePearsonForBounds()
    {
        var result = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = -0.75, SampleCount = 50, ExpectedDirection = "negative", MinPearson = 0.30, MaxPearson = 0.85
        });
        Assert.Equal("Passed", result.Result);
        Assert.True(result.MaxStrengthMet);
    }

    private static R4EndToEndVerificationReport BuildPassingReport() =>
        new()
        {
            Correlations = [new R4CorrelationEvaluation { Result = "Passed" }],
            PhaseSegments = [PhysicalAp3R4TestHelpers.CreateValidSegment()],
            Statistics = PhysicalAp3R4TestHelpers.CreateMinimalStatistics(),
            Machines = [PhysicalAp3R4TestHelpers.CreateMachineReport()],
            DataChangeSamples = [new R4DataChangeSample { SourceTimestampUpdated = true }],
            JobSnapshotValidation = new R4JobSnapshotValidation
            {
                Passed = true,
                MachineResults = [new R4JobSnapshotMachineResult { Passed = true }]
            },
            PhaseComparisons = new R4PhaseComparisonReport
            {
                Passed = true,
                Items = [new R4PhaseComparisonItem
                {
                    IdleLoadBelowProcessing = true,
                    PeakLoadAboveProcessing = true,
                    IdleCurrentBelowProcessing = true
                }]
            },
            TotalOpcUaUpdates = 1000,
            OpcUaMetrics = new R4OpcUaUpdateMetrics { SuccessfulOpcUaUpdates = 1000 }
        };
}

internal static class PhysicalAp3R4TestHelpers
{
    public static PhysicalPhaseSegmentSnapshot CreateValidSegment() =>
        new()
        {
            MachineId = Guid.NewGuid(),
            Phase = "Processing",
            IsValid = true,
            SampleCount = 10,
            AverageLoad = 20,
            JobName = "JOB-001",
            PartName = "PART-A"
        };

    public static List<SignalStatisticsSnapshot> CreateMinimalStatistics()
    {
        var laser = Enumerable.Range(0, 31).Select(i => new SignalStatisticsSnapshot
        {
            ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId,
            SignalId = $"Signal{i}",
            PhaseEvaluationPassed = true
        });
        var bending = Enumerable.Range(0, 31).Select(i => new SignalStatisticsSnapshot
        {
            ProfileId = BendingHydraulicMachine300ProfileFactory.ProfileId,
            SignalId = $"Signal{i}",
            PhaseEvaluationPassed = true
        });
        return laser.Concat(bending).ToList();
    }

    public static R4MachineReport CreateMachineReport() =>
        new()
        {
            MachineId = Guid.NewGuid(),
            JobChanges = 2,
            DistinctPhases = 8,
            TotalPublishedUpdates = 500,
            AveragePublishDurationMs = 0.5
        };
}
