using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Nodes;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Publishing;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

public sealed class PhysicalSignalPublishingCoordinator : IPhysicalSignalPublishingCoordinator
{
	private sealed class MachineContext
	{
		public required PhysicalMachineSession Session { get; init; }

		public IPhysicalSignalNodeRegistry Registry { get; } = new PhysicalSignalNodeRegistry();

		public IPhysicalSignalPublisher? Publisher { get; set; }

		public ISystemContext? SystemContext { get; set; }

		public int Seed { get; set; }
	}

	private readonly IPhysicalMachineSessionFactory _sessionFactory;

	private readonly IPhysicalSignalTypeMapper _typeMapper;

	private readonly TechnicalSignalValueGenerator _valueGenerator;

	private readonly IPhysicalRuntimeCoordinator _runtimeCoordinator;

	private readonly ILogService _logService;

	private readonly IFaultScenarioService? _faultScenarioService;

	private readonly PhysicalSignalAddressSpaceBuilder _addressSpaceBuilder;

	private readonly object _sync = new object();

	private readonly Dictionary<Guid, MachineContext> _contexts = new Dictionary<Guid, MachineContext>();

	public PhysicalSignalPublishingCoordinator(IPhysicalMachineSessionFactory sessionFactory, IPhysicalSignalTypeMapper typeMapper, TechnicalSignalValueGenerator valueGenerator, IPhysicalRuntimeCoordinator runtimeCoordinator, ILogService logService, IFaultScenarioService? faultScenarioService = null)
	{
		_sessionFactory = sessionFactory;
		_typeMapper = typeMapper;
		_valueGenerator = valueGenerator;
		_runtimeCoordinator = runtimeCoordinator;
		_logService = logService;
		_faultScenarioService = faultScenarioService;
		_addressSpaceBuilder = new PhysicalSignalAddressSpaceBuilder(new PhysicalSignalNodeFactory(typeMapper));
	}

	public PhysicalMachineSession? GetSession(Guid machineId)
	{
		lock (_sync)
		{
			MachineContext value;
			return _contexts.TryGetValue(machineId, out value) ? value.Session : null;
		}
	}

	public IReadOnlyList<PhysicalMachineSession> GetSessions()
	{
		lock (_sync)
		{
			return _contexts.Values.Select((MachineContext c) => c.Session).ToList();
		}
	}

