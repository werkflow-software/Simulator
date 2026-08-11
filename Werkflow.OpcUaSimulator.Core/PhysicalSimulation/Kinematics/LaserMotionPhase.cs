namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public enum LaserMotionPhase
{
	Idle,
	Setup,
	NozzleChange,
	RapidPositioning,
	Piercing,
	Cutting,
	Repositioning,
	JobChange,
	Recovery
}
