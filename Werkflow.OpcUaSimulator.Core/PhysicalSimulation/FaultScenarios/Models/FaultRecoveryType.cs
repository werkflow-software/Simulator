namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public enum FaultRecoveryType
{
	Exponential,
	Linear,
	RateLimited,
	ThermalCooldown,
	PressureRecovery,
	OscillationDecay,
	ManualHold
}
