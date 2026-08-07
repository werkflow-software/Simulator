using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalPhysicsR2VerificationTests
{
    [Fact]
    public void Physics_R2_ModelVerification()
    {
        var report = PhysicalPhysicsR2VerificationHarness.RunModelVerification(42);
        Assert.True(report.Passed);
        Assert.True(report.Laser.PhaseChanges >= 4);
        Assert.True(report.Bending.PhaseChanges >= 4);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Physics_R2_EndToEnd_TwoMachines()
    {
        var report = await PhysicalPhysicsR2VerificationHarness.RunEndToEndAsync(42, 99, TimeSpan.FromSeconds(90));
        Assert.True(report.TotalOpcUaUpdates > 0, "Expected OPC UA updates > 0");
        Assert.True(report.TotalPhaseChanges >= 4, $"Expected phase changes >= 4, got {report.TotalPhaseChanges}");
        Assert.Equal(2, report.Machines.Count);
        Assert.All(report.Machines, m => Assert.True(m.TotalPublishedUpdates > 0));
        Assert.All(report.Machines, m => Assert.True(m.AveragePublishDurationMs > 0));
        Assert.Contains(report.DataChangeSamples, s => s.SourceTimestampUpdated);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Physics_R2_EvidenceExport_WhenRequested()
    {
        if (!PhysicalVerificationSettings.IsExportMode)
        {
            return;
        }

        var model = PhysicalPhysicsR2VerificationHarness.RunModelVerification(42);
        var endToEnd = await PhysicalPhysicsR2VerificationHarness.RunEndToEndAsync(
            42, 99, TimeSpan.FromMinutes(5));
        await PhysicalPhysicsR2VerificationHarness.ExportEvidenceAsync(model, endToEnd);
        Assert.True(Directory.Exists(PhysicalPhysicsR2VerificationHarness.EvidenceDirectory));
    }
}
