using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class PhysicalRuntimeCoordinator : IPhysicalRuntimeCoordinator
{
	private readonly IPhysicalSimulationEngine _engine;

	private readonly HashSet<Guid> _initialized = new HashSet<Guid>();

	public PhysicalRuntimeCoordinator(IPhysicalSimulationEngine engine)
	{
		_engine = engine;
	}

	public void EnsureEngine(PhysicalMachineSession session, int seed)
	{
		if (!_initialized.Contains(session.MachineId))
		{
			_engine.Initialize(session, seed);
			_initialized.Add(session.MachineId);
		}
	}

	public void Tick(PhysicalMachineSession session, TimeSpan deltaTime)
	{
		_engine.Tick(session, deltaTime);
	}

	public void StopEngine(PhysicalMachineSession session)
	{
		_engine.Stop(session);
		_initialized.Remove(session.MachineId);
	}

	public bool TrySetGenerationMode(PhysicalMachineSession session, SignalGenerationMode mode)
	{
		if (session.Metrics.State == PhysicalPublisherState.Running
			&& mode != session.Simulation.GenerationMode
			&& mode != SignalGenerationMode.Physical)
		{
			return false;
		}

		if (mode == SignalGenerationMode.Physical && !_initialized.Contains(session.MachineId))
		{
			_engine.Initialize(session, session.Simulation.Seed);
			_initialized.Add(session.MachineId);
		}
		session.Simulation.GenerationMode = mode;
		return true;
	}

	public SignalGenerationMode GetGenerationMode(PhysicalMachineSession session)
	{
		return session.Simulation.GenerationMode;
	}
}
