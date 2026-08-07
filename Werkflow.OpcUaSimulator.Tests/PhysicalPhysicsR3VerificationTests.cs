using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalPhysicsR3VerificationTests
{
    [Fact]
    public void Physics_R3_ModelVerification()
    {
        var report = PhysicalPhysicsR3VerificationHarness.RunModelVerification(42);
        Assert.True(report.Passed);
        Assert.True(report.Laser.PhaseChanges >= 4);
        Assert.True(report.Bending.PhaseChanges >= 4);
        Assert.True(report.DependencyChecks.Count >= 12);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Physics_R3_EndToEnd_TwoMachines()
    {
        var report = await PhysicalPhysicsR3VerificationHarness.RunEndToEndAsync(42, 99, TimeSpan.FromSeconds(90));
        Assert.True(report.TotalOpcUaUpdates > 0);
        Assert.True(report.TotalPhaseChanges >= 4);
        Assert.Equal(2, report.Machines.Count);
        Assert.All(report.Machines, m => Assert.True(m.TotalPublishedUpdates > 0));
        Assert.Contains(report.DataChangeSamples, s => s.SourceTimestampUpdated);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Physics_R3_EvidenceExport_WhenRequested()
    {
        if (!PhysicalVerificationSettings.IsExportMode)
        {
            return;
        }

        var model = PhysicalPhysicsR3VerificationHarness.RunModelVerification(42);
        var endToEnd = await PhysicalPhysicsR3VerificationHarness.RunEndToEndAsync(42, 99, TimeSpan.FromMinutes(5));
        await PhysicalPhysicsR3VerificationHarness.ExportEvidenceAsync(model, endToEnd);
        Assert.True(Directory.Exists(PhysicalPhysicsR3VerificationHarness.EvidenceDirectory));
        Assert.True(File.Exists(Path.Combine(PhysicalPhysicsR3VerificationHarness.EvidenceDirectory, "AP-03-R3-opcua-end-to-end.json")));
        Assert.True(endToEnd.Passed, "R3 end-to-end verification must pass");
    }
}
