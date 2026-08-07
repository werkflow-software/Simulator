using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultScenarioRuntimeFactory
{
	FaultScenarioInstance CreateInstance(FaultScenarioDefinition definition, FaultScenarioStartRequest request, int baseSeed);
}
