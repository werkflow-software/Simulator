using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public sealed class GroundTruthRecorder : IGroundTruthRecorder, IDisposable
{
	private readonly object _sync = new();
	private readonly Dictionary<string, List<GroundTruthEvent>> _byExperiment = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<Guid, List<GroundTruthEvent>> _byMachine = new();
	private readonly HashSet<GroundTruthEventType> _lifecycleEmitted = new();

	private readonly IFaultScenarioService _faultScenarioService;
	private readonly FaultScenarioEventHub _eventHub;

	private string? _activeExperimentId;
	private Guid _activeMachineId;
	private string? _activeRunId;
	private int _activeRunSeed;
	private int _activeRepetitionIndex;
	private TimeSpan _experimentClock;
	private TimeSpan _runStartExperimentTime;
	private TimeSpan _scenarioStartExperimentTime;
	private bool _subscribed;

	public GroundTruthRecorder(IFaultScenarioService faultScenarioService, FaultScenarioEventHub eventHub)
	{
		_faultScenarioService = faultScenarioService;
		_eventHub = eventHub;
	}

	public void BeginExperiment(string experimentId, Guid machineId, int baseSeed)
	{
		lock (_sync)
		{
			EnsureSubscribed();
			_activeExperimentId = experimentId;
			_activeMachineId = machineId;
			_experimentClock = TimeSpan.Zero;
			if (!_byExperiment.ContainsKey(experimentId))
			{
				_byExperiment[experimentId] = [];
			}
			if (!_byMachine.ContainsKey(machineId))
			{
				_byMachine[machineId] = [];
			}
		}
	}

	public void BeginRun(string runId, string runType, int runSeed, int faultRepetitionIndex)
	{
		lock (_sync)
		{
			_activeRunId = runId;
			_activeRunSeed = runSeed;
			_activeRepetitionIndex = faultRepetitionIndex;
			_runStartExperimentTime = _experimentClock;
			_scenarioStartExperimentTime = TimeSpan.Zero;
			_lifecycleEmitted.Clear();
		}
	}

	public void UpdateExperimentClock(TimeSpan experimentSimulationTime)
	{
		lock (_sync)
		{
			_experimentClock = experimentSimulationTime;
		}
	}

	public void RecordEvent(
		GroundTruthEventType eventType,
		TimeSpan experimentSimulationTime,
		TimeSpan runRelativeTime,
		string? scenarioId = null,
		string? scenarioPhase = null,
		string? severity = null,
		double intensity = 0,
		int seed = 0,
		IReadOnlyDictionary<string, string>? metadata = null,
		TimeSpan scenarioRelativeTime = default)
	{
		lock (_sync)
		{
			if (_activeExperimentId == null || _activeRunId == null)
			{
				return;
			}

			if (IsUniqueLifecycleEvent(eventType) && _lifecycleEmitted.Contains(eventType))
			{
				return;
			}

			if (eventType == GroundTruthEventType.ScenarioStarted)
			{
				_scenarioStartExperimentTime = experimentSimulationTime;
			}

			var evt = new GroundTruthEvent
			{
				EventId = Guid.NewGuid().ToString("N"),
				ExperimentId = _activeExperimentId,
				RunId = _activeRunId,
				MachineId = _activeMachineId,
				ScenarioId = scenarioId,
				EventType = eventType,
				ExperimentSimulationTimestamp = experimentSimulationTime,
				RunRelativeTimestamp = runRelativeTime,
				ScenarioRelativeTimestamp = scenarioRelativeTime,
				RealTimestampUtc = DateTimeOffset.UtcNow,
				ScenarioPhase = scenarioPhase,
				Severity = severity,
				Intensity = intensity,
				Seed = seed != 0 ? seed : _activeRunSeed,
				FaultRepetitionIndex = _activeRepetitionIndex,
				Metadata = metadata ?? new Dictionary<string, string>()
			};

			_byExperiment[_activeExperimentId].Add(evt);
			_byMachine[_activeMachineId].Add(evt);

			if (IsUniqueLifecycleEvent(eventType))
			{
				_lifecycleEmitted.Add(eventType);
			}
		}
	}

	public void CompleteRun()
	{
		lock (_sync)
		{
			_activeRunId = null;
			_lifecycleEmitted.Clear();
		}
	}

	public void CompleteExperiment()
	{
		lock (_sync)
		{
			_activeExperimentId = null;
		}
	}

	public IReadOnlyList<GroundTruthEvent> GetEventsForExperiment(string experimentId)
	{
		lock (_sync)
		{
			return _byExperiment.TryGetValue(experimentId, out var list)
				? list.ToList()
				: Array.Empty<GroundTruthEvent>();
		}
	}

	public IReadOnlyList<GroundTruthEvent> GetEventsForMachine(Guid machineId)
	{
		lock (_sync)
		{
			return _byMachine.TryGetValue(machineId, out var list)
				? list.ToList()
				: Array.Empty<GroundTruthEvent>();
		}
	}

	public GroundTruthTimeline BuildTimeline(string experimentId, string runId)
	{
		lock (_sync)
		{
			var events = _byExperiment.TryGetValue(experimentId, out var list)
				? list.Where(e => e.RunId.Equals(runId, StringComparison.OrdinalIgnoreCase))
					.OrderBy(e => e.ExperimentSimulationTimestamp).ToList()
				: [];
			return new GroundTruthTimeline
			{
				ExperimentId = experimentId,
				RunId = runId,
				Events = events
			};
		}
	}

	public void Dispose()
	{
		lock (_sync)
		{
			if (_subscribed)
			{
				_eventHub.EventPublished -= OnHubEvent;
				_subscribed = false;
			}
		}
	}

	private void EnsureSubscribed()
	{
		if (!_subscribed)
		{
			_eventHub.EventPublished += OnHubEvent;
			_subscribed = true;
		}
	}

	private void OnHubEvent(object? sender, FaultScenarioEvent e) => MapFaultEvent(e);

	private void MapFaultEvent(FaultScenarioEvent e)
	{
		lock (_sync)
		{
			if (_activeExperimentId == null || _activeRunId == null || e.MachineId != _activeMachineId)
			{
				return;
			}

			var mapped = MapEventType(e.EventType);
			if (mapped == null)
			{
				return;
			}

			TimeSpan scenarioRelative = ResolveScenarioRelativeTime(e);
			TimeSpan experimentTime = _experimentClock;
			TimeSpan runRelative = experimentTime - _runStartExperimentTime;

			if (mapped == GroundTruthEventType.DegradationBecameDetectable
				&& _scenarioStartExperimentTime > TimeSpan.Zero
				&& experimentTime < _scenarioStartExperimentTime)
			{
				return;
			}

			var metadata = BuildEventMetadata(e);

			RecordEvent(
				mapped.Value,
				experimentTime,
				runRelative,
				e.ScenarioId,
				e.Phase?.ToString(),
				null,
				0,
				0,
				metadata,
				scenarioRelative);
		}
	}

	private static Dictionary<string, string>? BuildEventMetadata(FaultScenarioEvent e)
	{
		if (e.Metadata != null && e.Metadata.Count > 0)
		{
			return new Dictionary<string, string>(e.Metadata);
		}

		if (!string.IsNullOrEmpty(e.Detail))
		{
			return new Dictionary<string, string> { ["detail"] = e.Detail };
		}

		return null;
	}

	private TimeSpan ResolveScenarioRelativeTime(FaultScenarioEvent e)
	{
		var session = _faultScenarioService.GetSession(e.MachineId);
		if (session != null && e.InstanceId != Guid.Empty)
		{
			var instance = session.Simulation.FaultScenarios.ActiveInstances.Values
				.FirstOrDefault(i => i.InstanceId == e.InstanceId);
			if (instance != null)
			{
				return instance.ScenarioElapsedTime;
			}
		}

		if (_scenarioStartExperimentTime > TimeSpan.Zero)
		{
			return _experimentClock - _scenarioStartExperimentTime;
		}
		return TimeSpan.Zero;
	}

	private static bool IsUniqueLifecycleEvent(GroundTruthEventType type) => type is
		GroundTruthEventType.ScenarioStarted
		or GroundTruthEventType.DegradationBecameDetectable
		or GroundTruthEventType.ThresholdFirstReached
		or GroundTruthEventType.ThresholdConfirmed
		or GroundTruthEventType.MachineFaulted
		or GroundTruthEventType.RecoveryStarted
		or GroundTruthEventType.RecoveryCompleted
		or GroundTruthEventType.ScenarioStopped
		or GroundTruthEventType.NormalObservationStarted
		or GroundTruthEventType.ExperimentStarted
		or GroundTruthEventType.ExperimentCompleted;

	private static GroundTruthEventType? MapEventType(FaultScenarioEventType type) => type switch
	{
		FaultScenarioEventType.ScenarioStarted => GroundTruthEventType.ScenarioStarted,
		FaultScenarioEventType.ScenarioPhaseChanged => GroundTruthEventType.ScenarioPhaseChanged,
		FaultScenarioEventType.DegradationBecameDetectable => GroundTruthEventType.DegradationBecameDetectable,
		FaultScenarioEventType.ThresholdApproaching => GroundTruthEventType.ThresholdApproaching,
		FaultScenarioEventType.ThresholdReached => GroundTruthEventType.ThresholdFirstReached,
		FaultScenarioEventType.ThresholdEntered => GroundTruthEventType.ThresholdEntered,
		FaultScenarioEventType.ThresholdExited => GroundTruthEventType.ThresholdExited,
		FaultScenarioEventType.ThresholdConfirmed => GroundTruthEventType.ThresholdConfirmed,
		FaultScenarioEventType.MachineFaulted => GroundTruthEventType.MachineFaulted,
		FaultScenarioEventType.RecoveryStarted => GroundTruthEventType.RecoveryStarted,
		FaultScenarioEventType.RecoveryCompleted => GroundTruthEventType.RecoveryCompleted,
		FaultScenarioEventType.ScenarioStopped => GroundTruthEventType.ScenarioStopped,
		_ => null
	};
}
