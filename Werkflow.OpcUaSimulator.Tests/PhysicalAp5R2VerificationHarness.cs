using System.Text.Json;
using System.Text.Json.Serialization;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.OpcUa;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalAp5R2VerificationHarness
{
	public static string EvidenceDirectory =>
		Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-05-r2-final"));

	public static string CreateVerificationRunId() =>
		$"ap5r2-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 48);

	public static async Task<Ap5R2LifecycleVerificationReport> RunLifecycleVerificationAsync(
		CancellationToken cancellationToken = default)
	{
		var exp001 = await RunExperimentLifecycleAsync(ExperimentCatalog.CreateExp001Short(), cancellationToken);
		var exp002 = await RunExperimentLifecycleAsync(ExperimentCatalog.CreateExp002Short(), cancellationToken);
		var reproducibility = await RunReproducibilityRegressionAsync(cancellationToken);
		var metrics = PhysicalAp5VerificationHarness.RunMetricsVerification();

		var report = new Ap5R2LifecycleVerificationReport
		{
			VerificationRunId = CreateVerificationRunId(),
			StartedAtUtc = DateTime.UtcNow,
			EXP001 = exp001,
			EXP002 = exp002,
			NormalRunSemantics = new Ap5CheckResult("normal-run-semantics", exp001.NormalRunsPassed && exp002.NormalRunsPassed),
			ControlRunSemantics = new Ap5CheckResult("control-run-semantics", exp001.ControlRunsPassed && exp002.ControlRunsPassed),
			FaultPhaseChecks = new Ap5CheckResult("fault-phase-checks", AllFaultPhaseChecksPassed(exp001, exp002)),
			ThresholdConfirmationChecks = new Ap5CheckResult("threshold-confirmation-checks", AllThresholdChecksPassed(exp001, exp002)),
			ReproducibilityRegression = reproducibility,
			MetricsRegression = metrics,
			Passed = exp001.Passed
				&& exp002.Passed
				&& exp001.NormalRunsPassed && exp002.NormalRunsPassed
				&& exp001.ControlRunsPassed && exp002.ControlRunsPassed
				&& AllFaultPhaseChecksPassed(exp001, exp002)
				&& AllThresholdChecksPassed(exp001, exp002)
				&& reproducibility.Passed
				&& metrics.Passed,
			EndedAtUtc = DateTime.UtcNow
		};

		if (!report.Passed)
		{
			report.FailedCriteria.Add("ap5-r2-lifecycle-verification");
		}

		return report;
	}

	private static async Task<Ap5R2ExperimentLifecycleResult> RunExperimentLifecycleAsync(
		ExperimentDefinition definition,
		CancellationToken cancellationToken)
	{
		var log = new TestLogService();
		var stack = PhysicalAp5VerificationHarness.CreateStack(log);
		var session = await PhysicalAp5VerificationHarness.CreateSessionAsync(
			stack, definition.MachineProfileId, definition.BaseSeed, cancellationToken);
		var result = await stack.Runner.RunAsync(definition, session, cancellationToken);
		var gtEvents = stack.GroundTruthRecorder.GetEventsForExperiment(definition.ExperimentId).ToList();

		var validations = result.Runs
			.Select(r => GroundTruthRunValidator.ValidateRun(r, gtEvents, true))
			.ToList();

		var runDetails = result.Runs.Select(run =>
		{
			var validation = validations.First(v => v.RunId == run.RunId);
			return new Ap5R2RunLifecycleDetail
			{
				RunId = run.RunId,
				RunType = run.RunType,
				RunStartedAt = run.RunStartedAt,
				RunCompletedAt = run.RunCompletedAt,
				ScenarioStartedAt = run.ScenarioStartedAt,
				DetectableAt = run.DetectableAt,
				ThresholdApproachingAt = run.ThresholdApproachingAt,
				ThresholdFirstReachedAt = run.ThresholdFirstReachedAt,
				ThresholdConfirmedAt = run.ThresholdConfirmedAt,
				FaultAt = run.FaultAt,
				FirstFaultedPhaseAt = run.FirstFaultedPhaseAt,
				RecoveryStartedAt = run.RecoveryStartedAt,
				RecoveryCompletedAt = run.RecoveryCompletedAt,
				MinimumDuration = run.ThresholdMinimumDuration,
				MinimumDurationSatisfied = validation.MinimumDurationSatisfied,
				ChronologyPassed = validation.ChronologyPassed,
				RunSemanticsPassed = validation.RunSemanticsPassed,
				Passed = validation.Passed
			};
		}).ToList();

		int faultExpected = definition.FaultRunCount;
		int faultActual = result.Runs.Count(r => r.RunType == "Fault" && r.Outcome == "FaultRecovered");

		return new Ap5R2ExperimentLifecycleResult
		{
			ExperimentId = definition.ExperimentId,
			Runs = runDetails,
			FaultRunsExpected = faultExpected,
			FaultRunsActual = faultActual,
			NormalRunsPassed = result.Runs.Where(r => r.RunType == "Normal").All(r =>
				validations.First(v => v.RunId == r.RunId).Passed),
			ControlRunsPassed = result.Runs.Where(r => r.RunType == "Control").All(r =>
				validations.First(v => v.RunId == r.RunId).Passed),
			Passed = result.Passed && faultActual == faultExpected && validations.All(v => v.Passed)
		};
	}

	private static async Task<Ap5ReproducibilityVerificationReport> RunReproducibilityRegressionAsync(
		CancellationToken cancellationToken) =>
		await PhysicalAp5R1VerificationHarness.RunReproducibilityVerificationAsync(cancellationToken);

	private static bool AllFaultPhaseChecksPassed(Ap5R2ExperimentLifecycleResult exp001, Ap5R2ExperimentLifecycleResult exp002) =>
		exp001.Runs.Where(r => r.RunType == "Fault").All(r =>
			r.FirstFaultedPhaseAt == null || r.FaultAt == null || r.FirstFaultedPhaseAt >= r.FaultAt - TimeSpan.FromMilliseconds(50))
		&& exp002.Runs.Where(r => r.RunType == "Fault").All(r =>
			r.FirstFaultedPhaseAt == null || r.FaultAt == null || r.FirstFaultedPhaseAt >= r.FaultAt - TimeSpan.FromMilliseconds(50));

	private static bool AllThresholdChecksPassed(Ap5R2ExperimentLifecycleResult exp001, Ap5R2ExperimentLifecycleResult exp002) =>
		exp001.Runs.Where(r => r.RunType == "Fault").All(r =>
			r.ThresholdConfirmedAt != null && r.ThresholdFirstReachedAt != null && r.MinimumDurationSatisfied)
		&& exp002.Runs.Where(r => r.RunType == "Fault").All(r =>
			r.ThresholdConfirmedAt != null && r.ThresholdFirstReachedAt != null && r.MinimumDurationSatisfied);

	public static async Task ExportEvidenceAsync(Ap5R2LifecycleVerificationReport report)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
		await File.WriteAllTextAsync(
			Path.Combine(EvidenceDirectory, "AP-05-R2-ground-truth-lifecycle-verification.json"),
			JsonSerializer.Serialize(report, options));

		await CopyExperimentExports("EXP-001");
		await CopyExperimentExports("EXP-002");
	}

	private static async Task CopyExperimentExports(string experimentId)
	{
		string source = Path.Combine(PhysicalAp5VerificationHarness.EvidenceDirectory, "experiments", experimentId);
		string target = Path.Combine(EvidenceDirectory, experimentId);
		if (!Directory.Exists(source))
		{
			return;
		}

		Directory.CreateDirectory(target);
		foreach (var file in Directory.GetFiles(source))
		{
			File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
		}
	}
}

