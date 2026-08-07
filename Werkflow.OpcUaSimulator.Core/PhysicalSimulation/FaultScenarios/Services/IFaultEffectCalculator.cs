using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultEffectCalculator
{
	double ComputeEffectContribution(FaultScenarioInstance instance, FaultEffectDefinition effect, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, TimeSpan deltaTime);
}
