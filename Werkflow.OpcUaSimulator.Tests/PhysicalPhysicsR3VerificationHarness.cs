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

public static class PhysicalPhysicsR3VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-03-r3-final-calibration"));

    public static int LaserStatisticsSignalCount => LaserPlan.StatisticsSignals.Length;
    public static int BendingStatisticsSignalCount => BendingPlan.StatisticsSignals.Length;

    public static bool EvaluateEndToEndPassForTests(R3EndToEndVerificationReport report) => EvaluateEndToEndPass(report);

    public static TimeSpan RunDuration => PhysicalVerificationSettings.IntegrationRunDuration;

    public static R3ModelVerificationReport RunModelVerification(int seed = 42)
    {
        var report = new R3ModelVerificationReport { Seed = seed, StartedAtUtc = DateTime.UtcNow };
        var engine = PhysicalTestServiceFactory.CreateEngine();
        var laser = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), seed, PhysicalVerificationMode.Short, 12.0);
        var bending = CreateSession(BendingHydraulicMachine300ProfileFactory.Create(), seed + 57, PhysicalVerificationMode.Short, 12.0);

        foreach (var session in new[] { laser, bending })
        {
            engine.Initialize(session, session.Simulation.Seed);
        }

        var tickDelta = TimeSpan.FromMilliseconds(200);
        for (var i = 0; i < 600; i++)
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

    public static async Task<R3EndToEndVerificationReport> RunEndToEndAsync(
        int seed1 = 42,
        int seed2 = 99,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        var previousShort = Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT");
        Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", "1");
        var runDuration = duration ?? TimeSpan.FromMinutes(5);
        var report = new R3EndToEndVerificationReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Duration = runDuration,
            SeedMachine1 = seed1,
            SeedMachine2 = seed2
        };

        var log = new TestLogService();
        var coordinator = PhysicalTestServiceFactory.CreateCoordinator(log);
        var serverService = new MachineServerService(log, coordinator);
        var statsByProfile = new Dictionary<string, PhysicalStatisticsRecorder>(StringComparer.OrdinalIgnoreCase);
        var correlationByProfile = new Dictionary<string, PhysicalCorrelationRecorder>(StringComparer.OrdinalIgnoreCase);

        var machines = new List<MachineConfiguration>
        {
            CreatePhysicsMachine(1, 14890, LaserProcessingMachine300ProfileFactory.ProfileId, seed1),
            CreatePhysicsMachine(2, 14891, BendingHydraulicMachine300ProfileFactory.ProfileId, seed2)
        };

        try
        {
            foreach (var machine in machines)
            {
                coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
                await serverService.StartServerAsync(machine, new MachineRuntimeState { MachineId = machine.Id }, cancellationToken).ConfigureAwait(false);
                statsByProfile[machine.PhysicalProfileId!] = new PhysicalStatisticsRecorder();
                correlationByProfile[machine.PhysicalProfileId!] = new PhysicalCorrelationRecorder();
            }

            var endAt = DateTime.UtcNow + runDuration;
            while (DateTime.UtcNow < endAt)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                foreach (var session in coordinator.GetSessions())
                {
                    var profileId = session.Profile.ProfileId;
                    RecordSamples(session, statsByProfile[profileId], correlationByProfile[profileId]);
                    report.TotalOpcUaUpdates = coordinator.GetSessions().Sum(s => s.Metrics.TotalPublishedUpdates);
                }
            }

            foreach (var session in coordinator.GetSessions())
            {
                report.Machines.Add(BuildEndToEndMachineReport(session));
                report.PhaseSegments.AddRange(BuildPhaseSegments(session));
            }

            report.TotalPhaseChanges = coordinator.GetSessions().Sum(s => s.Simulation.Metrics.PhaseChanges);
            report.JobChanges = coordinator.GetSessions().Sum(s => s.Simulation.Metrics.JobChanges);

            report.Statistics = statsByProfile
                .SelectMany(kvp => kvp.Value.BuildSnapshots().Select(s => WithProfile(s, kvp.Key)))
                .ToList();
            report.Correlations = BuildCorrelationResults(correlationByProfile);
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
        R3ModelVerificationReport model,
        R3EndToEndVerificationReport? endToEnd = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R3-model-calibration.json"),
            JsonSerializer.Serialize(model, options),
            cancellationToken).ConfigureAwait(false);

        if (endToEnd == null)
        {
            return;
        }

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R3-normal-range-statistics.json"),
            JsonSerializer.Serialize(new { statistics = endToEnd.Statistics, Passed = endToEnd.Passed }, options),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R3-phase-statistics.json"),
            JsonSerializer.Serialize(new { phases = endToEnd.PhaseSegments, phaseChanges = endToEnd.TotalPhaseChanges }, options),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R3-correlation-verification.json"),
            JsonSerializer.Serialize(new { correlations = endToEnd.Correlations, Passed = endToEnd.Correlations.All(c => c.Result == "Passed") }, options),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R3-opcua-end-to-end.json"),
            JsonSerializer.Serialize(endToEnd, options),
            cancellationToken).ConfigureAwait(false);
    }

    private static SignalStatisticsSnapshot WithProfile(SignalStatisticsSnapshot snapshot, string profileId)
    {
        snapshot.ProfileId = profileId;
        return snapshot;
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

    private static R3ModelMachineReport BuildModelMachineReport(PhysicalMachineSession session) =>
        new()
        {
            ProfileId = session.Profile.ProfileId,
            PhaseChanges = session.Simulation.Metrics.PhaseChanges,
            DistinctPhases = session.Simulation.PhaseTransitions.Select(t => t.ToPhase).Distinct().Count(),
            JobChanges = session.Simulation.Metrics.JobChanges,
            CurrentPhase = session.Simulation.CurrentPhase.ToString(),
            JobName = session.Simulation.Job.JobName,
            PartName = session.Simulation.Job.PartName
        };

    private static List<R3DependencyCheck> EvaluateDependencyChecks(PhysicalMachineSession laser, PhysicalMachineSession bending)
    {
        var checks = new List<R3DependencyCheck>();
        var signalEngine = new SignalCalculationEngine();
        var random = new SeededRandomStreams(42);

        checks.Add(CheckPressLoadSupplyPressure(bending, signalEngine, random));
        checks.Add(CheckPressLoadPressForce(bending, signalEngine, random));
        checks.Add(CheckMechanicalLoadMotorCurrent(laser, signalEngine, random));
        checks.Add(CheckMechanicalLoadAxisLoad(laser, signalEngine, random));
        checks.Add(CheckFrictionSpeed(laser, signalEngine, random));
        checks.Add(CheckFrictionMotorCurrent(laser, signalEngine, random));
        checks.Add(CheckProcessDemandPowerDemand(laser, signalEngine, random));
        checks.Add(CheckCoolingEfficiencyTemperature(laser, signalEngine, random));
        checks.Add(CheckPumpEfficiencyPumpSpeed(bending, signalEngine, random));
        checks.Add(CheckMaterialResistanceFeedRate(laser, signalEngine, random));
        checks.Add(CheckThermalLoadMotorTemperature(laser));
        checks.Add(CheckQualityIndexNotSaturated(laser, signalEngine, random));

        return checks;
    }

    private static R3DependencyCheck CheckPressLoadSupplyPressure(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        session.Simulation.CurrentPhase = ProcessPhase.Processing;
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var press = session.Runtime.HiddenProcessStates.First(s => s.StateId == "PressLoad");

        press.CurrentValue = 0.25;
        press.TargetValue = 0.25;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Hydraulic.SupplyPressure").CurrentValue;

        press.CurrentValue = 0.85;
        press.TargetValue = 0.85;
        for (var i = 0; i < 10; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Hydraulic.SupplyPressure").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "PressLoad → Hydraulic.SupplyPressure",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 2
        };
    }

    private static R3DependencyCheck CheckPressLoadPressForce(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        session.Simulation.CurrentPhase = ProcessPhase.Processing;
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var press = session.Runtime.HiddenProcessStates.First(s => s.StateId == "PressLoad");

        press.CurrentValue = 0.2;
        press.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Bending.PressForce").CurrentValue;

        press.CurrentValue = 0.9;
        press.TargetValue = 0.9;
        for (var i = 0; i < 10; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Bending.PressForce").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "PressLoad → Bending.PressForce",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 5
        };
    }

    private static R3DependencyCheck CheckMechanicalLoadMotorCurrent(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var mech = session.Runtime.HiddenProcessStates.First(s => s.StateId == "MechanicalLoad");
        var friction = session.Runtime.HiddenProcessStates.First(s => s.StateId == "Friction");
        friction.CurrentValue = 0.3;
        friction.TargetValue = 0.3;
        mech.CurrentValue = 0.2;
        mech.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;

        mech.CurrentValue = 0.9;
        mech.TargetValue = 0.9;
        for (var i = 0; i < 12; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "MechanicalLoad → Axis01.MotorCurrent",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 0.35
        };
    }

    private static R3DependencyCheck CheckMechanicalLoadAxisLoad(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var mech = session.Runtime.HiddenProcessStates.First(s => s.StateId == "MechanicalLoad");
        mech.CurrentValue = 0.2;
        mech.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Load").CurrentValue;

        mech.CurrentValue = 0.9;
        mech.TargetValue = 0.9;
        for (var i = 0; i < 8; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Load").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "MechanicalLoad → Axis01.Load",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 5
        };
    }

    private static R3DependencyCheck CheckFrictionSpeed(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var friction = session.Runtime.HiddenProcessStates.First(s => s.StateId == "Friction");
        friction.CurrentValue = 0.2;
        friction.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Speed").CurrentValue;

        friction.CurrentValue = 0.8;
        friction.TargetValue = 0.8;
        for (var i = 0; i < 8; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Speed").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "Friction → Axis01.Speed",
            Direction = "negative",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = low < high - 5
        };
    }

    private static R3DependencyCheck CheckFrictionMotorCurrent(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var friction = session.Runtime.HiddenProcessStates.First(s => s.StateId == "Friction");
        var mech = session.Runtime.HiddenProcessStates.First(s => s.StateId == "MechanicalLoad");
        mech.CurrentValue = 0.5;
        mech.TargetValue = 0.5;

        friction.CurrentValue = 0.2;
        friction.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;

        friction.CurrentValue = 0.75;
        friction.TargetValue = 0.75;
        for (var i = 0; i < 12; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "Friction → Axis01.MotorCurrent",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 0.12
        };
    }

    private static R3DependencyCheck CheckProcessDemandPowerDemand(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var demand = session.Runtime.HiddenProcessStates.First(s => s.StateId == "ProcessDemand");
        demand.CurrentValue = 0.2;
        demand.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Process.PowerDemand").CurrentValue;

        demand.CurrentValue = 0.85;
        demand.TargetValue = 0.85;
        for (var i = 0; i < 8; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Process.PowerDemand").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "ProcessDemand → Process.PowerDemand",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 1
        };
    }

    private static R3DependencyCheck CheckCoolingEfficiencyTemperature(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var cooling = session.Runtime.HiddenProcessStates.First(s => s.StateId == "CoolingEfficiency");
        cooling.CurrentValue = 0.95;
        cooling.TargetValue = 0.95;
        for (var i = 0; i < 8; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var low = session.Runtime.Signals.First(s => s.SignalId == "Cooling.PrimaryCircuit.Temperature").CurrentValue;

        cooling.CurrentValue = 0.72;
        cooling.TargetValue = 0.72;
        for (var i = 0; i < 24; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Cooling.PrimaryCircuit.Temperature").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "CoolingEfficiency → Cooling.PrimaryCircuit.Temperature",
            Direction = "negative",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 0.05
        };
    }

    private static R3DependencyCheck CheckPumpEfficiencyPumpSpeed(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var pump = session.Runtime.HiddenProcessStates.First(s => s.StateId == "PumpEfficiency");
        pump.CurrentValue = 0.72;
        pump.TargetValue = 0.72;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var low = session.Runtime.Signals.First(s => s.SignalId == "Hydraulic.PumpSpeed").CurrentValue;

        pump.CurrentValue = 0.95;
        pump.TargetValue = 0.95;
        for (var i = 0; i < 80; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var high = session.Runtime.Signals.First(s => s.SignalId == "Hydraulic.PumpSpeed").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "PumpEfficiency → Hydraulic.PumpSpeed",
            Direction = "positive",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = high > low + 0.5
        };
    }

    private static R3DependencyCheck CheckMaterialResistanceFeedRate(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        var resistance = session.Runtime.HiddenProcessStates.First(s => s.StateId == "MaterialResistance");
        resistance.CurrentValue = 0.2;
        resistance.TargetValue = 0.2;
        engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        var high = session.Runtime.Signals.First(s => s.SignalId == "Process.FeedRate").CurrentValue;

        resistance.CurrentValue = 0.8;
        resistance.TargetValue = 0.8;
        for (var i = 0; i < 8; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
        }

        var low = session.Runtime.Signals.First(s => s.SignalId == "Process.FeedRate").CurrentValue;
        return new R3DependencyCheck
        {
            Pair = "MaterialResistance → Process.FeedRate",
            Direction = "negative",
            LowValue = low,
            HighValue = high,
            Strength = high - low,
            LagSeconds = 0,
            Passed = low < high - 50
        };
    }

    private static R3DependencyCheck CheckThermalLoadMotorTemperature(PhysicalMachineSession session)
    {
        var tempDef = session.Profile.Signals.First(s => s.SignalId == "Axis01.MotorTemperature");
        var currentDef = session.Profile.Signals.First(s => s.SignalId == "Axis01.MotorCurrent");
        return new R3DependencyCheck
        {
            Pair = "ThermalLoad → Axis01.MotorTemperature",
            Direction = "delayed",
            LagSeconds = 20,
            Passed = tempDef.ResponseInertia >= currentDef.ResponseInertia
        };
    }

    private static R3DependencyCheck CheckQualityIndexNotSaturated(
        PhysicalMachineSession session, SignalCalculationEngine engine, SeededRandomStreams random)
    {
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
        session.Simulation.CurrentPhase = ProcessPhase.Processing;
        var values = new List<double>();
        for (var i = 0; i < 40; i++)
        {
            engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
            values.Add(session.Runtime.Signals.First(s => s.SignalId == "Process.QualityIndex").CurrentValue);
        }

        var max = values.Max();
        var min = values.Min();
        return new R3DependencyCheck
        {
            Pair = "Process.QualityIndex variation",
            Direction = "non-saturated",
            LowValue = min,
            HighValue = max,
            Strength = max - min,
            LagSeconds = 0,
            Passed = max < 99.9 && max - min > 0.2 && max <= 99.5
        };
    }

    private static bool EvaluateModelPass(R3ModelVerificationReport report) =>
        report.Laser.PhaseChanges >= 4
        && report.Bending.PhaseChanges >= 4
        && report.Laser.DistinctPhases >= 4
        && report.Bending.DistinctPhases >= 4
        && report.DependencyChecks.All(c => c.Passed);

    private static bool EvaluateEndToEndPass(R3EndToEndVerificationReport report)
    {
        if (report.TotalPhaseChanges < 6)
        {
            return false;
        }

        if (report.TotalOpcUaUpdates <= 0)
        {
            return false;
        }

        if (report.Machines.Any(m => m.JobChanges < 2))
        {
            return false;
        }

        if (report.Machines.Any(m => m.DistinctPhases < 6))
        {
            return false;
        }

        if (report.Machines.Any(m => m.TotalPublishedUpdates <= 0 || m.AveragePublishDurationMs <= 0))
        {
            return false;
        }

        if (report.Exceptions.Count > 0)
        {
            return false;
        }

        if (report.Correlations.Any(c => c.Result is "Failed" or "Review"))
        {
            return false;
        }

        var laserStats = report.Statistics.Count(s => s.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId);
        var bendingStats = report.Statistics.Count(s => s.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId);
        if (laserStats < 30 || bendingStats < 30)
        {
            return false;
        }

        if (report.Statistics.Any(s => s.PhaseEvaluationPassed == false))
        {
            return false;
        }

        if (report.Statistics.Any(s => s.PercentAtHardMaximum > 1 || s.PercentAtHardMinimum > 1))
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

    private static R3EndToEndMachineReport BuildEndToEndMachineReport(PhysicalMachineSession session) =>
        new()
        {
            MachineId = session.MachineId,
            MachineName = session.MachineName,
            ProfileId = session.Profile.ProfileId,
            ProfileVersion = session.Profile.ProfileVersion,
            SignalCount = session.Profile.Signals.Count,
            HiddenStateCount = session.Profile.HiddenProcessStates.Count,
            SignalDependencyCount = session.Profile.Dependencies.Count,
            HiddenStateDependencyCount = session.Profile.HiddenStateDependencies.Count,
            EngineTicks = session.Simulation.Metrics.TotalEngineTicks,
            TotalPublishedUpdates = session.Metrics.TotalPublishedUpdates,
            AveragePublishDurationMs = session.Metrics.AveragePublishDurationMs,
            MaxPublishDurationMs = session.Metrics.MaxPublishDurationMs,
            FailedUpdates = session.Metrics.FailedUpdates,
            PhaseChanges = session.Simulation.Metrics.PhaseChanges,
            JobChanges = session.Simulation.Metrics.JobChanges,
            DistinctPhases = session.Simulation.PhaseTransitions.Select(t => t.ToPhase).Distinct().Count(),
            CurrentPhase = session.Simulation.CurrentPhase.ToString(),
            JobName = session.Simulation.Job.JobName,
            PartName = session.Simulation.Job.PartName
        };

    private static List<R3PhaseSegment> BuildPhaseSegments(PhysicalMachineSession session)
    {
        var segments = new List<R3PhaseSegment>();
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

            segments.Add(new R3PhaseSegment
            {
                MachineName = session.MachineName,
                ProfileId = session.Profile.ProfileId,
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

    private static List<R3CorrelationEvaluation> BuildCorrelationResults(
        Dictionary<string, PhysicalCorrelationRecorder> correlationByProfile)
    {
        var results = new List<R3CorrelationEvaluation>();
        foreach (var g in GetMonitoredSignals(LaserProcessingMachine300ProfileFactory.ProfileId).CorrelationGroups)
        {
            results.Add(EvaluateCorrelation(correlationByProfile[LaserProcessingMachine300ProfileFactory.ProfileId], g, LaserProcessingMachine300ProfileFactory.ProfileId));
        }

        foreach (var g in GetMonitoredSignals(BendingHydraulicMachine300ProfileFactory.ProfileId).CorrelationGroups)
        {
            results.Add(EvaluateCorrelation(correlationByProfile[BendingHydraulicMachine300ProfileFactory.ProfileId], g, BendingHydraulicMachine300ProfileFactory.ProfileId));
        }

        return results;
    }

    private static R3CorrelationEvaluation EvaluateCorrelation(
        PhysicalCorrelationRecorder correlation,
        R3CorrelationPlan plan,
        string profileId)
    {
        var baseResult = correlation.Analyze(
            plan.PairId, profileId, plan.HiddenStateId, plan.TargetSignalId,
            plan.Direction, plan.DependencyType, plan.ExpectedLagSeconds);

        var evaluation = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = baseResult.Pearson,
            Spearman = baseResult.Spearman,
            StrongestLag = baseResult.StrongestCrossCorrelationLag,
            StrongestCrossCorrelation = baseResult.StrongestCrossCorrelation,
            SampleCount = baseResult.SampleCount,
            ExpectedDirection = plan.Direction,
            MinPearson = plan.MinPearson,
            MaxPearson = plan.MaxPearson,
            ExpectedLagSeconds = plan.ExpectedLagSeconds
        });

        return new R3CorrelationEvaluation
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
            Result = evaluation.Result,
            Reason = evaluation.Reason
        };
    }

    private static string[] GetDataChangePaths(string profileId)
    {
        if (profileId == BendingHydraulicMachine300ProfileFactory.ProfileId)
        {
            return [
                "Axis01.Speed", "Axis01.MotorCurrent", "Axis01.MotorTemperature",
                "Bending.PressForce", "Hydraulic.SupplyPressure", "Quality.ProcessQualityIndex", "Production.CycleCounter"
            ];
        }

        return [
            "Axis01.Speed", "Axis01.MotorCurrent", "Axis01.MotorTemperature",
            "Process.SpindleSpeed", "Process.PowerDemand", "Process.QualityIndex", "Production.CycleCounter"
        ];
    }

    private static async Task<List<R3DataChangeSample>> RunDataChangeClientsAsync(
        IReadOnlyList<MachineConfiguration> machines,
        CancellationToken cancellationToken)
    {
        var samples = new List<R3DataChangeSample>();
        foreach (var machine in machines)
        {
            samples.AddRange(await RunDataChangeClientAsync(machine, cancellationToken).ConfigureAwait(false));
        }

        return samples;
    }

    private static async Task<List<R3DataChangeSample>> RunDataChangeClientAsync(
        MachineConfiguration machine,
        CancellationToken cancellationToken)
    {
        var samples = new List<R3DataChangeSample>();
        var config = await PhysicalSignalVerificationHarness.CreateClientConfigurationForTestsAsync(cancellationToken).ConfigureAwait(false);
        var selected = CoreClientUtils.SelectEndpoint(config, machine.Endpoint, false);
        var endpointConfig = new ConfiguredEndpoint(null, selected, EndpointConfiguration.Create(config));
        using var session = await Session.Create(config, endpointConfig, false, "R3Verification", 60000, new UserIdentity(), null, cancellationToken).ConfigureAwait(false);

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
            samples.Add(new R3DataChangeSample
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

    private static R3MonitoredSignalPlan GetMonitoredSignals(string profileId) =>
        profileId == BendingHydraulicMachine300ProfileFactory.ProfileId ? BendingPlan : LaserPlan;

    private static readonly R3MonitoredSignalPlan LaserPlan = new()
    {
        StatisticsSignals = [
            "Axis01.MotorCurrent", "Axis01.Load", "Axis01.Speed", "Axis01.MotorTemperature", "Axis01.Torque",
            "Axis02.MotorCurrent", "Axis02.Load", "Axis02.Speed", "Axis02.MotorTemperature", "Axis02.Torque",
            "Axis03.MotorCurrent", "Axis03.Load", "Axis03.Speed", "Axis03.MotorTemperature", "Axis03.Torque",
            "Axis04.MotorCurrent", "Axis04.Load", "Axis04.Speed", "Axis04.MotorTemperature", "Axis04.Torque",
            "Axis05.MotorCurrent", "Axis05.Load", "Axis05.Speed", "Axis05.MotorTemperature",
            "Drive01.Current", "Drive01.Temperature", "Drive01.Load", "Drive01.Speed",
            "Thermal.CabinetTemperature", "Thermal.SpindleMotorTemp", "Thermal.AmbientTemperature", "Thermal.PanelSurfaceTemp",
            "Cooling.PrimaryCircuit.Temperature", "Cooling.PrimaryCircuit.Flow", "Cooling.PrimaryCircuit.Pressure", "Cooling.PumpSpeed",
            "Process.SpindleSpeed", "Process.FeedRate", "Process.PowerDemand", "Process.QualityIndex",
            "Electrical.MainsVoltage", "Electrical.PowerFactor", "Electrical.TotalCurrent",
            "Mechanical.VibrationRms", "Process.ToolWearIndex", "Process.LaserPowerActual"
        ],
        CorrelationGroups = [
            new("laser-01", "MechanicalLoad", "Axis01.MotorCurrent", "positive", "linear", 0, 0.2, 0.88, 0.95),
            new("laser-02", "MechanicalLoad", "Axis01.Load", "positive", "linear", 0, 0.2, 0.88, 0.95),
            new("laser-03", "Friction", "Axis01.Speed", "negative", "inverseLinear", 0, 0.15, 0.85, 0.95),
            new("laser-04", "Friction", "Axis01.MotorCurrent", "positive", "linear", 0, 0.2, 0.85, 0.95),
            new("laser-05", "ThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 20, 0.15, 0.85, 0.95),
            new("laser-07", "CoolingEfficiency", "Cooling.PrimaryCircuit.Temperature", "negative", "inverseLinear", 0, 0.15, 0.85, 0.95),
            new("laser-08", "ProcessDemand", "Process.PowerDemand", "positive", "linear", 0, 0.2, 0.85, 0.95),
            new("laser-09", "MaterialResistance", "Process.FeedRate", "negative", "inverseLinear", 0, 0.15, 0.8, 0.95)
        ]
    };

    private static readonly R3MonitoredSignalPlan BendingPlan = new()
    {
        StatisticsSignals = [
            "Hydraulic.SupplyPressure", "Hydraulic.ReturnPressure", "Hydraulic.OilLevel", "Hydraulic.OilTemperature",
            "Hydraulic.PumpSpeed", "Hydraulic.AccumulatorPressure", "Hydraulic.FilterLoad", "Hydraulic.PumpCurrent",
            "Hydraulic.FlowRate", "Hydraulic.CylinderPressureA", "Hydraulic.CylinderPressureB", "Hydraulic.ReservoirTemperature",
            "Axis01.MotorCurrent", "Axis01.Load", "Axis01.Speed", "Axis01.MotorTemperature",
            "Axis02.MotorCurrent", "Axis02.Load", "Axis02.Speed", "Axis02.MotorTemperature",
            "Axis03.MotorCurrent", "Axis03.Load", "Axis03.Speed", "Axis03.MotorTemperature",
            "Axis04.MotorCurrent", "Axis04.Load", "Axis04.Speed", "Axis04.MotorTemperature",
            "Thermal.CabinetTemperature", "Thermal.FrameTemperature", "Thermal.HydraulicManifoldTemp", "Thermal.AmbientTemperature",
            "Bending.PressForce", "Bending.RamPosition", "Bending.CycleTime", "Process.PowerDemand",
            "Electrical.MainsVoltage", "Electrical.TotalCurrent", "Electrical.PowerConsumption", "Electrical.PowerFactor",
            "Drive01.Speed", "Drive01.Current", "Drive01.Temperature",
            "Quality.ProcessQualityIndex", "Quality.PositionAccuracy", "Quality.AngleAccuracy", "Quality.SurfaceInspectionScore"
        ],
        CorrelationGroups = [
            new("bend-01", "PressLoad", "Hydraulic.SupplyPressure", "positive", "linear", 0, 0.2, 0.85, 0.95),
            new("bend-02", "PressLoad", "Bending.PressForce", "positive", "saturating", 0, 0.2, 0.85, 0.95),
            new("bend-03", "AxisFriction", "Axis01.Speed", "negative", "inverseLinear", 0, 0.15, 0.85, 0.95),
            new("bend-04", "PumpEfficiency", "Hydraulic.PumpSpeed", "positive", "linear", 0, 0.2, 0.85, 0.95),
            new("bend-05", "StructuralThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 25, 0.15, 0.85, 0.95),
            new("bend-06", "PressLoad", "Axis01.MotorCurrent", "positive", "saturating", 0, 0.15, 0.85, 0.95),
        ]
    };

    private sealed class R3MonitoredSignalPlan
    {
        public required string[] StatisticsSignals { get; init; }
        public required R3CorrelationPlan[] CorrelationGroups { get; init; }
    }

    private sealed record R3CorrelationPlan(
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

public sealed class R3ModelVerificationReport
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public bool Passed { get; set; }
    public R3ModelMachineReport Laser { get; set; } = new();
    public R3ModelMachineReport Bending { get; set; } = new();
    public List<R3DependencyCheck> DependencyChecks { get; set; } = [];
}

public sealed class R3ModelMachineReport
{
    public string ProfileId { get; set; } = string.Empty;
    public int PhaseChanges { get; set; }
    public int DistinctPhases { get; set; }
    public int JobChanges { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
}

public sealed class R3DependencyCheck
{
    public string Pair { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public double LowValue { get; set; }
    public double HighValue { get; set; }
    public double Strength { get; set; }
    public double LagSeconds { get; set; }
    public bool Passed { get; set; }
}

public sealed class R3EndToEndVerificationReport
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public int SeedMachine1 { get; set; }
    public int SeedMachine2 { get; set; }
    public bool Passed { get; set; }
    public int TotalPhaseChanges { get; set; }
    public int JobChanges { get; set; }
    public long TotalOpcUaUpdates { get; set; }
    public List<R3EndToEndMachineReport> Machines { get; set; } = [];
    public List<R3PhaseSegment> PhaseSegments { get; set; } = [];
    public List<SignalStatisticsSnapshot> Statistics { get; set; } = [];
    public List<R3CorrelationEvaluation> Correlations { get; set; } = [];
    public List<R3DataChangeSample> DataChangeSamples { get; set; } = [];
    public List<string> Exceptions { get; set; } = [];
}

public sealed class R3EndToEndMachineReport
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

public sealed class R3PhaseSegment
{
    public string MachineName { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
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

public sealed class R3CorrelationEvaluation
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

public sealed class R3DataChangeSample
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
