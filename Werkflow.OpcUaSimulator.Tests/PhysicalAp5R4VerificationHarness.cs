using System.Text.Json;
using System.Text.Json.Serialization;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp5R4VerificationHarness
{
	public static string EvidenceDirectory =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-05-r4-final"));

	public static string CreateVerificationRunId() =>
		$"ap5r4-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

	public static ExperimentDefinition CreateR4HydraulicMini() => new()
	{
		ExperimentId = "R4-HYD",
		DisplayName = "AP5 R4 Hydraulic Mini",
		MachineProfileId = "bending-hydraulic-machine-300",
		ScenarioId = "hydraulic-leak",
		ExperimentType = ExperimentType.FaultLearningSeries,
		WarmupDuration = TimeSpan.FromSeconds(10),
		NormalLearningDuration = TimeSpan.FromSeconds(30),
		FaultRunCount = 1,
		ControlRunCount = 0,
		RecoveryDuration = TimeSpan.FromMinutes(2),
		CooldownDuration = TimeSpan.FromSeconds(15),
		TimeFactor = 50.0,
		BaseSeed = 202
	};

	public static ExperimentDefinition CreateR4LaserMini() => new()
	{
		ExperimentId = "R4-LAS",
		DisplayName = "AP5 R4 Laser Mini",
		MachineProfileId = "laser-processing-machine-300",
		ScenarioId = "laser-overheating-axis-drive",
		ExperimentType = ExperimentType.FaultLearningSeries,
		WarmupDuration = TimeSpan.FromSeconds(10),
		NormalLearningDuration = TimeSpan.FromSeconds(30),
		FaultRunCount = 1,
		ControlRunCount = 0,
		RecoveryDuration = TimeSpan.FromMinutes(2),
		CooldownDuration = TimeSpan.FromSeconds(15),
		TimeFactor = 50.0,
		BaseSeed = 101
	};

	public static async Task<Ap5R4EventHygieneReport> RunEventHygieneVerificationAsync(
		CancellationToken cancellationToken = default)
	{
		var hydraulic = await RunScenarioMiniAsync(CreateR4HydraulicMini(), cancellationToken);
		var laser = await RunScenarioMiniAsync(CreateR4LaserMini(), cancellationToken);
		var continuity = await RunThresholdContinuityRegressionAsync(cancellationToken);
		var detectability = hydraulic.FaultRuns.All(r => r.DetectableBeforeFirstReached)
			&& laser.FaultRuns.All(r => r.DetectableBeforeFirstReached);
		var control = await RunControlRegressionAsync(cancellationToken);
		var normal = await RunNormalRegressionAsync(cancellationToken);
		var metrics = PhysicalAp5VerificationHarness.RunMetricsVerification();

		bool ap5R4Passed = hydraulic.Passed
			&& laser.Passed
			&& hydraulic.EventStateReconstructionPassed
			&& laser.EventStateReconstructionPassed;

		var report = new Ap5R4EventHygieneReport
		{
			VerificationRunId = CreateVerificationRunId(),
			StartedAtUtc = DateTime.UtcNow,
			HydraulicLeak = hydraulic,
			Laser = laser,
			EventStateReconstruction = new Ap5R4EventStateReconstructionResult
			{
				Passed = hydraulic.EventStateReconstructionPassed && laser.EventStateReconstructionPassed
			},
			ThresholdContinuityRegression = continuity,
			DetectabilityRegression = detectability,
			ControlRegression = control,
			NormalRegression = normal,
			MetricsRegression = metrics,
			AP5R4Passed = ap5R4Passed,
			AP5OverallPassed = ap5R4Passed
				&& continuity
				&& detectability
				&& control
				&& normal
				&& metrics.Passed,
			EndedAtUtc = DateTime.UtcNow
		};

		if (!report.AP5R4Passed)
		{
			report.FailedCriteria.Add("ap5-r4-event-hygiene");
		}

		if (!report.AP5OverallPassed)
		{
			report.FailedCriteria.Add("ap5-overall");
		}

		return report;
	}

	public static async Task<Ap5R4ScenarioResult> RunScenarioMiniAsync(
		ExperimentDefinition definition,
		CancellationToken cancellationToken)
	{
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(
			stack, definition.MachineProfileId, definition.BaseSeed, cancellationToken);
		var result = await stack.Runner.RunAsync(definition, session, cancellationToken);
		var gtEvents = stack.GroundTruthRecorder.GetEventsForExperiment(definition.ExperimentId).ToList();

		var faultRuns = result.Runs
			.Where(r => r.RunType == "Fault")
			.Select(run =>
			{
				var runEvents = gtEvents.Where(e => e.RunId == run.RunId).ToList();
				GroundTruthRunValidator.PopulateManifestFromEvents(run, runEvents);
				var validation = GroundTruthRunValidator.ValidateRun(run, runEvents, strictFaultLearningSeries: true);
				var hygiene = GroundTruthEventHygieneValidator.ValidateRun(run.RunId, gtEvents);
				return BuildFaultRunDetail(run, runEvents, validation, hygiene);
			})
			.ToList();

		return new Ap5R4ScenarioResult
		{
			ExperimentId = definition.ExperimentId,
			FaultRuns = faultRuns,
			Passed = faultRuns.All(r => r.Passed && r.HygienePassed),
			EventStateReconstructionPassed = faultRuns.All(r => r.EventStateReconstructionPassed)
		};
	}

	private static async Task<bool> RunThresholdContinuityRegressionAsync(CancellationToken cancellationToken)
	{
		var definition = ExperimentCatalog.CreateExp002Short();
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(
			stack, definition.MachineProfileId, definition.BaseSeed, cancellationToken);
		var result = await stack.Runner.RunAsync(definition, session, cancellationToken);
		var gtEvents = stack.GroundTruthRecorder.GetEventsForExperiment(definition.ExperimentId).ToList();

		return result.Runs
			.Where(r => r.RunType == "Fault")
			.All(run =>
			{
				var runEvents = gtEvents.Where(e => e.RunId == run.RunId).ToList();
				GroundTruthRunValidator.PopulateManifestFromEvents(run, runEvents);
				var validation = GroundTruthRunValidator.ValidateRun(run, runEvents, strictFaultLearningSeries: true);
				var hygiene = GroundTruthEventHygieneValidator.ValidateRun(run.RunId, gtEvents);
				return validation.Passed && hygiene.Passed;
			});
	}

	private static async Task<bool> RunControlRegressionAsync(CancellationToken cancellationToken)
	{
		var definition = ExperimentCatalog.CreateExp002Short();
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(
			stack, definition.MachineProfileId, definition.BaseSeed, cancellationToken);
		var result = await stack.Runner.RunAsync(definition, session, cancellationToken);
		var gtEvents = stack.GroundTruthRecorder.GetEventsForExperiment(definition.ExperimentId).ToList();
		return result.Runs.Where(r => r.RunType == "Control")
			.All(r => GroundTruthRunValidator.ValidateRun(r, gtEvents).Passed);
	}

	private static async Task<bool> RunNormalRegressionAsync(CancellationToken cancellationToken)
	{
		var definition = ExperimentCatalog.CreateExp002Short();
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(
			stack, definition.MachineProfileId, definition.BaseSeed, cancellationToken);
		var result = await stack.Runner.RunAsync(definition, session, cancellationToken);
		var gtEvents = stack.GroundTruthRecorder.GetEventsForExperiment(definition.ExperimentId).ToList();
		return result.Runs.Where(r => r.RunType == "Normal")
			.All(r => GroundTruthRunValidator.ValidateRun(r, gtEvents).Passed);
	}

	private static Ap5R4FaultRunDetail BuildFaultRunDetail(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		GroundTruthRunValidationResult validation,
		ThresholdEventHygieneResult hygiene)
	{
		TimeSpan? streakStart = run.ConfirmedThresholdStreakStartedAt;
		TimeSpan? confirmedAt = run.ThresholdConfirmedAt;
		TimeSpan? minimum = run.ThresholdMinimumDuration;
		TimeSpan? streakDuration = streakStart != null && confirmedAt != null
			? confirmedAt.Value - streakStart.Value
			: null;

		return new Ap5R4FaultRunDetail
		{
			RunId = run.RunId,
			ThresholdApproachingCount = hygiene.ThresholdApproachingCount,
			ThresholdEnteredCount = hygiene.ThresholdEnteredCount,
			ThresholdExitedCount = hygiene.ThresholdExitedCount,
			ThresholdConfirmedCount = hygiene.ThresholdConfirmedCount,
			DuplicateApproachingCount = hygiene.DuplicateApproachingCount,
			InvalidTransitionCount = hygiene.InvalidTransitionCount,
			ConfirmedStreakDuration = streakDuration,
			MinimumDuration = minimum,
			DetectableBeforeFirstReached = run.DetectableAt != null && run.ThresholdFirstReachedAt != null
				&& run.DetectableAt < run.ThresholdFirstReachedAt,
			EventStateReconstructionPassed = hygiene.EventStateReconstructionPassed,
			HygienePassed = hygiene.Passed,
			Passed = validation.Passed && hygiene.Passed
		};
	}

	public static async Task ExportEvidenceAsync(Ap5R4EventHygieneReport report, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
		await File.WriteAllTextAsync(
			Path.Combine(EvidenceDirectory, "AP-05-R4-event-hygiene-verification.json"),
			JsonSerializer.Serialize(report, options),
			cancellationToken);

		await ExportHydraulicGroundTruthSampleAsync(cancellationToken);
	}

	private static async Task ExportHydraulicGroundTruthSampleAsync(CancellationToken cancellationToken)
	{
		var definition = CreateR4HydraulicMini();
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(
			stack, definition.MachineProfileId, definition.BaseSeed, cancellationToken);
		var result = await stack.Runner.RunAsync(definition, session, cancellationToken);
		var gtEvents = stack.GroundTruthRecorder.GetEventsForExperiment(definition.ExperimentId).ToList();
		var faultRun = result.Runs.First(r => r.RunType == "Fault");
		var thresholdEvents = gtEvents
			.Where(e => e.RunId == faultRun.RunId)
			.Where(e => e.EventType is GroundTruthEventType.ThresholdApproaching
				or GroundTruthEventType.ThresholdEntered
				or GroundTruthEventType.ThresholdExited
				or GroundTruthEventType.ThresholdFirstReached
				or GroundTruthEventType.ThresholdConfirmed
				or GroundTruthEventType.MachineFaulted
				or GroundTruthEventType.DegradationBecameDetectable
				or GroundTruthEventType.RecoveryCompleted)
			.OrderBy(e => e.ExperimentSimulationTimestamp)
			.Select(e => new
			{
				e.EventType,
				ExperimentTime = e.ExperimentSimulationTimestamp.ToString("c"),
				ScenarioTime = e.ScenarioRelativeTimestamp.ToString("c"),
				Metadata = e.Metadata
			})
			.ToList();

		await File.WriteAllTextAsync(
			Path.Combine(EvidenceDirectory, "hydraulic-ground-truth-sample.json"),
			JsonSerializer.Serialize(thresholdEvents, new JsonSerializerOptions { WriteIndented = true }),
			cancellationToken);
	}
}

