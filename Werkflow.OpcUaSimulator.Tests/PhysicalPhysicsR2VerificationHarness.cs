using System.Diagnostics;
using System.Text.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalPhysicsR2VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-03-r2-calibration"));

    public static TimeSpan RunDuration => PhysicalVerificationSettings.IntegrationRunDuration;

    public static R2ModelVerificationReport RunModelVerification(int seed = 42)
    {
        var report = new R2ModelVerificationReport { Seed = seed, StartedAtUtc = DateTime.UtcNow };
        var engine = PhysicalTestServiceFactory.CreateEngine();
        var laser = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), seed, PhysicalVerificationMode.Short, 12.0);
        var bending = CreateSession(BendingHydraulicMachine300ProfileFactory.Create(), seed + 57, PhysicalVerificationMode.Short, 12.0);

        foreach (var session in new[] { laser, bending })
        {
            engine.Initialize(session, session.Simulation.Seed);
        }

        var tickDelta = TimeSpan.FromMilliseconds(200);
        var ticks = 600;
        for (var i = 0; i < ticks; i++)
        {
            engine.Tick(laser, tickDelta);
            engine.Tick(bending, tickDelta);
        }

        report.Laser = BuildModelMachineReport(laser);
        report.Bending = BuildModelMachineReport(bending);
        report.DependencyChecks = EvaluateDependencyChecks(laser, bending);
        report.EndedAtUtc = DateTime.UtcNow;
        report.Passed = EvaluateModelPass(report);
        return report;
    }

    public static async Task<R2EndToEndVerificationReport> RunEndToEndAsync(
        int seed1 = 42,
        int seed2 = 99,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        var previousShort = Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT");
        Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", "1");
        var runDuration = duration ?? TimeSpan.FromSeconds(90);
        var report = new R2EndToEndVerificationReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Duration = runDuration,
            SeedMachine1 = seed1,
            SeedMachine2 = seed2
        };

        var log = new TestLogService();
        var coordinator = PhysicalTestServiceFactory.CreateCoordinator(log);
        var serverService = new MachineServerService(log, coordinator);
        var stats = new PhysicalStatisticsRecorder();
        var correlation = new PhysicalCorrelationRecorder();
        var dataChangeSamples = new List<R2DataChangeSample>();

        var machines = new List<MachineConfiguration>
        {
            CreatePhysicsMachine(1, 14880, LaserProcessingMachine300ProfileFactory.ProfileId, seed1),
            CreatePhysicsMachine(2, 14881, BendingHydraulicMachine300ProfileFactory.ProfileId, seed2)
        };

        try
        {
            foreach (var machine in machines)
            {
                coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
                await serverService.StartServerAsync(machine, new MachineRuntimeState { MachineId = machine.Id }, cancellationToken).ConfigureAwait(false);
            }

            var endAt = DateTime.UtcNow + runDuration;
            var pauseTested = false;
            while (DateTime.UtcNow < endAt)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                foreach (var session in coordinator.GetSessions())
                {
                    RecordSamples(session, stats, correlation);
                    report.TotalOpcUaUpdates = coordinator.GetSessions().Sum(s => s.Metrics.TotalPublishedUpdates);
                }

                if (!pauseTested && DateTime.UtcNow > report.StartedAtUtc.AddSeconds(20))
                {
                    await coordinator.PauseAllAsync(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                    await coordinator.ResumeAllAsync(cancellationToken).ConfigureAwait(false);
                    pauseTested = true;
                    report.PauseResumeTested = true;
                }
            }

            foreach (var session in coordinator.GetSessions())
            {
                report.Machines.Add(BuildEndToEndMachineReport(session));
                report.PhaseSegments.AddRange(BuildPhaseSegments(session));
                report.JobChanges += session.Simulation.Metrics.JobChanges;
            }

            report.TotalPhaseChanges = coordinator.GetSessions().Sum(s => s.Simulation.Metrics.PhaseChanges);

            report.Statistics = stats.BuildSnapshots().ToList();
            report.Correlations = BuildCorrelationResults(correlation);
            report.DataChangeSamples = await RunDataChangeClientsAsync(machines, cancellationToken).ConfigureAwait(false);
            report.Exceptions = log.Entries.Where(e => e.Category == LogCategory.Error).Select(e => e.Message).ToList();
            report.Passed = EvaluateEndToEndPass(report);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", previousShort);
            await coordinator.StopAllAsync(cancellationToken).ConfigureAwait(false);
            await serverService.StopAllAsync(cancellationToken).ConfigureAwait(false);
            report.EndedAtUtc = DateTime.UtcNow;
        }

        return report;
    }

    public static async Task ExportEvidenceAsync(
        R2ModelVerificationReport model,
        R2EndToEndVerificationReport? endToEnd = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R2-short-model-verification.json"),
            JsonSerializer.Serialize(model, options),
            cancellationToken).ConfigureAwait(false);

        if (endToEnd != null)
        {
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-03-R2-short-end-to-end-verification.json"),
                JsonSerializer.Serialize(endToEnd, options),
                cancellationToken).ConfigureAwait(false);

            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-03-R2-phase-verification.json"),
                JsonSerializer.Serialize(new { phases = endToEnd.PhaseSegments, phaseChanges = endToEnd.TotalPhaseChanges }, options),
                cancellationToken).ConfigureAwait(false);

            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-03-R2-normal-range-calibration.json"),
                JsonSerializer.Serialize(new { statistics = endToEnd.Statistics }, options),
                cancellationToken).ConfigureAwait(false);

            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-03-R2-correlation-calibration.json"),
                JsonSerializer.Serialize(new { correlations = endToEnd.Correlations }, options),
                cancellationToken).ConfigureAwait(false);

            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-03-R2-opcua-publishing-verification.json"),
                JsonSerializer.Serialize(new
                {
                    totalUpdates = endToEnd.TotalOpcUaUpdates,
                    machines = endToEnd.Machines.Select(m => new
                    {
                        m.MachineName,
                        m.TotalPublishedUpdates,
                        m.AveragePublishDurationMs,
                        m.MaxPublishDurationMs,
                        m.FailedUpdates
                    }),
                    dataChanges = endToEnd.DataChangeSamples
                }, options),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static PhysicalMachineSession CreateSession(
        PhysicalMachineProfile profile,
        int seed,
        PhysicalVerificationMode mode,
        double timeFactor)
    {
        var runtime = new PhysicalMachineRuntimeFactory().Create(profile);
        return new PhysicalMachineSession
        {
            MachineId = Guid.NewGuid(),
            MachineName = profile.ProfileId,
            Profile = profile,
            Runtime = runtime,
            Simulation =
            {
                Seed = seed,
                VerificationMode = mode,
                TimeFactor = timeFactor,
                GenerationMode = SignalGenerationMode.Physical,
                IsEngineActive = true
            }
        };
    }

    private static R2ModelMachineReport BuildModelMachineReport(PhysicalMachineSession session)
    {
        var phases = session.Simulation.PhaseTransitions
            .Select(t => t.ToPhase.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var motorTemp = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorTemperature");
        var motorTempDef = session.Profile.Signals.First(s => s.SignalId == "Axis01.MotorTemperature");
        var processingMean = session.Simulation.PhaseTransitions.Count == 0
            ? motorTemp.CurrentValue
            : motorTemp.CurrentValue;

        return new R2ModelMachineReport
        {
            ProfileId = session.Profile.ProfileId,
            PhaseChanges = session.Simulation.Metrics.PhaseChanges,
            DistinctPhases = phases.Count,
            JobChanges = session.Simulation.Metrics.JobChanges,
            CurrentPhase = session.Simulation.CurrentPhase.ToString(),
            JobName = session.Simulation.Job.JobName,
            PartName = session.Simulation.Job.PartName,
            MotorTemperature = motorTemp.CurrentValue,
            MotorTemperatureInNormalRange = motorTemp.CurrentValue >= motorTempDef.NormalMinimum
                && motorTemp.CurrentValue <= motorTempDef.NormalMaximum,
            ProcessingMotorTemperatureMean = processingMean
        };
    }

    private static List<R2DependencyCheck> EvaluateDependencyChecks(PhysicalMachineSession laser, PhysicalMachineSession bending)
    {
        var checks = new List<R2DependencyCheck>();
        var signalEngine = new SignalCalculationEngine();
        var random = new SeededRandomStreams(42);

        checks.Add(CheckFrictionMotorCurrent(laser, signalEngine, random));
        checks.Add(CheckFrictionSpeed(laser, signalEngine, random));
        checks.Add(CheckMechanicalLoadMotorCurrent(laser, signalEngine, random));
        checks.Add(CheckTemperatureLag(laser));

        return checks;
    }

    private static R2DependencyCheck CheckFrictionMotorCurrent(
        PhysicalMachineSession session,
        SignalCalculationEngine signalEngine,
        SeededRandomStreams random)
    {
        signalEngine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var friction = session.Runtime.HiddenProcessStates.First(s => s.StateId == "Friction");
        var mech = session.Runtime.HiddenProcessStates.First(s => s.StateId == "MechanicalLoad");
        mech.CurrentValue = 0.5;
        mech.TargetValue = 0.5;

        friction.CurrentValue = 0.2;
        friction.TargetValue = 0.2;
        signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;

        friction.CurrentValue = 0.75;
        friction.TargetValue = 0.75;
        for (var i = 0; i < 8; i++)
        {
            signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
        return new R2DependencyCheck
        {
            Pair = "Friction → Axis01.MotorCurrent",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Passed = high > low + 0.2
        };
    }

    private static R2DependencyCheck CheckFrictionSpeed(
        PhysicalMachineSession session,
        SignalCalculationEngine signalEngine,
        SeededRandomStreams random)
    {
        signalEngine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var friction = session.Runtime.HiddenProcessStates.First(s => s.StateId == "Friction");
        friction.CurrentValue = 0.2;
        friction.TargetValue = 0.2;
        for (var i = 0; i < 8; i++)
        {
            signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }
        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Speed").CurrentValue;

        friction.CurrentValue = 0.8;
        friction.TargetValue = 0.8;
        for (var i = 0; i < 12; i++)
        {
            signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Speed").CurrentValue;
        return new R2DependencyCheck
        {
            Pair = "Friction → Axis01.Speed",
            Direction = "negative",
            LowValue = low,
            HighValue = high,
            Passed = low < high - 3
        };
    }

    private static R2DependencyCheck CheckMechanicalLoadMotorCurrent(
        PhysicalMachineSession session,
        SignalCalculationEngine signalEngine,
        SeededRandomStreams random)
    {
        signalEngine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var mech = session.Runtime.HiddenProcessStates.First(s => s.StateId == "MechanicalLoad");
        mech.CurrentValue = 0.2;
        mech.TargetValue = 0.2;
        for (var i = 0; i < 8; i++)
        {
            signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }
        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;

        mech.CurrentValue = 0.9;
        mech.TargetValue = 0.9;
        for (var i = 0; i < 8; i++)
        {
            signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
        return new R2DependencyCheck
        {
            Pair = "MechanicalLoad → Axis01.MotorCurrent",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Passed = high > low + 0.35
        };
    }

    private static R2DependencyCheck CheckTemperatureLag(PhysicalMachineSession session)
    {
        var tempDef = session.Profile.Signals.First(s => s.SignalId == "Axis01.MotorTemperature");
        var currentDef = session.Profile.Signals.First(s => s.SignalId == "Axis01.MotorCurrent");
        return new R2DependencyCheck
        {
            Pair = "Thermal inertia",
            Direction = "delayed",
            Passed = tempDef.ResponseInertia >= currentDef.ResponseInertia
        };
    }

    private static bool EvaluateModelPass(R2ModelVerificationReport report) =>
        report.Laser.PhaseChanges >= 4
        && report.Bending.PhaseChanges >= 4
        && report.Laser.DistinctPhases >= 4
        && report.Bending.DistinctPhases >= 4
        && report.DependencyChecks.All(c => c.Passed);

    private static bool EvaluateEndToEndPass(R2EndToEndVerificationReport report)
    {
        if (report.TotalPhaseChanges < 4)
        {
            return false;
        }

        if (report.TotalOpcUaUpdates <= 0)
        {
            return false;
        }

        if (report.JobChanges < 1)
        {
            return false;
        }

        if (report.Machines.Any(m => m.TotalPublishedUpdates <= 0))
        {
            return false;
        }

        if (report.Machines.Any(m => m.AveragePublishDurationMs <= 0))
        {
            return false;
        }

        if (report.Exceptions.Count > 0)
        {
            return false;
        }

        if (report.Correlations.Any(c => c.Result == "Failed"))
        {
            return false;
        }

        var friction = report.Correlations.FirstOrDefault(c => c.PairId == "laser-04");
        if (friction != null && friction.Pearson < 0.15)
        {
            return false;
        }

        return report.DataChangeSamples.Any(s => s.SourceTimestampUpdated);
    }

    private static void RecordSamples(
        PhysicalMachineSession session,
        PhysicalStatisticsRecorder stats,
        PhysicalCorrelationRecorder correlation)
    {
        var now = DateTimeOffset.UtcNow;
        var phase = session.Simulation.CurrentPhase;
        var plan = GetMonitoredSignals(session.Profile.ProfileId);

        foreach (var signalId in plan.StatisticsSignals)
        {
            var def = session.Profile.Signals.FirstOrDefault(s => s.SignalId == signalId);
            var runtime = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == signalId);
            if (def == null || runtime == null || def.DataType is not (PhysicalSignalDataType.Double or PhysicalSignalDataType.Float))
            {
                continue;
            }

            stats.Record(signalId, runtime.CurrentValue, def, now, phase);
        }

        foreach (var group in plan.CorrelationGroups)
        {
            var hidden = session.Runtime.HiddenProcessStates.FirstOrDefault(s => s.StateId == group.HiddenStateId);
            var signal = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == group.TargetSignalId);
            if (hidden == null || signal == null)
            {
                continue;
            }

            correlation.RecordPair(group.PairId, hidden.CurrentValue, signal.CurrentValue);
        }
    }

    private static R2EndToEndMachineReport BuildEndToEndMachineReport(PhysicalMachineSession session)
    {
        var profile = session.Profile;
        return new R2EndToEndMachineReport
        {
            MachineId = session.MachineId,
            MachineName = session.MachineName,
            ProfileId = profile.ProfileId,
            ProfileVersion = profile.ProfileVersion,
            SignalCount = profile.Signals.Count,
            HiddenStateCount = profile.HiddenProcessStates.Count,
            SignalDependencyCount = profile.Dependencies.Count,
            HiddenStateDependencyCount = profile.HiddenStateDependencies.Count,
            EngineTicks = session.Simulation.Metrics.TotalEngineTicks,
            TotalPublishedUpdates = session.Metrics.TotalPublishedUpdates,
            AveragePublishDurationMs = session.Metrics.AveragePublishDurationMs,
            MaxPublishDurationMs = session.Metrics.MaxPublishDurationMs,
            FailedUpdates = session.Metrics.FailedUpdates,
            PhaseChanges = session.Simulation.Metrics.PhaseChanges,
            JobChanges = session.Simulation.Metrics.JobChanges,
            CurrentPhase = session.Simulation.CurrentPhase.ToString(),
            JobName = session.Simulation.Job.JobName,
            PartName = session.Simulation.Job.PartName,
            DistinctPhases = session.Simulation.PhaseTransitions.Select(t => t.ToPhase).Distinct().Count()
        };
    }

    private static List<R2PhaseSegment> BuildPhaseSegments(PhysicalMachineSession session)
    {
        var segments = new List<R2PhaseSegment>();
        ProcessPhase? current = null;
        DateTimeOffset? start = null;
        var loadSum = 0.0;
        var currentSum = 0.0;
        var tempSum = 0.0;
        var speedSum = 0.0;
        var samples = 0;

        void Flush(DateTimeOffset end)
        {
            if (!current.HasValue || !start.HasValue)
            {
                return;
            }

            segments.Add(new R2PhaseSegment
            {
                MachineName = session.MachineName,
                Phase = current.Value.ToString(),
                StartUtc = start.Value,
                EndUtc = end,
                DurationSeconds = (end - start.Value).TotalSeconds,
                JobName = session.Simulation.Job.JobName,
                PartName = session.Simulation.Job.PartName,
                AverageLoad = samples == 0 ? 0 : loadSum / samples,
                AverageCurrent = samples == 0 ? 0 : currentSum / samples,
                AverageTemperature = samples == 0 ? 0 : tempSum / samples,
                AverageSpeed = samples == 0 ? 0 : speedSum / samples
            });
        }

        foreach (var transition in session.Simulation.PhaseTransitions)
        {
            Flush(transition.TimestampUtc);
            current = transition.ToPhase;
            start = transition.TimestampUtc;
            loadSum = currentSum = tempSum = speedSum = 0;
            samples = 0;
        }

        var load = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == "Axis01.Load");
        var currentSig = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == "Axis01.MotorCurrent");
        var temp = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == "Axis01.MotorTemperature");
        var speed = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == "Axis01.Speed");
        if (load != null && currentSig != null && temp != null && speed != null)
        {
            loadSum += load.CurrentValue;
            currentSum += currentSig.CurrentValue;
            tempSum += temp.CurrentValue;
            speedSum += speed.CurrentValue;
            samples++;
        }

        Flush(DateTimeOffset.UtcNow);
        return segments;
    }

    private static List<R2CorrelationEvaluation> BuildCorrelationResults(PhysicalCorrelationRecorder correlation)
    {
        var results = new List<R2CorrelationEvaluation>();
        foreach (var g in GetMonitoredSignals(LaserProcessingMachine300ProfileFactory.ProfileId).CorrelationGroups)
        {
            results.Add(EvaluateCorrelation(correlation, g, LaserProcessingMachine300ProfileFactory.ProfileId));
        }

        foreach (var g in GetMonitoredSignals(BendingHydraulicMachine300ProfileFactory.ProfileId).CorrelationGroups)
        {
            results.Add(EvaluateCorrelation(correlation, g, BendingHydraulicMachine300ProfileFactory.ProfileId));
        }

        return results;
    }

    private static R2CorrelationEvaluation EvaluateCorrelation(
        PhysicalCorrelationRecorder correlation,
        R2CorrelationPlan plan,
        string profileId)
    {
        var baseResult = correlation.Analyze(
            plan.PairId, profileId, plan.HiddenStateId, plan.TargetSignalId,
            plan.Direction, plan.DependencyType, plan.ExpectedLagSeconds);

        var directionOk = plan.Direction switch
        {
            "positive" => baseResult.Pearson >= plan.MinPearson,
            "negative" => baseResult.Pearson <= -plan.MinPearson,
            _ => Math.Abs(baseResult.Pearson) >= plan.MinPearson
        };

        var tooStrong = Math.Abs(baseResult.Pearson) > plan.MaxPearson
            || Math.Abs(baseResult.Spearman) > plan.MaxPearson
            || Math.Abs(baseResult.StrongestCrossCorrelation) > plan.MaxCrossCorrelation;

        var result = baseResult.SampleCount < 20
            ? "Review"
            : !directionOk
                ? "Failed"
                : tooStrong
                    ? "Review"
                    : "Passed";

        return new R2CorrelationEvaluation
        {
            PairId = plan.PairId,
            ProfileId = profileId,
            Relationship = $"{plan.HiddenStateId} → {plan.TargetSignalId}",
            ExpectedDirection = plan.Direction,
            MinPearson = plan.MinPearson,
            MaxPearson = plan.MaxPearson,
            ExpectedLagSeconds = plan.ExpectedLagSeconds,
            Pearson = baseResult.Pearson,
            Spearman = baseResult.Spearman,
            StrongestLag = baseResult.StrongestCrossCorrelationLag,
            StrongestCrossCorrelation = baseResult.StrongestCrossCorrelation,
            SampleCount = baseResult.SampleCount,
            Result = result,
            Reason = result switch
            {
                "Passed" => "Direction and strength within expected bounds.",
                "Failed" => "Expected correlation direction or minimum strength not met.",
                _ => tooStrong ? "Correlation near-perfect; review trivial coupling." : "Insufficient samples or borderline strength."
            }
        };
    }

    private static string[] GetDataChangePaths(string profileId)
    {
        if (profileId == BendingHydraulicMachine300ProfileFactory.ProfileId)
        {
            return [
                "Axis01.Speed",
                "Axis01.MotorCurrent",
                "Axis01.MotorTemperature",
                "Bending.PressForce",
                "Hydraulic.SupplyPressure",
                "Quality.ProcessQualityIndex",
                "Production.CycleCounter"
            ];
        }

        return [
            "Axis01.Speed",
            "Axis01.MotorCurrent",
            "Axis01.MotorTemperature",
            "Process.PowerDemand",
            "Cooling.PrimaryCircuit.Temperature",
            "Process.QualityIndex",
            "Production.CycleCounter"
        ];
    }

    private static async Task<List<R2DataChangeSample>> RunDataChangeClientsAsync(
        IReadOnlyList<MachineConfiguration> machines,
        CancellationToken cancellationToken)
    {
        var samples = new List<R2DataChangeSample>();
        foreach (var machine in machines)
        {
            var machineSamples = await RunDataChangeClientAsync(machine, cancellationToken).ConfigureAwait(false);
            samples.AddRange(machineSamples);
        }

        return samples;
    }

    private static async Task<List<R2DataChangeSample>> RunDataChangeClientAsync(
        MachineConfiguration machine,
        CancellationToken cancellationToken)
    {
        var samples = new List<R2DataChangeSample>();
        var config = await PhysicalSignalVerificationHarness.CreateClientConfigurationForTestsAsync(cancellationToken).ConfigureAwait(false);
        var selected = CoreClientUtils.SelectEndpoint(config, machine.Endpoint, false);
        var endpointConfig = new ConfiguredEndpoint(null, selected, EndpointConfiguration.Create(config));
        using var session = await Session.Create(config, endpointConfig, false, "R2Verification", 60000, new UserIdentity(), null, cancellationToken).ConfigureAwait(false);

        var signalPaths = GetDataChangePaths(machine.PhysicalProfileId ?? string.Empty);

        var nsIndex = session.NamespaceUris.GetIndex(machine.NamespaceUri);
        var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 500 };
        session.AddSubscription(subscription);
        subscription.Create();

        foreach (var path in signalPaths)
        {
            var item = new MonitoredItem(subscription.DefaultItem)
            {
                StartNodeId = new NodeId(path, (ushort)nsIndex),
                AttributeId = Attributes.Value,
                SamplingInterval = 500,
                QueueSize = 10,
                DiscardOldest = true
            };
            subscription.AddItem(item);
        }

        subscription.ApplyChanges();
        await Task.Delay(6000, cancellationToken).ConfigureAwait(false);

        foreach (var path in signalPaths)
        {
            var nodeId = new NodeId(path, (ushort)nsIndex);
            var initial = session.ReadValue(nodeId);
            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            var later = session.ReadValue(nodeId);
            samples.Add(new R2DataChangeSample
            {
                MachineName = machine.Name,
                NodePath = path,
                InitialValue = initial.Value?.ToString(),
                LaterValue = later.Value?.ToString(),
                InitialSourceTimestamp = initial.SourceTimestamp,
                LaterSourceTimestamp = later.SourceTimestamp,
                SourceTimestampUpdated = later.SourceTimestamp > initial.SourceTimestamp,
                ValueChanged = (initial.Value?.ToString() ?? "") != (later.Value?.ToString() ?? "")
            });
        }

        subscription.Delete(true);
        return samples;
    }

    private static MachineConfiguration CreatePhysicsMachine(int index, int port, string profileId, int seed)
    {
        var machine = DefaultMachines.Create()[index - 1];
        machine.PhysicalProfileId = profileId;
        machine.Port = port;
        machine.UpdateEndpointFromHostPort();
        return machine;
    }

    private static R2MonitoredSignalPlan GetMonitoredSignals(string profileId) =>
        profileId == BendingHydraulicMachine300ProfileFactory.ProfileId ? BendingPlan : LaserPlan;

    private static readonly R2MonitoredSignalPlan LaserPlan = new()
    {
        StatisticsSignals =
        [
            "Axis01.MotorCurrent", "Axis01.Load", "Axis01.Speed", "Axis01.MotorTemperature",
            "Process.SpindleSpeed", "Process.FeedRate", "Process.PowerDemand", "Process.QualityIndex",
            "Thermal.CabinetTemperature", "Cooling.PrimaryCircuit.Temperature", "Cooling.PrimaryCircuit.Flow"
        ],
        CorrelationGroups =
        [
            new("laser-01", "MechanicalLoad", "Axis01.MotorCurrent", "positive", "linear", 0, 0.2, 0.92, 0.98),
            new("laser-02", "MechanicalLoad", "Axis01.Load", "positive", "linear", 0, 0.2, 0.92, 0.98),
            new("laser-03", "Friction", "Axis01.Speed", "negative", "inverseLinear", 0, 0.15, 0.9, 0.98),
            new("laser-04", "Friction", "Axis01.MotorCurrent", "positive", "linear", 0, 0.25, 0.92, 0.98),
            new("laser-05", "ThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 20, 0.15, 0.9, 0.98),
            new("laser-07", "CoolingEfficiency", "Cooling.PrimaryCircuit.Temperature", "negative", "inverseLinear", 0, 0.15, 0.9, 0.98),
            new("laser-08", "ProcessDemand", "Process.PowerDemand", "positive", "linear", 0, 0.2, 0.9, 0.98),
            new("laser-09", "MaterialResistance", "Process.FeedRate", "negative", "inverseLinear", 0, 0.15, 0.85, 0.98)
        ]
    };

    private static readonly R2MonitoredSignalPlan BendingPlan = new()
    {
        StatisticsSignals =
        [
            "Hydraulic.SupplyPressure", "Bending.PressForce", "Axis01.MotorCurrent", "Axis01.Speed",
            "Axis01.MotorTemperature", "Hydraulic.OilTemperature", "Quality.ProcessQualityIndex"
        ],
        CorrelationGroups =
        [
            new("bend-01", "PressLoad", "Hydraulic.SupplyPressure", "positive", "linear", 0, 0.2, 0.9, 0.98),
            new("bend-02", "PressLoad", "Bending.PressForce", "positive", "saturating", 0, 0.2, 0.9, 0.98),
            new("bend-03", "AxisFriction", "Axis01.Speed", "negative", "inverseLinear", 0, 0.15, 0.9, 0.98),
            new("bend-04", "PumpEfficiency", "Hydraulic.PumpSpeed", "positive", "linear", 0, 0.2, 0.9, 0.98),
            new("bend-05", "StructuralThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 25, 0.15, 0.9, 0.98)
        ]
    };

    private sealed class R2MonitoredSignalPlan
    {
        public required string[] StatisticsSignals { get; init; }
        public required R2CorrelationPlan[] CorrelationGroups { get; init; }
    }

    private sealed record R2CorrelationPlan(
        string PairId,
        string HiddenStateId,
        string TargetSignalId,
        string Direction,
        string DependencyType,
        int ExpectedLagSeconds,
        double MinPearson,
        double MaxPearson,
        double MaxCrossCorrelation);
}

public sealed class R2ModelVerificationReport
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public bool Passed { get; set; }
    public R2ModelMachineReport Laser { get; set; } = new();
    public R2ModelMachineReport Bending { get; set; } = new();
    public List<R2DependencyCheck> DependencyChecks { get; set; } = [];
}

