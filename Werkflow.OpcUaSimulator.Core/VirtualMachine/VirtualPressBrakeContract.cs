namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Stable contract for Machine 2 — Virtual Press Brake / Bending Machine (port 4841).
/// </summary>
public static class VirtualPressBrakeContract
{
	public static readonly Guid MachineId = new("c3333333-3333-4333-8333-333333333333");

	public const int Port = 4841;

	public const string Endpoint = "opc.tcp://localhost:4841";

	public const string DisplayName = "Werkflow Virtual Press Brake 02";

	public const string NamespaceUri = "urn:werkflow:simulator:press-brake";

	public const string PhysicalProfileId = "vigil-press-brake-reduced";

	public const string Purpose = "VIGIL cross-machine generalization validation (Machine 2)";
}
