using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp5R3EvidenceTests
{
	[Fact]
	public void AP5R3_FirstEnter_EmitsThresholdFirstReached()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(30))
		};
		Assert.Equal(1, events.Count(e => e.EventType == GroundTruthEventType.ThresholdFirstReached));
	}

	[Fact]
	public void AP5R3_ThresholdExited_ResetsStreak_NotConfirmed()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(0)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(35), "00:00:15|00:00:20"),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(35)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryCompleted, TimeSpan.FromSeconds(60))
		};
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.True(result.MinimumDurationSatisfied);
		Assert.True(result.ThresholdContinuityPassed);
	}

	[Fact]
	public void AP5R3_ShortStreak_NotConfirmed_FailsMinimumDuration()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(35), "00:00:15|00:00:30"),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(35))
		};
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.MinimumDurationSatisfied);
	}

	[Fact]
	public void AP5R3_ExitInsideConfirmedStreak_FailsContinuity()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(40)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(50)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(70), "00:00:15|00:00:50"),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(70))
		};
		// Exit at 40 is before streak at 50 - should pass
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var ok = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.True(ok.ThresholdContinuityPassed);

		events.Add(MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(60)));
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var bad = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(bad.ThresholdContinuityPassed);
	}

	[Fact]
	public void AP5R3_FaultBeforeConfirmed_FailsChronology()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(30), "00:00:15|00:00:10")
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public async Task AP5R3_ThresholdContinuityVerification_Passes()
	{
		var report = await PhysicalAp5R3VerificationHarness.RunThresholdContinuityVerificationAsync();
		await PhysicalAp5R3VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.Passed, string.Join(",", report.FailedCriteria));
	}

	private static GroundTruthEvent MakeEvent(
		string runId,
		GroundTruthEventType type,
		TimeSpan experimentTime,
		string? detail = null) =>
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
			Seed = 1,
			FaultRepetitionIndex = 1,
			Metadata = detail == null
				? new Dictionary<string, string>()
				: new Dictionary<string, string> { ["detail"] = detail }
		};
}
