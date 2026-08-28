namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

public static class VirtualAutonomousCellMachineRegistry
{
	public static bool IsVirtualAutonomousCellMachine(Guid machineId) =>
		machineId == VirtualAutonomousProductionCellContract.MachineId;

	public static IReadOnlyList<Guid> MachineIds { get; } =
		[VirtualAutonomousProductionCellContract.MachineId];
}
