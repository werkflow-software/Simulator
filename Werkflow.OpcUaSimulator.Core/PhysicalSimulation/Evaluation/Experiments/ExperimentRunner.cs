using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Export;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Recording;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public sealed class ExperimentRunner : IExperimentRunner
{
	private const int FaultRunMaxTicks = 4000;
	private const int ControlRunMaxTicks = 2500;

	private readonly IFaultScenarioService _faultScenarioService;
	private readonly IPhysicalRuntimeCoordinator _runtimeCoordinator;
	private readonly IGroundTruthRecorder _groundTruthRecorder;
	private readonly ISignalRecorder _signalRecorder;
	private readonly IVigilEventSource _vigilEventSource;
	private readonly MetricsEngine _metricsEngine;
	private readonly ExperimentExporter _exporter;

	private readonly object _sync = new();
	private ExperimentRunnerState _state = ExperimentRunnerState.Created;
	private bool _paused;
	private CancellationTokenSource? _cts;

	public ExperimentRunner(
		IFaultScenarioService faultScenarioService,
		IPhysicalRuntimeCoordinator runtimeCoordinator,
		IGroundTruthRecorder groundTruthRecorder,
		ISignalRecorder signalRecorder,
		IVigilEventSource vigilEventSource,
		MetricsEngine metricsEngine,
		ExperimentExporter exporter)
	{
		_faultScenarioService = faultScenarioService;
		_runtimeCoordinator = runtimeCoordinator;
		_groundTruthRecorder = groundTruthRecorder;
		_signalRecorder = signalRecorder;
		_vigilEventSource = vigilEventSource;
		_metricsEngine = metricsEngine;
		_exporter = exporter;
	}

	private string? _activeExperimentId;

	public ExperimentRunnerState State
	{
		get { lock (_sync) return _state; }
	}

	public ExperimentResult? LastResult { get; private set; }

	public void Pause()
	{
		lock (_sync) { _paused = true; }
	}

	public void Resume()
	{
		lock (_sync) { _paused = false; }
	}

	public void Cancel()
	{
		_cts?.Cancel();
		lock (_sync) { _state = ExperimentRunnerState.Cancelled; }
	}

	public async Task<ExperimentResult> RunAsync(
		ExperimentDefinition definition,
		PhysicalMachineSession session,
		CancellationToken cancellationToken = default)
	{
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		var token = _cts.Token;
		lock (_sync)
		{
			_state = ExperimentRunnerState.Warmup;
			_paused = false;
		}

		var started = DateTime.UtcNow;
		var runs = new List<RunManifestEntry>();
		session.Simulation.TimeFactor = definition.TimeFactor;
		session.Simulation.CurrentPhase = ProcessPhase.Processing;
		_runtimeCoordinator.EnsureEngine(session, definition.BaseSeed);

		_groundTruthRecorder.BeginExperiment(definition.ExperimentId, session.MachineId, definition.BaseSeed);
		_activeExperimentId = definition.ExperimentId;
		TimeSpan simClock = TimeSpan.Zero;
		const int tickMs = 50;

		try
		{
			var runPlan = BuildRunPlan(definition);
			int runIndex = 0;

			_groundTruthRecorder.BeginRun("exp-start", "Experiment", definition.BaseSeed, 0);
			_groundTruthRecorder.UpdateExperimentClock(simClock);
			_groundTruthRecorder.RecordEvent(GroundTruthEventType.ExperimentStarted, simClock, TimeSpan.Zero);
			_groundTruthRecorder.CompleteRun();

			simClock = await RunDurationAsync(
				definition.WarmupDuration, session, simClock, tickMs, token, ExperimentRunnerState.Warmup);

			lock (_sync) { _state = ExperimentRunnerState.NormalLearning; }

			string normalRunId = $"normal-{runIndex}";
			TimeSpan normalRunStartedAt = simClock;
			var normalRun = new RunManifestEntry
			{
				RunId = normalRunId,
				RunType = "Normal",
				RunSeed = ExperimentSeedDeriver.DeriveRunSeed(definition.BaseSeed, runIndex, "Normal"),
				RepetitionIndex = 0,
				RunStartedAt = normalRunStartedAt,
				Outcome = "NoFault"
			};
			_groundTruthRecorder.BeginRun(normalRunId, "Normal", normalRun.RunSeed, 0);
			_groundTruthRecorder.UpdateExperimentClock(simClock);
			_groundTruthRecorder.RecordEvent(GroundTruthEventType.NormalObservationStarted, simClock, TimeSpan.Zero);
			_signalRecorder.BeginRun(normalRunId);
			simClock = await RunDurationAsync(
				definition.NormalLearningDuration, session, simClock, tickMs, token, ExperimentRunnerState.NormalLearning, normalRunId);
			normalRun.RunCompletedAt = simClock;
			_signalRecorder.CompleteRun();
			runs.Add(normalRun);
			_groundTruthRecorder.CompleteRun();
			runIndex++;

			int faultRep = 0;
			foreach (var planned in runPlan)
			{
				if (definition.ResetBetweenRuns)
				{
					await _faultScenarioService.ResetMachineAsync(session.MachineId, token);
				}

				session.Simulation.CurrentPhase = ProcessPhase.Processing;

				if (planned.RunType == ExperimentRunType.Control)
				{
					simClock = await RunControlAsync(definition, session, planned, runs, simClock, tickMs, token, runIndex);
					runIndex++;
					continue;
				}

				if (planned.RunType == ExperimentRunType.Fault)
				{
					faultRep++;
					simClock = await RunFaultAsync(definition, session, planned, faultRep, runs, simClock, tickMs, token, runIndex);
					runIndex++;
					simClock = await RunDurationAsync(
						definition.CooldownDuration, session, simClock, tickMs, token, ExperimentRunnerState.Cooldown);
				}
			}

			_groundTruthRecorder.BeginRun("exp-end", "Experiment", definition.BaseSeed, 0);
			_groundTruthRecorder.UpdateExperimentClock(simClock);
			_groundTruthRecorder.RecordEvent(GroundTruthEventType.ExperimentCompleted, simClock, TimeSpan.Zero);
			_groundTruthRecorder.CompleteRun();
			_groundTruthRecorder.CompleteExperiment();

			lock (_sync) { _state = ExperimentRunnerState.Completed; }

			var completed = DateTime.UtcNow;
			var gtEvents = _groundTruthRecorder.GetEventsForExperiment(definition.ExperimentId);
			FinalizeRunsFromGroundTruth(runs, gtEvents);

			var vigilEvents = definition.VigilMode == VigilMode.VigilEvaluation && _vigilEventSource.IsConnected
				? _vigilEventSource.GetEvents(definition.ExperimentId)
				: [];
			var evidenceType = vigilEvents.Count > 0 && _vigilEventSource.IsConnected
				? EvidenceType.RealVigilEvidence
				: EvidenceType.NotAvailable;
			var metrics = _metricsEngine.Compute(gtEvents, vigilEvents, runs, _vigilEventSource.IsConnected, evidenceType);

			var validationResults = runs
				.Select(r => GroundTruthRunValidator.ValidateRun(
					r,
					gtEvents,
					definition.ExperimentType == ExperimentType.FaultLearningSeries))
				.ToList();
			var failedCriteria = validationResults
				.Where(v => !v.Passed)
				.SelectMany(v => v.FailedCriteria)
				.ToList();

			bool allFaultRunsPassed = runs
				.Where(r => r.RunType == "Fault")
				.All(r => r.Outcome == "FaultRecovered");
			bool expectedFaultCount = runs.Count(r => r.RunType == "Fault") == definition.FaultRunCount
				&& runs.Count(r => r.RunType == "Fault" && r.Outcome == "FaultRecovered") == definition.FaultRunCount;

			var result = new ExperimentResult
			{
				ExperimentId = definition.ExperimentId,
				ExperimentHash = EvaluationHashUtility.ComputeSha256(definition),
				ProfileHash = EvaluationHashUtility.ComputeSha256(definition.MachineProfileId),
				ScenarioHash = EvaluationHashUtility.ComputeSha256(definition.ScenarioId),
				StartedAtUtc = started,
				CompletedAtUtc = completed,
				FinalState = ExperimentRunnerState.Completed,
				Runs = runs,
				Passed = failedCriteria.Count == 0 && allFaultRunsPassed && expectedFaultCount
			};
			result.FailedCriteria.AddRange(failedCriteria);
			if (!allFaultRunsPassed)
			{
				result.FailedCriteria.Add("fault-runs-not-all-recovered");
			}
			if (!expectedFaultCount)
			{
				result.FailedCriteria.Add("fault-run-count-mismatch");
			}

			result.ExportPath = await _exporter.ExportAsync(definition, result, gtEvents, vigilEvents, metrics, token);
			LastResult = result;
			return result;
		}
		catch (OperationCanceledException)
		{
			lock (_sync) { _state = ExperimentRunnerState.Cancelled; }
			throw;
		}
		catch
		{
			lock (_sync) { _state = ExperimentRunnerState.Failed; }
			throw;
		}
	}

	private async Task<TimeSpan> RunFaultAsync(
		ExperimentDefinition definition,
		PhysicalMachineSession session,
		PlannedRun planned,
		int faultRep,
		List<RunManifestEntry> runs,
		TimeSpan simClock,
		int tickMs,
		CancellationToken token,
		int runIndex)
	{
		lock (_sync) { _state = ExperimentRunnerState.Running; }

		int runSeed = ExperimentSeedDeriver.DeriveRunSeed(definition.BaseSeed, runIndex, "Fault");
		double intensity = Math.Max(
			1.0,
			ExperimentSeedDeriver.DeriveIntensity(1.0, runIndex, definition.BaseSeed, definition.Variation));
		var startOffset = ExperimentSeedDeriver.DeriveStartOffset(runIndex, definition.BaseSeed, definition.Variation);

		_groundTruthRecorder.BeginRun(planned.RunId, "Fault", runSeed, faultRep);
		_signalRecorder.BeginRun(planned.RunId);
		TimeSpan faultRunStartedAt = simClock;

		if (startOffset > TimeSpan.Zero)
		{
			simClock = await RunDurationAsync(startOffset, session, simClock, tickMs, token, ExperimentRunnerState.Running, planned.RunId);
		}

		await _faultScenarioService.StartAsync(new FaultScenarioStartRequest
		{
			MachineId = session.MachineId,
			ScenarioId = definition.ScenarioId,
			Intensity = intensity,
			TimeFactor = 1.0,
			Seed = runSeed,
			AutoThresholdFaultEnabled = true,
			AutoScenarioEndEnabled = false,
			RunMode = FaultScenarioRunMode.Normal
		}, token);

		var manifest = new RunManifestEntry
		{
			RunId = planned.RunId,
			RunType = "Fault",
			RunSeed = runSeed,
			RepetitionIndex = faultRep,
			Intensity = intensity,
			RunStartedAt = faultRunStartedAt,
			ScenarioStart = simClock,
			Outcome = "Pending"
		};

		bool recoveryStarted = false;
		for (int i = 0; i < FaultRunMaxTicks; i++)
		{
			token.ThrowIfCancellationRequested();
			await WaitIfPausedAsync(token);
			_runtimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(tickMs));
			simClock += TimeSpan.FromMilliseconds(tickMs * definition.TimeFactor);
			_groundTruthRecorder.UpdateExperimentClock(simClock);

			var instance = session.Simulation.FaultScenarios.ActiveInstances.Values
				.FirstOrDefault(inst => inst.ScenarioId.Equals(definition.ScenarioId, StringComparison.OrdinalIgnoreCase));
			if (instance == null)
			{
				if (recoveryStarted)
				{
					break;
				}
				continue;
			}

			_signalRecorder.Record(session, simClock);

			if (instance.ThresholdFaultTriggered && !recoveryStarted)
			{
				await _faultScenarioService.StopAsync(session.MachineId, definition.ScenarioId, token);
				recoveryStarted = true;
				lock (_sync) { _state = ExperimentRunnerState.Recovering; }
			}
		}

		manifest.RunCompletedAt = simClock;
		runs.Add(manifest);
		_signalRecorder.CompleteRun();
		_groundTruthRecorder.CompleteRun();
		return simClock;
	}

	private async Task<TimeSpan> RunControlAsync(
		ExperimentDefinition definition,
		PhysicalMachineSession session,
		PlannedRun planned,
		List<RunManifestEntry> runs,
		TimeSpan simClock,
		int tickMs,
		CancellationToken token,
		int runIndex)
	{
		lock (_sync) { _state = ExperimentRunnerState.Running; }

		int runSeed = ExperimentSeedDeriver.DeriveRunSeed(definition.BaseSeed, runIndex, "Control");
		string controlScenario = planned.ControlScenarioId ?? definition.ScenarioId;

		_groundTruthRecorder.BeginRun(planned.RunId, "Control", runSeed, 0);
		_signalRecorder.BeginRun(planned.RunId);
		TimeSpan controlRunStartedAt = simClock;

		await _faultScenarioService.StartAsync(new FaultScenarioStartRequest
		{
			MachineId = session.MachineId,
			ScenarioId = controlScenario,
			Intensity = 0.9,
			TimeFactor = 1.0,
			Seed = runSeed,
			AutoThresholdFaultEnabled = false,
			AutoScenarioEndEnabled = true,
			RunMode = FaultScenarioRunMode.NonFaultingControlRun
		}, token);

		var manifest = new RunManifestEntry
		{
			RunId = planned.RunId,
			RunType = "Control",
			RunSeed = runSeed,
			RepetitionIndex = 0,
			RunStartedAt = controlRunStartedAt,
			ScenarioStart = simClock,
			Outcome = "NoFault"
		};

		for (int i = 0; i < ControlRunMaxTicks; i++)
		{
			token.ThrowIfCancellationRequested();
			await WaitIfPausedAsync(token);
			_runtimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(tickMs));
			simClock += TimeSpan.FromMilliseconds(tickMs * definition.TimeFactor);
			_groundTruthRecorder.UpdateExperimentClock(simClock);
			_signalRecorder.Record(session, simClock);

			if (!session.Simulation.FaultScenarios.ScenarioIdToInstance.ContainsKey(controlScenario))
			{
				break;
			}
		}

		if (session.Simulation.FaultScenarios.ScenarioIdToInstance.ContainsKey(controlScenario))
		{
			await _faultScenarioService.StopAsync(session.MachineId, controlScenario, token);
			for (int i = 0; i < 1500; i++)
			{
				token.ThrowIfCancellationRequested();
				await WaitIfPausedAsync(token);
				_runtimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(tickMs));
				simClock += TimeSpan.FromMilliseconds(tickMs * definition.TimeFactor);
				_groundTruthRecorder.UpdateExperimentClock(simClock);
				if (!session.Simulation.FaultScenarios.ScenarioIdToInstance.ContainsKey(controlScenario))
				{
					break;
				}
			}
		}

		manifest.RunCompletedAt = simClock;
		runs.Add(manifest);
		_signalRecorder.CompleteRun();
		_groundTruthRecorder.CompleteRun();
		return simClock;
	}

	private async Task<TimeSpan> RunDurationAsync(
		TimeSpan duration,
		PhysicalMachineSession session,
		TimeSpan simClock,
		int tickMs,
		CancellationToken token,
		ExperimentRunnerState stateDuring,
		string? runId = null)
	{
		lock (_sync) { _state = stateDuring; }

		int ticks = (int)Math.Max(1, duration.TotalMilliseconds / (tickMs * session.Simulation.TimeFactor));
		for (int i = 0; i < ticks; i++)
		{
			token.ThrowIfCancellationRequested();
			await WaitIfPausedAsync(token);
			_runtimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(tickMs));
			simClock += TimeSpan.FromMilliseconds(tickMs * session.Simulation.TimeFactor);
			_groundTruthRecorder.UpdateExperimentClock(simClock);
			if (runId != null)
			{
				_signalRecorder.Record(session, simClock);
			}
		}

		return simClock;
	}

	private async Task WaitIfPausedAsync(CancellationToken token)
	{
		while (_paused)
		{
			await Task.Delay(50, token);
		}
	}

	private static void FinalizeRunsFromGroundTruth(List<RunManifestEntry> runs, IReadOnlyList<GroundTruthEvent> gtEvents)
	{
		foreach (var run in runs)
		{
			var runEvents = gtEvents.Where(e => e.RunId.Equals(run.RunId, StringComparison.OrdinalIgnoreCase)).ToList();
			GroundTruthRunValidator.PopulateManifestFromEvents(run, runEvents);
		}
	}

	private static List<PlannedRun> BuildRunPlan(ExperimentDefinition definition)
	{
		var plan = new List<PlannedRun>();
		int idx = 0;
		for (int f = 0; f < definition.FaultRunCount; f++)
		{
			if (f > 0 && definition.ControlRunCount > 0 && f <= definition.ControlRunCount)
			{
				plan.Add(new PlannedRun($"control-{idx}", ExperimentRunType.Control,
					definition.ControlScenarioIds.Length > 0 ? definition.ControlScenarioIds[0] : null));
				idx++;
			}
			plan.Add(new PlannedRun($"fault-{f + 1}", ExperimentRunType.Fault, null));
			idx++;
		}
		return plan;
	}

	private sealed record PlannedRun(string RunId, ExperimentRunType RunType, string? ControlScenarioId);
}

internal static class ExperimentDefinitionExtensions
{
	public static TimeSpan DefaultControlDuration(this ExperimentDefinition definition) =>
		TimeSpan.FromTicks(definition.NormalLearningDuration.Ticks / 2);
}
