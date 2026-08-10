using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp5EvidenceTests
{
	[Fact]
	public void AP5_GroundTruthEvent_IsImmutableSnapshot()
	{
		var evt = new GroundTruthEvent
		{
			EventId = "e1",
			ExperimentId = "EXP-TEST",
			RunId = "run-1",
			MachineId = Guid.NewGuid(),
			EventType = GroundTruthEventType.ScenarioStarted,
			SimulationTimestamp = TimeSpan.FromSeconds(10),
			RelativeTimeSinceRunStart = TimeSpan.FromSeconds(10),
			RealTimestampUtc = DateTimeOffset.UtcNow,
			Seed = 42,
			FaultRepetitionIndex = 1
		};
		Assert.Equal("e1", evt.EventId);
		Assert.Equal(GroundTruthEventType.ScenarioStarted, evt.EventType);
	}

	[Fact]
	public void AP5_Metrics_TruePositive()
	{
		var report = PhysicalAp5VerificationHarness.RunMetricsVerification();
		Assert.True(report.TruePositivePassed);
		Assert.True(report.LeadTimePassed);
		Assert.Equal("SyntheticTestEvidence", report.EvidenceType);
	}

	[Fact]
	public void AP5_Metrics_GroundTruthOnly_NoFakeZeroRates()
	{
		var engine = new MetricsEngine();
		var metrics = engine.Compute([], [], [], false, EvidenceType.NotAvailable);
		Assert.False(metrics.VigilEvaluationAvailable);
		Assert.Null(metrics.DetectionRate);
		Assert.Null(metrics.FalsePositiveRate);
	}

	[Fact]
	public void AP5_SeedDeriver_Reproducible()
	{
		int a = ExperimentSeedDeriver.DeriveRunSeed(100, 2, "Fault");
		int b = ExperimentSeedDeriver.DeriveRunSeed(100, 2, "Fault");
		int c = ExperimentSeedDeriver.DeriveRunSeed(101, 2, "Fault");
		Assert.Equal(a, b);
		Assert.NotEqual(a, c);
	}

	[Fact]
	public void AP5_Variation_DifferentIntensityAcrossRuns()
	{
		var variation = new ExperimentVariationDefinition();
		double i1 = ExperimentSeedDeriver.DeriveIntensity(1.0, 1, 55, variation);
		double i2 = ExperimentSeedDeriver.DeriveIntensity(1.0, 2, 55, variation);
		Assert.NotEqual(i1, i2);
	}

	[Fact]
	public void AP5_ExperimentCatalog_ContainsFiveExperiments()
	{
		Assert.Equal(5, ExperimentCatalog.GetAll().Count);
		Assert.NotNull(ExperimentCatalog.GetById("EXP-001"));
	}

	[Fact]
	public async Task AP5_ShortGroundTruthVerification_Passes()
	{
		var report = await PhysicalAp5VerificationHarness.RunShortGroundTruthVerificationAsync();
		await PhysicalAp5VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.Passed, string.Join(",", report.FailedCriteria));
		Assert.True(report.FaultRuns >= 2);
		Assert.True(report.ControlRuns >= 1);
		Assert.Contains(report.GroundTruthEvents, e => e.EventType == GroundTruthEventType.ScenarioStarted);
	}

	[Fact]
	public async Task AP5_EXP002_ShortRun_Completes()
	{
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var exp = ExperimentCatalog.CreateExp002Short();
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(stack, exp.MachineProfileId, exp.BaseSeed, CancellationToken.None);
		var result = await stack.Runner.RunAsync(exp, session, CancellationToken.None);
		Assert.True(result.Runs.Count(r => r.RunType == "Fault") >= 2);
		Assert.True(result.Runs.Any(r => r.RunType == "Control"));
	}

	[Fact]
	public async Task AP5_MetricsEngineVerification_Passes()
	{
		var report = PhysicalAp5VerificationHarness.RunMetricsVerification();
		Assert.True(report.Passed);
		await PhysicalAp5VerificationHarness.ExportMetricsEvidenceAsync(report);
	}

	[Fact]
	public void AP5_RepetitionTrend_Synthetic()
	{
		var engine = new MetricsEngine();
		var vigil = new RecordedVigilEventSource();
		var expId = "trend-test";
		var runs = new List<RunManifestEntry>
		{
			new() { RunId = "f1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1, FaultAt = TimeSpan.FromSeconds(100) },
			new() { RunId = "f2", RunType = "Fault", RunSeed = 2, RepetitionIndex = 2, FaultAt = TimeSpan.FromSeconds(200) },
			new() { RunId = "f3", RunType = "Fault", RunSeed = 3, RepetitionIndex = 3, FaultAt = TimeSpan.FromSeconds(300) },
			new() { RunId = "f4", RunType = "Fault", RunSeed = 4, RepetitionIndex = 4, FaultAt = TimeSpan.FromSeconds(400) }
		};
		var gt = runs.Select(r => new GroundTruthEvent
		{
			EventId = Guid.NewGuid().ToString("N"),
			ExperimentId = expId,
			RunId = r.RunId,
			MachineId = Guid.NewGuid(),
			EventType = GroundTruthEventType.MachineFaulted,
			SimulationTimestamp = r.FaultAt ?? TimeSpan.Zero,
			RelativeTimeSinceRunStart = r.FaultAt ?? TimeSpan.Zero,
			RealTimestampUtc = DateTimeOffset.UtcNow,
			Seed = r.RunSeed,
			FaultRepetitionIndex = r.RepetitionIndex
		}).ToList();
		vigil.AddEvents(expId, new[]
		{
			MakeWarning(expId, "f2", TimeSpan.FromSeconds(190), 0.8),
			MakeWarning(expId, "f3", TimeSpan.FromSeconds(270), 0.85),
			MakeWarning(expId, "f4", TimeSpan.FromSeconds(340), 0.9)
		});
		var metrics = engine.Compute(gt, vigil.GetEvents(expId), runs, true, EvidenceType.SyntheticTestEvidence);
		Assert.Equal(3, metrics.DetectedFaultCount);
		Assert.Equal(1, metrics.MissedFaultCount);
		Assert.True(metrics.PerRepetitionLeadTimes.Count >= 3);
	}

	private static VigilEvent MakeWarning(string expId, string runId, TimeSpan sim, double conf) =>
		new()
		{
			EventId = Guid.NewGuid().ToString("N"),
			ExperimentId = expId,
			RunId = runId,
			MachineId = Guid.NewGuid(),
			Timestamp = DateTimeOffset.UtcNow,
			SimulationTimestamp = sim,
			EventType = VigilEventType.Warning,
			Confidence = conf,
			Source = "SyntheticTestEvidence"
		};
}
