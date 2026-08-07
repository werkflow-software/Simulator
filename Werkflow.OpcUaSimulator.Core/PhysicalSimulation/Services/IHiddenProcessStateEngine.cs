using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IHiddenProcessStateEngine
{
	void Initialize(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random);

	void Tick(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random, TimeSpan deltaTime);
}
