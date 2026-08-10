namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Documented contract for the primary virtual production machine (Machine 1, port 4840).
/// The simulator does not integrate with VIGIL; this machine is exposed via OPC UA for inMotion.
/// </summary>
public static class VirtualMachineContract
{
	public static readonly Guid MachineId = new("a1111111-1111-4111-8111-111111111111");

	public const int Port = 4840;

	public const string Endpoint = "opc.tcp://localhost:4840";

	public const string DisplayName = "Werkflow Virtual Laser 01";

	public const string PhysicalProfileId = "laser-processing-machine-300";

	public const string Purpose = "Virtual Machine / inMotion / VIGIL Learning Test";
}
