namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Shared registry for machines driven by dedicated kinematics engines (laser or press brake).
/// </summary>
public static class VirtualKinematicsMachineRegistry
{
	public static bool IsKinematicsDrivenMachine(Guid machineId) =>
		VirtualLaserMachineRegistry.IsVirtualLaserMachine(machineId)
		|| VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId);
}
