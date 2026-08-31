using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalPhysicsR4VerificationTests
{
    [Fact]
    public void Physics_R4_IsolatedCorrelationCalibration_Passes()
    {
        var report = PhysicalPhysicsR4VerificationHarness.RunIsolatedCorrelationCalibration(42);
        Assert.True(report.Passed, string.Join("; ", report.Results.Where(r => r.Result != "Passed").Select(r => $"{r.PairId}:{r.Pearson:F3}:{r.Result}")));
        Assert.Equal(9, report.Results.Count);
    }

    [Machine12IntegrationFact]
    public async Task Physics_R4_EndToEnd_TwoMachines()
    {
        var runId = PhysicalPhysicsR4VerificationHarness.CreateVerificationRunId();
        var report = await PhysicalPhysicsR4VerificationHarness.RunEndToEndAsync(runId, 42, 99, TimeSpan.FromSeconds(90));
        Assert.True(report.TotalOpcUaUpdates > 0);
        Assert.True(report.TotalPhaseChanges >= 4);
        Assert.Equal(2, report.Machines.Count);
        Assert.All(report.Machines, m => Assert.True(m.TotalPublishedUpdates > 0));
        Assert.Contains(report.PhaseSegments, s => s.IsValid && s.SampleCount > 0);
        Assert.Equal(runId, report.VerificationRunId);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Physics_R4_EvidenceExport_WhenRequested()
    {
        if (!PhysicalVerificationSettings.IsExportMode)
        {
            return;
        }

        var runId = PhysicalPhysicsR4VerificationHarness.CreateVerificationRunId();
        var isolated = PhysicalPhysicsR4VerificationHarness.RunIsolatedCorrelationCalibration(42);
        Assert.True(isolated.Passed);
        var endToEnd = await PhysicalPhysicsR4VerificationHarness.RunEndToEndAsync(runId, 42, 99, TimeSpan.FromMinutes(5));
        await PhysicalPhysicsR4VerificationHarness.ExportEvidenceAsync(runId, isolated, endToEnd);
        Assert.True(Directory.Exists(PhysicalPhysicsR4VerificationHarness.EvidenceDirectory));
        Assert.True(File.Exists(Path.Combine(PhysicalPhysicsR4VerificationHarness.EvidenceDirectory, "AP-03-R4-opcua-end-to-end.json")));
        Assert.True(endToEnd.Passed, $"R4 failed: {string.Join(", ", endToEnd.FailedCriteria)}");
        Assert.Equal(runId, endToEnd.VerificationRunId);
        Assert.True(endToEnd.OpcUaMetrics.SuccessfulOpcUaUpdates == endToEnd.TotalOpcUaUpdates);
    }
}
