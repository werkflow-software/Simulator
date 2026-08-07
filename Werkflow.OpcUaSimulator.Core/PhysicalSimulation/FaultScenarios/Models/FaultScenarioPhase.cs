namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public enum FaultScenarioPhase
{
	Dormant,
	Initiating,
	Developing,
	Degraded,
	Critical,
	Faulted,
	Recovering,
	Completed,
	Cancelled
}
