using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp5R1EvidenceTests
{
	[Fact]
	public void AP5R1_DetectableBeforeScenarioStart_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(45)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(10))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public void AP5R1_DetectableNullOnRequiredFaultRun_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(100))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events);
		Assert.False(result.Passed);
	}

	[Fact]
	public void AP5R1_DuplicateScenarioStarted_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(11))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events);
		Assert.False(result.DuplicateLifecycleEventsPassed);
	}

	[Fact]
	public void AP5R1_ControlFaultedPhase_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "control-1", RunType = "Control", RunSeed = 1, RepetitionIndex = 0 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("control-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10), "Faulted")
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public void AP5R1_NormalDuration_ComputedForGroundTruthOnly()
	{
		var engine = new MetricsEngine();
		var runs = new List<RunManifestEntry>
		{
			new() { RunId = "normal-0", RunType = "Normal", RunSeed = 1, RepetitionIndex = 0, RunStartedAt = TimeSpan.FromSeconds(10), RunCompletedAt = TimeSpan.FromSeconds(40) }
		};
		var metrics = engine.Compute([], [], runs, false, EvidenceType.NotAvailable);
		Assert.Equal(TimeSpan.FromSeconds(30), metrics.NormalDuration);
	}

	[Fact]
	public async Task AP5R1_SameSeed_Reproducible()
	{
		var report = await PhysicalAp5R1VerificationHarness.RunReproducibilityVerificationAsync();
		Assert.True(report.SameSeed.Passed);
	}

	[Fact]
	public async Task AP5R1_DifferentSeed_Varies()
	{
		var report = await PhysicalAp5R1VerificationHarness.RunReproducibilityVerificationAsync();
		Assert.True(report.DifferentSeed.Passed);
		Assert.True(report.DifferentSeed.VariationCount >= 2);
	}

	[Fact]
	public async Task AP5R1_E2E_Verification_Passes()
	{
		var report = await PhysicalAp5R1VerificationHarness.RunE2eVerificationAsync();
		await PhysicalAp5R1VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.Passed, string.Join(",", report.FailedCriteria));
	}

	private static GroundTruthEvent MakeEvent(string runId, GroundTruthEventType type, TimeSpan experimentTime, string? phase = null) =>
		new()
		{
			EventId = Guid.NewGuid().ToString("N"),
			ExperimentId = "TEST",
			RunId = runId,
			MachineId = Guid.NewGuid(),
			EventType = type,
			ExperimentSimulationTimestamp = experimentTime,
			RunRelativeTimestamp = experimentTime,
			ScenarioRelativeTimestamp = TimeSpan.Zero,
			RealTimestampUtc = DateTimeOffset.UtcNow,
			ScenarioPhase = phase,
			Seed = 1,
			FaultRepetitionIndex = 1
		};
}
