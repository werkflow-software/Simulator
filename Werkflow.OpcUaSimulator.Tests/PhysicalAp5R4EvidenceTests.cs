using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp5R4EvidenceTests
{
	[Fact]
	public void AP5R4_NormalToApproaching_EmitsExactlyOne()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(25))
		});
		Assert.Equal(1, hygiene.ThresholdApproachingCount);
		Assert.Equal(0, hygiene.DuplicateApproachingCount);
		Assert.True(hygiene.Passed);
	}

	[Fact]
	public void AP5R4_RepeatedApproachingWhileInState_IsDuplicate()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(5)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(5.5))
		});
		Assert.Equal(1, hygiene.DuplicateApproachingCount);
		Assert.False(hygiene.Passed);
	}

	[Fact]
	public void AP5R4_ApproachingNormalApproaching_EmitsTwoEvents()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(5)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(20))
		});
		Assert.Equal(0, hygiene.DuplicateApproachingCount);
		Assert.True(hygiene.Passed);

		var valid = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(5)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(15)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(20))
		});
		Assert.Equal(2, valid.ThresholdApproachingCount);
		Assert.Equal(0, valid.DuplicateApproachingCount);
	}

	[Fact]
	public void AP5R4_ApproachingToSatisfied_EmitsThresholdEntered()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(5)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(10))
		});
		Assert.Equal(1, hygiene.ThresholdEnteredCount);
		Assert.True(hygiene.Passed);
	}

	[Fact]
	public void AP5R4_SatisfiedToApproaching_EmitsThresholdExited()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(15)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(20))
		});
		Assert.Equal(1, hygiene.ThresholdExitedCount);
		Assert.Equal(0, hygiene.DuplicateApproachingCount);
		Assert.True(hygiene.Passed);
	}

	[Fact]
	public void AP5R4_NoDuplicateThresholdEntered()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(15))
		});
		Assert.Equal(1, hygiene.InvalidTransitionCount);
		Assert.False(hygiene.Passed);
	}

	[Fact]
	public void AP5R4_NoThresholdExitedWithoutSatisfied()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(10))
		});
		Assert.Equal(1, hygiene.InvalidTransitionCount);
		Assert.False(hygiene.Passed);
	}

	[Fact]
	public void AP5R4_ThresholdFirstReached_ExactlyOnce()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(10))
		});
		Assert.True(hygiene.Passed);

		var bad = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(20))
		});
		Assert.False(bad.Passed);
	}

	[Fact]
	public void AP5R4_ConfirmedStreak_RequiresMinimumDuration()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(30), scenarioTime: TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(45),
				metadata: new Dictionary<string, string>
				{
					["MinimumDuration"] = "00:00:15",
					["ConfirmedStreakStartedAt"] = "00:00:30"
				}),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(45))
		};
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.True(result.MinimumDurationSatisfied);
	}

	[Fact]
	public void AP5R4_EventStateReconstruction_ValidSequence_Passes()
	{
		var hygiene = GroundTruthEventHygieneValidator.ValidateRun("fault-1", new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(5)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdExited, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdApproaching, TimeSpan.FromSeconds(25)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdEntered, TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(45)),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(45))
		});
		Assert.True(hygiene.EventStateReconstructionPassed);
		Assert.Equal(0, hygiene.DuplicateApproachingCount);
	}

	[Fact]
	public async Task AP5R4_HydraulicRegression_Passes()
	{
		var hydraulic = await PhysicalAp5R4VerificationHarness.RunScenarioMiniAsync(
			PhysicalAp5R4VerificationHarness.CreateR4HydraulicMini(),
			CancellationToken.None);
		Assert.True(hydraulic.Passed, DescribeFaultRuns(hydraulic.FaultRuns));
		Assert.True(hydraulic.EventStateReconstructionPassed, DescribeFaultRuns(hydraulic.FaultRuns));
		Assert.All(hydraulic.FaultRuns, r => Assert.Equal(0, r.DuplicateApproachingCount));
	}

	[Fact]
	public async Task AP5R4_LaserRegression_Passes()
	{
		var laser = await PhysicalAp5R4VerificationHarness.RunScenarioMiniAsync(
			PhysicalAp5R4VerificationHarness.CreateR4LaserMini(),
			CancellationToken.None);
		Assert.True(laser.Passed);
		Assert.All(laser.FaultRuns, r => Assert.Equal(0, r.DuplicateApproachingCount));
	}

	[Fact]
	public async Task AP5R4_EventHygieneVerification_Passes()
	{
		var report = await PhysicalAp5R4VerificationHarness.RunEventHygieneVerificationAsync();
		await PhysicalAp5R4VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.AP5R4Passed, string.Join(",", report.FailedCriteria));
		Assert.True(report.AP5OverallPassed, string.Join(",", report.FailedCriteria));
	}

	private static GroundTruthEvent MakeEvent(
		string runId,
		GroundTruthEventType type,
		TimeSpan experimentTime,
		string? detail = null,
		TimeSpan? scenarioTime = null,
		Dictionary<string, string>? metadata = null)
	{
		var meta = metadata ?? new Dictionary<string, string>();
		if (detail != null)
		{
			meta["detail"] = detail;
		}

		TimeSpan resolvedScenarioTime = scenarioTime ?? experimentTime;

		return new GroundTruthEvent
		{
			EventId = Guid.NewGuid().ToString("N"),
			ExperimentId = "TEST",
			RunId = runId,
			MachineId = Guid.NewGuid(),
			EventType = type,
			ExperimentSimulationTimestamp = experimentTime,
			RunRelativeTimestamp = experimentTime,
			ScenarioRelativeTimestamp = resolvedScenarioTime,
			RealTimestampUtc = DateTimeOffset.UtcNow,
			Seed = 1,
			FaultRepetitionIndex = 1,
			Metadata = meta
		};
	}

	private static string DescribeFaultRuns(IReadOnlyList<Ap5R4FaultRunDetail> runs) =>
		string.Join("; ", runs.Select(r =>
			$"{r.RunId}: passed={r.Passed} hygiene={r.HygienePassed} dup={r.DuplicateApproachingCount} invalid={r.InvalidTransitionCount} app={r.ThresholdApproachingCount}"));
}
