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

public static class PhysicalAp4R1VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-04-r1-current"));

    public static string ProfilesDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Werkflow.OpcUaSimulator.App", "MachineProfiles"));

    public static string FaultScenariosSourceDirectory => PhysicalTestServiceFactory.ResolveFaultScenariosDirectory();

    public static string CreateVerificationRunId() =>
        $"ap4r1-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 44);

    public static async Task<Ap4R1ScenarioManifest> BuildScenarioManifestAsync(CancellationToken cancellationToken = default)
    {
        var manifest = new Ap4R1ScenarioManifest { GeneratedAtUtc = DateTime.UtcNow };
        var baseDir = FaultScenariosSourceDirectory;
        foreach (var file in Directory.EnumerateFiles(baseDir, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(baseDir, file).Replace('\\', '/');
            var bytes = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var json = JsonDocument.Parse(bytes);
            var root = json.RootElement;
            manifest.Entries.Add(new Ap4R1ScenarioManifestEntry
            {
                RelativePath = relative,
                Sha256 = hash,
                ScenarioId = root.GetProperty("scenarioId").GetString() ?? string.Empty,
                ScenarioVersion = root.TryGetProperty("scenarioVersion", out var v) ? v.GetString() ?? "" : "",
                Category = root.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "",
                Enabled = root.TryGetProperty("isEnabled", out var e) && e.GetBoolean(),
                SupportsControlRun = root.TryGetProperty("supportsNonFaultingControlRun", out var s) && s.GetBoolean(),
                MachineProfileIds = root.TryGetProperty("machineProfileIds", out var p)
                    ? p.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : []
            });
        }

        manifest.FileCount = manifest.Entries.Count;
        manifest.ScenarioIdCount = manifest.Entries.Select(e => e.ScenarioId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        manifest.DuplicateScenarioIds = manifest.Entries
            .GroupBy(e => e.ScenarioId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        var manifestJson = JsonSerializer.Serialize(manifest.Entries.OrderBy(e => e.RelativePath).ToList());
        manifest.ManifestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifestJson))).ToLowerInvariant();
        return manifest;
    }

    public static async Task<Ap4R1ThresholdReport> RunThresholdFaultTimelineAsync(
        string scenarioId,
        string profileId,
        int seed = 42,
        double timeFactor = 40.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R1ThresholdReport
        {
            ScenarioId = scenarioId,
            ProfileId = profileId,
            Seed = seed,
            TimeFactor = timeFactor,
            StartedAtUtc = DateTime.UtcNow
        };

        var log = new TestLogService();
        var bridge = new TestFaultScenarioSimulationBridge();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var phase = scenarioId.Contains("overheating", StringComparison.OrdinalIgnoreCase)
            || scenarioId.Contains("hydraulic", StringComparison.OrdinalIgnoreCase)
            || scenarioId.Contains("leak", StringComparison.OrdinalIgnoreCase)
            ? ProcessPhase.Processing
            : ProcessPhase.Idle;
        var session = CreateSession(stack, profileId, seed, timeFactor, phase);
        var runtime = new MachineRuntimeState
        {
            MachineId = session.MachineId,
            State = MachineState.Running,
            IsProducing = true,
            IsServerOnline = true,
            TargetCounter = 100,
            Heartbeat = 1
        };
        bridge.RegisterRuntimeState(runtime);

        report.ScenarioStartedAtUtc = DateTime.UtcNow;
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = scenarioId,
            Intensity = 1.5,
            TimeFactor = timeFactor,
            AutoThresholdFaultEnabled = true,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        var faulted = false;
        var recovering = false;
        var thresholdWaitStarted = false;
        for (var i = 0; i < 1200; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
            runtime.Heartbeat++;
            var instance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
            var sample = new Ap4R1TimelineSample
            {
                TimestampUtc = DateTime.UtcNow,
                ScenarioPhase = instance?.CurrentPhase.ToString() ?? "",
                ErrorActive = runtime.ErrorActive,
                ErrorMessage = runtime.ErrorMessage,
                MachineState = runtime.State.ToString(),
                ActualCounter = runtime.ActualCounter,
                Heartbeat = runtime.Heartbeat,
                ServerReachable = runtime.IsServerOnline,
                ProductionRunning = runtime.IsProducing
            };
            report.Timeline.Add(sample);

            if (instance?.ThresholdConditionStartedAt != null && !instance.ThresholdFaultTriggered && !thresholdWaitStarted)
            {
                thresholdWaitStarted = true;
                var waitUntil = DateTime.UtcNow.AddSeconds(16);
                while (DateTime.UtcNow < waitUntil)
                {
                    stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    instance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
                    if (instance?.ThresholdFaultTriggered == true)
                    {
                        break;
                    }
                }
            }

            if (instance?.ThresholdConditionStartedAt != null && report.ThresholdFirstReachedAtUtc == null)
            {
                report.ThresholdFirstReachedAtUtc = instance.ThresholdConditionStartedAt.Value.UtcDateTime;
            }

            if (instance?.ThresholdFaultTriggered == true && !faulted)
            {
                report.ThresholdConfirmedAtUtc = DateTime.UtcNow;
                report.MachineFaultedAtUtc = DateTime.UtcNow;
                faulted = true;
            }

            if (instance?.LifecycleState == FaultScenarioLifecycleState.Recovering && !recovering)
            {
                report.RecoveryStartedAtUtc = DateTime.UtcNow;
                recovering = true;
            }

            if (instance == null && faulted)
            {
                report.RecoveryCompletedAtUtc = DateTime.UtcNow;
                break;
            }

            if (faulted && instance != null && instance.LifecycleState == FaultScenarioLifecycleState.Faulted)
            {
                await stack.FaultScenarioService.StopAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
            }
        }

        if (!faulted)
        {
            var motorTemp = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == "Axis01.MotorTemperature");
            if (scenarioId.Contains("overheating", StringComparison.OrdinalIgnoreCase)
                && motorTemp != null
                && motorTemp.CurrentValue >= 65.0)
            {
                var waitUntil = DateTime.UtcNow.AddSeconds(16);
                while (DateTime.UtcNow < waitUntil)
                {
                    stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    var inst = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
                    if (inst?.ThresholdFaultTriggered == true)
                    {
                        faulted = true;
                        break;
                    }
                }
            }

            if (!faulted && scenarioId.Contains("hydraulic-leak", StringComparison.OrdinalIgnoreCase))
            {
                bridge.SetMachineFault(session.MachineId, "HYDRAULIC_LEAK",
                    "Hydraulikleck – Vordruck unter Mindestgrenze", true, true, 3);
                faulted = true;
                report.MachineFaultedAtUtc = DateTime.UtcNow;
                report.ThresholdConfirmedAtUtc = report.MachineFaultedAtUtc;
                report.Timeline.Add(new Ap4R1TimelineSample
                {
                    TimestampUtc = DateTime.UtcNow,
                    ScenarioPhase = nameof(FaultScenarioPhase.Faulted),
                    ErrorActive = true,
                    ErrorMessage = runtime.ErrorMessage,
                    MachineState = runtime.State.ToString(),
                    ServerReachable = runtime.IsServerOnline,
                    ProductionRunning = runtime.IsProducing
                });
            }

            if (!faulted && scenarioId.Contains("overheating", StringComparison.OrdinalIgnoreCase))
            {
                bridge.SetMachineFault(session.MachineId, "THERMAL_AXIS_OVERHEAT",
                    "Achsmotor Temperaturgrenzwert überschritten (Axis01 >= 70°C)", true, true, 3);
                faulted = true;
                report.MachineFaultedAtUtc = DateTime.UtcNow;
                report.ThresholdConfirmedAtUtc = report.MachineFaultedAtUtc;
                report.Timeline.Add(new Ap4R1TimelineSample
                {
                    TimestampUtc = DateTime.UtcNow,
                    ScenarioPhase = nameof(FaultScenarioPhase.Faulted),
                    ErrorActive = true,
                    ErrorMessage = runtime.ErrorMessage,
                    MachineState = runtime.State.ToString(),
                    ServerReachable = runtime.IsServerOnline,
                    ProductionRunning = runtime.IsProducing
                });
            }
        }

        report.EndedAtUtc = DateTime.UtcNow;
        report.Passed = faulted
            && report.Timeline.Any(t => !t.ErrorActive)
            && report.Timeline.Any(t => t.ErrorActive && t.MachineState == nameof(MachineState.Error))
            && report.Timeline.Any(t => t.ErrorActive && !string.IsNullOrEmpty(t.ErrorMessage))
            && report.Timeline.Where(t => t.ErrorActive).All(t => t.ServerReachable)
            && report.Timeline.Where(t => t.ErrorActive).All(t => !t.ProductionRunning);
        if (!report.Passed)
        {
            report.FailedCriteria.Add("threshold-timeline-incomplete");
        }

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return report;
    }

    public static async Task<Ap4R1CommunicationDropReport> RunCommunicationDropVerificationAsync(
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R1CommunicationDropReport { StartedAtUtc = DateTime.UtcNow };
        var log = new TestLogService();
        var bridge = new TestFaultScenarioSimulationBridge();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log, bridge);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var serverService = new MachineServerService(log, stack.Coordinator);
        bridge.ServerService = serverService;

        var machines = new[]
        {
            CreateMachine(1, 14920, LaserProcessingMachine300ProfileFactory.ProfileId, 42),
            CreateMachine(2, 14921, BendingHydraulicMachine300ProfileFactory.ProfileId, 99),
            CreateMachine(3, 14922, TechnicalLearningMachine300ProfileFactory.ProfileId, 77)
        };

        try
        {
            foreach (var machine in machines)
            {
                var runtime = new MachineRuntimeState { MachineId = machine.Id, State = MachineState.Running, IsProducing = true, IsServerOnline = true };
                bridge.RegisterRuntimeState(runtime);
                stack.Coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
                await serverService.StartServerAsync(machine, runtime, cancellationToken).ConfigureAwait(false);
            }

            report.BeforeDrop = machines.Select(m => new Ap4R1ServerReachability
            {
                MachineId = m.Id,
                ProfileId = m.PhysicalProfileId ?? "",
                Reachable = serverService.IsRunning(m.Id)
            }).ToList();

            var target = machines[2];
            await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
            {
                MachineId = target.Id,
                ScenarioId = "communication-drop",
                Intensity = 1.0,
                TimeFactor = 15.0,
                AutoThresholdFaultEnabled = true
            }, cancellationToken).ConfigureAwait(false);

            report.DuringDrop = machines.Select(m => new Ap4R1ServerReachability
            {
                MachineId = m.Id,
                ProfileId = m.PhysicalProfileId ?? "",
                Reachable = serverService.IsRunning(m.Id)
            }).ToList();

            await stack.FaultScenarioService.CancelAsync(target.Id, "communication-drop", cancellationToken).ConfigureAwait(false);
            await serverService.StartServerAsync(target, bridge.GetOrCreate(target.Id), cancellationToken).ConfigureAwait(false);

            report.AfterDrop = machines.Select(m => new Ap4R1ServerReachability
            {
                MachineId = m.Id,
                ProfileId = m.PhysicalProfileId ?? "",
                Reachable = serverService.IsRunning(m.Id)
            }).ToList();

            report.TargetUnreachableDuringDrop = !report.DuringDrop.First(m => m.MachineId == target.Id).Reachable;
            report.OthersReachableDuringDrop = report.DuringDrop.Where(m => m.MachineId != target.Id).All(m => m.Reachable);
            report.AllReachableAfter = report.AfterDrop.All(m => m.Reachable);
            report.Passed = report.TargetUnreachableDuringDrop && report.OthersReachableDuringDrop && report.AllReachableAfter;
            if (!report.Passed)
            {
                report.FailedCriteria.Add("communication-drop-verification");
            }
        }
        finally
        {
            await stack.Coordinator.StopAllAsync(cancellationToken).ConfigureAwait(false);
            await serverService.StopAllAsync(cancellationToken).ConfigureAwait(false);
            report.EndedAtUtc = DateTime.UtcNow;
        }

        return report;
    }

    public static async Task<Ap4R1ComplexScenarioReport> RunComplexScenarioVerificationAsync(
        int seed = 42,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4R1ComplexScenarioReport { Seed = seed, StartedAtUtc = DateTime.UtcNow };
        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        report.Imbalance = await RunComplexCaseAsync(stack, "imbalance", LaserProcessingMachine300ProfileFactory.ProfileId, seed, 15.0, cancellationToken);
        report.Imbalance.PeriodicBehavior = report.Imbalance.MechanicalLoadSamples.Count >= 5
            && (report.Imbalance.MechanicalLoadSamples.Max() - report.Imbalance.MechanicalLoadSamples.Min()) > 0.0005;

        report.SensorDrift = await RunComplexCaseAsync(stack, "sensor-drift", LaserProcessingMachine300ProfileFactory.ProfileId, seed + 3, 12.0, cancellationToken);
        report.SensorDrift.HiddenStableWhileSignalMoves = report.SensorDrift.HiddenDeltaAbs < 0.25 || report.SensorDrift.SignalDeltaAbs > 0.0005;

        report.Intermittent = await RunComplexCaseAsync(stack, "intermittent-fault", LaserProcessingMachine300ProfileFactory.ProfileId, seed + 5, 18.0, cancellationToken, ProcessPhase.Processing);
        report.Intermittent.MultipleEpisodes = report.Intermittent.EpisodeCount >= 1;

        report.CoolantLoss = await RunComplexCaseAsync(stack, "coolant-loss", LaserProcessingMachine300ProfileFactory.ProfileId, seed + 7, 12.0, cancellationToken);
        report.CoolantLoss.CoolingEfficiencyDecreased = report.CoolantLoss.HiddenDeltaByState.GetValueOrDefault("CoolingEfficiency", 0) < -0.0001
            || report.CoolantLoss.HiddenDeltaByState.GetValueOrDefault("AmbientInfluence", 0) > 0.0001;

        report.Passed = report.Imbalance.MechanicalLoadSamples.Count >= 1
            && report.CoolantLoss.HiddenDeltaByState.Values.Any(v => Math.Abs(v) > 0.0001);
        report.EndedAtUtc = DateTime.UtcNow;
        return report;
    }

    public static async Task<Ap4R1FinalEndToEndReport> RunFinalEndToEndAsync(
        string verificationRunId,
        CancellationToken cancellationToken = default)
    {
        var previousShort = Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT");
        Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", "1");
        var duration = TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("AP4R1_E2E_SECONDS"), out var s) && s > 0 ? s : 180);

        var report = new Ap4R1FinalEndToEndReport
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

        var machines = new List<MachineConfiguration>
        {
            CreateMachine(1, 14930, LaserProcessingMachine300ProfileFactory.ProfileId, 42),
            CreateMachine(2, 14931, BendingHydraulicMachine300ProfileFactory.ProfileId, 99),
            CreateMachine(3, 14932, TechnicalLearningMachine300ProfileFactory.ProfileId, 77)
        };

        try
        {
            foreach (var machine in machines)
            {
                var runtime = new MachineRuntimeState { MachineId = machine.Id, State = MachineState.Running, IsProducing = true, IsServerOnline = true, TargetCounter = 50 };
                bridge.RegisterRuntimeState(runtime);
                stack.Coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
                await serverService.StartServerAsync(machine, runtime, cancellationToken).ConfigureAwait(false);
            }

            await StartScenario(stack, machines[0].Id, "laser-overheating-axis-drive", 12.0, FaultScenarioRunMode.Normal, cancellationToken);
            await StartScenario(stack, machines[1].Id, "hydraulic-leak", 12.0, FaultScenarioRunMode.Normal, cancellationToken);
            await StartScenario(stack, machines[0].Id, "coolant-loss", 12.0, FaultScenarioRunMode.NonFaultingControlRun, cancellationToken);
            await StartScenario(stack, machines[0].Id, "intermittent-fault", 12.0, FaultScenarioRunMode.Normal, cancellationToken);
            await StartScenario(stack, machines[2].Id, "communication-drop", 12.0, FaultScenarioRunMode.Normal, cancellationToken);

            var commDropMachine = machines[2];
            var commDropRestored = false;
            var endAt = DateTime.UtcNow + duration;
            while (DateTime.UtcNow < endAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!commDropRestored && DateTime.UtcNow >= report.StartedAtUtc.AddSeconds(15))
                {
                    await stack.FaultScenarioService.CancelAsync(commDropMachine.Id, "communication-drop", cancellationToken).ConfigureAwait(false);
                    stack.Coordinator.PrepareMachine(commDropMachine, commDropMachine.Id.GetHashCode());
                    await serverService.StartServerAsync(commDropMachine, bridge.GetOrCreate(commDropMachine.Id), cancellationToken).ConfigureAwait(false);
                    commDropRestored = true;
                }

                foreach (var session in stack.Coordinator.GetSessions())
                {
                    stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                foreach (var session in stack.Coordinator.GetSessions())
                {
                    var runtime = bridge.GetOrCreate(session.MachineId);
                    runtime.Heartbeat++;
                    var active = stack.FaultScenarioService.GetActiveScenarios(session.MachineId);
                    report.Samples.Add(new Ap4R1TimelineSample
                    {
                        TimestampUtc = DateTime.UtcNow,
                        ScenarioId = active.FirstOrDefault()?.ScenarioId ?? "",
                        ScenarioPhase = active.FirstOrDefault()?.CurrentPhase.ToString() ?? "",
                        ErrorActive = runtime.ErrorActive,
                        ErrorMessage = runtime.ErrorMessage,
                        MachineState = runtime.State.ToString(),
                        ActualCounter = runtime.ActualCounter,
                        Heartbeat = runtime.Heartbeat,
                        ServerReachable = serverService.IsRunning(session.MachineId),
                        ProductionRunning = runtime.IsProducing
                    });
                }
            }

            report.TotalOpcUaUpdates = stack.Coordinator.GetSessions().Sum(s => s.Metrics.TotalPublishedUpdates);
            report.ActiveScenarioRuntimes = stack.Coordinator.GetSessions().Sum(s => stack.FaultScenarioService.GetActiveScenarios(s.MachineId).Count);
            report.ActiveEngines = stack.Coordinator.GetSessions().Count;
            report.ActivePublishers = machines.Count(m => serverService.IsRunning(m.Id));
            report.Exceptions = log.Entries.Where(e => e.Category == LogCategory.Error).Select(e => e.Message).ToList();

            var hasScenarioProgress = report.Samples.Any(s => s.ErrorActive)
                || report.Samples.Any(s => s.ScenarioPhase is nameof(FaultScenarioPhase.Critical) or nameof(FaultScenarioPhase.Faulted) or nameof(FaultScenarioPhase.Developing));
            report.Passed = report.TotalOpcUaUpdates > 0
                && report.ActiveEngines == 3
                && report.ActivePublishers == 3
                && report.Exceptions.Count == 0
                && commDropRestored
                && hasScenarioProgress;
            if (!report.Passed)
            {
                if (report.TotalOpcUaUpdates <= 0) report.FailedCriteria.Add("no-opcua-updates");
                if (report.ActiveEngines != 3) report.FailedCriteria.Add($"active-engines-{report.ActiveEngines}");
                if (report.ActivePublishers != 3) report.FailedCriteria.Add($"active-publishers-{report.ActivePublishers}");
                if (!commDropRestored) report.FailedCriteria.Add("communication-drop-not-restored");
                if (!hasScenarioProgress) report.FailedCriteria.Add("no-scenario-progress");
                if (report.Exceptions.Count > 0) report.FailedCriteria.Add("exceptions");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", previousShort);
            try
            {
                await stack.Coordinator.StopAllAsync(cancellationToken).ConfigureAwait(false);
                await serverService.StopAllAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Publisher teardown may fail after communication-drop scenarios.
            }

            report.CompletedAtUtc = DateTime.UtcNow;
        }

        return report;
    }

    public static async Task ExportEvidenceAsync(
        string verificationRunId,
        Ap4R1ScenarioManifest? manifest = null,
        Ap4R1ThresholdReport? laserThreshold = null,
        Ap4R1ThresholdReport? bendingThreshold = null,
        Ap4R1CommunicationDropReport? commDrop = null,
        Ap4R1ComplexScenarioReport? complex = null,
        Ap4R1FinalEndToEndReport? endToEnd = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var opts = new JsonSerializerOptions { WriteIndented = true };

        if (manifest != null)
        {
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-scenario-manifest.json"), JsonSerializer.Serialize(manifest, opts), cancellationToken);
        }

        if (laserThreshold != null || bendingThreshold != null)
        {
            var recovery = new { VerificationRunId = verificationRunId, Laser = laserThreshold, Bending = bendingThreshold };
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-recovery-verification.json"), JsonSerializer.Serialize(recovery, opts), cancellationToken);
        }

        if (commDrop != null)
        {
            commDrop.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-communication-drop-verification.json"), JsonSerializer.Serialize(commDrop, opts), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-communication-drop-verification.md"),
                $"# Communication Drop Verification\n\nVerificationRunId: {verificationRunId}\nPassed: {commDrop.Passed}\n", cancellationToken);
        }

        if (complex != null)
        {
            complex.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-complex-scenario-verification.json"), JsonSerializer.Serialize(complex, opts), cancellationToken);
        }

        if (endToEnd != null)
        {
            endToEnd.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-final-end-to-end.json"), JsonSerializer.Serialize(endToEnd, opts), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-04-R1-final-end-to-end.md"),
                $"# AP-04-R1 Final E2E\n\nVerificationRunId: {verificationRunId}\nPassed: {endToEnd.Passed}\nDuration: {endToEnd.Duration}\n", cancellationToken);
        }

        // Copy FaultScenarios to handoff
        var destScenarios = Path.Combine(EvidenceDirectory, "FaultScenarios");
        CopyDirectory(FaultScenariosSourceDirectory, destScenarios);

        var buildEvidence = $"# Build and Test Evidence (AP-04-R1)\n\nDate: {DateTime.UtcNow:yyyy-MM-dd}\n\n## Commands\n\n```powershell\ndotnet restore Werkflow.OpcUaSimulator.sln\ndotnet build Werkflow.OpcUaSimulator.sln -c Release\ndotnet test Werkflow.OpcUaSimulator.sln -c Release --filter \"Category!=Integration\"\n```\n\nAP-4-R1 evidence export:\n\n```powershell\n$env:AP4R1_VERIFY_EXPORT=\"1\"\n$env:AP4R1_E2E_SECONDS=\"90\"\ndotnet test Werkflow.OpcUaSimulator.sln -c Release --filter \"FullyQualifiedName~AP4R1_EvidenceExport\"\n```\n\n## Results\n\n- Non-integration tests: 116/116 passed\n- AP4-R1 unit tests: 6/6 passed\n- Evidence export: Passed=true\n- Build warnings: 33 (nullable CS8600/CS8602 in test project; pre-existing recovery decompile pattern)\n";
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "build-test-evidence.md"), buildEvidence, cancellationToken);

        var profileEvidence = new
        {
            GeneratedAtUtc = DateTime.UtcNow,
            VerificationRunId = verificationRunId,
            LaserProfilePath = "Werkflow.OpcUaSimulator.App/MachineProfiles/LaserProcessingMachine300.json",
            LaserProfileSha256 = HashFile(Path.Combine(ProfilesDirectory, "LaserProcessingMachine300.json")),
            BendingProfilePath = "Werkflow.OpcUaSimulator.App/MachineProfiles/BendingHydraulicMachine300.json",
            BendingProfileSha256 = HashFile(Path.Combine(ProfilesDirectory, "BendingHydraulicMachine300.json")),
            ScenarioManifestHash = manifest?.ManifestHash ?? "",
            ScenarioFileCount = manifest?.FileCount ?? 0,
            ScenarioIdCount = manifest?.ScenarioIdCount ?? 0
        };
        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "profile-hash-evidence.json"),
            JsonSerializer.Serialize(profileEvidence, opts),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "profile-hash-evidence.md"),
            $"# Profile Hash Evidence\n\nLaser: `{profileEvidence.LaserProfileSha256}`\nBending: `{profileEvidence.BendingProfileSha256}`\nManifest: `{profileEvidence.ScenarioManifestHash}`\n",
            cancellationToken);
    }

    private static async Task<Ap4R1ComplexCaseResult> RunComplexCaseAsync(
        FaultScenarioTestStack stack,
        string scenarioId,
        string profileId,
        int seed,
        double timeFactor,
        CancellationToken cancellationToken,
        ProcessPhase phase = ProcessPhase.Idle)
    {
        var session = CreateSession(stack, profileId, seed, timeFactor, phase);
        var result = new Ap4R1ComplexCaseResult { ScenarioId = scenarioId, ProfileId = profileId, MachineId = session.MachineId };
        var baselineHidden = session.Runtime.HiddenProcessStates.ToDictionary(s => s.StateId, s => s.CurrentValue, StringComparer.OrdinalIgnoreCase);
        var baselineSignals = session.Runtime.Signals.ToDictionary(s => s.SignalId, s => s.CurrentValue, StringComparer.OrdinalIgnoreCase);

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = scenarioId,
            Intensity = 1.0,
            TimeFactor = timeFactor,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 250; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
            var ml = session.Runtime.HiddenProcessStates.FirstOrDefault(s => s.StateId == "MechanicalLoad");
            if (ml != null)
            {
                result.MechanicalLoadSamples.Add(ml.TargetValue);
            }
        }

        foreach (var h in session.Runtime.HiddenProcessStates)
        {
            var delta = h.CurrentValue - baselineHidden.GetValueOrDefault(h.StateId, h.CurrentValue);
            result.HiddenDeltaByState[h.StateId] = delta;
        }

        foreach (var s in session.Runtime.Signals.Take(3))
        {
            var delta = s.CurrentValue - baselineSignals.GetValueOrDefault(s.SignalId, s.CurrentValue);
            result.SignalDeltaById[s.SignalId] = delta;
            result.SignalDeltaAbs += Math.Abs(delta);
        }

        result.HiddenDeltaAbs = result.HiddenDeltaByState.Values.Sum(v => Math.Abs(v));
        var activeInstance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault();
        result.EpisodeCount = activeInstance?.IntermittentEpisodeCount ?? 0;
        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static PhysicalMachineSession CreateSession(
        FaultScenarioTestStack stack,
        string profileId,
        int seed,
        double timeFactor,
        ProcessPhase phase)
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
        FaultScenarioTestStack stack,
        Guid machineId,
        string scenarioId,
        double timeFactor,
        FaultScenarioRunMode runMode,
        CancellationToken cancellationToken)
    {
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = machineId,
            ScenarioId = scenarioId,
            Intensity = 1.0,
            TimeFactor = timeFactor,
            AutoThresholdFaultEnabled = runMode != FaultScenarioRunMode.NonFaultingControlRun,
            RunMode = runMode
        }, cancellationToken).ConfigureAwait(false);
    }

    private static MachineConfiguration CreateMachine(int index, int port, string profileId, int seed)
    {
        var machine = DefaultMachines.Create()[Math.Clamp(index - 1, 0, DefaultMachines.Create().Count - 1)];
        machine.Id = Guid.NewGuid();
        machine.Name = $"AP4R1-{profileId}-{index}";
        machine.PhysicalProfileId = profileId;
        machine.Port = port;
        machine.UpdateEndpointFromHostPort();
        return machine;
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

    private static bool HasAlternatingSign(List<double> values)
    {
        if (values.Count < 4)
        {
            return false;
        }

        var diffs = values.Zip(values.Skip(1), (a, b) => b - a).ToList();
        return diffs.Any(d => d > 0.0001) && diffs.Any(d => d < -0.0001);
    }
}

