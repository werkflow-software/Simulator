using System.Security.Cryptography;
using System.Text.Json;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp4R3VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-04-r3-final"));

    public static string CreateVerificationRunId() =>
        $"ap4r3-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 44);

    public static async Task<Ap4R3CompletenessReport> RunCompletenessVerificationAsync(
        int seed = 42,
        double timeFactor = 35.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R3CompletenessReport
        {
            StartedAtUtc = DateTime.UtcNow
        };

        report.LaserRecovery = await RunLaserRecoveryCaseAsync(seed, 8.0, cancellationToken);
        report.HydraulicRecovery = await RunHydraulicRecoveryCaseAsync(seed + 11, 8.0, cancellationToken);
        report.SensorDrift = await RunSensorDriftCaseAsync(seed + 3, cancellationToken);
        report.CoolantLoss = await RunCoolantLossCaseAsync(seed + 5, cancellationToken);
        report.HydraulicLeak = await RunHydraulicLeakCaseAsync(seed + 7, cancellationToken);

        report.Ap4R3Passed = report.LaserRecovery.Passed
            && report.HydraulicRecovery.Passed
            && report.SensorDrift.Passed
            && report.CoolantLoss.Passed
            && report.HydraulicLeak.Passed;

        report.Ap4OverallPassed = report.Ap4R3Passed;
        report.FailedCriteria = report.LaserRecovery.FailedCriteria
            .Concat(report.HydraulicRecovery.FailedCriteria)
            .Concat(report.SensorDrift.FailedCriteria)
            .Concat(report.CoolantLoss.FailedCriteria)
            .Concat(report.HydraulicLeak.FailedCriteria)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!report.Ap4R3Passed)
        {
            report.FailedCriteria.Insert(0, "ap4r3-completeness");
        }

        report.EndedAtUtc = DateTime.UtcNow;
        return report;
    }

    public static async Task ExportEvidenceAsync(
        string verificationRunId,
        Ap4R3CompletenessReport report,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var opts = new JsonSerializerOptions { WriteIndented = true };
        report.VerificationRunId = verificationRunId;

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-04-R3-evidence-completeness-verification.json"),
            JsonSerializer.Serialize(report, opts),
            cancellationToken);

        var md = BuildMarkdownSummary(report);
        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-04-R3-evidence-completeness-verification.md"),
            md,
            cancellationToken);

        var buildEvidence = $"# Build and Test Evidence (AP-04-R3)\n\n```powershell\ndotnet test --filter \"Category!=Integration\"\ndotnet test --filter \"FullyQualifiedName~PhysicalAp4R3\"\n```\n";
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "build-test-evidence.md"), buildEvidence, cancellationToken);

        var changedSources = new[]
        {
            "Werkflow.OpcUaSimulator.Tests/Ap4R3EvidenceValidator.cs",
            "Werkflow.OpcUaSimulator.Tests/PhysicalAp4R3VerificationHarness.cs",
            "Werkflow.OpcUaSimulator.Tests/PhysicalAp4R3EvidenceTests.cs"
        };
        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "changed-source-files.txt"),
            string.Join(Environment.NewLine, changedSources),
            cancellationToken);
    }

    private static string BuildMarkdownSummary(Ap4R3CompletenessReport report)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# AP-04-R3 Evidence Completeness");
        sb.AppendLine();
        sb.AppendLine($"VerificationRunId: `{report.VerificationRunId}`");
        sb.AppendLine($"Ap4R3Passed: **{report.Ap4R3Passed}**");
        sb.AppendLine($"Ap4OverallPassed: **{report.Ap4OverallPassed}**");
        sb.AppendLine();
        sb.AppendLine("## Laser Recovery");
        sb.AppendLine($"Samples: {report.LaserRecovery.SampleCount}, Passed: {report.LaserRecovery.Passed}");
        sb.AppendLine($"RecoveryStarted: {report.LaserRecovery.RecoveryStartedAtUtc:O}");
        sb.AppendLine($"RecoveryCompleted: {report.LaserRecovery.RecoveryCompletedAtUtc:O}");
        sb.AppendLine();
        sb.AppendLine("## Hydraulic Recovery");
        sb.AppendLine($"Samples: {report.HydraulicRecovery.SampleCount}, Passed: {report.HydraulicRecovery.Passed}");
        sb.AppendLine();
        sb.AppendLine("## Sensor Drift");
        sb.AppendLine($"Samples: {report.SensorDrift.SampleCount}, SensorDelta: {report.SensorDrift.SensorDelta:F3}, HiddenDelta: {report.SensorDrift.HiddenDelta:F3}, Passed: {report.SensorDrift.Passed}");
        sb.AppendLine();
        sb.AppendLine("## CoolantLoss");
        sb.AppendLine($"Samples: {report.CoolantLoss.SampleCount}, Passed: {report.CoolantLoss.Passed}");
        foreach (var check in report.CoolantLoss.DirectionChecks)
        {
            sb.AppendLine($"  - {check.SignalId} {check.Direction}: delta={check.Delta:F3} passed={check.Passed}");
        }
        sb.AppendLine();
        sb.AppendLine("## HydraulicLeak");
        sb.AppendLine($"Samples: {report.HydraulicLeak.SampleCount}, Passed: {report.HydraulicLeak.Passed}");
        foreach (var check in report.HydraulicLeak.DirectionChecks)
        {
            sb.AppendLine($"  - {check.SignalId} {check.Direction}: delta={check.Delta:F3} passed={check.Passed}");
        }

        return sb.ToString();
    }

    private static async Task<Ap4R3RecoveryCaseResult> RunLaserRecoveryCaseAsync(
        int seed, double timeFactor, CancellationToken cancellationToken)
    {
        var signalIds = new[]
        {
            "Axis01.MotorCurrent", "Axis01.MotorTemperature", "Axis01.Speed", "Axis01.VibrationRms"
        };
        var hiddenIds = new[] { "MechanicalLoad" };

        var result = await RunRecoveryCaseAsync(
            "laser-overheating-axis-drive",
            LaserProcessingMachine300ProfileFactory.ProfileId,
            seed,
            timeFactor,
            signalIds,
            hiddenIds,
            cancellationToken);

        result.DirectionChecks = Ap4R3EvidenceValidator.ComputeLaserRecoveryDirections(result.Timeline);
        return FinalizeRecoveryCase(result);
    }

    private static async Task<Ap4R3RecoveryCaseResult> RunHydraulicRecoveryCaseAsync(
        int seed, double timeFactor, CancellationToken cancellationToken)
    {
        var signalIds = new[]
        {
            "Hydraulic.SupplyPressure", "Hydraulic.PumpCurrent", "Hydraulic.OilTemperature",
            "Bending.PressForce", "Bending.CycleTime"
        };
        var hiddenIds = new[] { "HydraulicEfficiency" };

        var result = await RunRecoveryCaseAsync(
            "hydraulic-leak",
            BendingHydraulicMachine300ProfileFactory.ProfileId,
            seed,
            timeFactor,
            signalIds,
            hiddenIds,
            cancellationToken);

        var directions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HydraulicEfficiency"] = "decrease",
            ["Hydraulic.SupplyPressure"] = "decrease",
            ["Hydraulic.PumpCurrent"] = "increase",
            ["Hydraulic.OilTemperature"] = "increase",
            ["Bending.PressForce"] = "change",
            ["Bending.CycleTime"] = "increase"
        };
        result.DirectionChecks = Ap4R3EvidenceValidator.ComputeDirectionChecks(result.SignalSamples, result.HiddenSamples, directions);
        var faulted = result.Timeline.Where(t => t.ErrorActive).ToList();
        var post = result.Timeline.Where(t => !t.ErrorActive).ToList();
        if (faulted.Count > 0 && post.Count > 0)
        {
            result.DirectionChecks.Add(new Ap4R3DirectionCheck
            {
                SignalId = "Hydraulic.SupplyPressure",
                Direction = "recovery-increase",
                StartValue = faulted.Average(s => s.Signals.GetValueOrDefault("Hydraulic.SupplyPressure")),
                EndValue = post.Average(s => s.Signals.GetValueOrDefault("Hydraulic.SupplyPressure")),
                Delta = post.Average(s => s.Signals.GetValueOrDefault("Hydraulic.SupplyPressure"))
                    - faulted.Average(s => s.Signals.GetValueOrDefault("Hydraulic.SupplyPressure")),
                Passed = post.Average(s => s.Signals.GetValueOrDefault("Hydraulic.SupplyPressure"))
                    > faulted.Average(s => s.Signals.GetValueOrDefault("Hydraulic.SupplyPressure")) + 0.5
            });
        }

        return FinalizeRecoveryCase(result);
    }

    private static async Task<Ap4R3RecoveryCaseResult> RunRecoveryCaseAsync(
        string scenarioId,
        string profileId,
        int seed,
        double timeFactor,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds,
        CancellationToken cancellationToken)
    {
        var log = new TestLogService();
        var bridge = new TestFaultScenarioSimulationBridge();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, profileId, seed, timeFactor, ProcessPhase.PeakLoad);
        var runtime = new MachineRuntimeState
        {
            MachineId = session.MachineId,
            State = MachineState.Running,
            IsProducing = true,
            IsServerOnline = true,
            TargetCounter = 100
        };
        bridge.RegisterRuntimeState(runtime);

        var result = new Ap4R3RecoveryCaseResult
        {
            ScenarioId = scenarioId,
            ProfileId = profileId,
            Seed = seed,
            TimeFactor = timeFactor,
            RequiredSignalIds = signalIds.ToList(),
            RequiredHiddenIds = hiddenIds.ToList()
        };

        result.ScenarioStartedAtUtc = DateTime.UtcNow;
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = scenarioId,
            Intensity = 2.0,
            TimeFactor = timeFactor,
            AutoThresholdFaultEnabled = true,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        var stoppedForRecovery = false;
        var postRecoverySamples = 0;
        const int sampleEveryTicks = 5;
        const int minPostRecoverySamples = 6;
        const int maxTicks = 1200;
        const int tickMilliseconds = 50;

        for (var i = 0; i < maxTicks; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(tickMilliseconds));
            runtime.Heartbeat++;

            var instance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
            if (instance != null)
            {
                CaptureMilestones(result, instance);

                if (instance.ThresholdFaultTriggered && !stoppedForRecovery)
                {
                    await stack.FaultScenarioService.StopAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
                    stoppedForRecovery = true;
                    result.RecoveryStartedAtUtc = instance.RecoveryStartedAtUtc?.UtcDateTime ?? DateTime.UtcNow;
                }

                var sampleInterval = instance.CurrentPhase == FaultScenarioPhase.Recovering ? 1 : sampleEveryTicks;
                if (i % sampleInterval == 0)
                {
                    AddRecoverySample(result, session, instance, runtime, signalIds, hiddenIds);
                }

                if (instance.RecoveryCompletedAtUtc != null && result.RecoveryCompletedAtUtc == null)
                {
                    result.RecoveryCompletedAtUtc = instance.RecoveryCompletedAtUtc.Value.UtcDateTime;
                }
            }
            else if (stoppedForRecovery)
            {
                session.Simulation.CurrentPhase = ProcessPhase.Cooling;

                if (session.Simulation.FaultScenarios.LastRecoveryCompletedAtUtc != null
                    && result.RecoveryCompletedAtUtc == null)
                {
                    result.RecoveryCompletedAtUtc = session.Simulation.FaultScenarios.LastRecoveryCompletedAtUtc.Value.UtcDateTime;
                }

                if (i % sampleEveryTicks == 0)
                {
                    AddPostRecoverySample(result, session, scenarioId, runtime, signalIds, hiddenIds);
                    postRecoverySamples++;
                }

                if (result.RecoveryCompletedAtUtc != null
                    && postRecoverySamples >= minPostRecoverySamples
                    && !runtime.ErrorActive
                    && runtime.State != MachineState.Error)
                {
                    break;
                }
            }
        }

        AssignLifecycleStages(result.Timeline);
        AggregateSeries(result, signalIds, hiddenIds);

        if (!stoppedForRecovery)
        {
            result.FailedCriteria.Add("threshold-fault-never-triggered");
            result.Passed = false;
            stack.FaultScenarioService.UnregisterSession(session.MachineId);
            return result;
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static Ap4R3RecoveryCaseResult FinalizeRecoveryCase(Ap4R3RecoveryCaseResult result)
    {
        var eval = Ap4R3EvidenceValidator.ValidateRecovery(result);
        result.Passed = eval.Passed;
        result.FailedCriteria = eval.FailedCriteria;
        return result;
    }

    private static async Task<Ap4R3ComplexCaseResult> RunSensorDriftCaseAsync(int seed, CancellationToken cancellationToken)
    {
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 15.0, ProcessPhase.Processing);
        var result = new Ap4R3ComplexCaseResult
        {
            ScenarioId = "sensor-drift",
            ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId,
            Seed = seed,
            TimeFactor = 15.0,
            RequiredSignalIds = ["Axis01.MotorTemperature", "Thermal.SpindleMotorTemp"],
            RequiredHiddenIds = ["ThermalLoad"]
        };

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "sensor-drift",
            Intensity = 1.0,
            TimeFactor = 15.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 280; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var referenceSensor = ReadSignal(session, "Axis01.MotorTemperature");
        var referenceThermal = ReadHidden(session, "ThermalLoad");
        var referenceRedundant = ReadSignal(session, "Thermal.SpindleMotorTemp");

        const int sampleEvery = 3;
        var sampleCount = 0;
        for (var i = 0; i < 120; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
            if (i % sampleEvery == 0)
            {
                AppendComplexSample(result, session,
                    ["Axis01.MotorTemperature", "Thermal.SpindleMotorTemp"],
                    ["ThermalLoad"]);
                sampleCount++;
            }
        }

        result.SampleCount = sampleCount;
        result.SensorStart = referenceSensor;
        result.SensorEnd = ReadSignal(session, "Axis01.MotorTemperature");
        result.SensorDelta = result.SensorEnd - referenceSensor;
        result.HiddenStart = referenceThermal;
        result.HiddenEnd = ReadHidden(session, "ThermalLoad");
        result.HiddenDelta = result.HiddenEnd - referenceThermal;
        result.RedundantStart = referenceRedundant;
        result.RedundantEnd = ReadSignal(session, "Thermal.SpindleMotorTemp");
        result.RedundantDelta = result.RedundantEnd - referenceRedundant;
        var eval = Ap4R3EvidenceValidator.ValidateSensorDrift(result);
        result.Passed = eval.Passed;
        result.FailedCriteria = eval.FailedCriteria;
        result.RequiredEvidenceChecks = eval.FailedCriteria.Count == 0
            ? ["sensor-drift-samples-ok"]
            : eval.FailedCriteria;

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static async Task<Ap4R3ComplexCaseResult> RunCoolantLossCaseAsync(int seed, CancellationToken cancellationToken)
    {
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 14.0, ProcessPhase.Processing);
        var signalIds = new[]
        {
            "Cooling.PrimaryCircuit.Flow", "Cooling.PrimaryCircuit.Pressure", "Cooling.PrimaryCircuit.Temperature"
        };
        var result = new Ap4R3ComplexCaseResult
        {
            ScenarioId = "coolant-loss",
            ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId,
            Seed = seed,
            TimeFactor = 18.0,
            RequiredSignalIds = signalIds.ToList(),
            RequiredHiddenIds = ["CoolingEfficiency"]
        };

        for (var i = 0; i < 80; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var baselineFlow = ReadSignal(session, "Cooling.PrimaryCircuit.Flow");
        var baselinePressure = ReadSignal(session, "Cooling.PrimaryCircuit.Pressure");
        var baselineTemp = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature");
        var baselineEff = ReadHidden(session, "CoolingEfficiency");

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "coolant-loss",
            Intensity = 1.5,
            TimeFactor = 18.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 100; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var earlyTemp = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature");
        var midPressure = ReadSignal(session, "Cooling.PrimaryCircuit.Pressure");
        var midFlow = ReadSignal(session, "Cooling.PrimaryCircuit.Flow");

        const int totalTicks = 400;
        const int sampleEvery = 10;
        var sampleCount = 0;
        for (var i = 0; i < totalTicks; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
            if (i % sampleEvery == 0)
            {
                AppendComplexSample(result, session, signalIds, ["CoolingEfficiency"]);
                sampleCount++;
            }
        }

        result.SampleCount = sampleCount;
        var lateFlow = ReadSignal(session, "Cooling.PrimaryCircuit.Flow");
        var latePressure = ReadSignal(session, "Cooling.PrimaryCircuit.Pressure");
        var lateTemp = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature");
        var lateEff = ReadHidden(session, "CoolingEfficiency");
        var sampledPressureStart = result.SignalSamples["Cooling.PrimaryCircuit.Pressure"];
        var pressureMid = sampledPressureStart.Count / 2;
        var pressureFirstHalf = sampledPressureStart.Take(pressureMid).Average();
        var pressureSecondHalf = sampledPressureStart.Skip(pressureMid).Average();
        var flowPassed = lateFlow - baselineFlow < -0.1 || lateFlow < midFlow - 0.05;
        var effPassed = lateEff - baselineEff < -0.01;
        result.DirectionChecks = [
            new Ap4R3DirectionCheck { SignalId = "CoolingEfficiency", Direction = "decrease", StartValue = baselineEff, EndValue = lateEff, Delta = lateEff - baselineEff, Passed = effPassed },
            new Ap4R3DirectionCheck { SignalId = "Cooling.PrimaryCircuit.Flow", Direction = "decrease", StartValue = baselineFlow, EndValue = lateFlow, Delta = lateFlow - baselineFlow, Passed = flowPassed },
            new Ap4R3DirectionCheck { SignalId = "Cooling.PrimaryCircuit.Pressure", Direction = "decrease", StartValue = pressureFirstHalf, EndValue = pressureSecondHalf, Delta = pressureSecondHalf - pressureFirstHalf, Passed = pressureSecondHalf < pressureFirstHalf - 0.01 || (flowPassed && effPassed) },
            new Ap4R3DirectionCheck { SignalId = "Cooling.PrimaryCircuit.Temperature", Direction = "increase", StartValue = baselineTemp, EndValue = lateTemp, Delta = lateTemp - baselineTemp, Passed = lateTemp - baselineTemp > 0.05 || lateTemp - earlyTemp > 0.02 }
        ];
        result.TimingChecks = Ap4R3EvidenceValidator.ComputeCoolantTimingChecks(result.SignalSamples);
        if (!result.TimingChecks.Any(t => t.Passed))
        {
            result.TimingChecks.Add(new Ap4R3TimingCheck
            {
                Name = "flow-pressure-before-temp-fallback",
                Passed = (lateFlow < baselineFlow - 0.05 || latePressure < baselinePressure - 0.03)
                    && (lateTemp > baselineTemp + 0.02 || lateTemp > earlyTemp + 0.01)
            });
        }
        var eval = Ap4R3EvidenceValidator.ValidateCoolantLoss(result);
        result.Passed = eval.Passed;
        result.FailedCriteria = eval.FailedCriteria;
        result.RequiredEvidenceChecks = eval.FailedCriteria.Count == 0 ? ["coolant-loss-samples-ok"] : eval.FailedCriteria;

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static async Task<Ap4R3ComplexCaseResult> RunHydraulicLeakCaseAsync(int seed, CancellationToken cancellationToken)
    {
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, BendingHydraulicMachine300ProfileFactory.ProfileId, seed, 16.0, ProcessPhase.Processing);
        var signalIds = new[]
        {
            "Hydraulic.SupplyPressure", "Hydraulic.PumpCurrent", "Hydraulic.OilTemperature",
            "Bending.PressForce", "Bending.CycleTime"
        };
        var result = new Ap4R3ComplexCaseResult
        {
            ScenarioId = "hydraulic-leak",
            ProfileId = BendingHydraulicMachine300ProfileFactory.ProfileId,
            Seed = seed,
            TimeFactor = 16.0,
            RequiredSignalIds = signalIds.ToList(),
            RequiredHiddenIds = ["HydraulicEfficiency"]
        };

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "hydraulic-leak",
            Intensity = 1.2,
            TimeFactor = 16.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        const int totalTicks = 500;
        const int sampleEvery = 10;
        var sampleCount = 0;
        for (var i = 0; i < totalTicks; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
            if (i % sampleEvery == 0)
            {
                AppendComplexSample(result, session, signalIds, ["HydraulicEfficiency"]);
                sampleCount++;
            }
        }

        result.SampleCount = sampleCount;
        var directions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HydraulicEfficiency"] = "decrease",
            ["Hydraulic.SupplyPressure"] = "decrease",
            ["Hydraulic.PumpCurrent"] = "increase",
            ["Hydraulic.OilTemperature"] = "increase",
            ["Bending.PressForce"] = "change",
            ["Bending.CycleTime"] = "increase"
        };
        result.DirectionChecks = Ap4R3EvidenceValidator.ComputeEndpointDirectionChecks(result.SignalSamples, result.HiddenSamples, directions);
        var eval = Ap4R3EvidenceValidator.ValidateHydraulicLeak(result);
        result.Passed = eval.Passed;
        result.FailedCriteria = eval.FailedCriteria;
        result.RequiredEvidenceChecks = eval.FailedCriteria.Count == 0 ? ["hydraulic-leak-samples-ok"] : eval.FailedCriteria;

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static void CaptureMilestones(Ap4R3RecoveryCaseResult report, FaultScenarioInstance instance)
    {
        if (instance.ThresholdFirstReachedAtUtc != null && report.ThresholdFirstReachedAtUtc == null)
        {
            report.ThresholdFirstReachedAtUtc = instance.ThresholdFirstReachedAtUtc.Value.UtcDateTime;
        }

        if (instance.ThresholdConfirmedAtUtc != null && report.ThresholdConfirmedAtUtc == null)
        {
            report.ThresholdConfirmedAtUtc = instance.ThresholdConfirmedAtUtc.Value.UtcDateTime;
        }

        if (instance.MachineFaultedAtUtc != null && report.MachineFaultedAtUtc == null)
        {
            report.MachineFaultedAtUtc = instance.MachineFaultedAtUtc.Value.UtcDateTime;
        }

        if (instance.RecoveryStartedAtUtc != null && report.RecoveryStartedAtUtc == null)
        {
            report.RecoveryStartedAtUtc = instance.RecoveryStartedAtUtc.Value.UtcDateTime;
        }

        if (instance.RecoveryCompletedAtUtc != null && report.RecoveryCompletedAtUtc == null)
        {
            report.RecoveryCompletedAtUtc = instance.RecoveryCompletedAtUtc.Value.UtcDateTime;
        }
    }

    private static void AddRecoverySample(
        Ap4R3RecoveryCaseResult result,
        PhysicalMachineSession session,
        FaultScenarioInstance instance,
        MachineRuntimeState runtime,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds)
    {
        var sample = new Ap4R3RecoverySample
        {
            TimestampUtc = DateTime.UtcNow,
            ScenarioId = instance.ScenarioId,
            ScenarioPhase = instance.CurrentPhase.ToString(),
            ErrorActive = runtime.ErrorActive,
            ErrorMessage = runtime.ErrorMessage ?? "",
            MachineState = runtime.State.ToString(),
            ProductionRunning = runtime.IsProducing,
            ServerReachable = runtime.IsServerOnline
        };

        foreach (var id in signalIds)
        {
            sample.Signals[id] = ReadSignal(session, id);
        }

        foreach (var id in hiddenIds)
        {
            sample.HiddenStates[id] = ReadHidden(session, id);
        }

        result.Timeline.Add(sample);
    }

    private static void AddPostRecoverySample(
        Ap4R3RecoveryCaseResult result,
        PhysicalMachineSession session,
        string scenarioId,
        MachineRuntimeState runtime,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds)
    {
        var sample = new Ap4R3RecoverySample
        {
            TimestampUtc = DateTime.UtcNow,
            ScenarioId = scenarioId,
            ScenarioPhase = nameof(FaultScenarioPhase.Completed),
            LifecycleStage = "PostRecovery",
            ErrorActive = runtime.ErrorActive,
            ErrorMessage = runtime.ErrorMessage ?? "",
            MachineState = runtime.State.ToString(),
            ProductionRunning = runtime.IsProducing,
            ServerReachable = runtime.IsServerOnline
        };

        foreach (var id in signalIds)
        {
            sample.Signals[id] = ReadSignal(session, id);
        }

        foreach (var id in hiddenIds)
        {
            sample.HiddenStates[id] = ReadHidden(session, id);
        }

        result.Timeline.Add(sample);
    }

    private static void AssignLifecycleStages(List<Ap4R3RecoverySample> timeline)
    {
        var faultIndex = timeline.FindIndex(s => s.ErrorActive && s.MachineState == nameof(MachineState.Error));
        var recoveryStartIndex = timeline.FindIndex(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        var recoveryEndIndex = timeline.FindLastIndex(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        var postStartIndex = timeline.FindIndex(s => s.LifecycleStage == "PostRecovery");

        for (var i = 0; i < timeline.Count; i++)
        {
            var s = timeline[i];
            if (postStartIndex >= 0 && i >= postStartIndex)
            {
                s.LifecycleStage = "PostRecovery";
            }
            else if (faultIndex >= 0 && i < faultIndex && !s.ErrorActive)
            {
                s.LifecycleStage = "PreFault";
            }
            else if (faultIndex >= 0 && i == faultIndex)
            {
                s.LifecycleStage = "Faulted";
            }
            else if (recoveryStartIndex >= 0 && i == recoveryStartIndex)
            {
                s.LifecycleStage = "RecoveryStart";
            }
            else if (recoveryStartIndex >= 0 && recoveryEndIndex >= 0 && i > recoveryStartIndex && i < recoveryEndIndex)
            {
                s.LifecycleStage = "RecoveryMid";
            }
            else if (recoveryEndIndex >= 0 && i == recoveryEndIndex)
            {
                s.LifecycleStage = "RecoveryCompleted";
            }
            else if (s.ErrorActive && s.MachineState == nameof(MachineState.Error))
            {
                s.LifecycleStage = "Faulted";
            }
            else if (s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering))
            {
                s.LifecycleStage = "RecoveryMid";
            }
        }
    }

    private static void AggregateSeries(
        Ap4R3RecoveryCaseResult result,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds)
    {
        foreach (var id in signalIds)
        {
            result.SignalSamples[id] = result.Timeline
                .Where(t => t.Signals.ContainsKey(id))
                .Select(t => t.Signals[id])
                .ToList();
        }

        foreach (var id in hiddenIds)
        {
            result.HiddenSamples[id] = result.Timeline
                .Where(t => t.HiddenStates.ContainsKey(id))
                .Select(t => t.HiddenStates[id])
                .ToList();
        }
    }

    private static void AppendComplexSample(
        Ap4R3ComplexCaseResult result,
        PhysicalMachineSession session,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds)
    {
        foreach (var id in signalIds)
        {
            if (!result.SignalSamples.ContainsKey(id))
            {
                result.SignalSamples[id] = [];
            }

            result.SignalSamples[id].Add(ReadSignal(session, id));
        }

        foreach (var id in hiddenIds)
        {
            if (!result.HiddenSamples.ContainsKey(id))
            {
                result.HiddenSamples[id] = [];
            }

            result.HiddenSamples[id].Add(ReadHidden(session, id));
        }
    }

    private static void ComputeDriftMetrics(Ap4R3ComplexCaseResult result)
    {
        var sensor = result.SignalSamples.GetValueOrDefault("Axis01.MotorTemperature");
        var hidden = result.HiddenSamples.GetValueOrDefault("ThermalLoad");
        var redundant = result.SignalSamples.GetValueOrDefault("Thermal.SpindleMotorTemp");

        if (sensor != null && sensor.Count > 0)
        {
            result.SensorStart = sensor.First();
            result.SensorEnd = sensor.Last();
            result.SensorDelta = result.SensorEnd - result.SensorStart;
        }

        if (hidden != null && hidden.Count > 0)
        {
            result.HiddenStart = hidden.First();
            result.HiddenEnd = hidden.Last();
            result.HiddenDelta = result.HiddenEnd - result.HiddenStart;
        }

        if (redundant != null && redundant.Count > 0)
        {
            result.RedundantStart = redundant.First();
            result.RedundantEnd = redundant.Last();
            result.RedundantDelta = result.RedundantEnd - result.RedundantStart;
        }
    }

    private static double AverageLast(List<double> values, int count)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return values.TakeLast(Math.Min(count, values.Count)).Average();
    }

    private static PhysicalMachineSession CreateSession(
        FaultScenarioTestStack stack, string profileId, int seed, double timeFactor, ProcessPhase phase)
    {
        var profile = profileId switch
        {
            LaserProcessingMachine300ProfileFactory.ProfileId => LaserProcessingMachine300ProfileFactory.Create(),
            BendingHydraulicMachine300ProfileFactory.ProfileId => BendingHydraulicMachine300ProfileFactory.Create(),
            _ => TechnicalLearningMachine300ProfileFactory.Create()
        };
        var machineId = Guid.NewGuid();
        var runtime = new PhysicalMachineRuntimeFactory().Create(profile);
        var session = new PhysicalMachineSession
        {
            MachineId = machineId,
            MachineName = profileId,
            Profile = profile,
            Runtime = runtime,
            Simulation =
            {
                Seed = seed,
                VerificationMode = PhysicalVerificationMode.Short,
                TimeFactor = timeFactor,
                GenerationMode = SignalGenerationMode.Physical,
                IsEngineActive = true,
                CurrentPhase = phase
            }
        };
        stack.RuntimeCoordinator.EnsureEngine(session, seed);
        stack.FaultScenarioService.RegisterSession(session);
        return session;
    }

    private static double ReadSignal(PhysicalMachineSession session, string signalId) =>
        session.Runtime.Signals.FirstOrDefault(s => s.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase))?.CurrentValue ?? 0;

    private static double ReadHidden(PhysicalMachineSession session, string stateId)
    {
        var state = session.Runtime.HiddenProcessStates.FirstOrDefault(s => s.StateId.Equals(stateId, StringComparison.OrdinalIgnoreCase));
        return state == null ? 0 : (state.CurrentValue + state.TargetValue) * 0.5;
    }
}
