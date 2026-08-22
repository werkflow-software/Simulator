using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Utilities;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class SimulationEngine : ISimulationEngine, IDisposable
{
	private sealed class MachineLoopContext
	{
		public CancellationTokenSource Cts { get; }

		public Task LoopTask { get; set; } = Task.CompletedTask;

		public MachineLoopContext(CancellationTokenSource cts)
		{
			Cts = cts;
		}
	}

	private readonly IConfigurationService _configurationService;

	private readonly IMachineServerService _machineServerService;

	private readonly IMachineValuePublisher _valuePublisher;

	private readonly IJobGenerator _jobGenerator;

	private readonly IJobDispatcher _jobDispatcher;

	private readonly IValidationService _validationService;

	private readonly ILogService _logService;

	private readonly IPhysicalSignalPublishingCoordinator? _physicalCoordinator;

	private readonly object _sync = new object();

	private readonly Dictionary<Guid, MachineRuntimeState> _runtimeStates = new Dictionary<Guid, MachineRuntimeState>();

	private readonly Dictionary<Guid, MachineLoopContext> _loops = new Dictionary<Guid, MachineLoopContext>();

	private readonly Dictionary<Guid, int> _manualIntervals = new Dictionary<Guid, int>();

	private readonly Dictionary<Guid, int> _manualStepSizes = new Dictionary<Guid, int>();

	private readonly Dictionary<Guid, object> _jobChangeLocks = new Dictionary<Guid, object>();

	private CancellationTokenSource? _globalCts;

	private SimulationState _state = SimulationState.Stopped;

	private Random _random = new Random();

	private DateTime? _startedAt;

	private int _totalProducedParts;

	private int _currentSeed;

	public SimulationState State
	{
		get
		{
			lock (_sync)
			{
				return _state;
			}
		}
	}

	public DateTime? StartedAt
	{
		get
		{
			lock (_sync)
			{
				return _startedAt;
			}
		}
	}

	public int TotalProducedParts
	{
		get
		{
			lock (_sync)
			{
				return _totalProducedParts;
			}
		}
	}

	public int ActiveErrorCount
	{
		get
		{
			lock (_sync)
			{
				return _runtimeStates.Values.Count((MachineRuntimeState s) => s.ErrorActive);
			}
		}
	}

	public int RunningServerCount => _configurationService.Configuration.Machines.Count((MachineConfiguration m) => m.IsActive && _machineServerService.IsRunning(m.Id));

	public int TotalConnectedClients => _configurationService.Configuration.Machines.Sum((MachineConfiguration m) => _machineServerService.GetConnectedClients(m.Id));

	public int CurrentSeed
	{
		get
		{
			lock (_sync)
			{
				return _currentSeed;
			}
		}
	}

	public event EventHandler? StateChanged;

	public event EventHandler<MachineRuntimeState>? MachineStateChanged;

	public SimulationEngine(IConfigurationService configurationService, IMachineServerService machineServerService, IMachineValuePublisher valuePublisher, IJobGenerator jobGenerator, IJobDispatcher jobDispatcher, IValidationService validationService, ILogService logService, IPhysicalSignalPublishingCoordinator? physicalCoordinator = null)
	{
		_configurationService = configurationService;
		_machineServerService = machineServerService;
		_valuePublisher = valuePublisher;
		_jobGenerator = jobGenerator;
		_jobDispatcher = jobDispatcher;
		_validationService = validationService;
		_logService = logService;
		_physicalCoordinator = physicalCoordinator;
		_machineServerService.ServerStatusChanged += OnServerStatusChanged;
		EnsureRuntimeStates();
	}

	public IReadOnlyDictionary<Guid, MachineRuntimeState> GetRuntimeStates()
	{
		lock (_sync)
		{
			return _runtimeStates.ToDictionary<KeyValuePair<Guid, MachineRuntimeState>, Guid, MachineRuntimeState>((KeyValuePair<Guid, MachineRuntimeState> kv) => kv.Key, (KeyValuePair<Guid, MachineRuntimeState> kv) => kv.Value.CloneValues());
		}
	}

	public MachineRuntimeState? GetRuntimeState(Guid machineId)
	{
		lock (_sync)
		{
			MachineRuntimeState value;
			return _runtimeStates.TryGetValue(machineId, out value) ? value.CloneValues() : null;
		}
	}

	public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidationResult validation = _validationService.ValidateForSimulationStart(_configurationService.Configuration);
		if (!validation.IsValid)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
		}
		lock (_sync)
		{
			if (_state == SimulationState.Running)
			{
				return;
			}
			SimulationSettings settings = _configurationService.Configuration.Settings;
			_currentSeed = (settings.GenerateNewSeedOnStart ? Environment.TickCount : settings.RandomSeed);
			_random = SimulationRandom.Create(_currentSeed);
			_globalCts?.Cancel();
			_globalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_state = SimulationState.Running;
			_startedAt = DateTime.UtcNow;
		}
		_logService.Log(LogCategory.Production, $"Simulation gestartet (Seed: {_currentSeed})");
		_jobGenerator.RegenerateJobs(_configurationService.Configuration, _random);
		await _configurationService.SaveJobsAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_jobDispatcher.AssignJobs(_configurationService.Configuration, _random);
		EnsureRuntimeStates();
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines.Where((MachineConfiguration m) => m.IsActive))
		{
			await StartMachineInternalAsync(machine, assignJob: true, _globalCts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		NotifyStateChanged();
	}

	public async Task PauseAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			if (_state != SimulationState.Running)
			{
				return;
			}
			_state = SimulationState.Paused;
			foreach (MachineRuntimeState runtime in _runtimeStates.Values)
			{
				runtime.IsProducing = false;
				MachineState state = runtime.State;
				if ((uint)(state - 2) <= 1u)
				{
					SetMachineState(runtime, MachineState.Paused);
				}
			}
		}
		PublishAll();
		if (_physicalCoordinator != null)
		{
			await _physicalCoordinator.PauseAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		_logService.Log(LogCategory.Production, "Simulation pausiert");
		NotifyStateChanged();
	}

	public async Task ResumeAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			if (_state != SimulationState.Paused)
			{
				return;
			}
			_state = SimulationState.Running;
			foreach (MachineRuntimeState runtime in _runtimeStates.Values.Where((MachineRuntimeState r) => r.IsServerOnline && !r.ErrorActive && !r.IsCounterFrozen))
			{
				if (runtime.State == MachineState.Paused)
				{
					SetMachineState(runtime, MachineState.Running);
				}
				runtime.IsProducing = true;
			}
		}
		PublishAll();
		if (_physicalCoordinator != null)
		{
			await _physicalCoordinator.ResumeAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		_logService.Log(LogCategory.Production, "Simulation fortgesetzt");
		NotifyStateChanged();
	}

	public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			_globalCts?.Cancel();
			_state = SimulationState.Stopped;
		}
		await StopAllLoopsAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (_physicalCoordinator != null)
		{
			await _physicalCoordinator.StopAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		await _machineServerService.StopAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		lock (_sync)
		{
			ResetRuntimeValues(keepServersOffline: true);
			_startedAt = null;
			_totalProducedParts = 0;
		}
		_logService.Log(LogCategory.Production, "Simulation gestoppt");
		NotifyStateChanged();
	}

	public Task ResetAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			foreach (MachineRuntimeState value in _runtimeStates.Values)
			{
				value.ActualCounter = 0;
				value.ErrorActive = false;
				value.ErrorMessage = string.Empty;
				value.DisruptedStateStartedAt = null;
				value.IsCounterFrozen = false;
				value.IsProducing = false;
				value.AssignedJobId = null;
				if (value.IsServerOnline)
				{
					SetMachineState(value, MachineState.Idle);
				}
			}
			foreach (SimulationJob job in _configurationService.Configuration.Jobs)
			{
				if (job.Status != JobState.Completed)
				{
					job.Status = JobState.Pending;
					job.AssignedMachineId = null;
					job.ActualCounter = 0;
					job.StartedAt = null;
					job.CompletedAt = null;
				}
			}
			_totalProducedParts = 0;
		}
		PublishAll();
		_logService.Log(LogCategory.Production, "Laufzeitwerte zurückgesetzt");
		NotifyStateChanged();
		return Task.CompletedTask;
	}

	public async Task StartMachineServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = _configurationService.Configuration.Machines.FirstOrDefault((MachineConfiguration m) => m.Id == machineId) ?? throw new InvalidOperationException("Maschine nicht gefunden.");
		ValidationResult validation = _validationService.ValidateMachine(machine, _configurationService.Configuration.Machines);
		if (!validation.IsValid)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
		}
		EnsureRuntimeStates();
		bool isVirtualMachine = VirtualLaserMachineRegistry.IsVirtualLaserMachine(machineId);
		CancellationToken loopToken = isVirtualMachine ? EnsureStandaloneEngineReady(cancellationToken) : cancellationToken;
		await StartMachineInternalAsync(machine, assignJob: isVirtualMachine, loopToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task StopMachineServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SetMachineOfflineAsync(machineId, cancellationToken);
	}

	public void ApplyManualValues(Guid machineId, string partName, string jobName, int actualCounter, int targetCounter, MachineState state, bool errorActive, string errorMessage, int productionIntervalMs, int stepSize)
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.PartName = partName;
		runtime.JobName = jobName;
		runtime.ActualCounter = actualCounter;
		runtime.TargetCounter = targetCounter;
		runtime.ErrorActive = errorActive;
		runtime.ErrorMessage = errorMessage;
		runtime.DisruptedStateStartedAt = (errorActive ? new DateTime?(DateTime.UtcNow) : ((DateTime?)null));
		SetMachineState(runtime, state);
		_manualIntervals[machineId] = productionIntervalMs;
		_manualStepSizes[machineId] = Math.Max(1, stepSize);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Manuelle Werte übernommen", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public Task StartProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (!CanProduce(runtime))
		{
			return Task.CompletedTask;
		}

		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machineId) && _physicalCoordinator != null)
		{
			return StartVirtualMachineProductionAsync(machine, runtime, cancellationToken);
		}

		runtime.IsProducing = true;
		SetMachineState(runtime, MachineState.Running);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Produktion gestartet", machine.Name);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	private async Task StartVirtualMachineProductionAsync(
		MachineConfiguration machine,
		MachineRuntimeState runtime,
		CancellationToken cancellationToken)
	{
		if (_physicalCoordinator != null)
		{
			await _physicalCoordinator.ResumeProductionAsync(machine.Id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}

		runtime.IsProducing = true;
		SetMachineState(runtime, MachineState.Running);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Produktion gestartet", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public async Task PauseProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.IsProducing = false;
		SetMachineState(runtime, MachineState.Paused);
		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machineId) && _physicalCoordinator != null)
		{
			await _physicalCoordinator.PauseProductionAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}

		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Produktion pausiert", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public async Task ResumeProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machineId) && _physicalCoordinator != null)
		{
			await _physicalCoordinator.ResumeProductionAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}

		runtime.IsProducing = true;
		SetMachineState(runtime, MachineState.Running);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Produktion fortgesetzt", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public Task StopProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machineId) && _physicalCoordinator != null)
		{
			_physicalCoordinator.StopProduction(machineId);
			runtime.ActualCounter = 0;
			_physicalCoordinator.SyncProductionCounters(machineId, 0, runtime.TargetCounter);
			if (runtime.AssignedJobId.HasValue)
			{
				SimulationJob? job = _configurationService.Configuration.Jobs
					.FirstOrDefault(j => j.Id == runtime.AssignedJobId);
				if (job != null)
				{
					job.ActualCounter = 0;
				}
			}
		}

		runtime.IsProducing = false;
		SetMachineState(runtime, MachineState.Idle);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Produktion gestoppt (Job zurückgesetzt)", machine.Name);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public Task ProduceNextPartAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (!CanIncrement(runtime))
		{
			return Task.CompletedTask;
		}
		IncrementCounter(machine, runtime, 1);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public Task TriggerErrorAsync(Guid machineId, string? message = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		string text = message ?? GetRandomErrorMessage();
		runtime.ErrorActive = true;
		runtime.ErrorMessage = text;
		runtime.DisruptedStateStartedAt = DateTime.UtcNow;
		runtime.IsProducing = false;
		SetMachineState(runtime, MachineState.Error);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Error, text, machine.Name, "false", "true");
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public Task ClearErrorAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.ErrorActive = false;
		runtime.ErrorMessage = string.Empty;
		runtime.DisruptedStateStartedAt = null;
		SetMachineState(runtime, runtime.IsServerOnline ? MachineState.Idle : MachineState.Offline);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Error, "Fehler zurückgesetzt", machine.Name);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public async Task SetMachineOfflineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.IsProducing = false;
		runtime.IsDisconnected = true;
		await StopMachineLoopAsync(machineId).ConfigureAwait(continueOnCapturedContext: false);
		await _machineServerService.StopServerAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		SetMachineState(runtime, MachineState.Offline);
		runtime.IsServerOnline = false;
		_logService.Log(LogCategory.Connection, "Maschine offline geschaltet", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public async Task SetMachineOnlineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.IsDisconnected = false;
		runtime.DisruptedStateStartedAt = null;
		await _machineServerService.StartServerAsync(machine, runtime, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (_state == SimulationState.Running)
		{
			runtime.IsProducing = true;
			SetMachineState(runtime, MachineState.Running);
		}
		else
		{
			SetMachineState(runtime, MachineState.Idle);
		}
		runtime.IsServerOnline = true;
		PublishMachine(machine, runtime);
		await StartMachineLoopAsync(machine, runtime, _globalCts?.Token ?? cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_logService.Log(LogCategory.Connection, "Maschine wieder online", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public Task CompleteJobAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.ActualCounter = runtime.TargetCounter;
		HandleJobCompletion(machine, runtime);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public Task ResetCountersAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.ActualCounter = 0;
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, "Zähler zurückgesetzt", machine.Name);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public void SetCounterFrozen(Guid machineId, bool frozen)
	{
		MachineRuntimeState runtime = GetRuntime(machineId);
		runtime.IsCounterFrozen = frozen;
		MachineConfiguration machine = GetMachine(machineId);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Production, frozen ? "Zähler eingefroren" : "Zähler fortgesetzt", machine.Name);
		NotifyMachineChanged(runtime);
	}

	public Task SetMachineStateManualAsync(Guid machineId, MachineState state, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		SetMachineState(runtime, state);
		PublishMachine(machine, runtime);
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public Task AssignJobIfMissingAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (IsJobUnassigned(runtime))
		{
			EnsureJobPoolReady();
			AssignJobToMachine(machine, runtime);
			PublishMachine(machine, runtime);
		}
		NotifyMachineChanged(runtime);
		return Task.CompletedTask;
	}

	public Task ChangeJobAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (runtime.IsJobChangeActive)
		{
			return Task.CompletedTask;
		}

		int nextIndex = runtime.CurrentJobCatalogIndex < 0
			? 0
			: FixedSimulationCatalog.GetNextCatalogIndex(runtime.CurrentJobCatalogIndex);
		return ScheduleJobChangeInternal(machine, runtime, nextIndex, cancellationToken);
	}

	public Task SelectJobAsync(Guid machineId, int catalogIndex, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (catalogIndex < 0 || catalogIndex >= FixedSimulationCatalog.JobCount)
		{
			throw new ArgumentOutOfRangeException(nameof(catalogIndex));
		}

		MachineConfiguration machine = GetMachine(machineId);
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (runtime.IsJobChangeActive)
		{
			return Task.CompletedTask;
		}

		return ScheduleJobChangeInternal(machine, runtime, catalogIndex, cancellationToken);
	}

	public (double partRemainingSeconds, double jobRemainingSeconds) GetProductionTimeEstimates(Guid machineId)
	{
		if (_physicalCoordinator == null)
		{
			return (0.0, 0.0);
		}

		return _physicalCoordinator.GetProductionTimeEstimates(machineId);
	}

	public double GetSetupRemainingSeconds(Guid machineId) =>
		_physicalCoordinator?.GetSetupRemainingSeconds(machineId) ?? 0.0;

	public double GetNozzleChangeRemainingSeconds(Guid machineId) =>
		_physicalCoordinator?.GetNozzleChangeRemainingSeconds(machineId) ?? 0.0;

	private Task ScheduleJobChangeInternal(
		MachineConfiguration machine,
		MachineRuntimeState runtime,
		int nextCatalogIndex,
		CancellationToken cancellationToken)
	{
		if (runtime.AssignedJobId.HasValue)
		{
			SimulationJob? currentJob = _configurationService.Configuration.Jobs.FirstOrDefault(j => j.Id == runtime.AssignedJobId);
			if (currentJob != null && currentJob.Status != JobState.Completed)
			{
				_jobDispatcher.CompleteJob(currentJob, runtime);
			}

			runtime.AssignedJobId = null;
		}

		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id)
			&& _physicalCoordinator != null
			&& (runtime.IsProducing || runtime.State == MachineState.Running))
		{
			FixedProductionJobDefinition nextDefinition = ResolveJobDefinition(machine.Id, nextCatalogIndex);
			_physicalCoordinator.AbortProductionForJobChange(machine.Id, nextDefinition);
		}

		ScheduleJobChange(machine, runtime, nextCatalogIndex, cancellationToken);
		return Task.CompletedTask;
	}

	private async Task StartMachineInternalAsync(MachineConfiguration machine, bool assignJob, CancellationToken cancellationToken)
	{
		MachineRuntimeState runtime = GetRuntime(machine.Id);
		runtime.MachineId = machine.Id;
		if (machine.StartInErrorState && CanAcceptAutomaticDisruption(machine.Id))
		{
			runtime.ErrorActive = true;
			runtime.ErrorMessage = GetRandomErrorMessage();
			runtime.DisruptedStateStartedAt = DateTime.UtcNow;
			runtime.State = MachineState.Error;
			ScheduleAutomaticDisruptionRecovery(machine.Id, MachineState.Error, 60000, cancellationToken);
		}
		else
		{
			runtime.State = machine.BaseState;
		}
		try
		{
			_physicalCoordinator?.PrepareMachine(machine, _currentSeed);
		}
		catch (Exception ex)
		{
			_logService.Log(LogCategory.Error, "Physisches Profil konnte nicht geladen werden: " + ex.Message, machine.Name);
			throw;
		}
		await _machineServerService.StartServerAsync(machine, runtime, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		runtime.IsServerOnline = true;
		if (assignJob)
		{
			AssignJobToMachine(machine, runtime);
		}
		PublishMachine(machine, runtime);
		await StartMachineLoopAsync(machine, runtime, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		NotifyMachineChanged(runtime);
	}

	private async Task StartMachineLoopAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken)
	{
		await StopMachineLoopAsync(machine.Id).ConfigureAwait(continueOnCapturedContext: false);
		CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		MachineLoopContext context = new MachineLoopContext(cts);
		_loops[machine.Id] = context;
		Task.Run(async delegate
		{
			DateTime lastProductionTick = DateTime.UtcNow;
			DateTime lastHeartbeatTick = DateTime.UtcNow;
			DateTime nextEventCheck = DateTime.UtcNow.AddMilliseconds(SimulationRandom.NextInRange(_random, 2000, 8000));
			while (!cts.Token.IsCancellationRequested)
			{
				try
				{
					SimulationSettings settings = _configurationService.Configuration.Settings;
					DateTime now = DateTime.UtcNow;
					double speed = settings.SimulationSpeedFactor * machine.ProductionSpeedFactor;
					if ((now - lastHeartbeatTick).TotalMilliseconds >= (double)settings.HeartbeatIntervalMs)
					{
						runtime.Heartbeat++;
						lastHeartbeatTick = now;
						_valuePublisher.PublishValue(machine.Id, NodeSemanticType.Heartbeat, runtime.Heartbeat, machine.Nodes);
					}
					int interval = (_manualIntervals.TryGetValue(machine.Id, out var manualInterval) ? manualInterval : machine.ProductionIntervalMs);
					interval = SimulationRandom.ScaleInterval(interval, speed);
					int manualStep;
					int step = (_manualStepSizes.TryGetValue(machine.Id, out manualStep) ? manualStep : machine.ProductionStepSize);
					if (ShouldTickProduction(machine, runtime) && (now - lastProductionTick).TotalMilliseconds >= (double)interval)
					{
						IncrementCounter(machine, runtime, step);
						lastProductionTick = now;
					}
					if (_physicalCoordinator != null
						&& VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id)
						&& runtime.IsProducing
						&& !runtime.IsJobChangeActive
						&& CanIncrement(runtime))
					{
						int pendingParts = _physicalCoordinator.ConsumePendingPartCompletions(machine.Id);
						for (int i = 0; i < pendingParts; i++)
						{
							IncrementCounter(machine, runtime, step);
						}
					}
					TryCompleteDueJobChange(machine, runtime);
					if (_state == SimulationState.Running && now >= nextEventCheck)
					{
						await EnforceDisruptionTimeoutsAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
						await ProcessRandomEventsAsync(machine, runtime, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
						nextEventCheck = now.AddMilliseconds(SimulationRandom.NextInRange(_random, 12000, 25000));
					}
					await Task.Delay(100, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex2)
				{
					_logService.Log(LogCategory.Error, "Simulationsfehler: " + ex2.Message, machine.Name);
					await Task.Delay(1000, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}, cts.Token);
	}

	private async Task ProcessRandomEventsAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken)
	{
		if (!machine.IsActive || _state != SimulationState.Running)
		{
			return;
		}
		if (!SimulationErrorPolicy.IsDisruptedState(runtime) && CanAcceptAutomaticDisruption(machine.Id) && SimulationRandom.Roll(_random, machine.DisconnectProbabilityPercent))
		{
			await TriggerAutomaticOfflineAsync(machine, runtime, cancellationToken, null).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		if (!SimulationErrorPolicy.IsDisruptedState(runtime) && CanAcceptAutomaticDisruption(machine.Id) && SimulationRandom.Roll(_random, machine.ErrorProbabilityPercent))
		{
			await TriggerAutomaticErrorAsync(machine, runtime, cancellationToken, null).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		foreach (EventTypeSettings evt in _configurationService.Configuration.Events.Events.Where((EventTypeSettings e) => e.IsEnabled))
		{
			SimulationEventType eventType = evt.EventType;
			bool flag = (((uint)eventType <= 1u || eventType == SimulationEventType.OpcUaDisconnect) ? true : false);
			if ((!flag || (!SimulationErrorPolicy.IsDisruptedState(runtime) && CanAcceptAutomaticDisruption(machine.Id))) && (evt.AffectedMachineIds.Count <= 0 || evt.AffectedMachineIds.Contains(machine.Id)) && SimulationRandom.Roll(_random, evt.ProbabilityPercent))
			{
				await ApplyConfiguredEventAsync(machine, runtime, evt, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private async Task ApplyConfiguredEventAsync(MachineConfiguration machine, MachineRuntimeState runtime, EventTypeSettings evt, CancellationToken cancellationToken)
	{
		int duration = SimulationErrorPolicy.CapDisruptedDuration(SimulationRandom.NextInRange(_random, evt.MinDurationMs, evt.MaxDurationMs));
		switch (evt.EventType)
		{
		case SimulationEventType.Warning:
			await TriggerAutomaticWarningAsync(machine, runtime, cancellationToken, duration).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.ProductionStop:
		case SimulationEventType.CounterFreeze:
			runtime.IsCounterFrozen = true;
			_logService.Log(LogCategory.Production, "Produktionsstillstand", machine.Name);
			Task.Run(async delegate
			{
				await Task.Delay(duration, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				runtime.IsCounterFrozen = false;
			}, cancellationToken);
			break;
		case SimulationEventType.CounterJump:
			IncrementCounter(machine, runtime, SimulationRandom.NextInRange(_random, 2, 8));
			break;
		case SimulationEventType.TargetQuantityChange:
			runtime.TargetCounter += SimulationRandom.NextInRange(_random, 5, 50);
			PublishMachine(machine, runtime);
			break;
		case SimulationEventType.SetupState:
			SetMachineState(runtime, MachineState.Setup);
			Task.Run(async delegate
			{
				await Task.Delay(duration, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				SetMachineState(runtime, MachineState.Running);
				PublishMachine(machine, runtime);
			}, cancellationToken);
			break;
		case SimulationEventType.JobChange:
			AssignJobToMachine(machine, runtime);
			break;
		case SimulationEventType.OpcUaDisconnect:
			await TriggerAutomaticOfflineAsync(machine, runtime, cancellationToken, duration).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.Error:
			await TriggerAutomaticErrorAsync(machine, runtime, cancellationToken, duration).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case SimulationEventType.SlowProduction:
		case SimulationEventType.FastProductionJump:
			break;
		}
	}

	private void IncrementCounter(MachineConfiguration machine, MachineRuntimeState runtime, int step)
	{
		int actualCounter = runtime.ActualCounter;
		runtime.ActualCounter += step;
		runtime.LastProductionChange = DateTime.UtcNow;
		lock (_sync)
		{
			_totalProducedParts += step;
		}
		if (runtime.AssignedJobId.HasValue)
		{
			SimulationJob simulationJob = _configurationService.Configuration.Jobs.FirstOrDefault(delegate(SimulationJob j)
			{
				Guid id = j.Id;
				Guid? assignedJobId = runtime.AssignedJobId;
				return id == assignedJobId;
			});
			if (simulationJob != null)
			{
				simulationJob.ActualCounter = runtime.ActualCounter;
				if (simulationJob.Status == JobState.Assigned)
				{
					simulationJob.Status = JobState.Running;
					SimulationJob simulationJob2 = simulationJob;
					DateTime? startedAt = simulationJob2.StartedAt;
					DateTime valueOrDefault = startedAt.GetValueOrDefault();
					if (!startedAt.HasValue)
					{
						valueOrDefault = DateTime.UtcNow;
						DateTime? startedAt2 = valueOrDefault;
						simulationJob2.StartedAt = startedAt2;
					}
				}
			}
		}
		PublishMachine(machine, runtime);
		_physicalCoordinator?.SyncProductionCounters(machine.Id, runtime.ActualCounter, runtime.TargetCounter);
		_logService.Log(LogCategory.Production, "Zähler erhöht", machine.Name, actualCounter.ToString(), runtime.ActualCounter.ToString());
		if (runtime.ActualCounter >= runtime.TargetCounter && runtime.TargetCounter > 0)
		{
			HandleJobCompletion(machine, runtime);
		}
	}

	private void HandleJobCompletion(MachineConfiguration machine, MachineRuntimeState runtime)
	{
		if (runtime.AssignedJobId.HasValue)
		{
			SimulationJob simulationJob = _configurationService.Configuration.Jobs.FirstOrDefault(delegate(SimulationJob j)
			{
				Guid id = j.Id;
				Guid? assignedJobId = runtime.AssignedJobId;
				return id == assignedJobId;
			});
			if (simulationJob != null)
			{
				_jobDispatcher.CompleteJob(simulationJob, runtime);
				_logService.Log(LogCategory.Job, "Auftrag abgeschlossen: " + simulationJob.JobName, machine.Name);
			}
		}

		runtime.IsProducing = false;
		SetMachineState(runtime, MachineState.Idle);
		PublishMachine(machine, runtime);

		if (!ShouldAutoContinueJobs(machine, runtime))
		{
			return;
		}

		int nextIndex = runtime.CurrentJobCatalogIndex < 0
			? 0
			: FixedSimulationCatalog.GetNextCatalogIndex(runtime.CurrentJobCatalogIndex);
		ScheduleJobChange(machine, runtime, nextIndex, _globalCts?.Token ?? CancellationToken.None);
	}

	private bool ShouldAutoContinueJobs(MachineConfiguration machine, MachineRuntimeState runtime)
	{
		if (!_configurationService.Configuration.Settings.AutoRestartCompletedJobs)
		{
			return false;
		}

		lock (_sync)
		{
			return _state == SimulationState.Running
				|| (_state == SimulationState.Stopped && runtime.IsServerOnline && VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id));
		}
	}

	private void ScheduleJobChange(
		MachineConfiguration machine,
		MachineRuntimeState runtime,
		int nextCatalogIndex,
		CancellationToken cancellationToken)
	{
		if (runtime.IsJobChangeActive)
		{
			return;
		}

		object jobChangeLock = GetJobChangeLock(machine.Id);
		lock (jobChangeLock)
		{
			if (runtime.IsJobChangeActive)
			{
				return;
			}

			FixedProductionJobDefinition nextDefinition = ResolveJobDefinition(machine.Id, nextCatalogIndex);
			VigilLabRunProfile.ResolveJobChangePauseRange(machine.Id, out int minPauseSeconds, out int maxPauseSeconds);
			int pauseSeconds = SimulationRandom.NextInRange(
				_random,
				minPauseSeconds,
				maxPauseSeconds);
			double speedFactor = Math.Max(0.1, _configurationService.Configuration.Settings.SimulationSpeedFactor * machine.ProductionSpeedFactor);
			int wallMs = (int)(pauseSeconds * 1000.0 / speedFactor);

			runtime.IsJobChangeActive = true;
			runtime.PendingNextJobCatalogIndex = nextCatalogIndex;
			runtime.IsProducing = false;
			runtime.JobChangePauseSeconds = pauseSeconds;
			runtime.JobChangeEndsAtUtc = DateTime.UtcNow.AddMilliseconds(wallMs);
			runtime.NextJobNamePreview = nextDefinition.JobName;
			runtime.NextPartNamePreview = nextDefinition.PartName;
			runtime.NextTargetQuantityPreview = nextDefinition.TargetQuantity;
			SetMachineState(runtime, MachineState.Setup);
			PublishMachine(machine, runtime);
			if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id) && _physicalCoordinator != null)
			{
				_physicalCoordinator.BeginJobChange(machine.Id, pauseSeconds, nextDefinition);
			}
			NotifyMachineChanged(runtime);

			Task.Run(async () =>
			{
				try
				{
					await Task.Delay(wallMs, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					if (!cancellationToken.IsCancellationRequested && runtime.IsJobChangeActive)
					{
						CompleteScheduledJobChange(machine, runtime, nextCatalogIndex);
					}
				}
				catch (OperationCanceledException)
				{
				}
			}, cancellationToken);
		}
	}

	private void CompleteScheduledJobChange(MachineConfiguration machine, MachineRuntimeState runtime, int catalogIndex)
	{
		object jobChangeLock = GetJobChangeLock(machine.Id);
		lock (jobChangeLock)
		{
			if (!runtime.IsJobChangeActive)
			{
				return;
			}

			EnsureJobPoolReady();
			SimulationJob? job = _jobDispatcher.GetJobByCatalogIndex(catalogIndex, _configurationService.Configuration);
			if (job == null)
			{
				runtime.IsJobChangeActive = false;
				runtime.JobChangeEndsAtUtc = null;
				runtime.PendingNextJobCatalogIndex = -1;
				return;
			}

			ApplyJobToRuntime(machine, runtime, job, ResolveJobDefinition(machine.Id, catalogIndex));
			runtime.IsJobChangeActive = false;
			runtime.JobChangeEndsAtUtc = null;
			runtime.PendingNextJobCatalogIndex = -1;
			runtime.IsProducing = true;
			SetMachineState(runtime, MachineState.Running);
			PublishMachine(machine, runtime);
			if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id) && _physicalCoordinator != null)
			{
				_ = _physicalCoordinator.ResumeProductionAsync(machine.Id).ConfigureAwait(false);
			}
			NotifyMachineChanged(runtime);
			_logService.Log(LogCategory.Job, "Auftrag geladen: " + job.JobName, machine.Name);
		}
	}

	private void AssignJobToMachine(MachineConfiguration machine, MachineRuntimeState runtime)
	{
		int catalogIndex = runtime.CurrentJobCatalogIndex < 0 ? 0 : runtime.CurrentJobCatalogIndex;
		EnsureJobPoolReady();
		SimulationJob? job = _jobDispatcher.GetJobByCatalogIndex(catalogIndex, _configurationService.Configuration);
		if (job != null)
		{
			ApplyJobToRuntime(machine, runtime, job, ResolveJobDefinition(machine.Id, catalogIndex));
		}
	}

	private void TryCompleteDueJobChange(MachineConfiguration machine, MachineRuntimeState runtime)
	{
		if (!runtime.IsJobChangeActive
			|| !runtime.JobChangeEndsAtUtc.HasValue
			|| DateTime.UtcNow < runtime.JobChangeEndsAtUtc.Value
			|| runtime.PendingNextJobCatalogIndex < 0)
		{
			return;
		}

		CompleteScheduledJobChange(machine, runtime, runtime.PendingNextJobCatalogIndex);
	}

	private void ApplyJobToRuntime(
		MachineConfiguration machine,
		MachineRuntimeState runtime,
		SimulationJob job,
		FixedProductionJobDefinition definition)
	{
		definition = ResolveJobDefinition(machine.Id, definition.CatalogIndex);
		VigilLabRunProfile.SynchronizeSimulationJob(job, machine.Id);
		VirtualPressBrakeRunProfile.SynchronizeSimulationJob(job, machine.Id);
		runtime.AssignedJobId = job.Id;
		runtime.CurrentJobCatalogIndex = definition.CatalogIndex;
		runtime.PartName = definition.PartName;
		runtime.JobName = definition.JobName;
		runtime.TargetCounter = definition.TargetQuantity;
		runtime.ActualCounter = 0;
		job.Status = JobState.Running;
		job.AssignedMachineId = machine.Id;
		job.StartedAt = DateTime.UtcNow;
		job.ActualCounter = 0;
		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id))
		{
			runtime.IsProducing = false;
			SetMachineState(runtime, MachineState.Idle);
		}
		else
		{
			runtime.IsProducing = true;
			SetMachineState(runtime, MachineState.Running);
		}

		_physicalCoordinator?.ApplyProductionJob(machine.Id, definition);
		_physicalCoordinator?.SyncProductionCounters(machine.Id, runtime.ActualCounter, runtime.TargetCounter);
		PublishMachine(machine, runtime);
		_logService.Log(LogCategory.Job, "Auftrag zugewiesen: " + job.JobName, machine.Name);
	}

	private object GetJobChangeLock(Guid machineId)
	{
		lock (_sync)
		{
			if (!_jobChangeLocks.TryGetValue(machineId, out object? jobChangeLock))
			{
				jobChangeLock = new object();
				_jobChangeLocks[machineId] = jobChangeLock;
			}

			return jobChangeLock;
		}
	}

	private static bool IsPlaceholderValue(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}
		string trimmed = value.Trim();
		return trimmed == "—" || trimmed == "-" || trimmed == "–";
	}

	private static bool IsJobUnassigned(MachineRuntimeState runtime)
	{
		if (runtime.AssignedJobId.HasValue)
		{
			return false;
		}
		return IsPlaceholderValue(runtime.JobName) || IsPlaceholderValue(runtime.PartName);
	}

	private CancellationToken EnsureStandaloneEngineReady(CancellationToken cancellationToken)
	{
		lock (_sync)
		{
			if (_globalCts == null || _globalCts.IsCancellationRequested)
			{
				_globalCts?.Dispose();
				_globalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			}
			if (_state == SimulationState.Stopped)
			{
				SimulationSettings settings = _configurationService.Configuration.Settings;
				_currentSeed = (settings.GenerateNewSeedOnStart ? Environment.TickCount : settings.RandomSeed);
				_random = SimulationRandom.Create(_currentSeed);
			}
		}
		EnsureJobPoolReady();
		return _globalCts!.Token;
	}

	private void EnsureJobPoolReady()
	{
		AppConfiguration configuration = _configurationService.Configuration;
		if (configuration.Jobs.Count != FixedSimulationCatalog.JobCount
			|| configuration.Jobs.Any(j => j.CatalogIndex < 0 || j.CatalogIndex >= FixedSimulationCatalog.JobCount))
		{
			_jobGenerator.RegenerateJobs(configuration, _random);
			return;
		}

		bool hasAssignableJob = configuration.Jobs.Any((SimulationJob j) =>
			j.Status == JobState.Pending || j.Status == JobState.Assigned
			|| (configuration.Settings.ReuseCompletedJobs && j.Status == JobState.Completed));
		if (!hasAssignableJob)
		{
			_jobGenerator.RegenerateJobs(configuration, _random);
		}

		SynchronizeJobPoolFromCatalog(configuration);
	}

	private static void SynchronizeJobPoolFromCatalog(AppConfiguration configuration)
	{
		foreach (SimulationJob job in configuration.Jobs)
		{
			if (job.CatalogIndex < 0 || job.CatalogIndex >= FixedSimulationCatalog.JobCount)
			{
				continue;
			}

			FixedProductionJobDefinition definition = FixedSimulationCatalog.GetDefinition(job.CatalogIndex);
			job.JobName = definition.JobName;
			job.PartName = definition.PartName;
			job.TargetQuantity = definition.TargetQuantity;
			job.MaterialName = definition.MaterialName;
			job.MaterialThicknessMm = definition.MaterialThicknessMm;
			job.RecipeName = definition.RecipeName;
			job.ProgramName = definition.ProgramName;
		}
	}

	private bool ShouldTickProduction(MachineConfiguration machine, MachineRuntimeState runtime)
	{
		if (VirtualKinematicsMachineRegistry.IsKinematicsDrivenMachine(machine.Id))
		{
			return false;
		}

		if (!runtime.IsProducing || !CanIncrement(runtime) || runtime.IsJobChangeActive)
		{
			return false;
		}
		lock (_sync)
		{
			return _state == SimulationState.Running || (_state == SimulationState.Stopped && runtime.IsServerOnline);
		}
	}

	private bool CanProduce(MachineRuntimeState runtime)
	{
		return runtime.IsServerOnline && !runtime.IsDisconnected;
	}

	private bool CanIncrement(MachineRuntimeState runtime)
	{
		if (!CanProduce(runtime) || runtime.IsCounterFrozen)
		{
			return false;
		}
		MachineConfiguration machineConfiguration = _configurationService.Configuration.Machines.FirstOrDefault((MachineConfiguration m) => m.Id == runtime.MachineId);
		if (runtime.ErrorActive && (machineConfiguration == null || !machineConfiguration.ContinueOnError))
		{
			return false;
		}
		if (runtime.State == MachineState.Warning && (machineConfiguration == null || !machineConfiguration.ContinueOnWarning))
		{
			return false;
		}
		return !runtime.ErrorActive || (machineConfiguration?.ContinueOnError ?? false);
	}

	private void SetMachineState(MachineRuntimeState runtime, MachineState state)
	{
		MachineState state2 = runtime.State;
		runtime.State = state;
		if (state2 != state)
		{
			MachineConfiguration machineConfiguration = _configurationService.Configuration.Machines.FirstOrDefault((MachineConfiguration m) => m.Id == runtime.MachineId);
			_valuePublisher.PublishValue(runtime.MachineId, NodeSemanticType.MachineState, (int)state, machineConfiguration?.Nodes ?? new List<NodeMapping>());
		}
	}

	private void PublishMachine(MachineConfiguration machine, MachineRuntimeState runtime)
	{
		_valuePublisher.PublishAll(machine.Id, runtime, machine.Nodes);
	}

	private void PublishAll()
	{
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines)
		{
			if (_runtimeStates.TryGetValue(machine.Id, out MachineRuntimeState value))
			{
				PublishMachine(machine, value);
			}
		}
	}

	private void EnsureRuntimeStates()
	{
		foreach (MachineConfiguration machine in _configurationService.Configuration.Machines)
		{
			if (!_runtimeStates.ContainsKey(machine.Id))
			{
				_runtimeStates[machine.Id] = new MachineRuntimeState
				{
					MachineId = machine.Id,
					State = MachineState.Offline,
					TargetCounter = 100
				};
			}
		}
	}

	private void ResetRuntimeValues(bool keepServersOffline)
	{
		foreach (MachineRuntimeState value in _runtimeStates.Values)
		{
			value.ActualCounter = 0;
			value.ErrorActive = false;
			value.ErrorMessage = string.Empty;
			value.DisruptedStateStartedAt = null;
			value.IsProducing = false;
			value.IsCounterFrozen = false;
			value.IsDisconnected = false;
			value.AssignedJobId = null;
			value.CurrentJobCatalogIndex = -1;
			value.IsJobChangeActive = false;
			value.JobChangeEndsAtUtc = null;
			value.JobChangePauseSeconds = 0;
			value.NextJobNamePreview = "—";
			value.NextPartNamePreview = "—";
			value.NextTargetQuantityPreview = 0;
			value.PendingNextJobCatalogIndex = -1;
			value.PartName = "—";
			value.JobName = "—";
			value.TargetCounter = 100;
			value.State = ((!keepServersOffline) ? value.State : MachineState.Offline);
			value.IsServerOnline = !keepServersOffline && value.IsServerOnline;
		}
	}

	private MachineConfiguration GetMachine(Guid machineId)
	{
		return _configurationService.Configuration.Machines.First((MachineConfiguration m) => m.Id == machineId);
	}

	private MachineRuntimeState GetRuntime(Guid machineId)
	{
		EnsureRuntimeStates();
		return _runtimeStates[machineId];
	}

	private bool CanAcceptAutomaticDisruption(Guid machineId)
	{
		lock (_sync)
		{
			int activeMachineCount = _configurationService.Configuration.Machines.Count((MachineConfiguration m) => m.IsActive);
			int maxConcurrentDisrupted = SimulationErrorPolicy.GetMaxConcurrentDisrupted(activeMachineCount);
			int num = _runtimeStates.Values.Count(SimulationErrorPolicy.IsDisruptedState);
			if (_runtimeStates.TryGetValue(machineId, out MachineRuntimeState value) && SimulationErrorPolicy.IsDisruptedState(value))
			{
				return false;
			}
			return num < maxConcurrentDisrupted;
		}
	}

	private async Task TriggerAutomaticErrorAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken, int? durationMs = null)
	{
		if (CanAcceptAutomaticDisruption(machine.Id))
		{
			int duration = SimulationErrorPolicy.CapDisruptedDuration(durationMs ?? SimulationRandom.NextInRange(_random, machine.MinErrorDurationMs, machine.MaxErrorDurationMs));
			await TriggerErrorAsync(machine.Id, GetRandomErrorMessage(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			ScheduleAutomaticDisruptionRecovery(machine.Id, MachineState.Error, duration, cancellationToken);
		}
	}

	private async Task TriggerAutomaticWarningAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken, int? durationMs = null)
	{
		if (CanAcceptAutomaticDisruption(machine.Id))
		{
			int duration = SimulationErrorPolicy.CapDisruptedDuration(durationMs ?? SimulationRandom.NextInRange(_random, 5000, 30000));
			runtime.DisruptedStateStartedAt = DateTime.UtcNow;
			runtime.IsProducing = false;
			SetMachineState(runtime, MachineState.Warning);
			PublishMachine(machine, runtime);
			_logService.Log(LogCategory.Warning, "Warnung ausgelöst", machine.Name);
			NotifyMachineChanged(runtime);
			ScheduleAutomaticDisruptionRecovery(machine.Id, MachineState.Warning, duration, cancellationToken);
		}
	}

	private async Task TriggerAutomaticOfflineAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken, int? durationMs = null)
	{
		if (CanAcceptAutomaticDisruption(machine.Id))
		{
			int duration = SimulationErrorPolicy.CapDisruptedDuration(durationMs ?? SimulationRandom.NextInRange(_random, machine.MinOfflineDurationMs, machine.MaxOfflineDurationMs));
			runtime.DisruptedStateStartedAt = DateTime.UtcNow;
			_logService.Log(LogCategory.Connection, $"Verbindungsabbruch für {duration} ms", machine.Name);
			await SetMachineOfflineAsync(machine.Id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			ScheduleAutomaticDisruptionRecovery(machine.Id, MachineState.Offline, duration, cancellationToken);
		}
	}

	private void ScheduleAutomaticDisruptionRecovery(Guid machineId, MachineState disruptedState, int durationMs, CancellationToken cancellationToken)
	{
		int cappedDuration = SimulationErrorPolicy.CapDisruptedDuration(durationMs);
		Task.Run(async delegate
		{
			await Task.Delay(cappedDuration, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (!cancellationToken.IsCancellationRequested)
			{
				MachineRuntimeState runtime = GetRuntime(machineId);
				if (SimulationErrorPolicy.IsDisruptedState(runtime) && IsMatchingDisruptedState(runtime, disruptedState))
				{
					await RecoverFromDisruptionAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}, cancellationToken);
	}

	private static bool IsMatchingDisruptedState(MachineRuntimeState runtime, MachineState disruptedState)
	{
		if (1 == 0)
		{
		}
		bool result = disruptedState switch
		{
			MachineState.Error => runtime.ErrorActive || runtime.State == MachineState.Error, 
			MachineState.Warning => runtime.State == MachineState.Warning, 
			MachineState.Offline => runtime.State == MachineState.Offline || !runtime.IsServerOnline, 
			_ => SimulationErrorPolicy.IsDisruptedState(runtime), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task RecoverFromDisruptionAsync(Guid machineId, CancellationToken cancellationToken)
	{
		MachineRuntimeState runtime = GetRuntime(machineId);
		if (!SimulationErrorPolicy.IsDisruptedState(runtime))
		{
			return;
		}
		if (runtime.State == MachineState.Offline || !runtime.IsServerOnline)
		{
			await SetMachineOnlineAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		if (runtime.ErrorActive)
		{
			await ClearErrorAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (runtime.State == MachineState.Warning)
		{
			SetMachineState(runtime, MachineState.Running);
			MachineConfiguration machine = GetMachine(machineId);
			PublishMachine(machine, runtime);
			_logService.Log(LogCategory.Warning, "Warnung zurückgesetzt", machine.Name);
			NotifyMachineChanged(runtime);
		}
		if (_state == SimulationState.Running)
		{
			runtime = GetRuntime(machineId);
			runtime.DisruptedStateStartedAt = null;
			runtime.IsProducing = true;
			MachineState state = runtime.State;
			if ((state == MachineState.Idle || state == MachineState.Paused) ? true : false)
			{
				SetMachineState(runtime, MachineState.Running);
			}
			MachineConfiguration machine2 = GetMachine(machineId);
			PublishMachine(machine2, runtime);
			NotifyMachineChanged(runtime);
		}
	}

	private async Task EnforceDisruptionTimeoutsAsync(CancellationToken cancellationToken)
	{
		List<Guid> expiredMachineIds;
		lock (_sync)
		{
			DateTime now = DateTime.UtcNow;
			expiredMachineIds = (from s in _runtimeStates.Values
				where SimulationErrorPolicy.IsDisruptedState(s) && s.DisruptedStateStartedAt.HasValue && (now - s.DisruptedStateStartedAt.Value).TotalMilliseconds >= 60000.0
				select s.MachineId).ToList();
		}
		foreach (Guid machineId in expiredMachineIds)
		{
			await RecoverFromDisruptionAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private string GetRandomErrorMessage()
	{
		List<string> errorMessages = _configurationService.Configuration.Events.ErrorMessages;
		return (errorMessages.Count == 0) ? "Unbekannter Fehler" : errorMessages[_random.Next(errorMessages.Count)];
	}

	private async Task StopMachineLoopAsync(Guid machineId)
	{
		if (_loops.TryGetValue(machineId, out MachineLoopContext loop))
		{
			loop.Cts.Cancel();
			_loops.Remove(machineId);
			try
			{
				await loop.LoopTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
			}
		}
	}

	private async Task StopAllLoopsAsync()
	{
		List<Guid> ids = _loops.Keys.ToList();
		foreach (Guid id in ids)
		{
			await StopMachineLoopAsync(id).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void OnServerStatusChanged(object? sender, (Guid MachineId, bool IsOnline, int ClientCount) e)
	{
		if (_runtimeStates.TryGetValue(e.MachineId, out MachineRuntimeState value))
		{
			value.IsServerOnline = e.IsOnline;
			value.ConnectedClients = e.ClientCount;
		}
	}

	private void NotifyStateChanged()
	{
		this.StateChanged?.Invoke(this, EventArgs.Empty);
	}

	private void NotifyMachineChanged(MachineRuntimeState runtime)
	{
		this.MachineStateChanged?.Invoke(this, runtime.CloneValues());
	}

	public void Dispose()
	{
		_globalCts?.Cancel();
		_machineServerService.ServerStatusChanged -= OnServerStatusChanged;
	}

	private static FixedProductionJobDefinition ResolveJobDefinition(Guid machineId, int catalogIndex)
	{
		if (VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId))
		{
			return VirtualPressBrakeRunProfile.ResolveJobDefinition(machineId, catalogIndex);
		}

		return VigilLabRunProfile.ResolveJobDefinition(machineId, catalogIndex);
	}
}
