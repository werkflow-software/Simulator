namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public enum FaultScenarioLifecycleState
{
	Created,
	Starting,
	Running,
	Paused,
	Faulted,
	Recovering,
	Completed,
	Cancelled,
	Failed
}
