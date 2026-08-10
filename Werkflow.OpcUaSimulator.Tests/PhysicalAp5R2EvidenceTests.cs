using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp5R2EvidenceTests
{
	[Fact]
	public void AP5R2_FaultedPhaseBeforeMachineFaulted_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(40)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(55)),
			MakePhaseEvent("fault-1", "Faulted", TimeSpan.FromSeconds(50)),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(55))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public void AP5R2_FaultedPhaseWithMachineFaulted_PassesValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(40)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(55), detail: "00:00:15"),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(55)),
			MakePhaseEvent("fault-1", "Faulted", TimeSpan.FromSeconds(55)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryStarted, TimeSpan.FromSeconds(60)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryCompleted, TimeSpan.FromSeconds(90))
		};
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.True(result.Passed);
	}

	[Fact]
	public void AP5R2_MissingThresholdConfirmed_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(40)),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(55)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryCompleted, TimeSpan.FromSeconds(90))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.Passed);
	}

	[Fact]
	public void AP5R2_ThresholdConfirmedBeforeFirstReached_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(30)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(40))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public void AP5R2_MinimumDurationNotSatisfied_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(20)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(40)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(45), detail: "00:00:15"),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(45)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryCompleted, TimeSpan.FromSeconds(90))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.MinimumDurationSatisfied);
	}

	[Fact]
	public void AP5R2_DetectableEqualsFault_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("fault-1", GroundTruthEventType.DegradationBecameDetectable, TimeSpan.FromSeconds(55)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdFirstReached, TimeSpan.FromSeconds(40)),
			MakeEvent("fault-1", GroundTruthEventType.ThresholdConfirmed, TimeSpan.FromSeconds(55), detail: "00:00:15"),
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(55)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryCompleted, TimeSpan.FromSeconds(90))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events, strictFaultLearningSeries: true);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public void AP5R2_NormalRunScenarioFieldsNull_PassesSemantics()
	{
		var run = new RunManifestEntry
		{
			RunId = "normal-0",
			RunType = "Normal",
			RunSeed = 1,
			RepetitionIndex = 0,
			RunStartedAt = TimeSpan.FromSeconds(10),
			RunCompletedAt = TimeSpan.FromSeconds(40)
		};
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("normal-0", GroundTruthEventType.NormalObservationStarted, TimeSpan.FromSeconds(10))
		};
		GroundTruthRunValidator.PopulateManifestFromEvents(run, events);
		var result = GroundTruthRunValidator.ValidateRun(run, events);
		Assert.True(result.RunSemanticsPassed);
		Assert.Null(run.ScenarioStartedAt);
		Assert.Null(run.RecoveryCompletedAt);
	}

	[Fact]
	public void AP5R2_NormalRunScenarioEvent_FailsValidation()
	{
		var run = new RunManifestEntry { RunId = "normal-0", RunType = "Normal", RunSeed = 1, RepetitionIndex = 0 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("normal-0", GroundTruthEventType.NormalObservationStarted, TimeSpan.FromSeconds(10)),
			MakeEvent("normal-0", GroundTruthEventType.ScenarioStarted, TimeSpan.FromSeconds(11))
		};
		var result = GroundTruthRunValidator.ValidateRun(run, events);
		Assert.False(result.ChronologyPassed);
	}

	[Fact]
	public void AP5R2_FaultRecoveredWithoutThresholdConfirmed_FailsOutcome()
	{
		var run = new RunManifestEntry { RunId = "fault-1", RunType = "Fault", RunSeed = 1, RepetitionIndex = 1 };
		var events = new List<GroundTruthEvent>
		{
			MakeEvent("fault-1", GroundTruthEventType.MachineFaulted, TimeSpan.FromSeconds(55)),
			MakeEvent("fault-1", GroundTruthEventType.RecoveryCompleted, TimeSpan.FromSeconds(90))
		};
		Assert.Equal("NoFaultTriggered", GroundTruthRunValidator.DeriveOutcome(run, events));
	}

	[Fact]
	public void AP5R2_NormalDuration_UsesRunStartedCompleted()
	{
		var engine = new MetricsEngine();
		var runs = new List<RunManifestEntry>
		{
			new()
			{
				RunId = "normal-0",
				RunType = "Normal",
				RunSeed = 1,
				RepetitionIndex = 0,
				RunStartedAt = TimeSpan.FromSeconds(10),
				RunCompletedAt = TimeSpan.FromSeconds(40)
			}
		};
		var metrics = engine.Compute([], [], runs, false, EvidenceType.NotAvailable);
		Assert.Equal(TimeSpan.FromSeconds(30), metrics.NormalDuration);
	}

	[Fact]
	public async Task AP5R2_SameSeed_ReproducibleWithThresholdConfirmed()
	{
		var report = await PhysicalAp5R1VerificationHarness.RunReproducibilityVerificationAsync();
		Assert.True(report.SameSeed.Passed);
	}

	[Fact]
	public async Task AP5R2_LifecycleVerification_Passes()
	{
		var report = await PhysicalAp5R2VerificationHarness.RunLifecycleVerificationAsync();
		await PhysicalAp5R2VerificationHarness.ExportEvidenceAsync(report);
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

	private static GroundTruthEvent MakePhaseEvent(string runId, string phase, TimeSpan experimentTime) =>
		new()
		{
			EventId = Guid.NewGuid().ToString("N"),
			ExperimentId = "TEST",
			RunId = runId,
			MachineId = Guid.NewGuid(),
			EventType = GroundTruthEventType.ScenarioPhaseChanged,
			ExperimentSimulationTimestamp = experimentTime,
			RunRelativeTimestamp = experimentTime,
			ScenarioRelativeTimestamp = TimeSpan.Zero,
			RealTimestampUtc = DateTimeOffset.UtcNow,
			ScenarioPhase = phase,
			Seed = 1,
			FaultRepetitionIndex = 1
		};
}
