namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public enum DependencyType
{
	Linear,
	InverseLinear,
	DelayedLinear,
	Threshold,
	Saturating,
	Polynomial,
	Sigmoid,
	PiecewiseLinear,
	RateLimited,
	Hysteresis
}
