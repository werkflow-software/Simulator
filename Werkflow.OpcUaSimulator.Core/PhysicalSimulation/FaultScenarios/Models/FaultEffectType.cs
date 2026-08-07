namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public enum FaultEffectType
{
	AdditiveDrift,
	MultiplicativeDrift,
	TargetShift,
	RateChange,
	NoiseIncrease,
	Oscillation,
	StepChange,
	IntermittentPulse,
	EfficiencyLoss,
	SaturationShift,
	DelayIncrease,
	HysteresisShift,
	SignalFreeze,
	ConnectionDrop
}
