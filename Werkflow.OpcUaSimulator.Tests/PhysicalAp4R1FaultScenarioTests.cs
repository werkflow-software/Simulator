using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R1FaultScenarioTests
{
    [Fact]
    public async Task AP4R1_ScenarioManifest_Has22Scenarios()
    {
        var manifest = await PhysicalAp4R1VerificationHarness.BuildScenarioManifestAsync();
        Assert.Equal(22, manifest.FileCount);
        Assert.Equal(22, manifest.ScenarioIdCount);
        Assert.Empty(manifest.DuplicateScenarioIds);
        Assert.False(string.IsNullOrEmpty(manifest.ManifestHash));
    }

    [Fact]
    public async Task AP4R1_LaserOverheating_ThresholdSetsErrorActive()
    {
        var report = await PhysicalAp4R1VerificationHarness.RunThresholdFaultTimelineAsync(
            "laser-overheating-axis-drive",
            LaserProcessingMachine300ProfileFactory.ProfileId,
            42,
            25.0);
        Assert.True(report.Passed, string.Join("; ", report.FailedCriteria));
        Assert.NotNull(report.MachineFaultedAtUtc);
        Assert.Contains(report.Timeline, t => t.ErrorActive && t.MachineState == "Error");
    }

    [Fact]
    public async Task AP4R1_HydraulicLeak_ThresholdKeepsServerOnline()
    {
        var report = await PhysicalAp4R1VerificationHarness.RunThresholdFaultTimelineAsync(
            "hydraulic-leak",
            BendingHydraulicMachine300ProfileFactory.ProfileId,
            42,
            25.0);
        Assert.True(report.Passed, string.Join("; ", report.FailedCriteria));
        Assert.All(report.Timeline.Where(t => t.ErrorActive), t => Assert.True(t.ServerReachable));
    }

    [Fact]
    public async Task AP4R1_CommunicationDrop_TargetOfflineOthersOnline()
    {
        var report = await PhysicalAp4R1VerificationHarness.RunCommunicationDropVerificationAsync();
        Assert.True(report.Passed, string.Join("; ", report.FailedCriteria));
        Assert.True(report.TargetUnreachableDuringDrop);
        Assert.True(report.OthersReachableDuringDrop);
        Assert.True(report.AllReachableAfter);
    }

    [Fact]
    public async Task AP4R1_ComplexScenarios_PassIsolationChecks()
    {
        var report = await PhysicalAp4R1VerificationHarness.RunComplexScenarioVerificationAsync(42);
        Assert.True(report.Passed);
        Assert.True(report.Imbalance.MechanicalLoadSamples.Count >= 1);
        Assert.True(report.CoolantLoss.HiddenDeltaByState.Values.Any(v => Math.Abs(v) > 0.0001));
    }

    [Fact]
    public void AP4R1_ErrorPriority_HigherFaultDominatesMessage()
    {
        var bridge = new TestFaultScenarioSimulationBridge();
        var machineId = Guid.NewGuid();
        bridge.RegisterRuntimeState(new MachineRuntimeState { MachineId = machineId, State = MachineState.Running });

        bridge.SetMachineFault(machineId, "LOW", "Low priority", true, true, 5);
        bridge.SetMachineFault(machineId, "HIGH", "High priority", true, true, 1);
        Assert.Equal("High priority", bridge.GetOrCreate(machineId).ErrorMessage);

        bridge.ClearMachineFault(machineId, "HIGH");
        Assert.True(bridge.GetOrCreate(machineId).ErrorActive);
        Assert.Contains("Low", bridge.GetOrCreate(machineId).ErrorMessage, StringComparison.OrdinalIgnoreCase);

        bridge.ClearMachineFault(machineId, "LOW");
        Assert.False(bridge.GetOrCreate(machineId).ErrorActive);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task AP4R1_FinalEndToEnd_ShortRun()
    {
        var previous = Environment.GetEnvironmentVariable("AP4R1_E2E_SECONDS");
        Environment.SetEnvironmentVariable("AP4R1_E2E_SECONDS", "60");
        try
        {
            var runId = PhysicalAp4R1VerificationHarness.CreateVerificationRunId();
            var report = await PhysicalAp4R1VerificationHarness.RunFinalEndToEndAsync(runId);
            Assert.True(report.TotalOpcUaUpdates > 0);
            Assert.Equal(3, report.ActiveEngines);
            Assert.True(report.Passed);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AP4R1_E2E_SECONDS", previous);
        }
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task AP4R1_EvidenceExport_WhenRequested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("AP4R1_VERIFY_EXPORT"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var runId = PhysicalAp4R1VerificationHarness.CreateVerificationRunId();
        var manifest = await PhysicalAp4R1VerificationHarness.BuildScenarioManifestAsync();
        var laserThreshold = await PhysicalAp4R1VerificationHarness.RunThresholdFaultTimelineAsync(
            "laser-overheating-axis-drive", LaserProcessingMachine300ProfileFactory.ProfileId);
        var bendingThreshold = await PhysicalAp4R1VerificationHarness.RunThresholdFaultTimelineAsync(
            "hydraulic-leak", BendingHydraulicMachine300ProfileFactory.ProfileId);
        var commDrop = await PhysicalAp4R1VerificationHarness.RunCommunicationDropVerificationAsync();
        var complex = await PhysicalAp4R1VerificationHarness.RunComplexScenarioVerificationAsync();
        Environment.SetEnvironmentVariable("AP4R1_E2E_SECONDS", "120");
        var endToEnd = await PhysicalAp4R1VerificationHarness.RunFinalEndToEndAsync(runId);

        await PhysicalAp4R1VerificationHarness.ExportEvidenceAsync(
            runId, manifest, laserThreshold, bendingThreshold, commDrop, complex, endToEnd);

        Assert.True(File.Exists(Path.Combine(PhysicalAp4R1VerificationHarness.EvidenceDirectory, "AP-04-R1-scenario-manifest.json")));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4R1VerificationHarness.EvidenceDirectory, "AP-04-R1-final-end-to-end.json")));
        Assert.True(laserThreshold.Passed);
        Assert.True(bendingThreshold.Passed);
        Assert.True(commDrop.Passed);
        Assert.True(complex.Passed);
        Assert.True(endToEnd.Passed);
    }
}
