using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Publishing;

public sealed class PhysicalSignalPublisher : IPhysicalSignalPublisher
{
	private readonly PhysicalMachineSession _session;

	private readonly IPhysicalSignalNodeRegistry _registry;

	private readonly IPhysicalSignalTypeMapper _typeMapper;

	private readonly TechnicalSignalValueGenerator _valueGenerator;

	private readonly IPhysicalRuntimeCoordinator _runtimeCoordinator;

	private readonly ILogService _logService;

	private readonly ISystemContext _systemContext;

	private readonly object _sync = new object();

	private readonly Dictionary<string, DateTimeOffset> _nextDue = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);

	private readonly Stopwatch _rateWatch = Stopwatch.StartNew();

	private CancellationTokenSource? _cts;

	private Task? _loopTask;

	private int _updatesInWindow;

	private double _durationSumMs;

	private int _durationCount;

	private int _seed;

	private int _resumePublishCycles;

	private DateTimeOffset _lastPhysicsTick = DateTimeOffset.UtcNow;

	public PhysicalPublisherState State => _session.Metrics.State;

	public PhysicalSignalPublisher(PhysicalMachineSession session, IPhysicalSignalNodeRegistry registry, IPhysicalSignalTypeMapper typeMapper, TechnicalSignalValueGenerator valueGenerator, IPhysicalRuntimeCoordinator runtimeCoordinator, ILogService logService, ISystemContext systemContext, int seed)
	{
		_session = session;
		_registry = registry;
		_typeMapper = typeMapper;
		_valueGenerator = valueGenerator;
		_runtimeCoordinator = runtimeCoordinator;
		_logService = logService;
		_systemContext = systemContext;
		_seed = seed;
	}

	public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			if (_loopTask != null && !_loopTask.IsCompleted)
			{
				return Task.CompletedTask;
			}
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_session.Metrics.State = PhysicalPublisherState.Running;
			DateTimeOffset utcNow = DateTimeOffset.UtcNow;
			foreach (SignalDefinition item in _session.Profile.Signals.Where((SignalDefinition s) => s.IsEnabled))
			{
				_nextDue[item.SignalId] = utcNow;
			}
			_loopTask = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
			_logService.Log(LogCategory.Server, $"Physikalischer Publisher gestartet ({_registry.Count} Nodes)", _session.MachineName);
		}
		return Task.CompletedTask;
	}

	public Task PauseAsync()
	{
		_session.Metrics.State = PhysicalPublisherState.Paused;
		_logService.Log(LogCategory.Server, "Physikalischer Publisher pausiert", _session.MachineName);
		return Task.CompletedTask;
	}

	public Task ResumeAsync()
	{
		_session.Metrics.State = PhysicalPublisherState.Running;
		foreach (SignalRuntimeState signal in _session.Runtime.Signals)
		{
			signal.UpdateSequence++;
		}
		DateTimeOffset utcNow = DateTimeOffset.UtcNow;
		foreach (SignalDefinition item in _session.Profile.Signals.Where((SignalDefinition s) => s.IsEnabled))
		{
			_nextDue[item.SignalId] = utcNow;
		}
		_resumePublishCycles = 2;
		_logService.Log(LogCategory.Server, "Physikalischer Publisher fortgesetzt", _session.MachineName);
		return Task.CompletedTask;
	}

	public async Task StopAsync()
	{
		CancellationTokenSource cts;
		Task loop;
		lock (_sync)
		{
			cts = _cts;
			loop = _loopTask;
			_cts = null;
			_loopTask = null;
			_session.Metrics.State = PhysicalPublisherState.Stopped;
		}
		if (cts != null)
		{
			await cts.CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
			cts.Dispose();
		}
		if (loop != null)
		{
			try
			{
				await loop.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
			}
		}
		_logService.Log(LogCategory.Server, "Physikalischer Publisher gestoppt", _session.MachineName);
	}

	public bool PublishSignal(string signalId, object? value, bool force = false)
	{
		if (!_registry.TryGetBySignalId(signalId, out PhysicalSignalNodeEntry entry))
		{
			_session.Metrics.FailedUpdates++;
			_session.Metrics.LastError = "Node für Signal '" + signalId + "' nicht gefunden.";
			_logService.Log(LogCategory.Error, _session.Metrics.LastError, _session.MachineName);
			return false;
		}
		try
		{
			SignalRuntimeState signalRuntimeState = _session.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == signalId);
			object obj = _typeMapper.ConvertToOpcValue(entry.Definition.DataType, value);
			if (!force && _typeMapper.AreValuesEqual(entry.Definition.DataType, entry.Variable.Value, obj))
			{
				_session.Metrics.SkippedIdenticalValues++;
				return true;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			SignalRuntimeValueHelper.SetCurrentValue(entry.Definition, signalRuntimeState, obj);
			signalRuntimeState.LastUpdatedAt = DateTimeOffset.UtcNow;
			signalRuntimeState.LastChangedAt = signalRuntimeState.LastUpdatedAt;
			entry.Variable.Value = obj;
			entry.Variable.Timestamp = DateTime.UtcNow;
			entry.Variable.StatusCode = 0u;
			entry.Variable.ClearChangeMasks(_systemContext, includeChildren: false);
			stopwatch.Stop();
			RecordMetrics(stopwatch.Elapsed.TotalMilliseconds);
			_session.Metrics.TotalPublishedUpdates++;
			return true;
		}
		catch (Exception ex)
		{
			_session.Metrics.FailedUpdates++;
			_session.Metrics.LastError = ex.Message;
			_logService.Log(LogCategory.Error, "Signalupdate fehlgeschlagen (" + signalId + "): " + ex.Message, _session.MachineName);
			return false;
		}
	}

	private async Task RunLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if (_session.Metrics.State != PhysicalPublisherState.Running || _session.IsPublisherManualOverride)
			{
				await Task.Delay(100, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				continue;
			}
			DateTimeOffset now = DateTimeOffset.UtcNow;
			if (_session.Simulation.GenerationMode == SignalGenerationMode.Physical)
			{
				TimeSpan physicsDelta = now - _lastPhysicsTick;
				if (physicsDelta > TimeSpan.FromMilliseconds(100.0))
				{
					_runtimeCoordinator.Tick(_session, physicsDelta);
					_lastPhysicsTick = now;
				}
			}
			foreach (SignalDefinition signal in _session.Profile.Signals.Where((SignalDefinition s) => s.IsEnabled))
			{
				if (!_nextDue.TryGetValue(signal.SignalId, out var due) || now < due)
				{
					continue;
				}
				SignalRuntimeState runtime = _session.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == signal.SignalId);
				long nextSeq = runtime.UpdateSequence + 1;
				SignalGenerationMode generationMode = _session.Simulation.GenerationMode;
				SignalGenerationMode signalGenerationMode = generationMode;
				SignalGenerationMode signalGenerationMode2 = signalGenerationMode;
				object nextValue;
				if (signalGenerationMode2 != SignalGenerationMode.Physical)
				{
					if (signalGenerationMode2 == SignalGenerationMode.Manual)
					{
						continue;
					}
					nextValue = _valueGenerator.GenerateNextValue(signal, runtime, _seed, nextSeq);
				}
				else
				{
					nextValue = SignalRuntimeValueHelper.GetCurrentValue(signal, runtime);
				}
				PublishSignal(signal.SignalId, nextValue, _resumePublishCycles > 0);
				runtime.UpdateSequence = nextSeq;
				_nextDue[signal.SignalId] = now.Add(signal.UpdateInterval);
			}
			if (_resumePublishCycles > 0)
			{
				_resumePublishCycles--;
			}
			if (_rateWatch.Elapsed.TotalSeconds >= 1.0)
			{
				_session.Metrics.UpdatesPerSecond = (double)_updatesInWindow / _rateWatch.Elapsed.TotalSeconds;
				_updatesInWindow = 0;
				_rateWatch.Restart();
			}
			await Task.Delay(50, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void RecordMetrics(double durationMs)
	{
		_session.Metrics.LastPublishAt = DateTimeOffset.UtcNow;
		_session.Metrics.PublishedSignalCount = _registry.Count;
		_durationSumMs += durationMs;
		_durationCount++;
		_session.Metrics.AveragePublishDurationMs = _durationSumMs / (double)_durationCount;
		if (durationMs > _session.Metrics.MaxPublishDurationMs)
		{
			_session.Metrics.MaxPublishDurationMs = durationMs;
		}
		_updatesInWindow++;
	}
}
