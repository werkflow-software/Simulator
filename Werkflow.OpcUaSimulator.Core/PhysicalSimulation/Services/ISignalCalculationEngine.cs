using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface ISignalCalculationEngine
{
	void Initialize(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random);

	void CalculateSignals(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random, TimeSpan deltaTime);
}
