namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Registry of machine identities that use the shared Virtual Laser engine.
/// </summary>
public static class VirtualLaserMachineRegistry
{
	public static bool IsVirtualLaserMachine(Guid machineId) =>
		machineId == VirtualMachineContract.MachineId
		|| machineId == VigilLabMachineContract.MachineId;

	public static bool IsVigilLabMachine(Guid machineId) =>
		machineId == VigilLabMachineContract.MachineId;

	public static IReadOnlyList<Guid> MachineIds { get; } =
		[VirtualMachineContract.MachineId, VigilLabMachineContract.MachineId];
}