public sealed class Ap5R2LifecycleVerificationReport
{
	public required string VerificationRunId { get; init; }

	public DateTime StartedAtUtc { get; init; }

	public DateTime EndedAtUtc { get; init; }

	public required Ap5R2ExperimentLifecycleResult EXP001 { get; init; }

	public required Ap5R2ExperimentLifecycleResult EXP002 { get; init; }

	public Ap5CheckResult NormalRunSemantics { get; init; } = new("normal", false);

	public Ap5CheckResult ControlRunSemantics { get; init; } = new("control", false);

	public Ap5CheckResult FaultPhaseChecks { get; init; } = new("fault-phase", false);

	public Ap5CheckResult ThresholdConfirmationChecks { get; init; } = new("threshold", false);

	public Ap5ReproducibilityVerificationReport ReproducibilityRegression { get; init; } = new();

	public Ap5MetricsVerificationReport MetricsRegression { get; init; } = new();

	public bool Passed { get; init; }

	public List<string> FailedCriteria { get; init; } = [];
}

public sealed class Ap5R2ExperimentLifecycleResult
{
	public required string ExperimentId { get; init; }

	public List<Ap5R2RunLifecycleDetail> Runs { get; init; } = [];

	public int FaultRunsExpected { get; init; }

	public int FaultRunsActual { get; init; }

	public bool NormalRunsPassed { get; init; }

	public bool ControlRunsPassed { get; init; }

	public bool Passed { get; init; }
}

public sealed class Ap5R2RunLifecycleDetail
{
	public required string RunId { get; init; }

	public required string RunType { get; init; }

	public TimeSpan? RunStartedAt { get; init; }

	public TimeSpan? RunCompletedAt { get; init; }

	public TimeSpan? ScenarioStartedAt { get; init; }

	public TimeSpan? DetectableAt { get; init; }

	public TimeSpan? ThresholdApproachingAt { get; init; }

	public TimeSpan? ThresholdFirstReachedAt { get; init; }

	public TimeSpan? ThresholdConfirmedAt { get; init; }

	public TimeSpan? FaultAt { get; init; }

	public TimeSpan? FirstFaultedPhaseAt { get; init; }

	public TimeSpan? RecoveryStartedAt { get; init; }

	public TimeSpan? RecoveryCompletedAt { get; init; }

	public TimeSpan? MinimumDuration { get; init; }

	public bool MinimumDurationSatisfied { get; init; }

	public bool ChronologyPassed { get; init; }

	public bool RunSemanticsPassed { get; init; }

	public bool Passed { get; init; }
}
