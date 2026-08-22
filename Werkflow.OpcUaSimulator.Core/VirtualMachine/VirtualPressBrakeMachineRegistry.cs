namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Registry of machine identities that use the press-brake kinematics engine.
/// </summary>
public static class VirtualPressBrakeMachineRegistry
{
	public static bool IsVirtualPressBrakeMachine(Guid machineId) =>
		machineId == VirtualPressBrakeContract.MachineId;

	public static IReadOnlyList<Guid> MachineIds { get; } =
		[VirtualPressBrakeContract.MachineId];
}
