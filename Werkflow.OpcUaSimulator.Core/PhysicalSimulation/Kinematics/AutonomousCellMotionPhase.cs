using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public enum AutonomousCellMotionPhase
{
	Idle,
	WaitRawMaterial,
	HiddenInboundDelivery,
	LoadPick,
	LoadTransfer,
	FixtureClamp,
	ProcessApproach,
	ProcessPressFit,
	ProcessRetract,
	FixtureRelease,
	TransferPickup,
	TransferToVision,
	VisionInspect,
	SortOutput,
	ContainerFill,
	WaitReplenishment,
	WaitContainerExchange,
	HiddenOutboundExchange,
	Complete
}
