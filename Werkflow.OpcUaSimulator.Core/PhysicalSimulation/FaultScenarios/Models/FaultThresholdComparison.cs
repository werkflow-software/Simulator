namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public enum FaultThresholdComparison
{
	GreaterThan,
	GreaterThanOrEqual,
	LessThan,
	LessThanOrEqual,
	OutsideRange,
	InsideRange,
	RateOfChangeGreaterThan
}
