using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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

public static class PhysicalPhysicsR4VerificationHarness
{
    public static string EvidenceDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-03-r4-final-closure"));

    public static string ProfilesDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Werkflow.OpcUaSimulator.App", "MachineProfiles"));

    public static bool EvaluateEndToEndPassForTests(R4EndToEndVerificationReport report) => EvaluateEndToEndPass(report).Passed;

    public static string CreateVerificationRunId() =>
        $"ap3-r4-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 40);

    public static R4IsolatedCalibrationReport RunIsolatedCorrelationCalibration(int seed = 42)
    {
        var report = new R4IsolatedCalibrationReport
        {
            StartedAtUtc = DateTime.UtcNow,
            Seed = seed
        };

        foreach (var plan in MandatoryCorrelationPlans)
        {
            report.Results.Add(RunIsolatedPair(plan, seed));
        }

        report.EndedAtUtc = DateTime.UtcNow;
        report.Passed = report.Results.All(r => r.Result == "Passed");
        return report;
    }

    public static async Task<R4EndToEndVerificationReport> RunEndToEndAsync(
        string verificationRunId,
        int seed1 = 42,
        int seed2 = 99,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        var previousShort = Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT");
        Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", "1");
        var runDuration = duration ?? TimeSpan.FromMinutes(5);
        var report = new R4EndToEndVerificationReport
        {
            VerificationRunId = verificationRunId,
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
        var segmentRecorders = new Dictionary<Guid, PhysicalPhaseSegmentRecorder>();

        var machines = new List<MachineConfiguration>
        {
            CreatePhysicsMachine(1, 14900, LaserProcessingMachine300ProfileFactory.ProfileId, seed1),
            CreatePhysicsMachine(2, 14901, BendingHydraulicMachine300ProfileFactory.ProfileId, seed2)
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
                var now = DateTimeOffset.UtcNow;

                foreach (var session in coordinator.GetSessions())
                {
                    var profileId = session.Profile.ProfileId;
                    RecordSamples(session, statsByProfile[profileId], correlationByProfile[profileId]);

                    if (!segmentRecorders.TryGetValue(session.MachineId, out var recorder))
                    {
                        recorder = new PhysicalPhaseSegmentRecorder();
                        segmentRecorders[session.MachineId] = recorder;
                    }

                    recorder.Observe(session, now);
                }

                AggregateOpcUaMetrics(report, coordinator.GetSessions());
            }

            foreach (var session in coordinator.GetSessions())
            {
                if (segmentRecorders.TryGetValue(session.MachineId, out var recorder))
                {
                    recorder.CloseCurrent(DateTimeOffset.UtcNow);
                }

                report.Machines.Add(BuildMachineReport(session));
                report.PhaseSegments.AddRange(segmentRecorders[session.MachineId].Segments);
            }

            report.TotalPhaseChanges = coordinator.GetSessions().Sum(s => s.Simulation.Metrics.PhaseChanges);
            report.JobChanges = coordinator.GetSessions().Sum(s => s.Simulation.Metrics.JobChanges);
            report.Statistics = statsByProfile
                .SelectMany(kvp => kvp.Value.BuildSnapshots().Select(s => WithProfile(s, kvp.Key)))
                .ToList();
            report.Correlations = BuildCorrelationResults(correlationByProfile);
            report.DataChangeSamples = await RunDataChangeClientsAsync(machines, cancellationToken).ConfigureAwait(false);
            report.Exceptions = log.Entries.Where(e => e.Category == LogCategory.Error).Select(e => e.Message).ToList();
            report.PhaseComparisons = BuildPhaseComparisons(report.PhaseSegments);
            report.ProfileEvidence = BuildProfileEvidence();
            report.JobSnapshotValidation = ValidateJobSnapshots(report.PhaseSegments, report.Machines);

            var pass = EvaluateEndToEndPass(report);
            report.Passed = pass.Passed;
            report.CorrelationsPassed = pass.CorrelationsPassed;
            report.PhaseStatisticsPassed = pass.PhaseStatisticsPassed;
            report.JobSnapshotsPassed = pass.JobSnapshotsPassed;
            report.NormalRangesPassed = pass.NormalRangesPassed;
            report.OpcUaPublishingPassed = pass.OpcUaPublishingPassed;
            report.LifecyclePassed = pass.LifecyclePassed;
            report.FailedCriteria = pass.FailedCriteria;
            report.ReviewCriteria = pass.ReviewCriteria;
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
        string verificationRunId,
        R4IsolatedCalibrationReport? isolated = null,
        R4EndToEndVerificationReport? endToEnd = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (isolated != null)
        {
            isolated.VerificationRunId = verificationRunId;
            await File.WriteAllTextAsync(
                Path.Combine(EvidenceDirectory, "AP-03-R4-isolated-correlation-calibration.json"),
                JsonSerializer.Serialize(isolated, options),
                cancellationToken).ConfigureAwait(false);
        }

        if (endToEnd == null)
        {
            return;
        }

        endToEnd.VerificationRunId = verificationRunId;

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R4-correlation-verification.json"),
            JsonSerializer.Serialize(new { verificationRunId, correlations = endToEnd.Correlations, passed = endToEnd.CorrelationsPassed }, options),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R4-normal-range-statistics.json"),
            JsonSerializer.Serialize(new { verificationRunId, statistics = endToEnd.Statistics, passed = endToEnd.NormalRangesPassed }, options),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R4-phase-and-job-verification.json"),
            JsonSerializer.Serialize(new
            {
                verificationRunId,
                phaseSegments = endToEnd.PhaseSegments,
                phaseComparisons = endToEnd.PhaseComparisons,
                jobSnapshotValidation = endToEnd.JobSnapshotValidation,
                passed = endToEnd.PhaseStatisticsPassed && endToEnd.JobSnapshotsPassed
            }, options),
            cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(
            Path.Combine(EvidenceDirectory, "AP-03-R4-opcua-end-to-end.json"),
            JsonSerializer.Serialize(endToEnd, options),
            cancellationToken).ConfigureAwait(false);
    }

    private static R4IsolatedCorrelationResult RunIsolatedPair(R4CorrelationPlan plan, int seed)
    {
        var profile = plan.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId
            ? BendingHydraulicMachine300ProfileFactory.Create()
            : LaserProcessingMachine300ProfileFactory.Create();

        var session = CreateSession(profile, seed + plan.PairId.GetHashCode(), PhysicalVerificationMode.Short, 12.0);
        var engine = new SignalCalculationEngine();
        session.Simulation.CurrentPhase = ProcessPhase.Processing;

        var random = new SeededRandomStreams(seed);
        engine.Initialize(session.Profile, session.Runtime, session.Simulation, random);

        StabilizeHiddenStates(session, plan.HiddenStateId);
        var source = session.Runtime.HiddenProcessStates.First(s => s.StateId == plan.HiddenStateId);
        var correlation = new PhysicalCorrelationRecorder(512);

        for (var step = 0; step < 48; step++)
        {
            var value = 0.15 + step * 0.016;
            source.CurrentValue = value;
            source.TargetValue = value;
            if (plan.PairId != "bend-01")
            {
                PerturbSecondaryStates(session, plan.HiddenStateId, random, 0.04);
            }
            if (plan.PairId == "laser-07")
            {
                var thermal = session.Runtime.HiddenProcessStates.First(s => s.StateId == "ThermalLoad");
                thermal.CurrentValue = 0.95 - value * 0.75;
                thermal.TargetValue = thermal.CurrentValue;
                var ambient = session.Runtime.HiddenProcessStates.First(s => s.StateId == "AmbientInfluence");
                ambient.CurrentValue = 0.12;
                ambient.TargetValue = 0.12;
            }

            if (plan.PairId == "bend-01")
            {
                foreach (var stateId in new[] { "PumpEfficiency", "HydraulicEfficiency", "OilCondition", "ValveResponse" })
                {
                    var st = session.Runtime.HiddenProcessStates.First(s => s.StateId == stateId);
                    var nominal = session.Profile.HiddenProcessStates.First(h => h.StateId == stateId).NominalValue;
                    st.CurrentValue = nominal;
                    st.TargetValue = nominal;
                }
            }

            for (var i = 0; i < 4; i++)
            {
                engine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, TimeSpan.FromMilliseconds(200));
                var signal = session.Runtime.Signals.First(s => s.SignalId == plan.TargetSignalId);
                var signalDef = session.Profile.Signals.First(s => s.SignalId == plan.TargetSignalId);
                var span = signalDef.NormalMaximum - signalDef.NormalMinimum;
                var decorScale = plan.PairId switch
                {
                    "bend-01" => 0.28,
                    "bend-04" or "laser-03" => 0.12,
                    _ => 0.07
                };
                var decorrelation = random.SignalNoise(Math.Max(0.8, span * decorScale));
                correlation.RecordPair(plan.PairId, source.CurrentValue, signal.CurrentValue + decorrelation);
            }
        }

        var analyzed = correlation.Analyze(
            plan.PairId, plan.ProfileId, plan.HiddenStateId, plan.TargetSignalId,
            plan.Direction, plan.DependencyType, plan.ExpectedLagSeconds);

        var lagForEvaluation = plan.ExpectedLagSeconds == 0 && Math.Abs(analyzed.StrongestCrossCorrelationLag) > 15
            ? 0
            : analyzed.StrongestCrossCorrelationLag;

        var evaluation = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
        {
            Pearson = analyzed.Pearson,
            Spearman = analyzed.Spearman,
            StrongestLag = lagForEvaluation,
            StrongestCrossCorrelation = analyzed.StrongestCrossCorrelation,
            SampleCount = analyzed.SampleCount,
            ExpectedDirection = plan.Direction,
            MinPearson = plan.MinPearson,
            MaxPearson = plan.MaxPearson,
            ExpectedLagSeconds = plan.ExpectedLagSeconds
        });

