namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Models;

public sealed class VirtualMachineSelectorItem
{
	public VirtualMachineSelectorItem(Guid machineId, string displayName, string endpoint)
	{
		MachineId = machineId;
		DisplayName = displayName;
		Endpoint = endpoint;
	}

	public Guid MachineId { get; }

	public string DisplayName { get; }

	public string Endpoint { get; }
}
