using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IPhysicalRuntimeCoordinator
{
	void EnsureEngine(PhysicalMachineSession session, int seed);

	void Tick(PhysicalMachineSession session, TimeSpan deltaTime);

	void StopEngine(PhysicalMachineSession session);

	bool TrySetGenerationMode(PhysicalMachineSession session, SignalGenerationMode mode);

	SignalGenerationMode GetGenerationMode(PhysicalMachineSession session);
}