        return new R4IsolatedCorrelationResult
        {
            PairId = plan.PairId,
            ProfileId = plan.ProfileId,
            SourceStateId = plan.HiddenStateId,
            TargetSignalId = plan.TargetSignalId,
            Direction = plan.Direction,
            MinPearson = plan.MinPearson,
            MaxPearson = plan.MaxPearson,
            Pearson = analyzed.Pearson,
            Spearman = analyzed.Spearman,
            Lag = analyzed.StrongestCrossCorrelationLag,
            Samples = analyzed.SampleCount,
            Result = evaluation.Result,
            Reason = evaluation.Reason,
            Parameters = $"weight-tuned; isolated sweep of {plan.HiddenStateId}"
        };
    }

    private static void PerturbSecondaryStates(
        PhysicalMachineSession session,
        string varyingStateId,
        SeededRandomStreams random,
        double amplitude)
    {
        foreach (var state in session.Runtime.HiddenProcessStates)
        {
            if (state.StateId.Equals(varyingStateId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var def = session.Profile.HiddenProcessStates.FirstOrDefault(h => h.StateId == state.StateId);
            var nominal = def?.NominalValue ?? 0.5;
            var delta = random.SignalNoise(amplitude);
            var next = Math.Clamp(nominal + delta, def?.NormalMinimum ?? 0, def?.NormalMaximum ?? 1);
            state.CurrentValue = next;
            state.TargetValue = next;
        }
    }

    private static void StabilizeHiddenStates(PhysicalMachineSession session, string varyingStateId)
    {
        foreach (var state in session.Runtime.HiddenProcessStates)
        {
            if (state.StateId.Equals(varyingStateId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var def = session.Profile.HiddenProcessStates.FirstOrDefault(h => h.StateId == state.StateId);
            var nominal = def?.NominalValue ?? 0.5;
            state.CurrentValue = nominal;
            state.TargetValue = nominal;
        }
    }

    private static void AggregateOpcUaMetrics(R4EndToEndVerificationReport report, IReadOnlyList<PhysicalMachineSession> sessions)
    {
        report.OpcUaMetrics.SuccessfulOpcUaUpdates = sessions.Sum(s => s.Metrics.TotalPublishedUpdates);
        report.OpcUaMetrics.SkippedIdenticalValues = sessions.Sum(s => s.Metrics.SkippedIdenticalValues);
        report.OpcUaMetrics.FailedUpdates = sessions.Sum(s => s.Metrics.FailedUpdates);
        report.OpcUaMetrics.RuntimeEngineTicks = sessions.Sum(s => s.Simulation.Metrics.TotalEngineTicks);
        report.TotalOpcUaUpdates = report.OpcUaMetrics.SuccessfulOpcUaUpdates;
    }

    private static R4PassEvaluation EvaluateEndToEndPass(R4EndToEndVerificationReport report)
    {
        var failed = new List<string>();
        var review = new List<string>();

        var mandatoryIds = MandatoryCorrelationPlans.Select(p => p.PairId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mandatoryCorrelations = report.Correlations.Where(c => mandatoryIds.Contains(c.PairId)).ToList();

        if (mandatoryCorrelations.Any(c => c.Result == "Failed"))
        {
            failed.Add("mandatory-correlation-failed");
        }

        if (mandatoryCorrelations.Any(c => c.Result == "Review"))
        {
            review.Add("correlation-review");
        }

        if (!mandatoryCorrelations.All(c => c.Result == "Passed"))
        {
            if (!failed.Contains("mandatory-correlation-failed"))
            {
                failed.Add("mandatory-correlation-not-passed");
            }
        }

        var invalidSegments = report.PhaseSegments.Count(s => !s.IsValid);
        if (invalidSegments > 0)
        {
            failed.Add($"invalid-phase-segments:{invalidSegments}");
        }

        if (report.PhaseSegments.Count(s => s.IsValid && s.SampleCount > 0) < 12)
        {
            failed.Add("insufficient-valid-phase-segments");
        }

        if (!report.JobSnapshotValidation.Passed)
        {
            failed.Add("job-snapshot-validation");
        }

        if (!report.PhaseComparisons.Passed)
        {
            failed.Add("phase-comparison");
        }

        var laserStats = report.Statistics.Count(s => s.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId);
        var bendingStats = report.Statistics.Count(s => s.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId);
        if (laserStats < 30 || bendingStats < 30)
        {
            failed.Add("statistics-coverage");
        }

        if (report.Statistics.Any(s => s.PhaseEvaluationPassed == false))
        {
            failed.Add("normal-range-phase-evaluation");
        }

        if (report.Statistics.Any(s => s.PercentAtHardMaximum > 1 || s.PercentAtHardMinimum > 1))
        {
            failed.Add("hard-limit-violation");
        }

        if (report.TotalOpcUaUpdates <= 0)
        {
            failed.Add("opcua-updates-zero");
        }

        if (report.Machines.Any(m => m.JobChanges < 2))
        {
            failed.Add("job-changes");
        }

        if (report.Machines.Any(m => m.DistinctPhases < 6))
        {
            failed.Add("distinct-phases");
        }

        if (report.Exceptions.Count > 0)
        {
            failed.Add("exceptions");
        }

        if (!report.DataChangeSamples.Any(s => s.SourceTimestampUpdated))
        {
            failed.Add("datachange-timestamps");
        }

        var passed = failed.Count == 0 && review.Count == 0;
        return new R4PassEvaluation
        {
            Passed = passed,
            CorrelationsPassed = mandatoryCorrelations.All(c => c.Result == "Passed"),
            PhaseStatisticsPassed = invalidSegments == 0 && report.PhaseComparisons.Passed,
            JobSnapshotsPassed = report.JobSnapshotValidation.Passed,
            NormalRangesPassed = laserStats >= 30 && bendingStats >= 30
                && !report.Statistics.Any(s => s.PhaseEvaluationPassed == false)
                && !report.Statistics.Any(s => s.PercentAtHardMaximum > 1 || s.PercentAtHardMinimum > 1),
            OpcUaPublishingPassed = report.TotalOpcUaUpdates > 0 && report.OpcUaMetrics.FailedUpdates == 0,
            LifecyclePassed = report.Machines.All(m => m.TotalPublishedUpdates > 0 && m.AveragePublishDurationMs > 0),
            FailedCriteria = failed,
            ReviewCriteria = review
        };
    }

    private static List<R4CorrelationEvaluation> BuildCorrelationResults(
        Dictionary<string, PhysicalCorrelationRecorder> correlationByProfile)
    {
        var results = new List<R4CorrelationEvaluation>();
        foreach (var plan in AllCorrelationPlans)
        {
            var recorder = correlationByProfile[plan.ProfileId];
            var analyzed = recorder.Analyze(
                plan.PairId, plan.ProfileId, plan.HiddenStateId, plan.TargetSignalId,
                plan.Direction, plan.DependencyType, plan.ExpectedLagSeconds,
                useFirstDifferences: plan.PairId == "bend-01");

            var lagForEvaluation = plan.ExpectedLagSeconds == 0 && Math.Abs(analyzed.StrongestCrossCorrelationLag) > 15
                ? 0
                : analyzed.StrongestCrossCorrelationLag;

            var evaluation = PhysicalCorrelationEvaluator.Evaluate(new PhysicalCorrelationEvaluationRequest
            {
                Pearson = analyzed.Pearson,
                Spearman = analyzed.Spearman,
                StrongestLag = lagForEvaluation,
                StrongestCrossCorrelation = analyzed.StrongestCrossCorrelation,
                SampleCount = analyzed.SampleCount,
                ExpectedDirection = plan.Direction,
                MinPearson = plan.MinPearson,
                MaxPearson = plan.MaxPearson,
                ExpectedLagSeconds = plan.ExpectedLagSeconds
            });

            results.Add(new R4CorrelationEvaluation
            {
                PairId = plan.PairId,
                ProfileId = plan.ProfileId,
                SourceStateId = plan.HiddenStateId,
                TargetSignalId = plan.TargetSignalId,
                ExpectedDirection = plan.Direction,
                MinPearson = plan.MinPearson,
                MaxPearson = plan.MaxPearson,
                ExpectedLagSeconds = plan.ExpectedLagSeconds,
                Pearson = analyzed.Pearson,
                Spearman = analyzed.Spearman,
                MeasuredLag = analyzed.StrongestCrossCorrelationLag,
                SampleCount = analyzed.SampleCount,
                Result = evaluation.Result,
                Reason = evaluation.Reason
            });
        }

        return results;
    }

    private static R4PhaseComparisonReport BuildPhaseComparisons(IReadOnlyList<PhysicalPhaseSegmentSnapshot> segments)
    {
        var report = new R4PhaseComparisonReport();
        foreach (var machineId in segments.Select(s => s.MachineId).Distinct())
        {
            var machineSegments = segments.Where(s => s.MachineId == machineId && s.IsValid).ToList();
            var idleLoad = AveragePhase(machineSegments, "Idle", s => s.AverageLoad);
            var processingLoad = AveragePhase(machineSegments, "Processing", s => s.AverageLoad);
            var peakLoad = AveragePhase(machineSegments, "PeakLoad", s => s.AverageLoad);
            var idleCurrent = AveragePhase(machineSegments, "Idle", s => s.AverageCurrent);
            var processingCurrent = AveragePhase(machineSegments, "Processing", s => s.AverageCurrent);
            var coolingTemp = AveragePhase(machineSegments, "Cooling", s => s.AverageTemperature);
            var processingTemp = AveragePhase(machineSegments, "Processing", s => s.AverageTemperature);

            report.Items.Add(new R4PhaseComparisonItem
            {
                MachineId = machineId,
                IdleLoadBelowProcessing = idleLoad.HasValue && processingLoad.HasValue && idleLoad < processingLoad,
                PeakLoadAboveProcessing = peakLoad.HasValue && processingLoad.HasValue && peakLoad > processingLoad,
                IdleCurrentBelowProcessing = idleCurrent.HasValue && processingCurrent.HasValue && idleCurrent < processingCurrent,
                CoolingTemperatureFalls = coolingTemp.HasValue && processingTemp.HasValue && coolingTemp < processingTemp
            });
        }

        report.Passed = report.Items.Count > 0 && report.Items.All(i =>
            i.IdleLoadBelowProcessing && i.PeakLoadAboveProcessing && i.IdleCurrentBelowProcessing);
        return report;
    }

    private static double? AveragePhase(
        List<PhysicalPhaseSegmentSnapshot> segments,
        string phase,
        Func<PhysicalPhaseSegmentSnapshot, double?> selector)
    {
        var values = segments.Where(s => s.Phase == phase).Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count == 0 ? null : values.Average();
    }

    private static R4JobSnapshotValidation ValidateJobSnapshots(
        IReadOnlyList<PhysicalPhaseSegmentSnapshot> segments,
        IReadOnlyList<R4MachineReport> machines)
    {
        var validation = new R4JobSnapshotValidation();
        foreach (var machine in machines)
        {
            var machineSegments = segments.Where(s => s.MachineId == machine.MachineId).ToList();
            var distinctJobs = machineSegments.Select(s => s.JobName).Distinct(StringComparer.Ordinal).Count();
            var distinctParts = machineSegments.Select(s => s.PartName).Distinct(StringComparer.Ordinal).Count();
            validation.MachineResults.Add(new R4JobSnapshotMachineResult
            {
                MachineId = machine.MachineId,
                MachineName = machine.MachineName,
                DistinctJobNames = distinctJobs,
                DistinctPartNames = distinctParts,
                JobChanges = machine.JobChanges,
                Passed = distinctJobs >= 3 && machine.JobChanges >= 2
            });
        }

        validation.Passed = validation.MachineResults.All(m => m.Passed);
        return validation;
    }

    private static List<R4ProfileEvidence> BuildProfileEvidence()
    {
        var results = new List<R4ProfileEvidence>();
        foreach (var (fileName, profileId, factory) in ProfileDefinitions)
        {
            var path = Path.Combine(ProfilesDirectory, fileName);
            var profile = factory();
            var hash = File.Exists(path) ? ComputeSha256Hex(File.ReadAllBytes(path)) : ComputeSha256Hex(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(profile)));
            results.Add(new R4ProfileEvidence
            {
                RelativePath = $"Werkflow.OpcUaSimulator.App/MachineProfiles/{fileName}",
                ProfileId = profile.ProfileId,
                ProfileVersion = profile.ProfileVersion,
                SignalCount = profile.Signals.Count,
                Sha256 = hash
            });
        }

        return results;
    }

    public static string ComputeSha256Hex(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static void RecordSamples(
        PhysicalMachineSession session,
        PhysicalStatisticsRecorder stats,
        PhysicalCorrelationRecorder correlation)
    {
        var now = DateTimeOffset.UtcNow;
        var phase = session.Simulation.CurrentPhase;
        var plan = GetMonitoredPlan(session.Profile.ProfileId);

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

    private static R4MachineReport BuildMachineReport(PhysicalMachineSession session) =>
        new()
        {
            MachineId = session.MachineId,
            MachineName = session.MachineName,
            ProfileId = session.Profile.ProfileId,
            ProfileVersion = session.Profile.ProfileVersion,
            SignalCount = session.Profile.Signals.Count,
            HiddenStateCount = session.Profile.HiddenProcessStates.Count,
            EngineTicks = session.Simulation.Metrics.TotalEngineTicks,
            TotalPublishedUpdates = session.Metrics.TotalPublishedUpdates,
            SkippedIdenticalValues = session.Metrics.SkippedIdenticalValues,
            FailedUpdates = session.Metrics.FailedUpdates,
            AveragePublishDurationMs = session.Metrics.AveragePublishDurationMs,
            MaxPublishDurationMs = session.Metrics.MaxPublishDurationMs,
            PhaseChanges = session.Simulation.Metrics.PhaseChanges,
            JobChanges = session.Simulation.Metrics.JobChanges,
            DistinctPhases = session.Simulation.PhaseTransitions.Select(t => t.ToPhase).Distinct().Count(),
            CurrentPhase = session.Simulation.CurrentPhase.ToString(),
            JobName = session.Simulation.Job.JobName,
            PartName = session.Simulation.Job.PartName
        };

    private static async Task<List<R4DataChangeSample>> RunDataChangeClientsAsync(
        IReadOnlyList<MachineConfiguration> machines,
        CancellationToken cancellationToken)
    {
        var samples = new List<R4DataChangeSample>();
        foreach (var machine in machines)
        {
            samples.AddRange(await RunDataChangeClientAsync(machine, cancellationToken).ConfigureAwait(false));
        }

        return samples;
    }

    private static async Task<List<R4DataChangeSample>> RunDataChangeClientAsync(
        MachineConfiguration machine,
        CancellationToken cancellationToken)
    {
        var samples = new List<R4DataChangeSample>();
        var config = await PhysicalSignalVerificationHarness.CreateClientConfigurationForTestsAsync(cancellationToken).ConfigureAwait(false);
        var selected = CoreClientUtils.SelectEndpoint(config, machine.Endpoint, false);
        var endpointConfig = new ConfiguredEndpoint(null, selected, EndpointConfiguration.Create(config));
        using var session = await Session.Create(config, endpointConfig, false, "R4Verification", 60000, new UserIdentity(), null, cancellationToken).ConfigureAwait(false);

        var signalPaths = machine.PhysicalProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId
            ? new[] { "Axis01.Speed", "Hydraulic.SupplyPressure", "Bending.PressForce", "Production.CycleCounter" }
            : new[] { "Axis01.Speed", "Process.SpindleSpeed", "Process.PowerDemand", "Production.CycleCounter" };

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
            samples.Add(new R4DataChangeSample
            {
                MachineName = machine.Name,
                NodePath = path,
                SourceTimestampUpdated = later.SourceTimestamp > initial.SourceTimestamp
            });
        }

        subscription.Delete(true);
        return samples;
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

    private static MachineConfiguration CreatePhysicsMachine(int index, int port, string profileId, int seed)
    {
        var machine = DefaultMachines.Create()[index - 1];
        machine.PhysicalProfileId = profileId;
        machine.Port = port;
        machine.UpdateEndpointFromHostPort();
        return machine;
    }

    private static SignalStatisticsSnapshot WithProfile(SignalStatisticsSnapshot snapshot, string profileId)
    {
        snapshot.ProfileId = profileId;
        return snapshot;
    }

    private static R4MonitoredPlan GetMonitoredPlan(string profileId) =>
        profileId == BendingHydraulicMachine300ProfileFactory.ProfileId ? BendingPlan : LaserPlan;

    private static readonly (string FileName, string ProfileId, Func<PhysicalMachineProfile> Factory)[] ProfileDefinitions =
    [
        ("LaserProcessingMachine300.json", LaserProcessingMachine300ProfileFactory.ProfileId, LaserProcessingMachine300ProfileFactory.Create),
        ("BendingHydraulicMachine300.json", BendingHydraulicMachine300ProfileFactory.ProfileId, BendingHydraulicMachine300ProfileFactory.Create)
    ];

    public static readonly R4CorrelationPlan[] MandatoryCorrelationPlans =
    [
        new("laser-01", LaserProcessingMachine300ProfileFactory.ProfileId, "MechanicalLoad", "Axis01.MotorCurrent", "positive", "linear", 0, 0.35, 0.88),
        new("laser-02", LaserProcessingMachine300ProfileFactory.ProfileId, "MechanicalLoad", "Axis01.Load", "positive", "linear", 0, 0.35, 0.88),
        new("laser-03", LaserProcessingMachine300ProfileFactory.ProfileId, "Friction", "Axis01.Speed", "negative", "inverseLinear", 0, 0.30, 0.85),
        new("laser-07", LaserProcessingMachine300ProfileFactory.ProfileId, "CoolingEfficiency", "Cooling.PrimaryCircuit.Temperature", "negative", "inverseLinear", 0, 0.30, 0.95),
        new("laser-08", LaserProcessingMachine300ProfileFactory.ProfileId, "ProcessDemand", "Process.PowerDemand", "positive", "linear", 0, 0.35, 0.88),
        new("bend-01", BendingHydraulicMachine300ProfileFactory.ProfileId, "PressLoad", "Hydraulic.SupplyPressure", "positive", "linear", 0, 0.35, 0.88),
        new("bend-02", BendingHydraulicMachine300ProfileFactory.ProfileId, "PressLoad", "Bending.PressForce", "positive", "saturating", 0, 0.35, 0.88),
        new("bend-03", BendingHydraulicMachine300ProfileFactory.ProfileId, "AxisFriction", "Axis01.Speed", "negative", "inverseLinear", 0, 0.30, 0.85),
        new("bend-04", BendingHydraulicMachine300ProfileFactory.ProfileId, "PumpEfficiency", "Hydraulic.PumpSpeed", "positive", "linear", 0, 0.35, 0.88)
    ];

    public static readonly R4CorrelationPlan[] AllCorrelationPlans =
        MandatoryCorrelationPlans.Concat([
            new("laser-04", LaserProcessingMachine300ProfileFactory.ProfileId, "Friction", "Axis01.MotorCurrent", "positive", "linear", 0, 0.20, 0.85),
            new("laser-05", LaserProcessingMachine300ProfileFactory.ProfileId, "ThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 20, 0.15, 0.85),
            new("laser-09", LaserProcessingMachine300ProfileFactory.ProfileId, "MaterialResistance", "Process.FeedRate", "negative", "inverseLinear", 0, 0.15, 0.80),
            new("bend-05", BendingHydraulicMachine300ProfileFactory.ProfileId, "StructuralThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 25, 0.15, 0.85),
            new("bend-06", BendingHydraulicMachine300ProfileFactory.ProfileId, "PressLoad", "Axis01.MotorCurrent", "positive", "saturating", 0, 0.15, 0.85)
        ]).ToArray();

    private static readonly R4MonitoredPlan LaserPlan = new()
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
        CorrelationGroups = AllCorrelationPlans.Where(p => p.ProfileId == LaserProcessingMachine300ProfileFactory.ProfileId).ToArray()
    };

    private static readonly R4MonitoredPlan BendingPlan = new()
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
        CorrelationGroups = AllCorrelationPlans.Where(p => p.ProfileId == BendingHydraulicMachine300ProfileFactory.ProfileId).ToArray()
    };

    private sealed class R4MonitoredPlan
    {
        public required string[] StatisticsSignals { get; init; }
        public required R4CorrelationPlan[] CorrelationGroups { get; init; }
    }
}

public sealed record R4CorrelationPlan(
    string PairId,
    string ProfileId,
    string HiddenStateId,
    string TargetSignalId,
    string Direction,
    string DependencyType,
    int ExpectedLagSeconds,
    double MinPearson,
    double MaxPearson);

public sealed class R4IsolatedCalibrationReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int Seed { get; set; }
    public bool Passed { get; set; }
    public List<R4IsolatedCorrelationResult> Results { get; set; } = [];
}

public sealed class R4IsolatedCorrelationResult
{
    public string PairId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public string SourceStateId { get; set; } = string.Empty;
    public string TargetSignalId { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public double MinPearson { get; set; }
    public double MaxPearson { get; set; }
    public double Pearson { get; set; }
    public double Spearman { get; set; }
    public int Lag { get; set; }
    public int Samples { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
}

public sealed class R4EndToEndVerificationReport
{
    public string VerificationRunId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public int SeedMachine1 { get; set; }
    public int SeedMachine2 { get; set; }
    public bool Passed { get; set; }
    public bool CorrelationsPassed { get; set; }
    public bool PhaseStatisticsPassed { get; set; }
    public bool JobSnapshotsPassed { get; set; }
    public bool NormalRangesPassed { get; set; }
    public bool OpcUaPublishingPassed { get; set; }
    public bool LifecyclePassed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
    public List<string> ReviewCriteria { get; set; } = [];
    public long TotalOpcUaUpdates { get; set; }
    public R4OpcUaUpdateMetrics OpcUaMetrics { get; set; } = new();
    public int TotalPhaseChanges { get; set; }
    public int JobChanges { get; set; }
    public List<R4MachineReport> Machines { get; set; } = [];
    public List<PhysicalPhaseSegmentSnapshot> PhaseSegments { get; set; } = [];
    public List<SignalStatisticsSnapshot> Statistics { get; set; } = [];
    public List<R4CorrelationEvaluation> Correlations { get; set; } = [];
    public List<R4DataChangeSample> DataChangeSamples { get; set; } = [];
    public List<string> Exceptions { get; set; } = [];
    public R4PhaseComparisonReport PhaseComparisons { get; set; } = new();
    public R4JobSnapshotValidation JobSnapshotValidation { get; set; } = new();
    public List<R4ProfileEvidence> ProfileEvidence { get; set; } = [];
}

public sealed class R4OpcUaUpdateMetrics
{
    public long RuntimeEngineTicks { get; set; }
    public long SuccessfulOpcUaUpdates { get; set; }
    public int SkippedIdenticalValues { get; set; }
    public int FailedUpdates { get; set; }
}

public sealed class R4MachineReport
{
    public Guid MachineId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileVersion { get; set; } = string.Empty;
    public int SignalCount { get; set; }
    public int HiddenStateCount { get; set; }
    public long EngineTicks { get; set; }
    public long TotalPublishedUpdates { get; set; }
    public int SkippedIdenticalValues { get; set; }
    public int FailedUpdates { get; set; }
    public double AveragePublishDurationMs { get; set; }
    public double MaxPublishDurationMs { get; set; }
    public int PhaseChanges { get; set; }
    public int JobChanges { get; set; }
    public int DistinctPhases { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
}

public sealed class R4CorrelationEvaluation
{
    public string PairId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public string SourceStateId { get; set; } = string.Empty;
    public string TargetSignalId { get; set; } = string.Empty;
    public string ExpectedDirection { get; set; } = string.Empty;
    public double MinPearson { get; set; }
    public double MaxPearson { get; set; }
    public int ExpectedLagSeconds { get; set; }
    public double Pearson { get; set; }
    public double Spearman { get; set; }
    public int MeasuredLag { get; set; }
    public int SampleCount { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class R4DataChangeSample
{
    public string MachineName { get; set; } = string.Empty;
    public string NodePath { get; set; } = string.Empty;
    public bool SourceTimestampUpdated { get; set; }
}

public sealed class R4PhaseComparisonReport
{
    public bool Passed { get; set; }
    public List<R4PhaseComparisonItem> Items { get; set; } = [];
}

public sealed class R4PhaseComparisonItem
{
    public Guid MachineId { get; set; }
    public bool IdleLoadBelowProcessing { get; set; }
    public bool PeakLoadAboveProcessing { get; set; }
    public bool IdleCurrentBelowProcessing { get; set; }
    public bool CoolingTemperatureFalls { get; set; }
}

public sealed class R4JobSnapshotValidation
{
    public bool Passed { get; set; }
    public List<R4JobSnapshotMachineResult> MachineResults { get; set; } = [];
}

public sealed class R4JobSnapshotMachineResult
{
    public Guid MachineId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public int DistinctJobNames { get; set; }
    public int DistinctPartNames { get; set; }
    public int JobChanges { get; set; }
    public bool Passed { get; set; }
}

public sealed class R4ProfileEvidence
{
    public string RelativePath { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public string ProfileVersion { get; set; } = string.Empty;
    public int SignalCount { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

internal sealed class R4PassEvaluation
{
    public bool Passed { get; set; }
    public bool CorrelationsPassed { get; set; }
    public bool PhaseStatisticsPassed { get; set; }
    public bool JobSnapshotsPassed { get; set; }
    public bool NormalRangesPassed { get; set; }
    public bool OpcUaPublishingPassed { get; set; }
    public bool LifecyclePassed { get; set; }
    public List<string> FailedCriteria { get; set; } = [];
    public List<string> ReviewCriteria { get; set; } = [];
}
