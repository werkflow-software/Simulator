using System.Text.Json;
using System.Text.Json.Serialization;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp5R3VerificationHarness
{
	public static string EvidenceDirectory =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-05-r3-final"));

	public static string CreateVerificationRunId() =>
		$"ap5r3-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

	public static async Task<Ap5R3ThresholdContinuityReport> RunThresholdContinuityVerificationAsync(
		CancellationToken cancellationToken = default)
	{
		var hydraulic = await RunHydraulicFaultSnapshotAsync(cancellationToken);
		var laser = await RunLaserFaultSnapshotAsync(cancellationToken);
		var reproducibility = await PhysicalAp5R1VerificationHarness.RunReproducibilityVerificationAsync(cancellationToken);
		var metrics = PhysicalAp5VerificationHarness.RunMetricsVerification();

		var report = new Ap5R3ThresholdContinuityReport
		{
			VerificationRunId = CreateVerificationRunId(),
			StartedAtUtc = DateTime.UtcNow,
			HydraulicLeak = hydraulic,
			LaserRegression = laser,
			DetectabilityRegression = laser.DetectabilityPassed && hydraulic.FaultRuns.All(r => r.DetectableBeforeFirstReached),
			ControlRegression = await RunControlRegressionAsync(cancellationToken),
			NormalRegression = await RunNormalRegressionAsync(cancellationToken),
			ReproducibilityRegression = reproducibility,
			MetricsRegression = metrics,
			Passed = hydraulic.Passed
				&& laser.Passed
				&& reproducibility.Passed
				&& metrics.Passed,
			EndedAtUtc = DateTime.UtcNow
		};

		if (!report.Passed)
		{
			report.FailedCriteria.Add("ap5-r3-threshold-continuity");
		}

		return report;
	}

	private static async Task<Ap5R3HydraulicLeakResult> RunHydraulicFaultSnapshotAsync(CancellationToken cancellationToken)
	{
		var definition = ExperimentCatalog.CreateExp002Short();
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
				return BuildFaultRunDetail(run, runEvents, validation);
			})
			.ToList();

		return new Ap5R3HydraulicLeakResult
		{
			FaultRuns = faultRuns,
			Passed = faultRuns.All(r => r.Passed)
		};
	}

	private static async Task<Ap5R3LaserRegressionResult> RunLaserFaultSnapshotAsync(CancellationToken cancellationToken)
	{
		var definition = ExperimentCatalog.CreateExp001Short();
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
				return BuildFaultRunDetail(run, runEvents, validation);
			})
			.ToList();

		return new Ap5R3LaserRegressionResult
		{
			FaultRuns = faultRuns,
			DetectabilityPassed = faultRuns.All(r => r.DetectableBeforeFirstReached),
			Passed = faultRuns.All(r => r.Passed)
		};
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

	private static Ap5R3FaultRunDetail BuildFaultRunDetail(
		RunManifestEntry run,
		IReadOnlyList<GroundTruthEvent> runEvents,
		GroundTruthRunValidationResult validation)
	{
		var confirmed = runEvents.FirstOrDefault(e => e.EventType == GroundTruthEventType.ThresholdConfirmed);
		TimeSpan? streakStart = run.ConfirmedThresholdStreakStartedAt;
		TimeSpan? confirmedAt = run.ThresholdConfirmedAt;
		TimeSpan? minimum = run.ThresholdMinimumDuration;
		TimeSpan? streakDuration = streakStart != null && confirmedAt != null
			? confirmedAt.Value - streakStart.Value
			: null;

		bool exitInside = streakStart != null && confirmedAt != null
			&& runEvents.Any(e => e.EventType == GroundTruthEventType.ThresholdExited
				&& e.ExperimentSimulationTimestamp > streakStart.Value
				&& e.ExperimentSimulationTimestamp < confirmedAt.Value);

		return new Ap5R3FaultRunDetail
		{
			RunId = run.RunId,
			ThresholdFirstReachedAt = run.ThresholdFirstReachedAt,
			ThresholdEnterEvents = runEvents
				.Where(e => e.EventType == GroundTruthEventType.ThresholdEntered)
				.Select(e => e.ExperimentSimulationTimestamp)
				.ToList(),
			ThresholdExitEvents = runEvents
				.Where(e => e.EventType == GroundTruthEventType.ThresholdExited)
				.Select(e => e.ExperimentSimulationTimestamp)
				.ToList(),
			ConfirmedThresholdStreakStartedAt = streakStart,
			ThresholdConfirmedAt = confirmedAt,
			MinimumDuration = minimum,
			ConfirmedStreakDuration = streakDuration,
			ExitInsideConfirmedStreak = exitInside,
			MachineFaultedAt = run.FaultAt,
			DetectableBeforeFirstReached = run.DetectableAt != null && run.ThresholdFirstReachedAt != null
				&& run.DetectableAt < run.ThresholdFirstReachedAt,
			Passed = validation.Passed
		};
	}

	public static async Task ExportEvidenceAsync(Ap5R3ThresholdContinuityReport report)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
		await File.WriteAllTextAsync(
			Path.Combine(EvidenceDirectory, "AP-05-R3-threshold-continuity-verification.json"),
			JsonSerializer.Serialize(report, options));

		string source = Path.Combine(PhysicalAp5VerificationHarness.EvidenceDirectory, "experiments", "EXP-002");
		string target = Path.Combine(EvidenceDirectory, "EXP-002");
		if (Directory.Exists(source))
		{
			Directory.CreateDirectory(target);
			foreach (var file in Directory.GetFiles(source))
			{
				File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
			}
		}
	}
}

public sealed class Ap5R3ThresholdContinuityReport
{
	public required string VerificationRunId { get; init; }

	public DateTime StartedAtUtc { get; init; }

	public DateTime EndedAtUtc { get; init; }

	public required Ap5R3HydraulicLeakResult HydraulicLeak { get; init; }

	public required Ap5R3LaserRegressionResult LaserRegression { get; init; }

	public bool DetectabilityRegression { get; init; }

	public bool ControlRegression { get; init; }

	public bool NormalRegression { get; init; }

	public Ap5ReproducibilityVerificationReport ReproducibilityRegression { get; init; } = new();

	public Ap5MetricsVerificationReport MetricsRegression { get; init; } = new();

	public bool Passed { get; init; }

	public List<string> FailedCriteria { get; init; } = [];
}

public sealed class Ap5R3HydraulicLeakResult
{
	public List<Ap5R3FaultRunDetail> FaultRuns { get; init; } = [];

	public bool Passed { get; init; }
}

public sealed class Ap5R3LaserRegressionResult
{
	public List<Ap5R3FaultRunDetail> FaultRuns { get; init; } = [];

	public bool DetectabilityPassed { get; init; }

	public bool Passed { get; init; }
}

public sealed class Ap5R3FaultRunDetail
{
	public required string RunId { get; init; }

	public TimeSpan? ThresholdFirstReachedAt { get; init; }

	public List<TimeSpan> ThresholdEnterEvents { get; init; } = [];

	public List<TimeSpan> ThresholdExitEvents { get; init; } = [];

	public TimeSpan? ConfirmedThresholdStreakStartedAt { get; init; }

	public TimeSpan? ThresholdConfirmedAt { get; init; }

	public TimeSpan? MinimumDuration { get; init; }

	public TimeSpan? ConfirmedStreakDuration { get; init; }

	public bool ExitInsideConfirmedStreak { get; init; }

	public TimeSpan? MachineFaultedAt { get; init; }

	public bool DetectableBeforeFirstReached { get; init; }

	public bool Passed { get; init; }
}
