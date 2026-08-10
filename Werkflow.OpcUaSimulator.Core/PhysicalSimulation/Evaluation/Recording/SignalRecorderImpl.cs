using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Recording;

public sealed class SignalRecorder : ISignalRecorder
{
	private readonly SignalRecordingConfiguration _config;
	private readonly object _sync = new();
	private string? _activeRunId;
	private DateTimeOffset _lastSampleUtc;
	private readonly Dictionary<string, List<SignalSample>> _samples = new(StringComparer.OrdinalIgnoreCase);

	public SignalRecorder(SignalRecordingConfiguration config)
	{
		_config = config;
	}

	public void BeginRun(string runId)
	{
		lock (_sync)
		{
			_activeRunId = runId;
			_lastSampleUtc = DateTimeOffset.MinValue;
			if (!_samples.ContainsKey(runId))
			{
				_samples[runId] = [];
			}
		}
	}

	public void Record(PhysicalMachineSession session, TimeSpan simulationTime)
	{
		lock (_sync)
		{
			if (_activeRunId == null)
			{
				return;
			}

			var now = DateTimeOffset.UtcNow;
			if (now - _lastSampleUtc < _config.RecordingInterval && _samples[_activeRunId].Count > 0)
			{
				return;
			}

			_lastSampleUtc = now;
			var sample = new SignalSample
			{
				RunId = _activeRunId,
				RealTimestampUtc = now,
				SimulationTimestamp = simulationTime
			};

			foreach (var signalId in _config.SignalIds)
			{
				var signal = session.Runtime.Signals.FirstOrDefault(s =>
					s.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase));
				if (signal != null)
				{
					sample.Signals[signalId] = signal.CurrentValue;
				}
			}

			if (_config.RecordStandardStatus)
			{
				var errorSignal = session.Runtime.Signals.FirstOrDefault(s =>
					s.SignalId.Equals("ErrorActive", StringComparison.OrdinalIgnoreCase));
				sample.ErrorActive = errorSignal?.CurrentValue > 0.5;
				sample.MachineState = session.Simulation.CurrentPhase.ToString();
				sample.ErrorMessage = session.Simulation.FaultScenarios.ActiveFaultCodes.FirstOrDefault();
			}

			_samples[_activeRunId].Add(sample);
		}
	}

	public IReadOnlyList<SignalSample> GetSamples(string runId)
	{
		lock (_sync)
		{
			return _samples.TryGetValue(runId, out var list) ? list.ToList() : [];
		}
	}

	public void CompleteRun()
	{
		lock (_sync)
		{
			_activeRunId = null;
		}
	}
}