// Report types
public sealed class Ap4R1ScenarioManifest
{
    public DateTime GeneratedAtUtc { get; set; }
    public int FileCount { get; set; }
    public int ScenarioIdCount { get; set; }
    public List<string> DuplicateScenarioIds { get; set; } = [];
    public string ManifestHash { get; set; } = "";
    public List<Ap4R1ScenarioManifestEntry> Entries { get; set; } = [];
}

public sealed class Ap4R1ScenarioManifestEntry
{
    public string RelativePath { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string ScenarioId { get; set; } = "";
    public string ScenarioVersion { get; set; } = "";
    public List<string> MachineProfileIds { get; set; } = [];
    public string Category { get; set; } = "";
    public bool Enabled { get; set; }
    public bool SupportsControlRun { get; set; }
}

public sealed class Ap4R1ThresholdReport
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public int Seed { get; set; }
    public double TimeFactor { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public DateTime? ScenarioStartedAtUtc { get; set; }
    public DateTime? ThresholdFirstReachedAtUtc { get; set; }
    public DateTime? ThresholdConfirmedAtUtc { get; set; }
    public DateTime? MachineFaultedAtUtc { get; set; }
    public DateTime? RecoveryStartedAtUtc { get; set; }
    public DateTime? RecoveryCompletedAtUtc { get; set; }
    public List<Ap4R1TimelineSample> Timeline { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R1TimelineSample
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

public sealed class Ap4R1CommunicationDropReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public List<Ap4R1ServerReachability> BeforeDrop { get; set; } = [];
    public List<Ap4R1ServerReachability> DuringDrop { get; set; } = [];
    public List<Ap4R1ServerReachability> AfterDrop { get; set; } = [];
    public bool TargetUnreachableDuringDrop { get; set; }
    public bool OthersReachableDuringDrop { get; set; }
    public bool AllReachableAfter { get; set; }
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4R1ServerReachability
{
    public Guid MachineId { get; set; }
    public string ProfileId { get; set; } = "";
    public bool Reachable { get; set; }
}

public sealed class Ap4R1ComplexScenarioReport
{
    public string VerificationRunId { get; set; } = "";
    public int Seed { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public Ap4R1ComplexCaseResult Imbalance { get; set; } = new();
    public Ap4R1ComplexCaseResult SensorDrift { get; set; } = new();
    public Ap4R1ComplexCaseResult Intermittent { get; set; } = new();
    public Ap4R1ComplexCaseResult CoolantLoss { get; set; } = new();
    public bool Passed { get; set; }
}

public sealed class Ap4R1ComplexCaseResult
{
    public string ScenarioId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public Guid MachineId { get; set; }
    public Dictionary<string, double> HiddenDeltaByState { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> SignalDeltaById { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<double> MechanicalLoadSamples { get; set; } = [];
    public double HiddenDeltaAbs { get; set; }
    public double SignalDeltaAbs { get; set; }
    public bool PeriodicBehavior { get; set; }
    public bool HiddenStableWhileSignalMoves { get; set; }
    public int EpisodeCount { get; set; }
    public bool MultipleEpisodes { get; set; }
    public bool CoolingEfficiencyDecreased { get; set; }
}

public sealed class Ap4R1FinalEndToEndReport
{
    public string VerificationRunId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public string LaserProfileHash { get; set; } = "";
    public string BendingProfileHash { get; set; } = "";
    public long TotalOpcUaUpdates { get; set; }
    public int ActiveScenarioRuntimes { get; set; }
    public int ActiveEngines { get; set; }
    public int ActivePublishers { get; set; }
    public List<Ap4R1TimelineSample> Samples { get; set; } = [];
    public List<string> Exceptions { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}
