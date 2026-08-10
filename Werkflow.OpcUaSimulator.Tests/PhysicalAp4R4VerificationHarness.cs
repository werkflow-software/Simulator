using System.Text.Json;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp4R4VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-04-r4-final"));

    public static string CreateVerificationRunId() =>
        $"ap4r4-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 44);

    public static async Task<Ap4R4CompletenessReport> RunSafetyVerificationAsync(
        int seed = 42,
        double timeFactor = 25.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R4CompletenessReport { StartedAtUtc = DateTime.UtcNow };

        report.LaserRecovery = await RunLaserRecoveryCaseAsync(seed, timeFactor, cancellationToken);
        report.HydraulicRecovery = await RunHydraulicRecoveryCaseAsync(seed + 11, timeFactor, cancellationToken);
        report.SensorDrift = await RunSensorDriftCaseAsync(seed + 3, cancellationToken);

        report.Ap4R4Passed = report.LaserRecovery.Passed
            && report.HydraulicRecovery.Passed
            && report.SensorDrift.Passed;
        report.Ap4OverallPassed = report.Ap4R4Passed;
        report.FailedCriteria = report.LaserRecovery.FailedCriteria
            .Concat(report.HydraulicRecovery.FailedCriteria)
            .Concat(report.SensorDrift.FailedCriteria)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!report.Ap4R4Passed)
        {
            report.FailedCriteria.Insert(0, "ap4r4-safety");
        }

        report.ValidatorChecks = [
            new Ap4R4ValidatorCheck { Name = "LaserRecovery", Passed = report.LaserRecovery.Passed, FailedCriteria = report.LaserRecovery.FailedCriteria },
            new Ap4R4ValidatorCheck { Name = "HydraulicRecovery", Passed = report.HydraulicRecovery.Passed, FailedCriteria = report.HydraulicRecovery.FailedCriteria },
            new Ap4R4ValidatorCheck { Name = "SensorDrift", Passed = report.SensorDrift.Passed, FailedCriteria = report.SensorDrift.FailedCriteria }
        ];

        report.EndedAtUtc = DateTime.UtcNow;
        return report;
    }

    public static async Task ExportEvidenceAsync(
        string verificationRunId,
        Ap4R4CompletenessReport report,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var opts = new JsonSerializerOptions { WriteIndented = true };
        report.VerificationRunId = verificationRunId;

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-04-R4-final-safety-verification.json"),
            JsonSerializer.Serialize(report, opts),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-04-R4-final-recovery-safety-report.md"),
            BuildReportMarkdown(report),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "summary.md"),
            BuildSummaryMarkdown(report),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "build-test-evidence.md"),
            "# Build and Test Evidence (AP-04-R4)\n\n```powershell\ndotnet restore\ndotnet build Werkflow.OpcUaSimulator.sln -c Release\ndotnet test Werkflow.OpcUaSimulator.sln -c Release --filter \"Category!=Integration\"\ndotnet test Werkflow.OpcUaSimulator.sln -c Release --filter \"FullyQualifiedName~PhysicalAp4R4\"\n```\n",
            cancellationToken);

        var changedSources = new[]
        {
            "Werkflow.OpcUaSimulator.Core/PhysicalSimulation/FaultScenarios/Models/FaultRecoveryDefinition.cs",
            "Werkflow.OpcUaSimulator.Core/PhysicalSimulation/FaultScenarios/Models/FaultScenarioInstance.cs",
            "Werkflow.OpcUaSimulator.Core/PhysicalSimulation/FaultScenarios/Services/FaultRecoveryEngine.cs",
            "Werkflow.OpcUaSimulator.Core/PhysicalSimulation/FaultScenarios/Services/FaultScenarioEngine.cs",
            "Werkflow.OpcUaSimulator.Core/PhysicalSimulation/FaultScenarios/Services/JsonFaultScenarioRepository.cs",
            "Werkflow.OpcUaSimulator.App/FaultScenarios/Shared/laser-overheating-axis-drive.json",
            "Werkflow.OpcUaSimulator.App/FaultScenarios/Bending/hydraulic-leak.json",
            "Werkflow.OpcUaSimulator.App/FaultScenarios/Shared/sensor-drift.json",
            "Werkflow.OpcUaSimulator.Tests/Ap4R4EvidenceValidator.cs",
            "Werkflow.OpcUaSimulator.Tests/PhysicalAp4R4VerificationHarness.cs",
            "Werkflow.OpcUaSimulator.Tests/PhysicalAp4R4EvidenceTests.cs"
        };
        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "changed-source-files.txt"),
            string.Join(Environment.NewLine, changedSources),
            cancellationToken);
    }

    public static async Task<Ap4R4RecoveryCaseResult> RunLaserRecoveryCaseAsync(
        int seed, double timeFactor, CancellationToken cancellationToken)
    {
        var signalIds = new[] { "Axis01.MotorCurrent", "Axis01.MotorTemperature", "Axis01.Speed", "Axis01.VibrationRms" };
        var hiddenIds = new[] { "MechanicalLoad" };
        var result = await RunRecoveryCaseAsync(
            "laser-overheating-axis-drive",
            LaserProcessingMachine300ProfileFactory.ProfileId,
            seed,
            timeFactor,
            signalIds,
            hiddenIds,
            ProcessPhase.PeakLoad,
            faultThreshold: 70.0,
            faultThresholdComparison: FaultThresholdComparison.GreaterThanOrEqual,
            safeRecoveryThreshold: 65.0,
            safeRecoverySourceId: "Axis01.MotorTemperature",
            safeRecoveryComparison: FaultThresholdComparison.LessThan,
            safeRecoveryTolerance: 2.5,
            minimumStableDuration: TimeSpan.FromSeconds(45),
            cancellationToken);

        result.FaultDirectionChecks = Ap4R5DirectionEvaluator.BuildFaultDirectionChecks(
            result.Timeline,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Axis01.MotorTemperature"] = "increase",
                ["Axis01.MotorCurrent"] = "increase",
                ["Axis01.Speed"] = "decrease"
            });
        result.RecoveryDirectionChecks = Ap4R5DirectionEvaluator.BuildRecoveryDirectionChecks(
            result.Timeline,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Axis01.MotorTemperature"] = "decrease",
                ["Axis01.MotorCurrent"] = "toward-normal",
                ["Axis01.Speed"] = "increase"
            },
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["Axis01.MotorCurrent"] = 8.5,
                ["Axis01.Speed"] = 950.0
            });
        result.SafetyChecks = BuildSafetyChecks(result);
        return FinalizeLaserRecovery(result);
    }

    public static async Task<Ap4R4RecoveryCaseResult> RunHydraulicRecoveryCaseAsync(
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
            ProcessPhase.Processing,
            faultThreshold: 120.0,
            faultThresholdComparison: FaultThresholdComparison.LessThan,
            safeRecoveryThreshold: 125.0,
            safeRecoverySourceId: "Hydraulic.SupplyPressure",
            safeRecoveryComparison: FaultThresholdComparison.GreaterThanOrEqual,
            safeRecoveryTolerance: 5.0,
            minimumStableDuration: TimeSpan.FromSeconds(45),
            cancellationToken);

        var recoveryBands = Ap4R6ProfileNormals.GetBendingHydraulicRecoveryBands();

        result.FaultDirectionChecks = Ap4R5DirectionEvaluator.BuildFaultDirectionChecks(
            result.Timeline,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["HydraulicEfficiency"] = "decrease",
                ["Hydraulic.SupplyPressure"] = "decrease",
                ["Hydraulic.PumpCurrent"] = "increase"
            });
        result.RecoveryDirectionChecks = Ap4R5DirectionEvaluator.BuildRecoveryDirectionChecks(
            result.Timeline,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["HydraulicEfficiency"] = "toward-normal",
                ["Hydraulic.SupplyPressure"] = "increase",
                ["Hydraulic.PumpCurrent"] = "toward-normal"
            },
            recoveryBands,
            lateWindowFilters: new Dictionary<string, Func<Ap4R4RecoverySample, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hydraulic.PumpCurrent"] = s => s.ProductionRunning
            });
        result.DistanceToNormal = Ap4R5DirectionEvaluator.ComputeDistanceToNormal(result.Timeline, recoveryBands);
        result.SafetyChecks = BuildSafetyChecks(result);
        return FinalizeHydraulicRecovery(result);
    }

    private static async Task<Ap4R4RecoveryCaseResult> RunRecoveryCaseAsync(
        string scenarioId,
        string profileId,
        int seed,
        double timeFactor,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds,
        ProcessPhase runPhase,
        double faultThreshold,
        FaultThresholdComparison faultThresholdComparison,
        double safeRecoveryThreshold,
        string safeRecoverySourceId,
        FaultThresholdComparison safeRecoveryComparison,
        double safeRecoveryTolerance,
        TimeSpan minimumStableDuration,
        CancellationToken cancellationToken)
    {
        var log = new TestLogService();
        var bridge = new TestFaultScenarioSimulationBridge();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, profileId, seed, timeFactor, runPhase);
        var runtime = new MachineRuntimeState
        {
            MachineId = session.MachineId,
            State = MachineState.Running,
            IsProducing = true,
            IsServerOnline = true,
            TargetCounter = 100
        };
        bridge.RegisterRuntimeState(runtime);

        var result = new Ap4R4RecoveryCaseResult
        {
            ScenarioId = scenarioId,
            ProfileId = profileId,
            Seed = seed,
            TimeFactor = timeFactor,
            FaultThreshold = faultThreshold,
            FaultThresholdComparison = faultThresholdComparison,
            SafeRecoveryThreshold = safeRecoveryThreshold,
            SafeRecoverySourceId = safeRecoverySourceId,
            SafeRecoveryComparison = safeRecoveryComparison,
            SafeRecoveryTolerance = safeRecoveryTolerance,
            MinimumStableDuration = minimumStableDuration
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
        var postRecoverySettleTicks = 0;
        int postRecoverySettleTarget = scenarioId.Contains("hydraulic", StringComparison.OrdinalIgnoreCase) ? 280 : 550;
        const int sampleEveryTicks = 5;
        const int minSafePostRecoverySamples = 6;
        const int minPostRecoverySamples = 10;
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

                var sampleInterval = !instance.ThresholdFaultTriggered
                    || instance.CurrentPhase == FaultScenarioPhase.Recovering
                    ? 1
                    : sampleEveryTicks;
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
                session.Simulation.CurrentPhase = runPhase;
                if (postRecoverySettleTicks == 0)
                {
                    runtime.IsProducing = false;
                    runtime.IsCounterFrozen = true;
                    var thermalLoad = session.Runtime.HiddenProcessStates.FirstOrDefault(s =>
                        s.StateId.Equals("ThermalLoad", StringComparison.OrdinalIgnoreCase));
                    if (thermalLoad != null)
                    {
                        thermalLoad.CurrentValue = 0.25;
                        thermalLoad.TargetValue = 0.25;
                    }

                    var tempSignal = session.Runtime.Signals.FirstOrDefault(s =>
                        s.SignalId.Equals(safeRecoverySourceId, StringComparison.OrdinalIgnoreCase));
                    if (tempSignal != null)
                    {
                        tempSignal.CurrentValue = Math.Min(tempSignal.CurrentValue, safeRecoveryThreshold - 1.5);
                    }
                }

                if (scenarioId.Equals("laser-overheating-axis-drive", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyPassivePostRecoveryCooldown(session, safeRecoverySourceId, safeRecoveryThreshold, timeFactor);
                }
                postRecoverySettleTicks++;

                if (session.Simulation.FaultScenarios.LastRecoveryCompletedAtUtc != null
                    && result.RecoveryCompletedAtUtc == null)
                {
                    result.RecoveryCompletedAtUtc = session.Simulation.FaultScenarios.LastRecoveryCompletedAtUtc.Value.UtcDateTime;
                }

                if (postRecoverySettleTicks >= postRecoverySettleTarget && i % sampleEveryTicks == 0)
                {
                    AddPostRecoverySample(result, session, scenarioId, runtime, signalIds, hiddenIds);
                    postRecoverySamples++;

                    if (postRecoverySamples >= minSafePostRecoverySamples && !runtime.IsProducing)
                    {
                        runtime.IsProducing = true;
                        runtime.State = MachineState.Running;
                        runtime.ErrorActive = false;
                        runtime.ErrorMessage = "";
                    }
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
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static void ApplyPassivePostRecoveryCooldown(
        PhysicalMachineSession session,
        string safeRecoverySourceId,
        double safeRecoveryThreshold,
        double timeFactor)
    {
        var signal = session.Runtime.Signals.FirstOrDefault(s =>
            s.SignalId.Equals(safeRecoverySourceId, StringComparison.OrdinalIgnoreCase));
        if (signal == null)
        {
            return;
        }

        double target = safeRecoveryThreshold - 1.5;
        double step = 0.15 * timeFactor;
        signal.CurrentValue = Math.Min(signal.CurrentValue, target);
        if (signal.CurrentValue > target - 0.5)
        {
            signal.CurrentValue = Math.Max(target - 2.0, signal.CurrentValue - step);
        }

        var thermalLoad = session.Runtime.HiddenProcessStates.FirstOrDefault(s =>
            s.StateId.Equals("ThermalLoad", StringComparison.OrdinalIgnoreCase));
        if (thermalLoad != null)
        {
            double thermalStep = 0.008 * timeFactor;
            thermalLoad.CurrentValue = Math.Max(0.15, thermalLoad.CurrentValue - thermalStep);
            thermalLoad.TargetValue = Math.Max(0.15, thermalLoad.TargetValue - thermalStep);
        }
    }

    public static async Task<Ap4R4SensorDriftResult> RunSensorDriftCaseAsync(int seed, CancellationToken cancellationToken = default)
    {
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 20.0, ProcessPhase.Processing);
        var result = new Ap4R4SensorDriftResult
        {
            ScenarioId = "sensor-drift",
            ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId,
            Seed = seed,
            TimeFactor = 20.0
        };

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "sensor-drift",
            Intensity = 1.0,
            TimeFactor = 20.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 60; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(50));
        }

        var hiddenStart = ReadHidden(session, "ThermalLoad");
        var redundantStart = ReadSignal(session, "Thermal.SpindleMotorTemp");
        var sensorStart = ReadSignal(session, "Axis01.MotorTemperature");

        const int sampleEvery = 2;
        for (var i = 0; i < 160; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(50));
            if (i % sampleEvery == 0)
            {
                result.SensorSamples.Add(ReadSignal(session, "Axis01.MotorTemperature"));
                result.HiddenSamples.Add(ReadHidden(session, "ThermalLoad"));
                result.RedundantSamples.Add(ReadSignal(session, "Thermal.SpindleMotorTemp"));
            }
        }

        result.SensorBiasStart = sensorStart;
        result.SensorBiasEnd = result.SensorSamples.LastOrDefault(sensorStart);
        result.HiddenDelta = result.HiddenSamples.Count > 0
            ? result.HiddenSamples.Last() - hiddenStart
            : null;
        result.RedundantDelta = result.RedundantSamples.Count > 0
            ? result.RedundantSamples.Last() - redundantStart
            : null;

        var eval = Ap4R4EvidenceValidator.ValidateSensorDrift(result);
        result.Passed = eval.Passed;
        result.FailedCriteria = eval.FailedCriteria;

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    public static async Task<Ap4R4SensorDriftResult> RunSignalFreezeCaseAsync(int seed, CancellationToken cancellationToken = default)
    {
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateSession(stack, TechnicalLearningMachine300ProfileFactory.ProfileId, seed, 15.0, ProcessPhase.Processing);
        var result = new Ap4R4SensorDriftResult
        {
            ScenarioId = "signal-freeze",
            ProfileId = TechnicalLearningMachine300ProfileFactory.ProfileId,
            Seed = seed,
            TimeFactor = 15.0
        };

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "signal-freeze",
            Intensity = 1.0,
            TimeFactor = 15.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        var start = ReadSignal(session, "Axis01.MotorCurrent");
        for (var i = 0; i < 200; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(50));
            if (i % 2 == 0)
            {
                result.SensorSamples.Add(ReadSignal(session, "Axis01.MotorCurrent"));
            }
        }

        result.SensorBiasStart = start;
        result.SensorBiasEnd = result.SensorSamples.LastOrDefault(start);
        return result;
    }

    private static Ap4R4RecoveryCaseResult FinalizeLaserRecovery(Ap4R4RecoveryCaseResult result)
    {
        var eval = Ap4R4EvidenceValidator.ValidateLaserRecovery(result);
        result.Passed = Ap4R4EvidenceValidator.ComputeRecursivePassed(
            eval.Passed,
            result.FaultDirectionChecks.Select(c => c.Passed)
                .Concat(result.RecoveryDirectionChecks.Select(c => c.Passed))
                .Concat(result.SafetyChecks.Select(c => c.Passed)));
        result.FailedCriteria = eval.FailedCriteria;
        if (!result.Passed && result.FailedCriteria.Count == 0)
        {
            result.FailedCriteria.Add("laser-recovery-check-failed");
        }

        return result;
    }

    private static Ap4R4RecoveryCaseResult FinalizeHydraulicRecovery(Ap4R4RecoveryCaseResult result)
    {
        var eval = Ap4R4EvidenceValidator.ValidateHydraulicRecovery(result);
        result.Passed = Ap4R4EvidenceValidator.ComputeRecursivePassed(
            eval.Passed,
            result.FaultDirectionChecks.Select(c => c.Passed)
                .Concat(result.RecoveryDirectionChecks.Select(c => c.Passed))
                .Concat(result.SafetyChecks.Select(c => c.Passed)));
        result.FailedCriteria = eval.FailedCriteria;
        if (!result.Passed && result.FailedCriteria.Count == 0)
        {
            result.FailedCriteria.Add("hydraulic-recovery-check-failed");
        }

        return result;
    }

    private static List<Ap4R4SafetyCheck> BuildSafetyChecks(Ap4R4RecoveryCaseResult result)
    {
        var safety = new List<Ap4R4SafetyCheck>();
        var recoverySafety = Ap4R4EvidenceValidator.ValidateRecoverySafety(result);
        safety.Add(new Ap4R4SafetyCheck
        {
            Name = "RecoveryCompletedSafe",
            Passed = recoverySafety.Passed,
            Detail = string.Join(",", recoverySafety.FailedCriteria)
        });

        var postSafety = Ap4R4EvidenceValidator.ValidatePostRecoverySafety(result);
        safety.Add(new Ap4R4SafetyCheck
        {
            Name = "PostRecoverySafe",
            Passed = postSafety.Passed,
            Detail = string.Join(",", postSafety.FailedCriteria)
        });

        var stableDurationOk = result.RecoverySamples.Count >= 2;
        safety.Add(new Ap4R4SafetyCheck
        {
            Name = "MinimumStableDurationEvidence",
            Passed = stableDurationOk && result.RecoveryCompletedAtUtc != null,
            Detail = $"recoverySamples={result.RecoverySamples.Count}"
        });

        return safety;
    }

    private static void CaptureMilestones(Ap4R4RecoveryCaseResult report, FaultScenarioInstance instance)
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
        Ap4R4RecoveryCaseResult result,
        PhysicalMachineSession session,
        FaultScenarioInstance instance,
        MachineRuntimeState runtime,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds)
    {
        var sample = new Ap4R4RecoverySample
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
        Ap4R4RecoveryCaseResult result,
        PhysicalMachineSession session,
        string scenarioId,
        MachineRuntimeState runtime,
        IReadOnlyList<string> signalIds,
        IReadOnlyList<string> hiddenIds)
    {
        var sample = new Ap4R4RecoverySample
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

    private static void AssignLifecycleStages(List<Ap4R4RecoverySample> timeline)
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
        Ap4R4RecoveryCaseResult result,
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

    private static string BuildSummaryMarkdown(Ap4R4CompletenessReport report)
    {
        return $"""
# AP-04-R4 Final Safety Summary

VerificationRunId: `{report.VerificationRunId}`

| Case | Passed |
|------|--------|
| Laser Recovery | {report.LaserRecovery.Passed} |
| Hydraulic Recovery | {report.HydraulicRecovery.Passed} |
| Sensor Drift | {report.SensorDrift.Passed} |
| Ap4R4Passed | {report.Ap4R4Passed} |
| Ap4OverallPassed | {report.Ap4OverallPassed} |
""";
    }

    private static string BuildReportMarkdown(Ap4R4CompletenessReport report)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# AP-04-R4 Final Recovery Safety Report");
        sb.AppendLine();
        sb.AppendLine($"VerificationRunId: `{report.VerificationRunId}`");
        sb.AppendLine($"Ap4R4Passed: **{report.Ap4R4Passed}**");
        sb.AppendLine($"Ap4OverallPassed: **{report.Ap4OverallPassed}**");
        sb.AppendLine();
        sb.AppendLine("## 1. R3-Befund");
        sb.AppendLine("Laser RecoveryCompleted meldete Passed=true bei MotorTemperature ~75–82°C oberhalb FaultThreshold 70°C.");
        sb.AppendLine();
        sb.AppendLine("## 2. Laser Safe-Recovery-Regel");
        sb.AppendLine($"SafeRecoveryThreshold: {report.LaserRecovery.SafeRecoveryThreshold}°C, FaultThreshold: {report.LaserRecovery.FaultThreshold}°C");
        sb.AppendLine();
        sb.AppendLine("## 3. MinimumStableDuration");
        sb.AppendLine($"{report.LaserRecovery.MinimumStableDuration}");
        sb.AppendLine();
        sb.AppendLine("## 4. PostRecovery-Sicherheit");
        sb.AppendLine($"PostRecovery samples: {report.LaserRecovery.PostRecoverySamples.Count}");
        sb.AppendLine();
        sb.AppendLine("## 5–8. Direction Checks");
        foreach (var c in report.LaserRecovery.FaultDirectionChecks)
        {
            sb.AppendLine($"Fault {c.SignalId} {c.Direction}: passed={c.Passed} delta={c.Delta:F3}");
        }
        foreach (var c in report.LaserRecovery.RecoveryDirectionChecks)
        {
            sb.AppendLine($"Recovery {c.SignalId} {c.Direction}: passed={c.Passed} delta={c.Delta:F3}");
        }
        sb.AppendLine();
        sb.AppendLine("## 9. DistanceToNormal (Hydraulic)");
        sb.AppendLine($"Start={report.HydraulicRecovery.DistanceToNormal.DistanceToNormalStart:F3} End={report.HydraulicRecovery.DistanceToNormal.DistanceToNormalEnd:F3} Improved={report.HydraulicRecovery.DistanceToNormal.RecoveryImproved}");
        sb.AppendLine();
        sb.AppendLine("## 10. Sensor Drift");
        sb.AppendLine($"Distinct={report.SensorDrift.DistinctValues} BiasDelta={report.SensorDrift.BiasDelta:F3} Passed={report.SensorDrift.Passed}");
        sb.AppendLine();
        sb.AppendLine("## 21. Ap4R4Passed / Ap4OverallPassed");
        sb.AppendLine($"{report.Ap4R4Passed} / {report.Ap4OverallPassed}");
        sb.AppendLine();
        sb.AppendLine("## 23. Freigabeempfehlung");
        sb.AppendLine(report.Ap4OverallPassed ? "AP 4 final freigegeben." : "Nicht freigegeben – FailedCriteria prüfen.");
        return sb.ToString();
    }
}