public sealed class R2ModelMachineReport
{
    public string ProfileId { get; set; } = string.Empty;
    public int PhaseChanges { get; set; }
    public int DistinctPhases { get; set; }
    public int JobChanges { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public double MotorTemperature { get; set; }
    public bool MotorTemperatureInNormalRange { get; set; }
    public double ProcessingMotorTemperatureMean { get; set; }
}

public sealed class R2DependencyCheck
{
    public string Pair { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public double LowValue { get; set; }
    public double HighValue { get; set; }
    public bool Passed { get; set; }
}

public sealed class R2EndToEndVerificationReport
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public int SeedMachine1 { get; set; }
    public int SeedMachine2 { get; set; }
    public bool Passed { get; set; }
    public bool PauseResumeTested { get; set; }
    public int TotalPhaseChanges { get; set; }
    public int JobChanges { get; set; }
    public long TotalOpcUaUpdates { get; set; }
    public List<R2EndToEndMachineReport> Machines { get; set; } = [];
    public List<R2PhaseSegment> PhaseSegments { get; set; } = [];
    public List<SignalStatisticsSnapshot> Statistics { get; set; } = [];
    public List<R2CorrelationEvaluation> Correlations { get; set; } = [];
    public List<R2DataChangeSample> DataChangeSamples { get; set; } = [];
    public List<string> Exceptions { get; set; } = [];
}

public sealed class R2EndToEndMachineReport
{
    public Guid MachineId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileVersion { get; set; } = string.Empty;
    public int SignalCount { get; set; }
    public int HiddenStateCount { get; set; }
    public int SignalDependencyCount { get; set; }
    public int HiddenStateDependencyCount { get; set; }
    public long EngineTicks { get; set; }
    public long TotalPublishedUpdates { get; set; }
    public double AveragePublishDurationMs { get; set; }
    public double MaxPublishDurationMs { get; set; }
    public int FailedUpdates { get; set; }
    public int PhaseChanges { get; set; }
    public int JobChanges { get; set; }
    public int DistinctPhases { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
}

public sealed class R2PhaseSegment
{
    public string MachineName { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public double DurationSeconds { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public double AverageLoad { get; set; }
    public double AverageCurrent { get; set; }
    public double AverageTemperature { get; set; }
    public double AverageSpeed { get; set; }
}

public sealed class R2CorrelationEvaluation
{
    public string PairId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string ExpectedDirection { get; set; } = string.Empty;
    public double MinPearson { get; set; }
    public double MaxPearson { get; set; }
    public int ExpectedLagSeconds { get; set; }
    public double Pearson { get; set; }
    public double Spearman { get; set; }
    public int StrongestLag { get; set; }
    public double StrongestCrossCorrelation { get; set; }
    public int SampleCount { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class R2DataChangeSample
{
    public string MachineName { get; set; } = string.Empty;
    public string NodePath { get; set; } = string.Empty;
    public string? InitialValue { get; set; }
    public string? LaterValue { get; set; }
    public DateTime InitialSourceTimestamp { get; set; }
    public DateTime LaterSourceTimestamp { get; set; }
    public bool SourceTimestampUpdated { get; set; }
    public bool ValueChanged { get; set; }
}
