using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class PhysicalSimulationEngine : IPhysicalSimulationEngine
{
	private readonly IHiddenProcessStateEngine _hiddenEngine;

	private readonly ISignalCalculationEngine _signalEngine;

	private readonly IPhysicalModelValidator _validator;

	private readonly IFaultScenarioEngine? _faultScenarioEngine;

	private readonly IFaultScenarioSimulationBridge? _faultBridge;

	private readonly Dictionary<Guid, int> _ticksInWindow = new Dictionary<Guid, int>();

	private readonly Dictionary<Guid, Stopwatch> _rateWatches = new Dictionary<Guid, Stopwatch>();

	public PhysicalSimulationEngine(IHiddenProcessStateEngine hiddenEngine, ISignalCalculationEngine signalEngine, IPhysicalModelValidator validator, IFaultScenarioEngine? faultScenarioEngine = null, IFaultScenarioSimulationBridge? faultBridge = null)
	{
		_hiddenEngine = hiddenEngine;
		_signalEngine = signalEngine;
		_validator = validator;
		_faultScenarioEngine = faultScenarioEngine;
		_faultBridge = faultBridge;
	}

	public void Initialize(PhysicalMachineSession session, int seed)
	{
		session.Simulation.Seed = seed;
		session.Simulation.IsEngineActive = true;
		if (PhysicalVerificationSettings.IsShortMode || session.Simulation.VerificationMode == PhysicalVerificationMode.Short)
		{
			session.Simulation.VerificationMode = PhysicalVerificationMode.Short;
			if (session.Simulation.TimeFactor <= 1.0)
			{
				session.Simulation.TimeFactor = PhysicalVerificationSettings.ShortModeTimeFactor;
			}
		}
		session.Simulation.GenerationMode = ResolveDefaultMode(session.Profile);
		SeededRandomStreams random = new SeededRandomStreams(seed);
		session.Simulation.PhysicsState.Random = random;
		_rateWatches[session.MachineId] = Stopwatch.StartNew();
		_ticksInWindow[session.MachineId] = 0;
		_hiddenEngine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
		_signalEngine.Initialize(session.Profile, session.Runtime, session.Simulation, random);
		LaserKinematicsEngine.Initialize(session.Simulation, seed, session.MachineId);
		PressBrakeKinematicsEngine.Initialize(session.Simulation, seed, session.MachineId);
	}

	public void Tick(PhysicalMachineSession session, TimeSpan deltaTime)
	{
		if (!session.Simulation.IsEngineActive || session.Simulation.GenerationMode != SignalGenerationMode.Physical)
		{
			return;
		}
		SeededRandomStreams random = session.Simulation.PhysicsState.Random;
		if (random == null)
		{
			return;
		}
		TimeSpan deltaTime2 = TimeSpan.FromTicks((long)((double)deltaTime.Ticks * session.Simulation.TimeFactor));
		Stopwatch stopwatch = Stopwatch.StartNew();
		_hiddenEngine.Tick(session.Profile, session.Runtime, session.Simulation, random, deltaTime2);
		_faultScenarioEngine?.Tick(session, deltaTime, _faultBridge);
		LaserKinematicsEngine.Tick(session.Profile, session.Runtime, session.Simulation, deltaTime2, session.Simulation.Seed);
		PressBrakeKinematicsEngine.Tick(
			session.Profile,
			session.Runtime,
			session.Simulation,
			deltaTime2,
			session.Simulation.Seed,
			session.MachineId,
			session.PressBrakeGroundTruth);
		_signalEngine.CalculateSignals(session.Profile, session.Runtime, session.Simulation, random, deltaTime2);
		_faultScenarioEngine?.EvaluateThresholdsAfterSignals(session, _faultBridge);
		_faultScenarioEngine?.ApplySignalOverrides(session, _faultBridge);
		_validator.ValidateTick(session);
		stopwatch.Stop();
		PhysicalSimulationMetrics metrics = session.Simulation.Metrics;
		metrics.TotalEngineTicks++;
		metrics.LastEngineTickAt = DateTimeOffset.UtcNow;
		session.Simulation.LastCalculationAt = metrics.LastEngineTickAt;
		double totalMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
		metrics.AverageCalculationDurationMs = ((metrics.TotalEngineTicks <= 1) ? totalMilliseconds : (metrics.AverageCalculationDurationMs * 0.9 + totalMilliseconds * 0.1));
		if (totalMilliseconds > metrics.MaxCalculationDurationMs)
		{
			metrics.MaxCalculationDurationMs = totalMilliseconds;
		}
		if (_rateWatches.TryGetValue(session.MachineId, out Stopwatch value) && value.Elapsed.TotalSeconds >= 1.0)
		{
			metrics.EngineTicksPerSecond = (double)_ticksInWindow.GetValueOrDefault(session.MachineId) / value.Elapsed.TotalSeconds;
			metrics.SignalCalculationsPerSecond = metrics.EngineTicksPerSecond * (double)session.Profile.Signals.Count((SignalDefinition s) => s.IsEnabled);
			_ticksInWindow[session.MachineId] = 0;
			value.Restart();
		}
		_ticksInWindow[session.MachineId] = _ticksInWindow.GetValueOrDefault(session.MachineId) + 1;
	}

	public void Stop(PhysicalMachineSession session)
	{
		session.Simulation.IsEngineActive = false;
		session.Simulation.PhysicsState.Reset();
		session.Simulation.ResetPhaseState();
		session.PressBrakeGroundTruth?.Flush();
		_rateWatches.Remove(session.MachineId);
		_ticksInWindow.Remove(session.MachineId);
	}

	public object? GetPublishValue(SignalDefinition signal, SignalRuntimeState runtime)
	{
		return SignalRuntimeValueHelper.GetCurrentValue(signal, runtime);
	}

	private static SignalGenerationMode ResolveDefaultMode(PhysicalMachineProfile profile)
	{
		if (profile.Metadata.TryGetValue("profileKind", out string value) && value.Contains("physical", StringComparison.OrdinalIgnoreCase))
		{
			return SignalGenerationMode.Physical;
		}
		return SignalGenerationMode.Technical;
	}
}