public sealed class Ap5R4EventHygieneReport
{
	public required string VerificationRunId { get; init; }

	public DateTime StartedAtUtc { get; init; }

	public DateTime EndedAtUtc { get; init; }

	public required Ap5R4ScenarioResult HydraulicLeak { get; init; }

	public required Ap5R4ScenarioResult Laser { get; init; }

	public required Ap5R4EventStateReconstructionResult EventStateReconstruction { get; init; }

	public bool ThresholdContinuityRegression { get; init; }

	public bool DetectabilityRegression { get; init; }

	public bool ControlRegression { get; init; }

	public bool NormalRegression { get; init; }

	public Ap5MetricsVerificationReport MetricsRegression { get; init; } = new();

	public bool AP5R4Passed { get; init; }

	public bool AP5OverallPassed { get; init; }

	public List<string> FailedCriteria { get; init; } = [];
}

public sealed class Ap5R4ScenarioResult
{
	public required string ExperimentId { get; init; }

	public List<Ap5R4FaultRunDetail> FaultRuns { get; init; } = [];

	public bool Passed { get; init; }

	public bool EventStateReconstructionPassed { get; init; }
}

public sealed class Ap5R4EventStateReconstructionResult
{
	public bool Passed { get; init; }
}

public sealed class Ap5R4FaultRunDetail
{
	public required string RunId { get; init; }

	public int ThresholdApproachingCount { get; init; }

	public int ThresholdEnteredCount { get; init; }

	public int ThresholdExitedCount { get; init; }

	public int ThresholdConfirmedCount { get; init; }

	public int DuplicateApproachingCount { get; init; }

	public int InvalidTransitionCount { get; init; }

	public TimeSpan? ConfirmedStreakDuration { get; init; }

	public TimeSpan? MinimumDuration { get; init; }

	public bool DetectableBeforeFirstReached { get; init; }

	public bool EventStateReconstructionPassed { get; init; }

	public bool HygienePassed { get; init; }

	public bool Passed { get; init; }
}
