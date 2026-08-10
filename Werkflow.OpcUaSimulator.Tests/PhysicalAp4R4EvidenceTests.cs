using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp4R4EvidenceTests
{
    [Fact]
    public async Task AP4R4_SafetyVerification_CompletesUnderTwoMinutes()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var report = await PhysicalAp4R4VerificationHarness.RunSafetyVerificationAsync(44, 25.0, cts.Token);
        Assert.True(report.Ap4R4Passed, string.Join(",", report.FailedCriteria));
        Assert.True(report.Ap4OverallPassed);
        Assert.True(report.LaserRecovery.Passed, string.Join(",", report.LaserRecovery.FailedCriteria));
        Assert.True(report.HydraulicRecovery.Passed, string.Join(",", report.HydraulicRecovery.FailedCriteria));
        Assert.True(report.SensorDrift.Passed, string.Join(",", report.SensorDrift.FailedCriteria));
    }

    [Fact]
    public void AP4R4_Recovery_FailsWhenCompletedAboveSafeThreshold()
    {
        var report = new Ap4R4RecoveryCaseResult
        {
            FaultThreshold = 70.0,
            SafeRecoveryThreshold = 65.0,
            SafeRecoverySourceId = "Axis01.MotorTemperature",
            SafeRecoveryComparison = FaultThresholdComparison.LessThan,
            SafeRecoveryTolerance = 1.0,
            RecoveryCompletedAtUtc = DateTime.UtcNow,
            Timeline = [
                new Ap4R4RecoverySample
                {
                    LifecycleStage = "RecoveryCompleted",
                    Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = 75.93 }
                },
                new Ap4R4RecoverySample { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = 76.0 } },
                new Ap4R4RecoverySample { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = 74.0 } },
                new Ap4R4RecoverySample { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = 73.0 } },
                new Ap4R4RecoverySample { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = 72.0 } },
                new Ap4R4RecoverySample { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = 71.0 } }
            ],
            FaultDirectionChecks = [new Ap4R4DirectionCheck { Passed = true, Required = true }],
            RecoveryDirectionChecks = [new Ap4R4DirectionCheck { Passed = true, Required = true }]
        };

        var eval = Ap4R4EvidenceValidator.ValidateLaserRecovery(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("safe-threshold", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AP4R4_PostRecovery_FailsWhenAboveFaultThreshold()
    {
        var report = new Ap4R4RecoveryCaseResult
        {
            FaultThreshold = 70.0,
            SafeRecoveryThreshold = 65.0,
            SafeRecoverySourceId = "Axis01.MotorTemperature",
            SafeRecoveryComparison = FaultThresholdComparison.LessThan,
            RecoveryCompletedAtUtc = DateTime.UtcNow,
            Timeline = Enumerable.Range(0, 6).Select(i => new Ap4R4RecoverySample
            {
                LifecycleStage = "PostRecovery",
                Signals = new Dictionary<string, double> { ["Axis01.MotorTemperature"] = i == 3 ? 78.0 : 62.0 }
            }).ToList(),
            FaultDirectionChecks = [new Ap4R4DirectionCheck { Passed = true, Required = true }],
            RecoveryDirectionChecks = [new Ap4R4DirectionCheck { Passed = true, Required = true }]
        };

        var eval = Ap4R4EvidenceValidator.ValidatePostRecoverySafety(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("fault-threshold"));
    }

    [Fact]
    public void AP4R4_Recovery_FailsWithoutMinimumStableDurationEvidence()
    {
        var report = new Ap4R4RecoveryCaseResult
        {
            RecoveryCompletedAtUtc = null,
            Timeline = [
                new Ap4R4RecoverySample { LifecycleStage = "PreFault", ErrorActive = false, MachineState = nameof(MachineState.Running) },
                new Ap4R4RecoverySample { LifecycleStage = "Faulted", ErrorActive = true, MachineState = nameof(MachineState.Error), ScenarioPhase = nameof(FaultScenarioPhase.Faulted) }
            ],
            FaultDirectionChecks = [new Ap4R4DirectionCheck { Passed = true, Required = true }],
            RecoveryDirectionChecks = [new Ap4R4DirectionCheck { Passed = true, Required = true }]
        };

        var eval = Ap4R4EvidenceValidator.ValidateRecoveryTimeline(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("recovery-completed-missing"));
    }

    [Fact]
    public void AP4R4_RequiredDirectionCheckFalse_FailsOverall()
    {
        var checks = new List<Ap4R4DirectionCheck>
        {
            new() { SignalId = "Axis01.MotorCurrent", Direction = "decrease", Required = true, Passed = false }
        };
        var eval = Ap4R4EvidenceValidator.ValidateRequiredDirectionChecks(checks, "recovery-direction");
        Assert.False(eval.Passed);
        Assert.False(Ap4R4EvidenceValidator.ComputeRecursivePassed(true, checks.Select(c => c.Passed)));
    }

    [Fact]
    public void AP4R4_Hydraulic_FaultDirectionOnRecovery_IsSeparated()
    {
        var timeline = new List<Ap4R4RecoverySample>
        {
            new() { LifecycleStage = "PreFault", MachineState = nameof(MachineState.Running), Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 150 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 1.0 } },
            new() { LifecycleStage = "PreFault", MachineState = nameof(MachineState.Running), Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 149 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 1.0 } },
            new() { LifecycleStage = "PreFault", MachineState = nameof(MachineState.Running), Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 148 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 1.0 } },
            new() { LifecycleStage = "Faulted", ErrorActive = true, MachineState = nameof(MachineState.Error), Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 110 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 0.6 } },
            new() { LifecycleStage = "Faulted", ErrorActive = true, MachineState = nameof(MachineState.Error), Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 105 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 0.55 } },
            new() { LifecycleStage = "RecoveryStart", Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 115 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 0.7 } },
            new() { LifecycleStage = "RecoveryMid", Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 130 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 0.85 } },
            new() { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 145 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 0.95 } },
            new() { LifecycleStage = "PostRecovery", Signals = new Dictionary<string, double> { ["Hydraulic.SupplyPressure"] = 146 }, HiddenStates = new Dictionary<string, double> { ["HydraulicEfficiency"] = 0.96 } }
        };

        var faultChecks = Ap4R4EvidenceValidator.ComputeFaultDirectionChecks(
            timeline,
            new Dictionary<string, string> { ["HydraulicEfficiency"] = "decrease", ["Hydraulic.SupplyPressure"] = "decrease" });
        var recoveryChecks = Ap4R4EvidenceValidator.ComputeRecoveryDirectionChecks(
            timeline,
            new Dictionary<string, string> { ["HydraulicEfficiency"] = "increase", ["Hydraulic.SupplyPressure"] = "increase" },
            new Dictionary<string, double> { ["HydraulicEfficiency"] = 1.0, ["Hydraulic.SupplyPressure"] = 150.0 });

        Assert.True(faultChecks.All(c => c.Passed));
        Assert.True(recoveryChecks.All(c => c.Passed));
        Assert.True(faultChecks.First(c => c.SignalId == "HydraulicEfficiency").Delta < 0);
        Assert.True(recoveryChecks.First(c => c.SignalId == "HydraulicEfficiency").Delta > 0);
    }

    [Fact]
    public void AP4R4_Recovery_FailsWhenDistanceToNormalNotImproved()
    {
        var report = new Ap4R4RecoveryCaseResult
        {
            DistanceToNormal = new Ap4R4DistanceToNormal
            {
                DistanceToNormalStart = 10,
                DistanceToNormalEnd = 12,
                RecoveryImproved = false
            },
            RecoveryDirectionChecks = [
                new Ap4R4DirectionCheck { SignalId = "HydraulicEfficiency", Required = true, Passed = false },
                new Ap4R4DirectionCheck { SignalId = "Hydraulic.SupplyPressure", Required = true, Passed = false }
            ]
        };
        var eval = Ap4R4EvidenceValidator.ValidateHydraulicRecovery(report);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("distance-to-normal"));
    }

    [Fact]
    public void AP4R4_SensorDrift_FailsWithDistinctValuesOne()
    {
        var report = new Ap4R4SensorDriftResult
        {
            SensorSamples = Enumerable.Repeat(45.0, 45).ToList(),
            HiddenSamples = Enumerable.Repeat(0.4, 45).ToList(),
            SensorBiasStart = 45.0,
            SensorBiasEnd = 45.0,
            HiddenDelta = 0.01
        };
        var eval = Ap4R4EvidenceValidator.ValidateSensorDrift(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("distinct"));
    }

    [Fact]
    public void AP4R4_SensorDrift_FailsWithZeroDelta()
    {
        var report = new Ap4R4SensorDriftResult
        {
            SensorSamples = Enumerable.Range(0, 45).Select(i => 45.0 + i * 0.01).ToList(),
            HiddenSamples = Enumerable.Repeat(0.4, 45).ToList(),
            SensorBiasStart = 45.0,
            SensorBiasEnd = 45.0,
            HiddenDelta = 0.01
        };
        var eval = Ap4R4EvidenceValidator.ValidateSensorDrift(report);
        Assert.False(eval.Passed);
        Assert.Contains(eval.FailedCriteria, c => c.Contains("bias-delta"));
    }

    [Fact]
    public async Task AP4R4_SignalFreeze_AllowsDistinctValuesOne()
    {
        var result = await PhysicalAp4R4VerificationHarness.RunSignalFreezeCaseAsync(99);
        var eval = Ap4R4EvidenceValidator.ValidateSignalFreezeDistinct(result);
        Assert.True(eval.Passed);
        Assert.Equal(1, result.DistinctValues);
    }

    [Fact]
    public async Task AP4R4_SensorDrift_AndSignalFreeze_AreDistinguishable()
    {
        var drift = await PhysicalAp4R4VerificationHarness.RunSensorDriftCaseAsync(42);
        var freeze = await PhysicalAp4R4VerificationHarness.RunSignalFreezeCaseAsync(99);
        Assert.True(drift.DistinctValues >= Ap4R4EvidenceValidator.MinimumSensorDistinctValues);
        Assert.Equal(1, freeze.DistinctValues);
        Assert.True(Math.Abs(drift.BiasDelta) > Math.Abs(freeze.BiasDelta));
    }

    [Fact]
    public async Task AP4R4_ExportEvidence_WritesHandoffFiles()
    {
        var runId = PhysicalAp4R4VerificationHarness.CreateVerificationRunId();
        var report = await PhysicalAp4R4VerificationHarness.RunSafetyVerificationAsync(44, 25.0);
        await PhysicalAp4R4VerificationHarness.ExportEvidenceAsync(runId, report);
        var dir = PhysicalAp4R4VerificationHarness.EvidenceDirectory;
        Assert.True(File.Exists(Path.Combine(dir, "AP-04-R4-final-safety-verification.json")));
        Assert.True(File.Exists(Path.Combine(dir, "AP-04-R4-final-recovery-safety-report.md")));
    }
}
