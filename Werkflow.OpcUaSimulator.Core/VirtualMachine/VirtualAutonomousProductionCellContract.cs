namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Stable contract for Machine 3 — VIGIL LAB Autonomous Production Cell (port 4842).
/// </summary>
public static class VirtualAutonomousProductionCellContract
{
	public static readonly Guid MachineId = new("d4444444-4444-4444-8444-444444444444");

	public const int Port = 4842;

	public const string Endpoint = "opc.tcp://localhost:4842";

	public const string DisplayName = "VIGIL LAB Machine-3 Autonomous Production Cell";

	public const string NamespaceUri = "urn:werkflow:simulator:autonomous-cell";

	public const string PhysicalProfileIdCore24 = "vigil-autonomous-cell-core24";

	public const string PhysicalProfileIdExpanded48 = "vigil-autonomous-cell-expanded48";

	public const string PhysicalProfileIdScale96 = "vigil-autonomous-cell-scale96";

	public const string Purpose = "VIGIL cross-machine generalization validation (Machine 3 autonomous production cell)";
}
