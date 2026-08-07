using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultRecoveryEngine
{
	void BeginRecovery(FaultScenarioInstance instance);

	void TickRecovery(FaultScenarioInstance instance, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, TimeSpan deltaTime);

	bool IsRecoveryComplete(FaultScenarioInstance instance);
}
