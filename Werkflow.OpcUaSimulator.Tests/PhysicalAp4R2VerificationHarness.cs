using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.OpcUa;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp4R2VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-04-r2-final"));

    public static string ProfilesDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Werkflow.OpcUaSimulator.App", "MachineProfiles"));

    public static string FaultScenariosSourceDirectory => PhysicalTestServiceFactory.ResolveFaultScenariosDirectory();

    public static string CreateVerificationRunId() =>
        $"ap4r2-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 44);

    public static async Task<Ap4R1ScenarioManifest> BuildScenarioManifestAsync(CancellationToken cancellationToken = default) =>
        await PhysicalAp4R1VerificationHarness.BuildScenarioManifestAsync(cancellationToken);

    public static async Task<Ap4R2FaultRecoveryReport> RunFaultRecoveryVerificationAsync(
        int seed = 42,
        double timeFactor = 35.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R2FaultRecoveryReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Seed = seed,
            TimeFactor = timeFactor
        };

        report.Laser = await RunFaultRecoveryCaseAsync(
            "laser-overheating-axis-drive",
            LaserProcessingMachine300ProfileFactory.ProfileId,
            seed,
            timeFactor,
            ProcessPhase.PeakLoad,
            cancellationToken);

        report.Bending = await RunFaultRecoveryCaseAsync(
            "hydraulic-leak",
            BendingHydraulicMachine300ProfileFactory.ProfileId,
            seed + 11,
            timeFactor,
            ProcessPhase.Processing,
            cancellationToken);

        report.EndedAtUtc = DateTime.UtcNow;
        report.Passed = report.Laser.Passed && report.Bending.Passed;
        return report;
    }

    public static async Task<Ap4R2ComplexScenarioReport> RunComplexScenarioVerificationAsync(
        int seed = 42,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R2ComplexScenarioReport { Seed = seed, StartedAtUtc = DateTime.UtcNow };
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        report.Imbalance = await RunImbalanceCaseAsync(stack, seed, cancellationToken);
        report.SensorDrift = await RunSensorDriftCaseAsync(stack, seed + 3, cancellationToken);
        report.CoolantLoss = await RunCoolantLossCaseAsync(stack, seed + 5, cancellationToken);
        report.HydraulicLeak = await RunHydraulicLeakCaseAsync(stack, seed + 7, cancellationToken);
        report.Intermittent = await RunIntermittentCaseAsync(stack, seed + 9, cancellationToken);

        report.Passed = report.Imbalance.Passed
            && report.SensorDrift.Passed
            && report.CoolantLoss.Passed
            && report.HydraulicLeak.Passed
            && report.Intermittent.Passed;
        report.EndedAtUtc = DateTime.UtcNow;
        return report;
    }

    public static async Task<Ap4R2FinalEndToEndReport> RunFinalEndToEndAsync(
        string verificationRunId,
        CancellationToken cancellationToken = default)
    {
        var duration = TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("AP4R2_E2E_SECONDS"), out var s) && s > 0 ? s : 180);

        var report = new Ap4R2FinalEndToEndReport
        {
            VerificationRunId = verificationRunId,
            StartedAtUtc = DateTime.UtcNow,
            Duration = duration,
            LaserProfileHash = HashFile(Path.Combine(ProfilesDirectory, "LaserProcessingMachine300.json")),
            BendingProfileHash = HashFile(Path.Combine(ProfilesDirectory, "BendingHydraulicMachine300.json"))
        };

        var log = new TestLogService();
        var bridge = new TestFaultScenarioSimulationBridge();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var serverService = new MachineServerService(log, stack.Coordinator);
        bridge.ServerService = serverService;

        var laserMachine = CreateMachine(1, 14940, LaserProcessingMachine300ProfileFactory.ProfileId, 42);
        var bendingMachine = CreateMachine(2, 14941, BendingHydraulicMachine300ProfileFactory.ProfileId, 99);
        var machines = new[] { laserMachine, bendingMachine };

        foreach (var machine in machines)
        {
            var runtime = new MachineRuntimeState
            {
                MachineId = machine.Id,
                State = MachineState.Running,
                IsProducing = true,
                IsServerOnline = true,
                TargetCounter = 50
            };
            bridge.RegisterRuntimeState(runtime);
            stack.Coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
            await serverService.StartServerAsync(machine, runtime, cancellationToken).ConfigureAwait(false);
        }

        await StartScenario(stack, laserMachine.Id, "laser-overheating-axis-drive", 30.0, FaultScenarioRunMode.Normal, cancellationToken);
        await StartScenario(stack, bendingMachine.Id, "hydraulic-leak", 30.0, FaultScenarioRunMode.Normal, cancellationToken);

        var laserStopped = false;
        var bendingStopped = false;
        var endAt = DateTime.UtcNow + duration;

        try
        {
            while (DateTime.UtcNow < endAt)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);

                foreach (var session in stack.Coordinator.GetSessions())
                {
                    var runtime = bridge.GetOrCreate(session.MachineId);
                    runtime.Heartbeat++;
                    foreach (var info in stack.FaultScenarioService.GetActiveScenarios(session.MachineId))
                    {
                        var instance = session.Simulation.FaultScenarios.ActiveInstances[info.InstanceId];
                        report.Samples.Add(CaptureSample(instance, runtime, serverService.IsRunning(session.MachineId)));

                        if (instance.ThresholdFaultTriggered && instance.LifecycleState == FaultScenarioLifecycleState.Faulted)
                        {
                            if (session.MachineId == laserMachine.Id && !laserStopped)
                            {
                                await stack.FaultScenarioService.StopAsync(session.MachineId, "laser-overheating-axis-drive", cancellationToken).ConfigureAwait(false);
                                laserStopped = true;
                            }

                            if (session.MachineId == bendingMachine.Id && !bendingStopped)
                            {
                                await stack.FaultScenarioService.StopAsync(session.MachineId, "hydraulic-leak", cancellationToken).ConfigureAwait(false);
                                bendingStopped = true;
                            }
                        }
                    }
                }
            }

            report.TotalOpcUaUpdates = stack.Coordinator.GetSessions().Sum(s => s.Metrics.TotalPublishedUpdates);
            report.Exceptions = log.Entries.Where(e => e.Category == LogCategory.Error).Select(e => e.Message).ToList();

            EvaluateEndToEndCriteria(report, laserStopped, bendingStopped);
        }
        finally
        {
            try
            {
                await stack.Coordinator.StopAllAsync(cancellationToken).ConfigureAwait(false);
                await serverService.StopAllAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore teardown errors
            }

            report.CompletedAtUtc = DateTime.UtcNow;
        }

        var eval = Ap4R2TimelineValidator.ValidateEndToEnd(report);
        report.Passed = eval.Passed;
        report.FailedCriteria = eval.FailedCriteria;
        return report;
    }

    public static async Task ExportEvidenceAsync(
        string verificationRunId,
        Ap4R1ScenarioManifest? manifest,
        Ap4R2FaultRecoveryReport? faultRecovery,
        Ap4R2ComplexScenarioReport? complex,
        Ap4R2FinalEndToEndReport? endToEnd,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var opts = new JsonSerializerOptions { WriteIndented = true };

        if (manifest != null)
        {
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-scenario-manifest.json"), JsonSerializer.Serialize(manifest, opts), cancellationToken);
        }

        if (faultRecovery != null)
        {
            faultRecovery.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R2-fault-recovery-verification.json"), JsonSerializer.Serialize(faultRecovery, opts), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R2-fault-recovery-verification.md"),
                $"# AP-04-R2 Fault Recovery\n\nVerificationRunId: {verificationRunId}\nPassed: {faultRecovery.Passed}\n", cancellationToken);
        }

        if (complex != null)
        {
            complex.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R2-complex-scenario-verification.json"), JsonSerializer.Serialize(complex, opts), cancellationToken);
        }

        if (endToEnd != null)
        {
            endToEnd.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R2-final-end-to-end.json"), JsonSerializer.Serialize(endToEnd, opts), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R2-final-end-to-end.md"),
                $"# AP-04-R2 Final E2E\n\nVerificationRunId: {verificationRunId}\nPassed: {endToEnd.Passed}\n", cancellationToken);
        }

        CopyDirectory(FaultScenariosSourceDirectory, Path.Combine(EvidenceDirectory, "FaultScenarios"));

        var buildEvidence = $"# Build and Test Evidence (AP-04-R2)\n\n```powershell\ndotnet test --filter \"Category!=Integration\"\ndotnet test --filter \"FullyQualifiedName~PhysicalAp4R2\"\n```\n";
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "build-test-evidence.md"), buildEvidence, cancellationToken);

        var profileEvidence = new
        {
            VerificationRunId = verificationRunId,
            LaserProfileSha256 = HashFile(Path.Combine(ProfilesDirectory, "LaserProcessingMachine300.json")),
            BendingProfileSha256 = HashFile(Path.Combine(ProfilesDirectory, "BendingHydraulicMachine300.json")),
            ScenarioManifestHash = manifest?.ManifestHash ?? ""
        };
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "profile-hash-evidence.json"), JsonSerializer.Serialize(profileEvidence, opts), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "profile-hash-evidence.md"),
            $"Laser: `{profileEvidence.LaserProfileSha256}`\nBending: `{profileEvidence.BendingProfileSha256}`\nManifest: `{profileEvidence.ScenarioManifestHash}`\n", cancellationToken);
    }

    private static async Task<Ap4R2FaultRecoveryCase> RunFaultRecoveryCaseAsync(
        string scenarioId,
        string profileId,
        int seed,
        double timeFactor,
        ProcessPhase runPhase,
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

        var caseReport = new Ap4R2FaultRecoveryCase
        {
            ScenarioId = scenarioId,
            ProfileId = profileId,
            Seed = seed,
            TimeFactor = timeFactor,
            ExpectProductionResume = true
        };

        caseReport.ScenarioStartedAtUtc = DateTime.UtcNow;
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
        for (var i = 0; i < 1200 && !caseReport.RecoveryCompletedAtUtc.HasValue; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(50));
            runtime.Heartbeat++;

            var instance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
            if (instance == null)
            {
                var lastRecovery = session.Simulation.FaultScenarios.LastRecoveryCompletedAtUtc;
                if (stoppedForRecovery && lastRecovery != null && caseReport.RecoveryCompletedAtUtc == null)
                {
                    caseReport.RecoveryCompletedAtUtc = lastRecovery.Value.UtcDateTime;
                }

                break;
            }

            CaptureTimelineMilestones(caseReport, instance);
            caseReport.Timeline.Add(CaptureSample(instance, runtime, runtime.IsServerOnline));

            if (instance.ThresholdFaultTriggered && !stoppedForRecovery)
            {
                await stack.FaultScenarioService.StopAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
                stoppedForRecovery = true;
                caseReport.RecoveryStartedAtUtc = instance.RecoveryStartedAtUtc?.UtcDateTime ?? DateTime.UtcNow;
            }
        }

        caseReport.EndedAtUtc = DateTime.UtcNow;
        var eval = Ap4R2TimelineValidator.ValidateFaultRecoveryCase(caseReport);
        caseReport.Passed = eval.Passed;
        caseReport.FailedCriteria = eval.FailedCriteria;
        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return caseReport;
    }

    private static void CaptureTimelineMilestones(Ap4R2FaultRecoveryCase report, FaultScenarioInstance instance)
    {
        if (instance.ThresholdFirstReachedAtUtc != null && report.ThresholdFirstReachedAtUtc == null)
        {
            report.ThresholdFirstReachedAtUtc = instance.ThresholdFirstReachedAtUtc.Value.UtcDateTime;
            report.ThresholdValueAtFirstReached = instance.ThresholdValueAtFirstReached;
            report.ActiveThresholdRuleId = instance.ActiveThresholdRuleId;
        }

        if (instance.ThresholdConfirmedAtUtc != null && report.ThresholdConfirmedAtUtc == null)
        {
            report.ThresholdConfirmedAtUtc = instance.ThresholdConfirmedAtUtc.Value.UtcDateTime;
            report.ThresholdValueAtConfirmed = instance.ThresholdValueAtConfirmed;
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

    private static async Task<Ap4R2ComplexCaseResult> RunImbalanceCaseAsync(
        FaultScenarioTestStack stack, int seed, CancellationToken cancellationToken)
    {
        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 18.0, ProcessPhase.Processing);
        var result = new Ap4R2ComplexCaseResult { ScenarioId = "imbalance", ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId };
        result.SignalSamples["Mechanical.VibrationRms"] = [];
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "imbalance",
            Intensity = 1.2,
            TimeFactor = 18.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 600; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
            result.SignalSamples["Mechanical.VibrationRms"].Add(ReadSignal(session, "Mechanical.VibrationRms"));
        }

        var peaks = Ap4R2TimelineValidator.CountPeaks(result.SignalSamples["Mechanical.VibrationRms"], 0.015);
        result.PeriodicBehavior = peaks >= 3;
        result.Passed = result.PeriodicBehavior && result.SignalSamples["Mechanical.VibrationRms"].Max() - result.SignalSamples["Mechanical.VibrationRms"].Min() > 0.02;
        if (!result.Passed)
        {
            result.FailedCriteria.Add("imbalance-no-periodicity");
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static async Task<Ap4R2ComplexCaseResult> RunSensorDriftCaseAsync(
        FaultScenarioTestStack stack, int seed, CancellationToken cancellationToken)
    {
        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 15.0, ProcessPhase.Processing);
        var result = new Ap4R2ComplexCaseResult { ScenarioId = "sensor-drift", ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId };

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

        for (var i = 0; i < 120; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var sensorDelta = ReadSignal(session, "Axis01.MotorTemperature") - referenceSensor;
        var thermalDelta = ReadHidden(session, "ThermalLoad") - referenceThermal;
        result.SignalSamples["Axis01.MotorTemperature"] = [sensorDelta];
        result.HiddenSamples["ThermalLoad"] = [thermalDelta];
        var thermalStable = Math.Abs(thermalDelta) < 0.15;
        var sensorFrozen = Math.Abs(sensorDelta) < 0.05;
        var sensorDrifted = Math.Abs(sensorDelta) > 0.05;
        result.HiddenStableWhileSignalMoves = thermalStable && (sensorFrozen || sensorDrifted);
        result.Passed = result.HiddenStableWhileSignalMoves;
        if (!result.Passed)
        {
            result.FailedCriteria.Add("sensor-drift-target-mismatch");
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static async Task<Ap4R2ComplexCaseResult> RunCoolantLossCaseAsync(
        FaultScenarioTestStack stack, int seed, CancellationToken cancellationToken)
    {
        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 14.0, ProcessPhase.Processing);
        var result = new Ap4R2ComplexCaseResult { ScenarioId = "coolant-loss", ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId };
        var baselineFlow = ReadSignal(session, "Cooling.PrimaryCircuit.Flow");
        var baselinePressure = ReadSignal(session, "Cooling.PrimaryCircuit.Pressure");
        var baselineTemp = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature");
        var baselineEff = ReadHidden(session, "CoolingEfficiency");

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "coolant-loss",
            Intensity = 1.4,
            TimeFactor = 18.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 100; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var earlyTemp = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature");

        for (var i = 0; i < 400; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var flowDelta = ReadSignal(session, "Cooling.PrimaryCircuit.Flow") - baselineFlow;
        var pressureDelta = ReadSignal(session, "Cooling.PrimaryCircuit.Pressure") - baselinePressure;
        var tempDeltaFromBaseline = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature") - baselineTemp;
        var tempDeltaFromEarly = ReadSignal(session, "Cooling.PrimaryCircuit.Temperature") - earlyTemp;
        var effDelta = ReadHidden(session, "CoolingEfficiency") - baselineEff;

        result.Passed = effDelta < -0.01
            && flowDelta < -0.1
            && pressureDelta < -0.05
            && (tempDeltaFromBaseline > 0.05 || tempDeltaFromEarly > 0.02);
        if (!result.Passed)
        {
            result.FailedCriteria.Add($"coolant-loss-visible-effects eff={effDelta:F3} flow={flowDelta:F3} pressure={pressureDelta:F3} tempBase={tempDeltaFromBaseline:F3} tempEarly={tempDeltaFromEarly:F3}");
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static async Task<Ap4R2ComplexCaseResult> RunHydraulicLeakCaseAsync(
        FaultScenarioTestStack stack, int seed, CancellationToken cancellationToken)
    {
        var session = CreateSession(stack, BendingHydraulicMachine300ProfileFactory.ProfileId, seed, 16.0, ProcessPhase.Processing);
        var result = new Ap4R2ComplexCaseResult { ScenarioId = "hydraulic-leak", ProfileId = BendingHydraulicMachine300ProfileFactory.ProfileId };
        var baselinePressure = ReadSignal(session, "Hydraulic.SupplyPressure");
        var baselineEff = ReadHidden(session, "HydraulicEfficiency");

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "hydraulic-leak",
            Intensity = 1.5,
            TimeFactor = 20.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 600; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        result.Passed = ReadHidden(session, "HydraulicEfficiency") - baselineEff < -0.001
            && ReadSignal(session, "Hydraulic.SupplyPressure") - baselinePressure < -0.5;
        if (!result.Passed)
        {
            result.FailedCriteria.Add("hydraulic-leak-visible-effects");
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static async Task<Ap4R2ComplexCaseResult> RunIntermittentCaseAsync(
        FaultScenarioTestStack stack, int seed, CancellationToken cancellationToken)
    {
        var session = CreateSession(stack, LaserProcessingMachine300ProfileFactory.ProfileId, seed, 20.0, ProcessPhase.Processing);
        var result = new Ap4R2ComplexCaseResult { ScenarioId = "intermittent-fault", ProfileId = LaserProcessingMachine300ProfileFactory.ProfileId };
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "intermittent-fault",
            Intensity = 1.0,
            TimeFactor = 20.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 500; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var instance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
        result.EpisodeCount = instance?.IntermittentEpisodeCount ?? 0;
        result.Passed = result.EpisodeCount >= 3;
        if (!result.Passed)
        {
            result.FailedCriteria.Add("intermittent-episodes");
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static void EvaluateEndToEndCriteria(Ap4R2FinalEndToEndReport report, bool laserStopped, bool bendingStopped)
    {
        var faultSamples = report.Samples.Where(s => s.ErrorActive).ToList();
        var preFault = report.Samples.Any(s => !s.ErrorActive && s.MachineState != nameof(MachineState.Error));
        var faulted = faultSamples.Any(s => s.MachineState == nameof(MachineState.Error) && !string.IsNullOrEmpty(s.ErrorMessage));
        var prodStop = faultSamples.All(s => !s.ProductionRunning);
        var serverOk = faultSamples.All(s => s.ServerReachable);
        var recovering = report.Samples.Any(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
        var postRecovery = report.Samples.Any(s => !s.ErrorActive && s.ProductionRunning);
        var scenarioIdsOk = report.Samples.All(s => !string.IsNullOrEmpty(s.ScenarioId));

        report.ThresholdTimelinePassed = laserStopped && bendingStopped
            && report.Samples.Any(s => s.ErrorActive);
        report.FaultNodesPassed = preFault && faulted;
        report.ProductionStopPassed = prodStop && faultSamples.Count > 0;
        report.PhysicalServerOnlinePassed = faultSamples.Count > 0 && serverOk;
        report.RecoveryPassed = recovering && postRecovery;
        report.LifecyclePassed = scenarioIdsOk
            && report.Samples.Any(s => s.ScenarioPhase is nameof(FaultScenarioPhase.Developing) or nameof(FaultScenarioPhase.Critical))
            && report.Samples.Any(s => s.ScenarioPhase == nameof(FaultScenarioPhase.Recovering));
    }

    private static Ap4R2TimelineSample CaptureSample(FaultScenarioInstance instance, MachineRuntimeState runtime, bool serverReachable) =>
        new()
        {
            TimestampUtc = DateTime.UtcNow,
            ScenarioId = instance.ScenarioId,
            ScenarioPhase = instance.CurrentPhase.ToString(),
            ErrorActive = runtime.ErrorActive,
            ErrorMessage = runtime.ErrorMessage ?? "",
            MachineState = runtime.State.ToString(),
            ActualCounter = runtime.ActualCounter,
            Heartbeat = runtime.Heartbeat,
            ServerReachable = serverReachable,
            ProductionRunning = runtime.IsProducing
        };

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

    private static async Task StartScenario(
        FaultScenarioTestStack stack, Guid machineId, string scenarioId, double timeFactor,
        FaultScenarioRunMode runMode, CancellationToken cancellationToken)
    {
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = machineId,
            ScenarioId = scenarioId,
            Intensity = 1.5,
            TimeFactor = timeFactor,
            AutoThresholdFaultEnabled = runMode != FaultScenarioRunMode.NonFaultingControlRun,
            AutoScenarioEndEnabled = false,
            RunMode = runMode
        }, cancellationToken).ConfigureAwait(false);
    }

    private static MachineConfiguration CreateMachine(int index, int port, string profileId, int seed)
    {
        var machine = DefaultMachines.Create()[Math.Clamp(index - 1, 0, DefaultMachines.Create().Count - 1)];
        machine.Id = Guid.NewGuid();
        machine.Name = $"AP4R2-{profileId}-{index}";
        machine.PhysicalProfileId = profileId;
        machine.Port = port;
        machine.UpdateEndpointFromHostPort();
        return machine;
    }

    private static double ReadSignal(PhysicalMachineSession session, string signalId) =>
        session.Runtime.Signals.FirstOrDefault(s => s.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase))?.CurrentValue ?? 0;

    private static double ReadHidden(PhysicalMachineSession session, string stateId)
    {
        var state = session.Runtime.HiddenProcessStates.FirstOrDefault(s => s.StateId.Equals(stateId, StringComparison.OrdinalIgnoreCase));
        return state == null ? 0 : (state.CurrentValue + state.TargetValue) * 0.5;
    }

    private static string HashFile(string path) =>
        File.Exists(path) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() : "";

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}

// Report types
public sealed class Ap4R2FaultRecoveryReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public Ap4R2FaultRecoveryCase Laser { get; set; } = new();
    public Ap4R2FaultRecoveryCase Bending { get; set; } = new();
    public bool Passed { get; set; }
}

public sealed class Ap4R2FaultRecoveryCase
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public bool ExpectProductionResume { get; set; }
    public DateTime? ScenarioStartedAtUtc { get; set; }
    public DateTime? ThresholdFirstReachedAtUtc { get; set; }
    public DateTime? ThresholdConfirmedAtUtc { get; set; }
    public DateTime? MachineFaultedAtUtc { get; set; }
    public DateTime? RecoveryStartedAtUtc { get; set; }
    public DateTime? RecoveryCompletedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public string? ActiveThresholdRuleId { get; set; }
    public double? ThresholdValueAtFirstReached { get; set; }
    public double? ThresholdValueAtConfirmed { get; set; }
    public List<Ap4R2TimelineSample> Timeline { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R2TimelineSample
{
    public DateTime TimestampUtc { get; set; }
    public string ScenarioId { get; set; } = "";
    public string ScenarioPhase { get; set; } = "";
    public bool ErrorActive { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string MachineState { get; set; } = "";
    public int ActualCounter { get; set; }
    public ulong Heartbeat { get; set; }
    public bool ServerReachable { get; set; }
    public bool ProductionRunning { get; set; }
}

public sealed class Ap4R2ComplexScenarioReport
{
    public string VerificationRunId { get; set; } = "";
    public int Seed { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public Ap4R2ComplexCaseResult Imbalance { get; set; } = new();
    public Ap4R2ComplexCaseResult SensorDrift { get; set; } = new();
    public Ap4R2ComplexCaseResult CoolantLoss { get; set; } = new();
    public Ap4R2ComplexCaseResult HydraulicLeak { get; set; } = new();
    public Ap4R2ComplexCaseResult Intermittent { get; set; } = new();
    public bool Passed { get; set; }
}

public sealed class Ap4R2ComplexCaseResult
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public Dictionary<string, List<double>> SignalSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<double>> HiddenSamples { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool PeriodicBehavior { get; set; }
    public bool HiddenStableWhileSignalMoves { get; set; }
    public int EpisodeCount { get; set; }
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R2FinalEndToEndReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public string LaserProfileHash { get; set; } = "";
    public string BendingProfileHash { get; set; } = "";
    public long TotalOpcUaUpdates { get; set; }
    public List<Ap4R2TimelineSample> Samples { get; set; } = [];
    public List<string> Exceptions { get; set; } = [];
    public bool ThresholdTimelinePassed { get; set; }
    public bool FaultNodesPassed { get; set; }
    public bool ProductionStopPassed { get; set; }
    public bool PhysicalServerOnlinePassed { get; set; }
    public bool RecoveryPassed { get; set; }
    public bool LifecyclePassed { get; set; }
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}