	public void PrepareMachine(MachineConfiguration machine, int simulationSeed)
	{
		lock (_sync)
		{
			RemoveContext(machine.Id);
		}
		if (!string.IsNullOrWhiteSpace(machine.PhysicalProfileId))
		{
			PhysicalMachineSession physicalMachineSession = _sessionFactory.TryCreateSession(machine.Id, machine.Name, machine.PhysicalProfileId) ?? throw new InvalidOperationException($"Physisches Profil '{machine.PhysicalProfileId}' für Maschine '{machine.Name}' wurde nicht gefunden.");
			int seed = ResolveSimulationSeed(machine.Id, simulationSeed) ^ machine.Id.GetHashCode();
			physicalMachineSession.Simulation.Seed = seed;
			physicalMachineSession.Simulation.ProductionDrivenJobs = true;
			physicalMachineSession.Simulation.VerificationMode = PhysicalVerificationSettings.VerificationMode;
			if (physicalMachineSession.Simulation.VerificationMode == PhysicalVerificationMode.Short)
			{
				physicalMachineSession.Simulation.TimeFactor = PhysicalVerificationSettings.ShortModeTimeFactor;
			}
			_runtimeCoordinator.EnsureEngine(physicalMachineSession, seed);
			if (VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machine.Id))
			{
				var recorder = new PressBrakeGroundTruthRecorder();
				recorder.BeginSession(machine.Id, seed);
				physicalMachineSession.PressBrakeGroundTruth = recorder;
			}

			MachineContext value = new MachineContext
			{
				Session = physicalMachineSession,
				Seed = seed
			};
			lock (_sync)
			{
				_contexts[machine.Id] = value;
			}
			_faultScenarioService?.RegisterSession(physicalMachineSession);
			_logService.Log(LogCategory.Server, $"Physisches Profil geladen: {physicalMachineSession.Profile.DisplayName} ({physicalMachineSession.Profile.Signals.Count((SignalDefinition s) => s.IsEnabled)} aktive Signale)", machine.Name);
		}
	}

	public int BuildAddressSpace(Guid machineId, ISystemContext systemContext, FolderState simulationRoot, ushort namespaceIndex, Action<NodeState> registerNode)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext value))
			{
				return 0;
			}
			value.SystemContext = systemContext;
			int num = _addressSpaceBuilder.Build(systemContext, simulationRoot, namespaceIndex, value.Session.Profile, value.Session.Runtime, value.Registry, registerNode);
			value.Session.OpcUaNodeCount = num;
			_logService.Log(LogCategory.Server, $"Physikalische OPC-UA-Nodes erzeugt: {num}", value.Session.MachineName);
			return num;
		}
	}

	public async Task StartForMachineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineContext context;
		lock (_sync)
		{
			_contexts.TryGetValue(machineId, out context);
		}
		if (context != null && context.Publisher == null)
		{
			if (context.SystemContext == null)
			{
				throw new InvalidOperationException("Physikalischer Adressraum wurde noch nicht erzeugt.");
			}
			PhysicalSignalPublisher publisher = (PhysicalSignalPublisher)(context.Publisher = new PhysicalSignalPublisher(context.Session, context.Registry, _typeMapper, _valueGenerator, _runtimeCoordinator, _logService, context.SystemContext, context.Seed));
			await publisher.StartAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task StopForMachineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineContext context;
		lock (_sync)
		{
			_contexts.TryGetValue(machineId, out context);
		}
		if (context?.Publisher != null)
		{
			await context.Publisher.StopAsync().ConfigureAwait(continueOnCapturedContext: false);
			context.Publisher = null;
		}
		if (context != null)
		{
			_runtimeCoordinator.StopEngine(context.Session);
		}
		lock (_sync)
		{
			if (_contexts.TryGetValue(machineId, out context))
			{
				context.Registry.Clear();
				_contexts.Remove(machineId);
				_logService.Log(LogCategory.Server, "Physikalische Registry geleert", context.Session.MachineName);
			}
		}
	}

	public async Task StopAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		List<Guid> ids;
		lock (_sync)
		{
			ids = _contexts.Keys.ToList();
		}
		foreach (Guid id in ids)
		{
			await StopForMachineAsync(id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task PauseAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		List<IPhysicalSignalPublisher> publishers;
		lock (_sync)
		{
			publishers = (from c in _contexts.Values
				select c.Publisher into p
				where p != null
				select p).Cast<IPhysicalSignalPublisher>().ToList();
		}
		foreach (IPhysicalSignalPublisher publisher in publishers)
		{
			await publisher.PauseAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task ResumeAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		List<IPhysicalSignalPublisher> publishers;
		lock (_sync)
		{
			publishers = (from c in _contexts.Values
				select c.Publisher into p
				where p != null
				select p).Cast<IPhysicalSignalPublisher>().ToList();
		}
		foreach (IPhysicalSignalPublisher publisher in publishers)
		{
			await publisher.ResumeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public Task<bool> SetManualValueAsync(Guid machineId, string signalId, object value, CancellationToken cancellationToken = default(CancellationToken))
	{
		IPhysicalSignalPublisher publisher;
		PhysicalMachineSession session;
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext value2))
			{
				return Task.FromResult(result: false);
			}
			publisher = value2.Publisher;
			session = value2.Session;
		}
		if (publisher == null || session == null)
		{
			return Task.FromResult(result: false);
		}
		if (session.Metrics.State == PhysicalPublisherState.Running && !session.IsPublisherManualOverride)
		{
			return Task.FromResult(result: false);
		}
		SignalDefinition signalDefinition = session.Profile.Signals.FirstOrDefault((SignalDefinition s) => s.SignalId == signalId);
		if (signalDefinition == null)
		{
			return Task.FromResult(result: false);
		}
		PhysicalSignalDataType dataType = signalDefinition.DataType;
		if ((uint)dataType <= 3u)
		{
			double num = Convert.ToDouble(value, CultureInfo.InvariantCulture);
			if (num < signalDefinition.HardMinimum || num > signalDefinition.HardMaximum)
			{
				session.Metrics.LastError = $"Wert außerhalb der Hard Limits ({signalDefinition.HardMinimum}..{signalDefinition.HardMaximum}).";
				return Task.FromResult(result: false);
			}
		}
		bool flag = publisher.PublishSignal(signalId, value, force: true);
		if (flag)
		{
			_logService.Log(LogCategory.Production, "Manueller physikalischer Wert gesetzt: " + signalId, session.MachineName);
		}
		return Task.FromResult(flag);
	}

	public void EnableManualOverride(Guid machineId, bool enabled)
	{
		lock (_sync)
		{
			if (_contexts.TryGetValue(machineId, out MachineContext value))
			{
				value.Session.IsPublisherManualOverride = enabled;
				if (enabled)
				{
					value.Session.Simulation.GenerationMode = SignalGenerationMode.Manual;
				}
				else
				{
					value.Session.Simulation.GenerationMode = SignalGenerationMode.Technical;
				}
			}
		}
	}

	public bool TrySetGenerationMode(Guid machineId, SignalGenerationMode mode)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext value))
			{
				return false;
			}
			return _runtimeCoordinator.TrySetGenerationMode(value.Session, mode);
		}
	}

	public SignalGenerationMode GetGenerationMode(Guid machineId)
	{
		lock (_sync)
		{
			MachineContext value;
			return _contexts.TryGetValue(machineId, out value) ? _runtimeCoordinator.GetGenerationMode(value.Session) : SignalGenerationMode.Technical;
		}
	}

	public void BeginJobChange(Guid machineId, int pauseSimulationSeconds, FixedProductionJobDefinition nextJob)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return;
			}

			PhysicalSimulationContext simulation = context.Session.Simulation;
			simulation.ProductionDrivenJobs = true;
			simulation.IsJobChangePauseActive = true;
			simulation.IsProductionMotionActive = false;
			simulation.PendingJobDefinition = nextJob;
			simulation.JobChangePauseUntil = simulation.SimulationTime + TimeSpan.FromSeconds(pauseSimulationSeconds);
			simulation.OverrideSetupDuration = TimeSpan.FromSeconds(pauseSimulationSeconds);
			simulation.CurrentPhase = ProcessPhase.Setup;
			simulation.PhaseStartedAt = DateTimeOffset.UtcNow;
			simulation.PhaseElapsedSimulationTime = TimeSpan.Zero;
			LaserKinematicsEngine.OnJobChangeBegin(simulation, nextJob);
			PressBrakeKinematicsEngine.OnJobChangeBegin(simulation, nextJob);
		}
	}

	public void ApplyProductionJob(Guid machineId, FixedProductionJobDefinition job)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return;
			}

			PhysicalSimulationContext simulation = context.Session.Simulation;
			simulation.IsJobChangePauseActive = false;
			simulation.PendingJobDefinition = null;
			simulation.OverrideSetupDuration = null;
			simulation.IsProductionMotionActive = false;
			PhysicalJobCoordinator.ApplyDefinition(simulation, job, context.Session.Runtime);
			simulation.CurrentPhase = ProcessPhase.RampUp;
			simulation.PhaseStartedAt = DateTimeOffset.UtcNow;
			simulation.PhaseElapsedSimulationTime = TimeSpan.Zero;
			LaserKinematicsEngine.OnJobApplied(simulation, context.Seed);
			PressBrakeKinematicsEngine.OnJobApplied(simulation, context.Seed);
		}
	}

	public int ConsumePendingPartCompletions(Guid machineId)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return 0;
			}

			return LaserKinematicsEngine.ConsumePendingPartCompletions(context.Session.Simulation)
				+ PressBrakeKinematicsEngine.ConsumePendingPartCompletions(context.Session.Simulation);
		}
	}

	public async Task PauseProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineContext? context;
		lock (_sync)
		{
			_contexts.TryGetValue(machineId, out context);
		}

		if (context == null)
		{
			return;
		}

		PhysicalSimulationContext simulation = context.Session.Simulation;
		if (simulation.ProductionRunStartedAtUtc.HasValue)
		{
			simulation.FrozenProductionElapsedSeconds =
				(DateTimeOffset.UtcNow - simulation.ProductionRunStartedAtUtc.Value).TotalSeconds;
		}

		if (VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(context.Session.MachineId))
		{
			simulation.FrozenPartRemainingSeconds = 0.0;
			simulation.FrozenJobRemainingSeconds = 0.0;
		}
		else
		{
			FixedProductionJobDefinition job = BuildJobDefinition(simulation);
			simulation.FrozenPartRemainingSeconds = LaserToolpathTimeEstimator.EstimateRemainingPartSeconds(simulation.Kinematics, job, context.Seed);
			simulation.FrozenJobRemainingSeconds = LaserToolpathTimeEstimator.EstimateRemainingJobSeconds(simulation, job, context.Seed);
		}

		simulation.IsProductionPaused = true;
		LaserKinematicsEngine.OnProductionPaused(simulation);
		PressBrakeKinematicsEngine.OnProductionPaused(simulation);
		if (context.Publisher != null)
		{
			await context.Publisher.PauseAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task ResumeProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineContext? context;
		lock (_sync)
		{
			_contexts.TryGetValue(machineId, out context);
		}

		if (context == null)
		{
			return;
		}

		PhysicalSimulationContext simulation = context.Session.Simulation;
		simulation.IsProductionPaused = false;
		simulation.IsProductionMotionActive = true;
		simulation.FrozenPartRemainingSeconds = 0.0;
		simulation.FrozenJobRemainingSeconds = 0.0;
		if (simulation.FrozenProductionElapsedSeconds > 0.0)
		{
			simulation.ProductionRunStartedAtUtc =
				DateTimeOffset.UtcNow.AddSeconds(-simulation.FrozenProductionElapsedSeconds);
		}
		else if (simulation.ProductionRunStartedAtUtc == null)
		{
			simulation.ProductionRunStartedAtUtc = DateTimeOffset.UtcNow;
		}

		LaserKinematicsEngine.OnProductionResumed(simulation);
		PressBrakeKinematicsEngine.OnProductionResumed(simulation);
		if (context.Publisher != null)
		{
			await context.Publisher.ResumeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void StopProduction(Guid machineId)
	{
		IPhysicalSignalPublisher? publisherToResume = null;
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return;
			}

			PhysicalSimulationContext simulation = context.Session.Simulation;
			simulation.IsProductionPaused = false;
			simulation.IsProductionMotionActive = false;
			simulation.FrozenPartRemainingSeconds = 0.0;
			simulation.FrozenJobRemainingSeconds = 0.0;
			simulation.FrozenProductionElapsedSeconds = 0.0;
			simulation.ProductionRunStartedAtUtc = null;
			LaserKinematicsEngine.StopAndResetProduction(simulation, context.Seed);
			PressBrakeKinematicsEngine.StopAndResetProduction(simulation, context.Seed);
			PhysicalJobCoordinator.SyncProductionCounters(simulation, 0, simulation.Job.TargetQuantity);
			if (context.Publisher != null && context.Session.Metrics.State == PhysicalPublisherState.Paused)
			{
				publisherToResume = context.Publisher;
			}
		}

		if (publisherToResume != null)
		{
			_ = publisherToResume.ResumeAsync();
		}
	}

	public void AbortProductionForJobChange(Guid machineId, FixedProductionJobDefinition nextJob)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return;
			}

			LaserKinematicsEngine.AbortProductionForJobChange(context.Session.Simulation, nextJob);
			PressBrakeKinematicsEngine.AbortProductionForJobChange(context.Session.Simulation, nextJob);
		}
	}

	public (double partRemainingSeconds, double jobRemainingSeconds) GetProductionTimeEstimates(Guid machineId)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return (0.0, 0.0);
			}

			PhysicalSimulationContext simulation = context.Session.Simulation;
			if (simulation.IsProductionPaused)
			{
				return (simulation.FrozenPartRemainingSeconds, simulation.FrozenJobRemainingSeconds);
			}

			if (VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId))
			{
				return PressBrakeProductionTimeEstimator.EstimateRemaining(context.Session.Simulation, context.Seed);
			}

			FixedProductionJobDefinition job = BuildJobDefinition(simulation);
			double part = LaserToolpathTimeEstimator.EstimateRemainingPartSeconds(simulation.Kinematics, job, context.Seed);
			double jobRemaining = LaserToolpathTimeEstimator.EstimateRemainingJobSeconds(simulation, job, context.Seed);
			return (part, jobRemaining);
		}
	}

	public double GetSetupRemainingSeconds(Guid machineId)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return 0.0;
			}

			if (VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId))
			{
				PressBrakeKinematicsState pressBrake = context.Session.Simulation.PressBrake;
				if (!pressBrake.IsEnabled)
				{
					return 0.0;
				}

				return PressBrakeKinematicsEngine.GetPhaseRemainingSeconds(pressBrake, context.Seed);
			}

			return LaserToolpathTimeEstimator.EstimateSetupRemainingSeconds(context.Session.Simulation);
		}
	}

	public double GetNozzleChangeRemainingSeconds(Guid machineId)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return 0.0;
			}

			LaserKinematicsState kinematics = context.Session.Simulation.Kinematics;
			if (!kinematics.NozzleChangeActive)
			{
				return 0.0;
			}

			return Math.Max(
				0.0,
				VirtualMachineKinematicsConfig.NozzleChangeDurationSeconds - kinematics.NozzleChangeElapsedSeconds);
		}
	}

	private static FixedProductionJobDefinition BuildJobDefinition(PhysicalSimulationContext simulation) =>
		new()
		{
			CatalogIndex = simulation.Job.CatalogIndex,
			JobName = simulation.Job.JobName,
			PartName = simulation.Job.PartName,
			TargetQuantity = simulation.Job.TargetQuantity,
			MaterialName = simulation.Job.MaterialName,
			MaterialThicknessMm = simulation.Job.MaterialThicknessMm,
			RecipeName = simulation.Job.RecipeName,
			ProgramName = simulation.Job.ProgramName
		};

	public void SyncProductionCounters(Guid machineId, int actualCounter, int targetCounter)
	{
		lock (_sync)
		{
			if (!_contexts.TryGetValue(machineId, out MachineContext context))
			{
				return;
			}

			PhysicalJobCoordinator.SyncProductionCounters(context.Session.Simulation, actualCounter, targetCounter);
		}
	}

	private void RemoveContext(Guid machineId)
	{
		if (_contexts.TryGetValue(machineId, out MachineContext value))
		{
			value.Registry.Clear();
			_contexts.Remove(machineId);
			_faultScenarioService?.UnregisterSession(machineId);
		}
	}

	private static int ResolveSimulationSeed(Guid machineId, int globalSeed)
	{
		if (VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId))
		{
			return VirtualPressBrakeRunProfile.ResolveSimulationSeed(machineId, globalSeed);
		}

		return VigilLabRunProfile.ResolveSimulationSeed(machineId, globalSeed);
	}
}
