namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public enum TechnicalSignalBehavior
{
	Continuous,
	SlowContinuous,
	Counter,
	DiscreteState,
	BooleanState,
	TextState,
	Timestamp,
	Stable
}
