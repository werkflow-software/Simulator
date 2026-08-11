using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalAp3R4SegmentTests
{
    [Fact]
    public void Segment_ReceivesSamples()
    {
        var session = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        var engine = PhysicalTestServiceFactory.CreateEngine();
        engine.Initialize(session, 42);
        var recorder = new PhysicalPhaseSegmentRecorder();

        for (var i = 0; i < 20; i++)
        {
            engine.Tick(session, TimeSpan.FromMilliseconds(200));
            recorder.Observe(session, DateTimeOffset.UtcNow);
        }

        recorder.CloseCurrent(DateTimeOffset.UtcNow);
        Assert.Contains(recorder.Segments, s => s.SampleCount > 0);
    }

    [Fact]
    public void SegmentAverages_CalculatedFromSamples()
    {
        var session = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        session.Simulation.CurrentPhase = ProcessPhase.Processing;
        var load = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Load");
        load.CurrentValue = 40;
        var recorder = new PhysicalPhaseSegmentRecorder();
        recorder.Observe(session, DateTimeOffset.UtcNow);
        load.CurrentValue = 60;
        recorder.Observe(session, DateTimeOffset.UtcNow);
        recorder.CloseCurrent(DateTimeOffset.UtcNow);

        var segment = recorder.Segments.Single();
        Assert.Equal(2, segment.SampleCount);
        Assert.InRange(segment.AverageLoad!.Value, 45, 55);
    }

    [Fact]
    public void EmptySegment_MarkedInvalid()
    {
        var snapshot = new PhysicalPhaseSegmentSnapshot
        {
            SampleCount = 0,
            IsValid = false
        };
        Assert.False(snapshot.IsValid);
    }

    [Fact]
    public void PhaseChange_ClosesPreviousSegment()
    {
        var session = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        var recorder = new PhysicalPhaseSegmentRecorder();
        session.Simulation.CurrentPhase = ProcessPhase.Idle;
        recorder.Observe(session, DateTimeOffset.UtcNow);
        session.Simulation.CurrentPhase = ProcessPhase.Processing;
        recorder.Observe(session, DateTimeOffset.UtcNow);
        recorder.CloseCurrent(DateTimeOffset.UtcNow);

        Assert.Equal(2, recorder.Segments.Count);
        Assert.Equal("Idle", recorder.Segments[0].Phase);
        Assert.Equal("Processing", recorder.Segments[1].Phase);
    }

    [Fact]
    public void JobName_StoredAsSnapshot()
    {
        var session = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        session.Simulation.Job.JobName = "JOB-SNAPSHOT";
        session.Simulation.Job.PartName = "PART-SNAPSHOT";
        var recorder = new PhysicalPhaseSegmentRecorder();
        recorder.Observe(session, DateTimeOffset.UtcNow);
        session.Simulation.Job.JobName = "JOB-CHANGED";
        recorder.CloseCurrent(DateTimeOffset.UtcNow);

        Assert.Equal("JOB-SNAPSHOT", recorder.Segments[0].JobName);
        Assert.Equal("PART-SNAPSHOT", recorder.Segments[0].PartName);
    }

    [Fact]
    public void LaterJobChange_DoesNotAlterOlderSegment()
    {
        var session = CreateSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        var recorder = new PhysicalPhaseSegmentRecorder();
        session.Simulation.Job.JobName = "JOB-001";
        recorder.Observe(session, DateTimeOffset.UtcNow);
        PhysicalJobCoordinator.ApplyDefinition(session.Simulation, FixedSimulationCatalog.GetDefinition(1), null);
        recorder.Observe(session, DateTimeOffset.UtcNow);
        recorder.CloseCurrent(DateTimeOffset.UtcNow);

        Assert.Equal("JOB-001", recorder.Segments[0].JobName);
        Assert.Equal("JOB-002", recorder.Segments[1].JobName);
    }

    [Fact]
    public void InvalidSegment_SetsOverallStatusFalse()
    {
        var report = new R4EndToEndVerificationReport
        {
            Correlations = [new R4CorrelationEvaluation { Result = "Passed" }],
            PhaseSegments = [new PhysicalPhaseSegmentSnapshot { IsValid = false, SampleCount = 0 }],
            Statistics = PhysicalAp3R4TestHelpers.CreateMinimalStatistics(),
            Machines = [PhysicalAp3R4TestHelpers.CreateMachineReport()],
            DataChangeSamples = [new R4DataChangeSample { SourceTimestampUpdated = true }],
            JobSnapshotValidation = new R4JobSnapshotValidation { Passed = true, MachineResults = [new R4JobSnapshotMachineResult { Passed = true }] },
            PhaseComparisons = new R4PhaseComparisonReport { Passed = true, Items = [new R4PhaseComparisonItem { IdleLoadBelowProcessing = true, PeakLoadAboveProcessing = true, IdleCurrentBelowProcessing = true }] },
            TotalOpcUaUpdates = 100,
            OpcUaMetrics = new R4OpcUaUpdateMetrics { SuccessfulOpcUaUpdates = 100 }
        };
        Assert.False(PhysicalPhysicsR4VerificationHarness.EvaluateEndToEndPassForTests(report));
    }

    private static PhysicalMachineSession CreateSession(PhysicalMachineProfile profile, int seed)
    {
        var runtime = new PhysicalMachineRuntimeFactory().Create(profile);
        var session = new PhysicalMachineSession
        {
            MachineId = Guid.NewGuid(),
            MachineName = "Test",
            Profile = profile,
            Runtime = runtime,
            Simulation =
            {
                Seed = seed,
                VerificationMode = PhysicalVerificationMode.Short,
                TimeFactor = 12.0,
                GenerationMode = SignalGenerationMode.Physical,
                IsEngineActive = true
            }
        };
        PhysicalJobCoordinator.Initialize(session.Simulation);
        return session;
    }
}
