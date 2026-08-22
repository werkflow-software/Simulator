namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public enum PressBrakeMotionPhase
{
	Idle,
	Setup,
	OperatorWait,
	ToolChange,
	ProgramTransition,
	BackgaugeMove,
	RamApproach,
	Forming,
	Hold,
	RamReturn,
	InterStepWait,
	InterPartWait,
	InterruptRecovery
}
