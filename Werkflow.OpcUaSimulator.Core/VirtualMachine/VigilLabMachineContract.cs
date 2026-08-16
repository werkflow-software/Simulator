namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Stable contract for the VIGIL LAB learning-test laser (port 4844).
/// </summary>
public static class VigilLabMachineContract
{
	public static readonly Guid MachineId = new("b2222222-2222-4222-8222-222222222222");

	public const int Port = 4844;

	public const string Endpoint = "opc.tcp://localhost:4844";

	public const string DisplayName = "VIGIL LAB Laser";

	public const string NamespaceUri = "urn:werkflow:simulator:vigil-lab";

	public const string PhysicalProfileId = "vigil-lab-laser-reduced";

	public const string Purpose = "VIGIL internal learning Run 001";
}
