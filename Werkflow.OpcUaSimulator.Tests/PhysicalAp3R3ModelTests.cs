using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalAp3R3ModelTests
{
    [Fact]
    public void SupplyPressure_ReactsPositivelyToPressLoad()
    {
        var report = PhysicalPhysicsR3VerificationHarness.RunModelVerification(42);
        var check = report.DependencyChecks.First(c => c.Pair.Contains("PressLoad → Hydraulic.SupplyPressure"));
        Assert.True(check.Passed, $"low={check.LowValue}, high={check.HighValue}");
    }

    [Fact]
    public void SpindleSpeed_HasPlausibleMagnitude()
    {
        var engine = PhysicalTestServiceFactory.CreateEngine();
        var session = CreateShortSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        engine.Initialize(session, 42);
        session.Simulation.CurrentPhase = ProcessPhase.Processing;

        for (var i = 0; i < 200; i++)
        {
            session.Simulation.CurrentPhase = ProcessPhase.Processing;
            engine.Tick(session, TimeSpan.FromMilliseconds(200));
        }

        var spindle = session.Runtime.Signals.First(s => s.SignalId == "Process.SpindleSpeed").CurrentValue;
        Assert.InRange(spindle, 2900, 3100);
    }

    [Fact]
    public void QualityIndex_IsNotPermanently100()
    {
        var report = PhysicalPhysicsR3VerificationHarness.RunModelVerification(42);
        var check = report.DependencyChecks.First(c => c.Pair.Contains("QualityIndex"));
        Assert.True(check.Passed, $"min={check.LowValue}, max={check.HighValue}");
    }

    [Fact]
    public void ProcessingMotorTemperature_WithinExpectedBand()
    {
        var engine = PhysicalTestServiceFactory.CreateEngine();
        var session = CreateShortSession(LaserProcessingMachine300ProfileFactory.Create(), 42);
        engine.Initialize(session, 42);

        for (var i = 0; i < 500; i++)
        {
            engine.Tick(session, TimeSpan.FromMilliseconds(200));
            if (session.Simulation.CurrentPhase == ProcessPhase.Processing && i > 80)
            {
                break;
            }
        }

        var temp = session.Runtime.Signals.First(s => s.SignalId == "Axis01.MotorTemperature").CurrentValue;
        Assert.InRange(temp, 45, 62);
    }

    [Fact]
    public void ProcessingHydraulicPressure_WithinExpectedBand()
    {
        var engine = PhysicalTestServiceFactory.CreateEngine();
        var session = CreateShortSession(BendingHydraulicMachine300ProfileFactory.Create(), 57);
        engine.Initialize(session, 57);

        var processingTicks = 0;
        for (var i = 0; i < 900; i++)
        {
            engine.Tick(session, TimeSpan.FromMilliseconds(200));
            if (session.Simulation.CurrentPhase == ProcessPhase.Processing)
            {
                processingTicks++;
                if (processingTicks >= 100)
                {
                    break;
                }
            }
        }

        var pressure = session.Runtime.Signals.First(s => s.SignalId == "Hydraulic.SupplyPressure").CurrentValue;
        Assert.InRange(pressure, 165, 198);
    }

    [Fact]
    public void ModelVerification_HasAtLeast30StatisticsSignalsPerProfileInHarness()
    {
        var laserCount = PhysicalPhysicsR3VerificationHarness.LaserStatisticsSignalCount;
        var bendingCount = PhysicalPhysicsR3VerificationHarness.BendingStatisticsSignalCount;
        Assert.True(laserCount >= 30);
        Assert.True(bendingCount >= 30);
    }

    [Fact]
    public void FailedCorrelation_SetsOverallStatusFalse()
    {
        var report = new R3EndToEndVerificationReport
        {
            Correlations = [new R3CorrelationEvaluation { Result = "Failed" }]
        };
        Assert.False(PhysicalPhysicsR3VerificationHarness.EvaluateEndToEndPassForTests(report));
    }

    [Fact]
    public void ReviewWithoutApproval_DoesNotAutoPass()
    {
        var report = new R3EndToEndVerificationReport
        {
            Correlations = [new R3CorrelationEvaluation { Result = "Review", SampleCount = 100 }]
        };
        Assert.False(PhysicalPhysicsR3VerificationHarness.EvaluateEndToEndPassForTests(report));
    }

    [Fact]
    public void ModelVerification_PassesAllChecks()
    {
        var report = PhysicalPhysicsR3VerificationHarness.RunModelVerification(42);
        Assert.True(report.Passed, string.Join("; ", report.DependencyChecks.Where(c => !c.Passed).Select(c => c.Pair)));
    }

    private static PhysicalMachineSession CreateShortSession(PhysicalMachineProfile profile, int seed)
    {
        var runtime = new PhysicalMachineRuntimeFactory().Create(profile);
        return new PhysicalMachineSession
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
    }
}
