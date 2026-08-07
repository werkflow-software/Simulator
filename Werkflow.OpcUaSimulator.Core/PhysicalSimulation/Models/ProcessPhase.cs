namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public enum ProcessPhase
{
	Idle,
	Setup,
	RampUp,
	Processing,
	PeakLoad,
	RampDown,
	Cooling,
	Waiting
}
