using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4FaultScenarioTests
{
    [Fact]
    public async Task AP4_CatalogValidation_PassesForAllScenarios()
    {
        var report = await PhysicalAp4VerificationHarness.RunCatalogValidationAsync();
        Assert.True(PhysicalAp4VerificationHarness.EvaluateCatalogPassForTests(report), string.Join("; ", report.FailedCriteria));
        Assert.Equal(PhysicalAp4VerificationSettings.ExpectedScenarioCount, report.TotalScenarios);
        Assert.True(report.CatalogValid, string.Join("; ", report.CatalogErrors));
        Assert.All(report.ProfileResults, r => Assert.True(r.IsValid, $"{r.ScenarioId}/{r.ProfileId}: {string.Join("; ", r.Errors)}"));
    }

    [Fact]
    public async Task AP4_EachScenario_LoadsAndValidatesForCompatibleProfile()
    {
        var report = await PhysicalAp4VerificationHarness.RunCatalogValidationAsync();
        Assert.Equal(PhysicalAp4VerificationSettings.ExpectedScenarioCount, report.TotalScenarios);
        Assert.Equal(
            report.TotalScenarios,
            report.ProfileResults.Select(r => r.ScenarioId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task AP4_ModelTests_AllScenariosProduceMeasuredEffects()
    {
        var report = await PhysicalAp4VerificationHarness.RunModelTestsForAllScenariosAsync(42, 12.0);
        Assert.True(PhysicalAp4VerificationHarness.EvaluateModelTestsPassForTests(report),
            string.Join("; ", report.Results.Where(r => !r.Passed).Select(r => $"{r.ScenarioId}/{r.ProfileId}:{r.Error}:{r.ActiveScenarioCount}")));
        Assert.NotEmpty(report.Results);
        Assert.All(report.Results, r => Assert.True(r.Passed, $"{r.ScenarioId}/{r.ProfileId}: {r.Error ?? string.Join(", ", r.DirectionChecks.Where(c => !c.Passed).Select(c => $"{c.HiddenStateId}={c.MeasuredDelta:F4}"))}"));
    }

    [Fact]
    public async Task AP4_Overheating_IncreasesMechanicalAndThermalLoad()
    {
        var report = await PhysicalAp4VerificationHarness.RunModelTestsForAllScenariosAsync(42, 12.0);
        var overheating = report.Results.Single(r =>
            r.ScenarioId == "laser-overheating-axis-drive"
            && r.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId);

        var mechanical = overheating.DirectionChecks.Single(c => c.HiddenStateId == "MechanicalLoad");
        var thermal = overheating.DirectionChecks.Single(c => c.HiddenStateId == "ThermalLoad");
        Assert.True(mechanical.Passed);
        Assert.True(thermal.Passed);
        Assert.True(mechanical.MeasuredDelta > 0);
        Assert.True(thermal.MeasuredDelta > 0);
    }

    [Fact]
    public async Task AP4_HydraulicLeak_DecreasesHydraulicEfficiency()
    {
        var report = await PhysicalAp4VerificationHarness.RunModelTestsForAllScenariosAsync(42, 12.0);
        var leak = report.Results.Single(r =>
            r.ScenarioId == "hydraulic-leak"
            && r.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId);

        var efficiency = leak.DirectionChecks.FirstOrDefault(c => c.HiddenStateId == "HydraulicEfficiency")
            ?? leak.DirectionChecks.First(c => c.HiddenStateId == "PressLoad");
        Assert.True(efficiency.Passed);
        Assert.True(efficiency.MeasuredDelta < 0);
    }

    [Fact]
    public async Task AP4_Lifecycle_DuplicateStartPauseResumeStopCancelReset()
    {
        var report = await PhysicalAp4VerificationHarness.RunLifecycleTestsAsync(42);
        Assert.True(PhysicalAp4VerificationHarness.EvaluateLifecyclePassForTests(report), string.Join("; ", report.FailedCriteria));
        Assert.Single(report.Results);
        Assert.True(report.Results[0].Passed);
    }

    [Fact]
    public async Task AP4_Combination_CoolantLossAndMaterialResistance()
    {
        var report = await PhysicalAp4VerificationHarness.RunCombinationTestsAsync(42, 12.0);
        Assert.True(PhysicalAp4VerificationHarness.EvaluateCombinationPassForTests(report), string.Join("; ", report.FailedCriteria));
        Assert.True(report.BothScenariosActive);
        Assert.True(report.CoolingEfficiencyDecreased);
        Assert.True(report.MaterialResistanceIncreased);
    }

    [Fact]
    public async Task AP4_Recovery_HydraulicLeakRecoversEfficiency()
    {
        var report = await PhysicalAp4VerificationHarness.RunRecoveryTestsAsync(42, 15.0);
        Assert.True(PhysicalAp4VerificationHarness.EvaluateRecoveryPassForTests(report), string.Join("; ", report.FailedCriteria));
        Assert.True(report.RecoveryStarted);
        Assert.True(report.RecoveryCompleted);
        Assert.True(report.EfficiencyAccumulator < 0 || report.PressLoadAccumulator < 0);
        Assert.True(report.EfficiencyImprovedAfterRecovery);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task AP4_ShortEndToEnd_MultiMachineScenarios()
    {
        var previousSeconds = Environment.GetEnvironmentVariable("AP4_E2E_SECONDS");
        Environment.SetEnvironmentVariable("AP4_E2E_SECONDS", "30");
        try
        {
            var runId = PhysicalAp4VerificationHarness.CreateVerificationRunId();
            var report = await PhysicalAp4VerificationHarness.RunShortEndToEndAsync(runId);
            Assert.True(report.TotalOpcUaUpdates > 0);
            Assert.Equal(3, report.Machines.Count);
            Assert.Contains(report.Samples, s =>
                s.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId && s.ActiveScenarioCount >= 2);
            Assert.Contains(report.Samples, s =>
                s.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId && s.ActiveScenarioCount > 0);
            Assert.Contains(report.Samples, s =>
                s.ProfileId == TechnicalLearningMachine300ProfileFactory.ProfileId && s.ActiveScenarioCount > 0);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AP4_E2E_SECONDS", previousSeconds);
        }
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task AP4_EvidenceExport_WhenRequested()
    {
        if (!PhysicalAp4VerificationSettings.IsExportMode)
        {
            return;
        }

        var runId = PhysicalAp4VerificationHarness.CreateVerificationRunId();
        var catalog = await PhysicalAp4VerificationHarness.RunCatalogValidationAsync();
        var modelTests = await PhysicalAp4VerificationHarness.RunModelTestsForAllScenariosAsync(42, 12.0);
        var lifecycle = await PhysicalAp4VerificationHarness.RunLifecycleTestsAsync(42);
        var combination = await PhysicalAp4VerificationHarness.RunCombinationTestsAsync(42, 12.0);
        var recovery = await PhysicalAp4VerificationHarness.RunRecoveryTestsAsync(42, 15.0);
        var endToEnd = await PhysicalAp4VerificationHarness.RunShortEndToEndAsync(runId);

        await PhysicalAp4VerificationHarness.ExportEvidenceAsync(
            runId, catalog, modelTests, lifecycle, combination, recovery, endToEnd);

        Assert.True(Directory.Exists(PhysicalAp4VerificationHarness.EvidenceDirectory));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4VerificationHarness.EvidenceDirectory, "AP-04-scenario-catalog-validation.json")));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4VerificationHarness.EvidenceDirectory, "AP-04-scenario-model-tests.json")));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4VerificationHarness.EvidenceDirectory, "AP-04-scenario-lifecycle-tests.json")));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4VerificationHarness.EvidenceDirectory, "AP-04-scenario-combination-tests.json")));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4VerificationHarness.EvidenceDirectory, "AP-04-scenario-recovery-tests.json")));
        Assert.True(File.Exists(Path.Combine(PhysicalAp4VerificationHarness.EvidenceDirectory, "AP-04-short-scenario-end-to-end.json")));

        Assert.True(catalog.Passed, string.Join(", ", catalog.FailedCriteria));
        Assert.True(modelTests.Passed, string.Join(", ", modelTests.FailedCriteria));
        Assert.True(lifecycle.Passed, string.Join(", ", lifecycle.FailedCriteria));
        Assert.True(combination.Passed, string.Join(", ", combination.FailedCriteria));
        Assert.True(recovery.Passed, string.Join(", ", recovery.FailedCriteria));
        Assert.True(endToEnd.Passed, string.Join(", ", endToEnd.FailedCriteria));
    }
}
