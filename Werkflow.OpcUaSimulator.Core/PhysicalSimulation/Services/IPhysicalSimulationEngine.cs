using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IPhysicalSimulationEngine
{
	void Initialize(PhysicalMachineSession session, int seed);

	void Tick(PhysicalMachineSession session, TimeSpan deltaTime);

	void Stop(PhysicalMachineSession session);

	object? GetPublishValue(SignalDefinition signal, SignalRuntimeState runtime);
}
