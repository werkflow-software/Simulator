using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R3EvidenceTests
{
    [Fact]
    public void AP4R3_Recovery_FailsWithoutRecoveryStartSample()
    {
        var report = new Ap4R3RecoveryCaseResult
        {
            Timeline = [
                new Ap4R3RecoverySample { ErrorActive = false, MachineState = nameof(MachineState.Running), ScenarioId = "test" },
                new Ap4R3RecoverySample { ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioId = "test" }
            ],
            RecoveryCompletedAtUtc = DateTime.UtcNow,
            DirectionChecks = [new Ap4R3DirectionCheck { Passed = true }]
        };
        report.SignalSamples["Axis01.MotorCurrent"] = [1, 2];
        report.HiddenSamples["MechanicalLoad"] = [0.5, 0.4];

        var eval = Ap4R3EvidenceValidator.ValidateRecovery(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("recovery-start"));
    }

    [Fact]
    public void AP4R3_Recovery_FailsWithoutRecoveryMidSample()
    {
        var report = new Ap4R3RecoveryCaseResult
        {
            Timeline = [
                new Ap4R3RecoverySample { ErrorActive = false, MachineState = nameof(MachineState.Running), ScenarioId = "test" },
                new Ap4R3RecoverySample { ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioId = "test", ScenarioPhase = nameof(FaultScenarioPhase.Faulted) },
                new Ap4R3RecoverySample { ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioId = "test", ScenarioPhase = nameof(FaultScenarioPhase.Recovering) }
            ],
            RecoveryCompletedAtUtc = DateTime.UtcNow,
            DirectionChecks = [new Ap4R3DirectionCheck { Passed = true }]
        };
        report.SignalSamples["Axis01.MotorCurrent"] = [1, 2, 3];
        report.HiddenSamples["MechanicalLoad"] = [0.5, 0.4, 0.3];

        var eval = Ap4R3EvidenceValidator.ValidateRecovery(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("recovery-mid"));
    }

    [Fact]
    public void AP4R3_Recovery_FailsWithoutPostRecoverySample()
    {
        var report = new Ap4R3RecoveryCaseResult
        {
            ExpectProductionResume = true,
            Timeline = [
                new Ap4R3RecoverySample { ErrorActive = false, MachineState = nameof(MachineState.Running), ScenarioId = "test" },
                new Ap4R3RecoverySample { ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioId = "test", ScenarioPhase = nameof(FaultScenarioPhase.Faulted) },
                new Ap4R3RecoverySample { ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioId = "test", ScenarioPhase = nameof(FaultScenarioPhase.Recovering) },
                new Ap4R3RecoverySample { ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioId = "test", ScenarioPhase = nameof(FaultScenarioPhase.Recovering) }
            ],
            RecoveryCompletedAtUtc = DateTime.UtcNow,
            DirectionChecks = [new Ap4R3DirectionCheck { Passed = true }]
        };
        report.SignalSamples["Axis01.MotorCurrent"] = [1, 2, 3, 4];
        report.HiddenSamples["MechanicalLoad"] = [0.5, 0.4, 0.3, 0.2];

        var eval = Ap4R3EvidenceValidator.ValidateRecovery(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("post-recovery"));
    }

    [Fact]
    public void AP4R3_SensorDrift_FailsWithSingleSample()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 1,
            RequiredSignalIds = ["Axis01.MotorTemperature"],
            RequiredHiddenIds = ["ThermalLoad"]
        };
        report.SignalSamples["Axis01.MotorTemperature"] = [25.0];
        report.HiddenSamples["ThermalLoad"] = [0.4];

        var eval = Ap4R3EvidenceValidator.ValidateSensorDrift(report);
        Assert.False(eval.Passed);
    }

    [Fact]
    public void AP4R3_SensorDrift_FailsBelowMinimumSampleCount()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 10,
            RequiredSignalIds = ["Axis01.MotorTemperature", "Thermal.SpindleMotorTemp"],
            RequiredHiddenIds = ["ThermalLoad"]
        };
        report.SignalSamples["Axis01.MotorTemperature"] = Enumerable.Repeat(25.0, 10).ToList();
        report.SignalSamples["Thermal.SpindleMotorTemp"] = Enumerable.Repeat(30.0, 10).ToList();
        report.HiddenSamples["ThermalLoad"] = Enumerable.Repeat(0.4, 10).ToList();

        var eval = Ap4R3EvidenceValidator.ValidateSensorDrift(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("sample-count"));
    }

    [Fact]
    public void AP4R3_CoolantLoss_FailsWithEmptySignalSamples()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 0,
            RequiredSignalIds = ["Cooling.PrimaryCircuit.Flow", "Cooling.PrimaryCircuit.Pressure", "Cooling.PrimaryCircuit.Temperature"],
            RequiredHiddenIds = ["CoolingEfficiency"]
        };

        var eval = Ap4R3EvidenceValidator.ValidateCoolantLoss(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("signal-samples-empty"));
    }

    [Fact]
    public void AP4R3_CoolantLoss_FailsWithoutFlowCheck()
    {
        var report = BuildCoolantReport();
        report.DirectionChecks = report.DirectionChecks.Where(d => !d.SignalId.Contains("Flow")).ToList();

        var eval = Ap4R3EvidenceValidator.ValidateCoolantLoss(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("flow"));
    }

    [Fact]
    public void AP4R3_CoolantLoss_FailsWithoutPressureCheck()
    {
        var report = BuildCoolantReport();
        report.DirectionChecks = report.DirectionChecks.Where(d => !d.SignalId.Contains("Pressure")).ToList();

        var eval = Ap4R3EvidenceValidator.ValidateCoolantLoss(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("pressure"));
    }

    [Fact]
    public void AP4R3_CoolantLoss_FailsWithoutTemperatureCheck()
    {
        var report = BuildCoolantReport();
        report.DirectionChecks = report.DirectionChecks.Where(d => !d.SignalId.Contains("Temperature")).ToList();

        var eval = Ap4R3EvidenceValidator.ValidateCoolantLoss(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("temperature"));
    }

    [Fact]
    public void AP4R3_HydraulicLeak_FailsWithEmptySamples()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 0,
            RequiredSignalIds = ["Hydraulic.SupplyPressure"],
            RequiredHiddenIds = ["HydraulicEfficiency"]
        };

        var eval = Ap4R3EvidenceValidator.ValidateHydraulicLeak(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("signal-samples-empty"));
    }

    [Fact]
    public void AP4R3_HydraulicLeak_FailsWithoutSupplyPressureDrop()
    {
        var report = BuildHydraulicReport();
        report.DirectionChecks = report.DirectionChecks
            .Where(d => !d.SignalId.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var eval = Ap4R3EvidenceValidator.ValidateHydraulicLeak(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("supply-pressure"));
    }

    [Fact]
    public void AP4R3_HydraulicLeak_FailsWithoutPumpCurrentRise()
    {
        var report = BuildHydraulicReport();
        report.DirectionChecks = report.DirectionChecks
            .Where(d => !d.SignalId.Contains("PumpCurrent", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var eval = Ap4R3EvidenceValidator.ValidateHydraulicLeak(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("pump-current"));
    }

    [Fact]
    public void AP4R3_Validator_RequiresSignalIdsPresent()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 30,
            RequiredSignalIds = ["Signal.A"],
            RequiredHiddenIds = ["Hidden.A"]
        };
        report.HiddenSamples["Hidden.A"] = Enumerable.Repeat(0.5, 30).ToList();

        var eval = Ap4R3EvidenceValidator.ValidateComplexCase(report, report.RequiredSignalIds, report.RequiredHiddenIds, 20);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("missing-signal") || c.Contains("signal-samples-empty"));
    }

    [Fact]
    public void AP4R3_Validator_NaNLeadsToFailed()
    {
        var report = new Ap4R3ComplexCaseResult { SampleCount = 30 };
        report.SignalSamples["Signal.A"] = [1.0, double.NaN, 3.0];

        var eval = Ap4R3EvidenceValidator.ValidateComplexCase(report, ["Signal.A"], [], 2);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("nan"));
    }

    [Fact]
    public void AP4R3_Validator_HardcodedPassedWithoutEvidenceFails()
    {
        var report = new Ap4R3ComplexCaseResult { SampleCount = 5, Passed = true };
        var eval = Ap4R3EvidenceValidator.ValidateCoolantLoss(report);
        Assert.False(eval.Passed);
    }

    [Fact]
    public async Task AP4R3_CompletenessVerification_Passes()
    {
        var report = await PhysicalAp4R3VerificationHarness.RunCompletenessVerificationAsync();
        Assert.True(report.Ap4R3Passed, string.Join("; ", report.FailedCriteria));
        Assert.True(report.Ap4OverallPassed);
        Assert.True(report.LaserRecovery.SampleCount >= Ap4R3EvidenceValidator.MinimumRecoveryTimelineSamples);
        Assert.True(report.HydraulicRecovery.SampleCount >= Ap4R3EvidenceValidator.MinimumRecoveryTimelineSamples);
        Assert.True(report.SensorDrift.SampleCount >= Ap4R3EvidenceValidator.MinimumComplexSampleCount);
        Assert.True(report.CoolantLoss.SignalSamples.Count > 0);
        Assert.True(report.HydraulicLeak.SignalSamples.Count > 0);
    }

    [Fact]
    public async Task AP4R3_EvidenceExport_WhenRequested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("AP4R3_VERIFY_EXPORT"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var runId = PhysicalAp4R3VerificationHarness.CreateVerificationRunId();
        var report = await PhysicalAp4R3VerificationHarness.RunCompletenessVerificationAsync();
        await PhysicalAp4R3VerificationHarness.ExportEvidenceAsync(runId, report);

        Assert.True(report.Ap4R3Passed);
        Assert.True(report.Ap4OverallPassed);
        Assert.True(File.Exists(Path.Combine(PhysicalAp4R3VerificationHarness.EvidenceDirectory, "AP-04-R3-evidence-completeness-verification.json")));
    }

    private static Ap4R3ComplexCaseResult BuildCoolantReport()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 40,
            RequiredSignalIds = ["Cooling.PrimaryCircuit.Flow", "Cooling.PrimaryCircuit.Pressure", "Cooling.PrimaryCircuit.Temperature"],
            RequiredHiddenIds = ["CoolingEfficiency"]
        };
        report.SignalSamples["Cooling.PrimaryCircuit.Flow"] = Enumerable.Range(0, 40).Select(i => 15.0 - i * 0.2).ToList();
        report.SignalSamples["Cooling.PrimaryCircuit.Pressure"] = Enumerable.Range(0, 40).Select(i => 4.0 - i * 0.05).ToList();
        report.SignalSamples["Cooling.PrimaryCircuit.Temperature"] = Enumerable.Range(0, 40).Select(i => 24.0 + i * 0.08).ToList();
        report.HiddenSamples["CoolingEfficiency"] = Enumerable.Range(0, 40).Select(i => 0.9 - i * 0.02).ToList();
        report.DirectionChecks = Ap4R3EvidenceValidator.ComputeDirectionChecks(
            report.SignalSamples,
            report.HiddenSamples,
            new Dictionary<string, string>
            {
                ["CoolingEfficiency"] = "decrease",
                ["Cooling.PrimaryCircuit.Flow"] = "decrease",
                ["Cooling.PrimaryCircuit.Pressure"] = "decrease",
                ["Cooling.PrimaryCircuit.Temperature"] = "increase"
            });
        report.TimingChecks = Ap4R3EvidenceValidator.ComputeCoolantTimingChecks(report.SignalSamples);
        return report;
    }

    private static Ap4R3ComplexCaseResult BuildHydraulicReport()
    {
        var report = new Ap4R3ComplexCaseResult
        {
            SampleCount = 40,
            RequiredSignalIds = ["Hydraulic.SupplyPressure", "Hydraulic.PumpCurrent", "Hydraulic.OilTemperature", "Bending.PressForce", "Bending.CycleTime"],
            RequiredHiddenIds = ["HydraulicEfficiency"]
        };
        report.SignalSamples["Hydraulic.SupplyPressure"] = Enumerable.Range(0, 40).Select(i => 120.0 - i * 1.0).ToList();
        report.SignalSamples["Hydraulic.PumpCurrent"] = Enumerable.Range(0, 40).Select(i => 5.0 + i * 0.1).ToList();
        report.SignalSamples["Hydraulic.OilTemperature"] = Enumerable.Range(0, 40).Select(i => 35.0 + i * 0.05).ToList();
        report.SignalSamples["Bending.PressForce"] = Enumerable.Range(0, 40).Select(i => 100.0 - i * 0.5).ToList();
        report.SignalSamples["Bending.CycleTime"] = Enumerable.Range(0, 40).Select(i => 22.0 + i * 0.1).ToList();
        report.HiddenSamples["HydraulicEfficiency"] = Enumerable.Range(0, 40).Select(i => 0.88 - i * 0.02).ToList();
        report.DirectionChecks = Ap4R3EvidenceValidator.ComputeDirectionChecks(
            report.SignalSamples,
            report.HiddenSamples,
            new Dictionary<string, string>
            {
                ["HydraulicEfficiency"] = "decrease",
                ["Hydraulic.SupplyPressure"] = "decrease",
                ["Hydraulic.PumpCurrent"] = "increase",
                ["Hydraulic.OilTemperature"] = "increase",
                ["Bending.PressForce"] = "change",
                ["Bending.CycleTime"] = "increase"
            });
        return report;
    }
}
