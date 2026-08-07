using System.Text.Json;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Validation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.OpcUa;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp4VerificationSettings
{
    public const int ExpectedScenarioCount = 22;

    public static bool IsExportMode =>
        string.Equals(Environment.GetEnvironmentVariable("AP4_VERIFY_EXPORT"), "1", StringComparison.Ordinal);

    public static TimeSpan EndToEndDuration
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("AP4_E2E_SECONDS");
            if (int.TryParse(raw, out var seconds) && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }

            return PhysicalVerificationSettings.IsShortMode
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromSeconds(90);
        }
    }
}

public static class PhysicalAp4VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-04-fault-scenarios"));

    public static string CreateVerificationRunId() =>
        $"ap4-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 40);

    public static bool EvaluateCatalogPassForTests(Ap4CatalogValidationReport report) => EvaluateCatalogPass(report).Passed;

    public static bool EvaluateModelTestsPassForTests(Ap4ModelTestsReport report) => EvaluateModelTestsPass(report).Passed;

    public static bool EvaluateLifecyclePassForTests(Ap4LifecycleTestsReport report) => EvaluateLifecyclePass(report).Passed;

    public static bool EvaluateCombinationPassForTests(Ap4CombinationTestsReport report) => EvaluateCombinationPass(report).Passed;

    public static bool EvaluateRecoveryPassForTests(Ap4RecoveryTestsReport report) => EvaluateRecoveryPass(report).Passed;

    public static bool EvaluateEndToEndPassForTests(Ap4ShortEndToEndReport report) => EvaluateEndToEndPass(report).Passed;

    public static async Task<Ap4CatalogValidationReport> RunCatalogValidationAsync(CancellationToken cancellationToken = default)
    {
        var report = new Ap4CatalogValidationReport
        {
            StartedAtUtc = DateTime.UtcNow,
            FaultScenariosDirectory = PhysicalTestServiceFactory.ResolveFaultScenariosDirectory()
        };

        var repository = new JsonFaultScenarioRepository(report.FaultScenariosDirectory);
        await repository.LoadAllAsync(cancellationToken).ConfigureAwait(false);
        var scenarios = repository.GetAll();
        var validator = new FaultScenarioValidator();

        report.TotalScenarios = scenarios.Count;
        report.EnabledScenarios = scenarios.Count(s => s.IsEnabled);

        var catalogValidation = validator.ValidateCatalog(scenarios);
        report.CatalogValid = catalogValidation.IsValid;
        report.CatalogErrors = catalogValidation.Errors
            .Select(e => $"{e.ScenarioId}:{e.FieldPath}:{e.Message}")
            .ToList();

        foreach (var scenario in scenarios.Where(s => s.IsEnabled))
        {
            foreach (var profileId in scenario.MachineProfileIds)
            {
                if (!ProfileFactories.TryGetValue(profileId, out var factory))
                {
                    report.ProfileResults.Add(new Ap4ScenarioProfileValidationResult
                    {
                        ScenarioId = scenario.ScenarioId,
                        ProfileId = profileId,
                        IsValid = false,
                        Errors = [$"Unknown profile '{profileId}'."]
                    });
                    continue;
                }

                var profile = factory();
                var validation = validator.ValidateForProfile(scenario, profile);
                report.ProfileResults.Add(new Ap4ScenarioProfileValidationResult
                {
                    ScenarioId = scenario.ScenarioId,
                    ProfileId = profileId,
                    IsValid = validation.IsValid,
                    Errors = validation.Errors.Select(e => $"{e.FieldPath}:{e.Message}").ToList()
                });
            }
        }

        report.EndedAtUtc = DateTime.UtcNow;
        var pass = EvaluateCatalogPass(report);
        report.Passed = pass.Passed;
        report.FailedCriteria = pass.FailedCriteria;
        return report;
    }

    public static async Task<Ap4ModelTestsReport> RunModelTestsForAllScenariosAsync(
        int seed = 42,
        double acceleratedTimeFactor = 12.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4ModelTestsReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Seed = seed,
            AcceleratedTimeFactor = acceleratedTimeFactor
        };

        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        foreach (var plan in stack.FaultScenarioService.GetCatalog())
        {
            foreach (var profileId in plan.MachineProfileIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                report.Results.Add(await RunSingleModelTestAsync(stack, plan.ScenarioId, profileId, seed, acceleratedTimeFactor, cancellationToken).ConfigureAwait(false));
            }
        }

        report.EndedAtUtc = DateTime.UtcNow;
        var pass = EvaluateModelTestsPass(report);
        report.Passed = pass.Passed;
        report.FailedCriteria = pass.FailedCriteria;
        return report;
    }

    public static async Task<Ap4LifecycleTestsReport> RunLifecycleTestsAsync(
        int seed = 42,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4LifecycleTestsReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Seed = seed
        };

        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        report.Results.Add(await RunLifecycleCaseAsync(stack, LaserProcessingMachine300ProfileFactory.ProfileId, "laser-overheating-axis-drive", seed, cancellationToken).ConfigureAwait(false));

        report.EndedAtUtc = DateTime.UtcNow;
        var pass = EvaluateLifecyclePass(report);
        report.Passed = pass.Passed;
        report.FailedCriteria = pass.FailedCriteria;
        return report;
    }

    public static async Task<Ap4CombinationTestsReport> RunCombinationTestsAsync(
        int seed = 42,
        double acceleratedTimeFactor = 12.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4CombinationTestsReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Seed = seed,
            AcceleratedTimeFactor = acceleratedTimeFactor
        };

        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateAndRegisterSession(
            stack,
            LaserProcessingMachine300ProfileFactory.ProfileId,
            seed,
            acceleratedTimeFactor,
            ProcessPhase.Idle);

        var baselineCooling = ReadHidden(session, "CoolingEfficiency");
        var baselineMaterial = ReadHidden(session, "MaterialResistance");

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "coolant-loss",
            Intensity = 1.0,
            TimeFactor = acceleratedTimeFactor,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "material-resistance-increased",
            Intensity = 1.0,
            TimeFactor = acceleratedTimeFactor,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        RunTicks(stack, session, 100);

        var afterCooling = ReadHidden(session, "CoolingEfficiency");
        var afterMaterial = ReadHidden(session, "MaterialResistance");
        var active = stack.FaultScenarioService.GetActiveScenarios(session.MachineId);

        report.CoolantLossTargetDelta = afterCooling - baselineCooling;
        report.MaterialResistanceTargetDelta = afterMaterial - baselineMaterial;
        report.ActiveScenarioCount = active.Count;
        report.BothScenariosActive = active.Count == 2;
        report.CoolingEfficiencyDecreased = afterCooling < baselineCooling;
        report.MaterialResistanceIncreased = afterMaterial > baselineMaterial;

        await stack.FaultScenarioService.ResetMachineAsync(session.MachineId, cancellationToken).ConfigureAwait(false);
        report.ResetClearsActiveScenarios = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).Count == 0;

        report.EndedAtUtc = DateTime.UtcNow;
        var pass = EvaluateCombinationPass(report);
        report.Passed = pass.Passed;
        report.FailedCriteria = pass.FailedCriteria;
        return report;
    }

    public static async Task<Ap4RecoveryTestsReport> RunRecoveryTestsAsync(
        int seed = 42,
        double acceleratedTimeFactor = 15.0,
        CancellationToken cancellationToken = default)
    {
        var report = new Ap4RecoveryTestsReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Seed = seed,
            AcceleratedTimeFactor = acceleratedTimeFactor
        };

        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);

        var session = CreateAndRegisterSession(
            stack,
            BendingHydraulicMachine300ProfileFactory.ProfileId,
            seed,
            acceleratedTimeFactor,
            ProcessPhase.Idle);

        var baselineEfficiency = ReadHidden(session, "HydraulicEfficiency");
        var baselinePressLoad = ReadHidden(session, "PressLoad");
        var controlSession = CreateAndRegisterSession(
            stack,
            BendingHydraulicMachine300ProfileFactory.ProfileId,
            seed + 77,
            acceleratedTimeFactor,
            ProcessPhase.Idle);
        await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = "hydraulic-leak",
            Intensity = 1.0,
            TimeFactor = acceleratedTimeFactor,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        }, cancellationToken).ConfigureAwait(false);

        RunTicks(stack, session, 80);
        RunTicks(stack, controlSession, 80);
        var degradedEfficiency = ReadHidden(session, "HydraulicEfficiency");
        var degradedPressLoad = ReadHidden(session, "PressLoad");
        var controlEfficiency = ReadHidden(controlSession, "HydraulicEfficiency");
        var controlPressLoad = ReadHidden(controlSession, "PressLoad");
        report.EfficiencyDeltaDuringFault = degradedEfficiency - baselineEfficiency;
        report.PressLoadDeltaDuringFault = degradedPressLoad - baselinePressLoad;
        report.EfficiencyAccumulator = degradedEfficiency - controlEfficiency;
        report.PressLoadAccumulator = degradedPressLoad - controlPressLoad;
        stack.FaultScenarioService.UnregisterSession(controlSession.MachineId);

        await stack.FaultScenarioService.StopAsync(session.MachineId, "hydraulic-leak", cancellationToken).ConfigureAwait(false);
        var activeAfterStop = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).First();
        report.RecoveryStarted = activeAfterStop.LifecycleState == FaultScenarioLifecycleState.Recovering;

        for (var i = 0; i < 200; i++)
        {
            RunTicks(stack, session, 1);
            activeAfterStop = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).FirstOrDefault();
            if (activeAfterStop == null)
            {
                break;
            }

            report.MaxRecoveryProgress = Math.Max(report.MaxRecoveryProgress, activeAfterStop.RecoveryProgress);
        }

        var recoveredEfficiency = ReadHidden(session, "HydraulicEfficiency");
        report.EfficiencyDeltaAfterRecovery = recoveredEfficiency - degradedEfficiency;
        report.RecoveryCompleted = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).Count == 0;
        report.EfficiencyImprovedAfterRecovery = recoveredEfficiency > degradedEfficiency;

        report.EndedAtUtc = DateTime.UtcNow;
        var pass = EvaluateRecoveryPass(report);
        report.Passed = pass.Passed;
        report.FailedCriteria = pass.FailedCriteria;
        return report;
    }

    public static async Task<Ap4ShortEndToEndReport> RunShortEndToEndAsync(
        string verificationRunId,
        CancellationToken cancellationToken = default)
    {
        var previousShort = Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT");
        Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", "1");
        var duration = PhysicalAp4VerificationSettings.EndToEndDuration;

        var report = new Ap4ShortEndToEndReport
        {
            VerificationRunId = verificationRunId,
            StartedAtUtc = DateTime.UtcNow,
            Duration = duration
        };

        var log = new TestLogService();
        var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
        await stack.FaultScenarioService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var serverService = new MachineServerService(log, stack.Coordinator);

        var machines = new List<MachineConfiguration>
        {
            CreateMachine(1, 14910, LaserProcessingMachine300ProfileFactory.ProfileId, 42),
            CreateMachine(2, 14911, BendingHydraulicMachine300ProfileFactory.ProfileId, 99),
            CreateMachine(3, 14912, TechnicalLearningMachine300ProfileFactory.ProfileId, 77)
        };

        try
        {
            foreach (var machine in machines)
            {
                stack.Coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
                await serverService.StartServerAsync(machine, new MachineRuntimeState { MachineId = machine.Id }, cancellationToken).ConfigureAwait(false);
            }

            await StartScenarioAsync(stack, machines[0].Id, "laser-overheating-axis-drive", 8.0, FaultScenarioRunMode.Normal, cancellationToken).ConfigureAwait(false);
            await StartScenarioAsync(stack, machines[0].Id, "coolant-loss", 8.0, FaultScenarioRunMode.NonFaultingControlRun, cancellationToken).ConfigureAwait(false);
            await StartScenarioAsync(stack, machines[0].Id, "intermittent-fault", 8.0, FaultScenarioRunMode.Normal, cancellationToken).ConfigureAwait(false);
            await StartScenarioAsync(stack, machines[1].Id, "hydraulic-leak", 8.0, FaultScenarioRunMode.Normal, cancellationToken).ConfigureAwait(false);
            await StartScenarioAsync(stack, machines[2].Id, "communication-drop", 8.0, FaultScenarioRunMode.Normal, cancellationToken).ConfigureAwait(false);

            var endAt = DateTime.UtcNow + duration;
            while (DateTime.UtcNow < endAt)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                foreach (var session in stack.Coordinator.GetSessions())
                {
                    var active = stack.FaultScenarioService.GetActiveScenarios(session.MachineId);
                    report.Samples.Add(new Ap4EndToEndSample
                    {
                        TimestampUtc = DateTime.UtcNow,
                        MachineId = session.MachineId,
                        ProfileId = session.Profile.ProfileId,
                        ActiveScenarioCount = active.Count,
                        MechanicalLoad = TryReadHidden(session, "MechanicalLoad"),
                        ThermalLoad = TryReadHidden(session, "ThermalLoad"),
                        HydraulicEfficiency = TryReadHidden(session, "HydraulicEfficiency"),
                        CoolingEfficiency = TryReadHidden(session, "CoolingEfficiency"),
                        PublishedUpdates = session.Metrics.TotalPublishedUpdates,
                        EngineTicks = session.Simulation.Metrics.TotalEngineTicks
                    });
                }
            }

            foreach (var machine in machines)
            {
                var session = stack.Coordinator.GetSession(machine.Id);
                if (session == null)
                {
                    continue;
                }

                report.Machines.Add(new Ap4EndToEndMachineReport
                {
                    MachineId = machine.Id,
                    MachineName = machine.Name,
                    ProfileId = session.Profile.ProfileId,
                    ActiveScenarios = stack.FaultScenarioService.GetActiveScenarios(machine.Id)
                        .Select(s => s.ScenarioId)
                        .ToList(),
                    TotalPublishedUpdates = session.Metrics.TotalPublishedUpdates,
                    EngineTicks = session.Simulation.Metrics.TotalEngineTicks,
                    MechanicalLoad = TryReadHidden(session, "MechanicalLoad"),
                    ThermalLoad = TryReadHidden(session, "ThermalLoad"),
                    HydraulicEfficiency = TryReadHidden(session, "HydraulicEfficiency"),
                    CoolingEfficiency = TryReadHidden(session, "CoolingEfficiency")
                });
            }

            report.TotalOpcUaUpdates = report.Machines.Sum(m => m.TotalPublishedUpdates);
            report.TotalEngineTicks = report.Machines.Sum(m => m.EngineTicks);
            report.Exceptions = log.Entries.Where(e => e.Category == LogCategory.Error).Select(e => e.Message).ToList();

            var pass = EvaluateEndToEndPass(report);
            report.Passed = pass.Passed;
            report.FailedCriteria = pass.FailedCriteria;
        }
        finally
        {
            Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", previousShort);
            await stack.Coordinator.StopAllAsync(cancellationToken).ConfigureAwait(false);
            await serverService.StopAllAsync(cancellationToken).ConfigureAwait(false);
            report.EndedAtUtc = DateTime.UtcNow;
        }

        return report;
    }

    public static async Task ExportEvidenceAsync(
        string verificationRunId,
        Ap4CatalogValidationReport? catalog = null,
        Ap4ModelTestsReport? modelTests = null,
        Ap4LifecycleTestsReport? lifecycle = null,
        Ap4CombinationTestsReport? combination = null,
        Ap4RecoveryTestsReport? recovery = null,
        Ap4ShortEndToEndReport? endToEnd = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (catalog != null)
        {
            catalog.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-04-scenario-catalog-validation.json"),
                JsonSerializer.Serialize(catalog, options),
                cancellationToken).ConfigureAwait(false);
        }

        if (modelTests != null)
        {
            modelTests.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-04-scenario-model-tests.json"),
                JsonSerializer.Serialize(modelTests, options),
                cancellationToken).ConfigureAwait(false);
        }

        if (lifecycle != null)
        {
            lifecycle.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-04-scenario-lifecycle-tests.json"),
                JsonSerializer.Serialize(lifecycle, options),
                cancellationToken).ConfigureAwait(false);
        }

        if (combination != null)
        {
            combination.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-04-scenario-combination-tests.json"),
                JsonSerializer.Serialize(combination, options),
                cancellationToken).ConfigureAwait(false);
        }

        if (recovery != null)
        {
            recovery.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-04-scenario-recovery-tests.json"),
                JsonSerializer.Serialize(recovery, options),
                cancellationToken).ConfigureAwait(false);
        }

        if (endToEnd != null)
        {
            endToEnd.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-04-short-scenario-end-to-end.json"),
                JsonSerializer.Serialize(endToEnd, options),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<Ap4ModelTestResult> RunSingleModelTestAsync(
        FaultScenarioTestStack stack,
        string scenarioId,
        string profileId,
        int seed,
        double acceleratedTimeFactor,
        CancellationToken cancellationToken)
    {
        var result = new Ap4ModelTestResult
        {
            ScenarioId = scenarioId,
            ProfileId = profileId
        };

        try
        {
            var session = CreateAndRegisterSession(
                stack,
                profileId,
                seed + scenarioId.GetHashCode(),
                acceleratedTimeFactor,
                ResolveModelTestPhase(scenarioId));
            var definition = stack.FaultScenarioService.GetCatalog().First(s => s.ScenarioId == scenarioId);
            result.BaselineHiddenStates = CaptureTrackedStates(session, definition);

            await stack.FaultScenarioService.StartAsync(new FaultScenarioStartRequest
            {
                MachineId = session.MachineId,
                ScenarioId = scenarioId,
                Intensity = 1.0,
                TimeFactor = acceleratedTimeFactor,
                AutoThresholdFaultEnabled = false,
                AutoScenarioEndEnabled = false
            }, cancellationToken).ConfigureAwait(false);

            RunTicks(stack, session, profileId.Contains("bending", StringComparison.OrdinalIgnoreCase) ? 250 : 96);
            result.AfterHiddenStates = CaptureTrackedStates(session, definition);
            result.Deltas = result.BaselineHiddenStates
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => result.AfterHiddenStates.GetValueOrDefault(kvp.Key, kvp.Value) - kvp.Value,
                    StringComparer.OrdinalIgnoreCase);

            var controlComparisonDeltas = MeasureControlComparisonDeltas(
                stack, scenarioId, profileId, seed, acceleratedTimeFactor, session);

            result.ActiveScenarioCount = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).Count;
            result.Phase = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).FirstOrDefault()?.CurrentPhase.ToString() ?? string.Empty;
            result.EngineTicks = session.Simulation.Metrics.TotalEngineTicks;
            result.DirectionChecks = EvaluateDirectionChecks(
                scenarioId,
                profileId,
                result.Deltas,
                controlComparisonDeltas);
            result.Passed = result.ActiveScenarioCount == 1
                && result.EngineTicks > 0
                && result.DirectionChecks.All(c => c.Passed);

            await stack.FaultScenarioService.ResetMachineAsync(session.MachineId, cancellationToken).ConfigureAwait(false);
            stack.FaultScenarioService.UnregisterSession(session.MachineId);
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Error = ex.Message;
        }

        return result;
    }

    private static async Task<Ap4LifecycleTestResult> RunLifecycleCaseAsync(
        FaultScenarioTestStack stack,
        string profileId,
        string scenarioId,
        int seed,
        CancellationToken cancellationToken)
    {
        var result = new Ap4LifecycleTestResult
        {
            ProfileId = profileId,
            ScenarioId = scenarioId
        };

        var session = CreateAndRegisterSession(stack, profileId, seed, 10.0);
        var request = new FaultScenarioStartRequest
        {
            MachineId = session.MachineId,
            ScenarioId = scenarioId,
            Intensity = 1.0,
            TimeFactor = 10.0,
            AutoThresholdFaultEnabled = false,
            AutoScenarioEndEnabled = false
        };

        await stack.FaultScenarioService.StartAsync(request, cancellationToken).ConfigureAwait(false);
        result.StartAccepted = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).Count == 1;
        RunTicks(stack, session, 1);

        try
        {
            await stack.FaultScenarioService.StartAsync(request, cancellationToken).ConfigureAwait(false);
            result.DuplicateStartRejected = false;
        }
        catch (InvalidOperationException)
        {
            result.DuplicateStartRejected = true;
        }

        await stack.FaultScenarioService.PauseAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
        var paused = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).First();
        result.PauseWorked = paused.LifecycleState == FaultScenarioLifecycleState.Paused;

        var elapsedBeforePause = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).First().SimulationElapsed;
        RunTicks(stack, session, 10);
        var elapsedAfterPause = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).First().SimulationElapsed;
        result.TicksWhilePausedUnchanged = elapsedAfterPause == elapsedBeforePause;

        await stack.FaultScenarioService.ResumeAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
        var resumed = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).First();
        result.ResumeWorked = resumed.LifecycleState == FaultScenarioLifecycleState.Running;

        RunTicks(stack, session, 20);
        await stack.FaultScenarioService.StopAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
        var recovering = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).First();
        result.StopStartedRecovery = recovering.LifecycleState == FaultScenarioLifecycleState.Recovering;

        await stack.FaultScenarioService.CancelAsync(session.MachineId, scenarioId, cancellationToken).ConfigureAwait(false);
        result.CancelClearsScenario = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).Count == 0;

        await stack.FaultScenarioService.StartAsync(request, cancellationToken).ConfigureAwait(false);
        await stack.FaultScenarioService.ResetMachineAsync(session.MachineId, cancellationToken).ConfigureAwait(false);
        result.ResetClearsScenario = stack.FaultScenarioService.GetActiveScenarios(session.MachineId).Count == 0;

        result.Passed = result.StartAccepted
            && result.DuplicateStartRejected
            && result.PauseWorked
            && result.TicksWhilePausedUnchanged
            && result.ResumeWorked
            && result.StopStartedRecovery
            && result.CancelClearsScenario
            && result.ResetClearsScenario;

        stack.FaultScenarioService.UnregisterSession(session.MachineId);
        return result;
    }

    private static Dictionary<string, double> MeasureControlComparisonDeltas(
        FaultScenarioTestStack stack,
        string scenarioId,
        string profileId,
        int seed,
        double acceleratedTimeFactor,
        PhysicalMachineSession faultSession)
    {
        if (!scenarioId.Equals("hydraulic-leak", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        var controlSession = CreateAndRegisterSession(
            stack,
            profileId,
            seed + 177,
            acceleratedTimeFactor,
            ProcessPhase.Idle);

        RunTicks(stack, controlSession, 96);

        var comparisons = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["HydraulicEfficiency"] = ReadHidden(faultSession, "HydraulicEfficiency") - ReadHidden(controlSession, "HydraulicEfficiency"),
            ["PressLoad"] = ReadHidden(faultSession, "PressLoad") - ReadHidden(controlSession, "PressLoad")
        };

        stack.FaultScenarioService.UnregisterSession(controlSession.MachineId);
        return comparisons;
    }

    private static List<Ap4DirectionCheck> EvaluateDirectionChecks(
        string scenarioId,
        string profileId,
        IReadOnlyDictionary<string, double> deltas,
        IReadOnlyDictionary<string, double>? comparisonDeltas)
    {
        return DirectionExpectations
            .Where(e => e.ScenarioId.Equals(scenarioId, StringComparison.OrdinalIgnoreCase)
                && e.ProfileId.Equals(profileId, StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                var delta = ResolveMeasuredDelta(e, deltas, comparisonDeltas);
                var passed = e.ExpectedDirection switch
                {
                    Ap4ExpectedDirection.Increase => delta > e.MinimumDelta,
                    Ap4ExpectedDirection.Decrease => delta < -e.MinimumDelta,
                    _ => Math.Abs(delta) <= e.MinimumDelta
                };
                return new Ap4DirectionCheck
                {
                    HiddenStateId = e.HiddenStateId,
                    ExpectedDirection = e.ExpectedDirection.ToString(),
                    MeasuredDelta = delta,
                    MinimumDelta = e.MinimumDelta,
                    Passed = passed
                };
            })
            .ToList();
    }

    private static double ResolveMeasuredDelta(
        Ap4DirectionExpectation expectation,
        IReadOnlyDictionary<string, double> deltas,
        IReadOnlyDictionary<string, double>? comparisonDeltas)
    {
        if (comparisonDeltas != null
            && comparisonDeltas.TryGetValue(expectation.HiddenStateId, out var comparisonDelta))
        {
            return comparisonDelta;
        }

        return deltas.GetValueOrDefault(expectation.HiddenStateId, 0);
    }

    private static Dictionary<string, double> CaptureTrackedStates(PhysicalMachineSession session, FaultScenarioDefinition definition)
    {
        var tracked = definition.Effects
            .Where(e => e.TargetType == FaultEffectTargetType.HiddenState)
            .Select(e => e.TargetId)
            .Where(id => session.Profile.HiddenProcessStates.Any(h => h.StateId.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tracked.ToDictionary(
            id => id,
            id => ReadHidden(session, id),
            StringComparer.OrdinalIgnoreCase);
    }

    private static ProcessPhase ResolveModelTestPhase(string scenarioId) =>
        scenarioId switch
        {
            "laser-overheating-axis-drive" or "intermittent-fault" => ProcessPhase.Processing,
            "hydraulic-leak" or "oil-aging" or "tool-deflection" or "valve-delay" or "pump-wear" => ProcessPhase.Processing,
            _ => ProcessPhase.Idle
        };

    private static PhysicalMachineSession CreateAndRegisterSession(
        FaultScenarioTestStack stack,
        string profileId,
        int seed,
        double timeFactor,
        ProcessPhase phase = ProcessPhase.Idle)
    {
        var machineId = Guid.NewGuid();
        var profile = ProfileFactories[profileId]();
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
        session.Simulation.GenerationMode = SignalGenerationMode.Physical;
        session.Simulation.IsEngineActive = true;
        stack.FaultScenarioService.RegisterSession(session);
        return session;
    }

    private static void RunTicks(FaultScenarioTestStack stack, PhysicalMachineSession session, int count)
    {
        for (var i = 0; i < count; i++)
        {
            stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
        }
    }

    private static double ReadHidden(PhysicalMachineSession session, string stateId)
    {
        var state = session.Runtime.HiddenProcessStates.First(s => s.StateId.Equals(stateId, StringComparison.OrdinalIgnoreCase));
        return (state.CurrentValue + state.TargetValue) * 0.5;
    }

    private static double? TryReadHidden(PhysicalMachineSession session, string stateId) =>
        session.Runtime.HiddenProcessStates.FirstOrDefault(s => s.StateId.Equals(stateId, StringComparison.OrdinalIgnoreCase))?.TargetValue;

    private static async Task StartScenarioAsync(
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
        machine.Name = $"AP4-{profileId}-{index}";
        machine.PhysicalProfileId = profileId;
        machine.Port = port;
        machine.UpdateEndpointFromHostPort();
        return machine;
    }

    private static Ap4PassEvaluation EvaluateCatalogPass(Ap4CatalogValidationReport report)
    {
        var failed = new List<string>();
        if (report.TotalScenarios != PhysicalAp4VerificationSettings.ExpectedScenarioCount)
        {
            failed.Add($"scenario-count:{report.TotalScenarios}");
        }

        if (!report.CatalogValid)
        {
            failed.Add("catalog-invalid");
        }

        if (report.ProfileResults.Any(r => !r.IsValid))
        {
            failed.Add("profile-validation");
        }

        return new Ap4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    private static Ap4PassEvaluation EvaluateModelTestsPass(Ap4ModelTestsReport report)
    {
        var failed = new List<string>();
        if (report.Results.Count == 0)
        {
            failed.Add("no-results");
        }

        if (report.Results.Any(r => !r.Passed))
        {
            failed.Add("model-test-failures");
        }

        var directionRequired = report.Results
            .Where(r => DirectionExpectations.Any(e =>
                e.ScenarioId.Equals(r.ScenarioId, StringComparison.OrdinalIgnoreCase)
                && e.ProfileId.Equals(r.ProfileId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (directionRequired.Any(r => r.DirectionChecks.Any(c => !c.Passed)))
        {
            failed.Add("direction-checks");
        }

        return new Ap4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    private static Ap4PassEvaluation EvaluateLifecyclePass(Ap4LifecycleTestsReport report)
    {
        var failed = new List<string>();
        if (!report.Results.All(r => r.Passed))
        {
            failed.Add("lifecycle-failures");
        }

        return new Ap4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    private static Ap4PassEvaluation EvaluateCombinationPass(Ap4CombinationTestsReport report)
    {
        var failed = new List<string>();
        if (!report.BothScenariosActive)
        {
            failed.Add("both-scenarios-active");
        }

        if (!report.CoolingEfficiencyDecreased)
        {
            failed.Add("cooling-efficiency");
        }

        if (!report.MaterialResistanceIncreased)
        {
            failed.Add("material-resistance");
        }

        if (!report.ResetClearsActiveScenarios)
        {
            failed.Add("reset");
        }

        return new Ap4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    private static Ap4PassEvaluation EvaluateRecoveryPass(Ap4RecoveryTestsReport report)
    {
        var failed = new List<string>();
        if (!report.RecoveryStarted)
        {
            failed.Add("recovery-started");
        }

        if (!report.RecoveryCompleted)
        {
            failed.Add("recovery-completed");
        }

        if (report.EfficiencyAccumulator >= -0.0001 && report.PressLoadAccumulator >= -0.0001)
        {
            failed.Add("efficiency-degraded");
        }

        if (!report.EfficiencyImprovedAfterRecovery)
        {
            failed.Add("efficiency-recovered");
        }

        return new Ap4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    private static Ap4PassEvaluation EvaluateEndToEndPass(Ap4ShortEndToEndReport report)
    {
        var failed = new List<string>();
        if (report.Machines.Count < 3)
        {
            failed.Add("machine-count");
        }

        if (report.TotalOpcUaUpdates <= 0)
        {
            failed.Add("opcua-updates");
        }

        if (report.TotalEngineTicks <= 0)
        {
            failed.Add("engine-ticks");
        }

        if (report.Exceptions.Count > 0)
        {
            failed.Add("exceptions");
        }

        var laser = report.Machines.FirstOrDefault(m => m.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId);
        var bending = report.Machines.FirstOrDefault(m => m.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId);
        var technical = report.Machines.FirstOrDefault(m => m.ProfileId == TechnicalLearningMachine300ProfileFactory.ProfileId);

        if (laser == null)
        {
            failed.Add("laser-machine");
        }
        else
        {
            var laserHadScenarios = report.Samples.Any(s =>
                s.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId && s.ActiveScenarioCount >= 2);
            if (!laserHadScenarios && (laser.MechanicalLoad is not > 0.45 || laser.ThermalLoad is not > 0.4))
            {
                failed.Add("laser-scenarios");
            }

            if (laser.MechanicalLoad is not > 0.45)
            {
                failed.Add("overheating-mechanical-load");
            }

            if (laser.ThermalLoad is not > 0.4)
            {
                failed.Add("overheating-thermal-load");
            }
        }

        if (bending == null)
        {
            failed.Add("bending-machine");
        }
        else
        {
            var bendingHadLeak = report.Samples.Any(s =>
                s.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId && s.ActiveScenarioCount > 0)
                || bending.ActiveScenarios.Contains("hydraulic-leak");
            if (!bendingHadLeak)
            {
                failed.Add("hydraulic-leak");
            }

            var bendingEfficiency = report.Samples
                .Where(s => s.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId && s.HydraulicEfficiency.HasValue)
                .Select(s => s.HydraulicEfficiency!.Value)
                .DefaultIfEmpty(bending.HydraulicEfficiency ?? 1.0)
                .Min();
            if (bendingEfficiency >= 0.88)
            {
                failed.Add("hydraulic-efficiency-drop");
            }
        }

        if (technical == null)
        {
            failed.Add("technical-machine");
        }
        else
        {
            var technicalHadDrop = report.Samples.Any(s =>
                s.ProfileId == TechnicalLearningMachine300ProfileFactory.ProfileId && s.ActiveScenarioCount > 0)
                || technical.ActiveScenarios.Contains("communication-drop");
            if (!technicalHadDrop)
            {
                failed.Add("communication-drop");
            }
        }

        return new Ap4PassEvaluation { Passed = failed.Count == 0, FailedCriteria = failed };
    }

    private static readonly Dictionary<string, Func<PhysicalMachineProfile>> ProfileFactories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [LaserProcessingMachine300ProfileFactory.ProfileId] = LaserProcessingMachine300ProfileFactory.Create,
            [BendingHydraulicMachine300ProfileFactory.ProfileId] = BendingHydraulicMachine300ProfileFactory.Create,
            [TechnicalLearningMachine300ProfileFactory.ProfileId] = TechnicalLearningMachine300ProfileFactory.Create
        };

    private static readonly Ap4DirectionExpectation[] DirectionExpectations =
    [
        new("laser-overheating-axis-drive", LaserProcessingMachine300ProfileFactory.ProfileId, "MechanicalLoad", Ap4ExpectedDirection.Increase, 0.01),
        new("laser-overheating-axis-drive", LaserProcessingMachine300ProfileFactory.ProfileId, "ThermalLoad", Ap4ExpectedDirection.Increase, 0.005),
        new("hydraulic-leak", BendingHydraulicMachine300ProfileFactory.ProfileId, "HydraulicEfficiency", Ap4ExpectedDirection.Decrease, 0.0001),
        new("hydraulic-leak", BendingHydraulicMachine300ProfileFactory.ProfileId, "PressLoad", Ap4ExpectedDirection.Decrease, 0.0001),
        new("coolant-loss", LaserProcessingMachine300ProfileFactory.ProfileId, "CoolingEfficiency", Ap4ExpectedDirection.Decrease, 0.005),
        new("coolant-loss", BendingHydraulicMachine300ProfileFactory.ProfileId, "AmbientInfluence", Ap4ExpectedDirection.Increase, 0.0001),
        new("material-resistance-increased", LaserProcessingMachine300ProfileFactory.ProfileId, "MaterialResistance", Ap4ExpectedDirection.Increase, 0.005),
        new("material-resistance-increased", BendingHydraulicMachine300ProfileFactory.ProfileId, "MaterialSpringback", Ap4ExpectedDirection.Increase, 0.005),
        new("oil-aging", BendingHydraulicMachine300ProfileFactory.ProfileId, "OilCondition", Ap4ExpectedDirection.Decrease, 0.0001),
        new("oil-aging", BendingHydraulicMachine300ProfileFactory.ProfileId, "PumpEfficiency", Ap4ExpectedDirection.Decrease, 0.0001),
        new("tool-deflection", BendingHydraulicMachine300ProfileFactory.ProfileId, "ToolDeflection", Ap4ExpectedDirection.Increase, 0.0001),
        new("valve-delay", BendingHydraulicMachine300ProfileFactory.ProfileId, "PressLoad", Ap4ExpectedDirection.Increase, 0.0001),
        new("bearing-degradation", LaserProcessingMachine300ProfileFactory.ProfileId, "MechanicalLoad", Ap4ExpectedDirection.Increase, 0.0001),
        new("bearing-degradation", LaserProcessingMachine300ProfileFactory.ProfileId, "ThermalLoad", Ap4ExpectedDirection.Increase, 0.0001),
        new("focus-drift", LaserProcessingMachine300ProfileFactory.ProfileId, "AxisAlignment", Ap4ExpectedDirection.Decrease, 0.0001),
        new("imbalance", LaserProcessingMachine300ProfileFactory.ProfileId, "MechanicalLoad", Ap4ExpectedDirection.Increase, 0.0001),
        new("lubricant-shortage", LaserProcessingMachine300ProfileFactory.ProfileId, "LubricationQuality", Ap4ExpectedDirection.Decrease, 0.0001),
        new("optics-contamination", LaserProcessingMachine300ProfileFactory.ProfileId, "OpticalCondition", Ap4ExpectedDirection.Decrease, 0.0001),
        new("stiff-linear-guide", LaserProcessingMachine300ProfileFactory.ProfileId, "Friction", Ap4ExpectedDirection.Increase, 0.0001),
        new("fan-degradation", LaserProcessingMachine300ProfileFactory.ProfileId, "AmbientInfluence", Ap4ExpectedDirection.Increase, 0.0001),
        new("filter-contamination", LaserProcessingMachine300ProfileFactory.ProfileId, "CoolingEfficiency", Ap4ExpectedDirection.Decrease, 0.0001),
        new("intermittent-fault", LaserProcessingMachine300ProfileFactory.ProfileId, "MechanicalLoad", Ap4ExpectedDirection.Increase, 0.0001),
        new("intermittent-fault", BendingHydraulicMachine300ProfileFactory.ProfileId, "PressLoad", Ap4ExpectedDirection.Increase, 0.0001),
        new("power-instability", LaserProcessingMachine300ProfileFactory.ProfileId, "ElectricalStability", Ap4ExpectedDirection.Decrease, 0.0001),
        new("pump-wear", BendingHydraulicMachine300ProfileFactory.ProfileId, "PumpEfficiency", Ap4ExpectedDirection.Decrease, 0.0001),
        new("tool-wear", LaserProcessingMachine300ProfileFactory.ProfileId, "ToolCondition", Ap4ExpectedDirection.Decrease, 0.0001),
        new("sensor-drift", LaserProcessingMachine300ProfileFactory.ProfileId, "ElectricalStability", Ap4ExpectedDirection.Stable, 0.5),
        new("communication-drop", TechnicalLearningMachine300ProfileFactory.ProfileId, "ElectricalStability", Ap4ExpectedDirection.Stable, 0.5),
        new("signal-freeze", TechnicalLearningMachine300ProfileFactory.ProfileId, "ElectricalStability", Ap4ExpectedDirection.Stable, 0.5)
    ];

    private sealed record Ap4DirectionExpectation(
        string ScenarioId,
        string ProfileId,
        string HiddenStateId,
        Ap4ExpectedDirection ExpectedDirection,
        double MinimumDelta);
}

internal enum Ap4ExpectedDirection
{
    Increase,
    Decrease,
    Stable
}

internal sealed class Ap4PassEvaluation
{
    public bool Passed { get; init; }
    public List<string> FailedCriteria { get; init; } = [];
}

public sealed class Ap4CatalogValidationReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public string FaultScenariosDirectory { get; set; } = string.Empty;
    public int TotalScenarios { get; set; }
    public int EnabledScenarios { get; set; }
    public bool CatalogValid { get; set; }
    public List<string> CatalogErrors { get; set; } = [];
    public List<Ap4ScenarioProfileValidationResult> ProfileResults { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4ScenarioProfileValidationResult
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}

public sealed class Ap4ModelTestsReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public double AcceleratedTimeFactor { get; set; }
    public List<Ap4ModelTestResult> Results { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4ModelTestResult
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public Dictionary<string, double> BaselineHiddenStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> AfterHiddenStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> Deltas { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Ap4DirectionCheck> DirectionChecks { get; set; } = [];
    public int ActiveScenarioCount { get; set; }
    public string Phase { get; set; } = string.Empty;
    public long EngineTicks { get; set; }
    public bool Passed { get; set; }
    public string? Error { get; set; }
}

public sealed class Ap4DirectionCheck
{
    public string HiddenStateId { get; set; } = string.Empty;
    public string ExpectedDirection { get; set; } = string.Empty;
    public double MeasuredDelta { get; set; }
    public double MinimumDelta { get; set; }
    public bool Passed { get; set; }
}

public sealed class Ap4LifecycleTestsReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public List<Ap4LifecycleTestResult> Results { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4LifecycleTestResult
{
    public string ProfileId { get; set; } = string.Empty;
    public string ScenarioId { get; set; } = string.Empty;
    public bool StartAccepted { get; set; }
    public bool DuplicateStartRejected { get; set; }
    public bool PauseWorked { get; set; }
    public bool TicksWhilePausedUnchanged { get; set; }
    public bool ResumeWorked { get; set; }
    public bool StopStartedRecovery { get; set; }
    public bool CancelClearsScenario { get; set; }
    public bool ResetClearsScenario { get; set; }
    public bool Passed { get; set; }
}

public sealed class Ap4CombinationTestsReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public double AcceleratedTimeFactor { get; set; }
    public int ActiveScenarioCount { get; set; }
    public bool BothScenariosActive { get; set; }
    public double CoolantLossTargetDelta { get; set; }
    public double MaterialResistanceTargetDelta { get; set; }
    public bool CoolingEfficiencyDecreased { get; set; }
    public bool MaterialResistanceIncreased { get; set; }
    public bool ResetClearsActiveScenarios { get; set; }
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4RecoveryTestsReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public double AcceleratedTimeFactor { get; set; }
    public bool RecoveryStarted { get; set; }
    public bool RecoveryCompleted { get; set; }
    public double MaxRecoveryProgress { get; set; }
    public double EfficiencyDeltaDuringFault { get; set; }
    public double PressLoadDeltaDuringFault { get; set; }
    public double EfficiencyAccumulator { get; set; }
    public double PressLoadAccumulator { get; set; }
    public double EfficiencyDeltaAfterRecovery { get; set; }
    public bool EfficiencyImprovedAfterRecovery { get; set; }
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4ShortEndToEndReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public long TotalOpcUaUpdates { get; set; }
    public long TotalEngineTicks { get; set; }
    public List<Ap4EndToEndMachineReport> Machines { get; set; } = [];
    public List<Ap4EndToEndSample> Samples { get; set; } = [];
    public List<string> Exceptions { get; set; } = [];
    public bool Passed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
}

public sealed class Ap4EndToEndMachineReport
{
    public Guid MachineId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public List<string> ActiveScenarios { get; set; } = [];
    public long TotalPublishedUpdates { get; set; }
    public long EngineTicks { get; set; }
    public double? MechanicalLoad { get; set; }
    public double? ThermalLoad { get; set; }
    public double? HydraulicEfficiency { get; set; }
    public double? CoolingEfficiency { get; set; }
}

public sealed class Ap4EndToEndSample
{
    public DateTime TimestampUtc { get; set; }
    public Guid MachineId { get; set; }
    public string ProfileId { get; set; } = string.Empty;
    public int ActiveScenarioCount { get; set; }
    public double? MechanicalLoad { get; set; }
    public double? ThermalLoad { get; set; }
    public double? HydraulicEfficiency { get; set; }
    public double? CoolingEfficiency { get; set; }
    public long PublishedUpdates { get; set; }
    public long EngineTicks { get; set; }
}
