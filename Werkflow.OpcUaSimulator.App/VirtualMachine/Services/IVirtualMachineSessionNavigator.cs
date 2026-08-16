namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Services;

public interface IVirtualMachineSessionNavigator
{
	Task EndSessionAndReturnToSelectorAsync();
}
