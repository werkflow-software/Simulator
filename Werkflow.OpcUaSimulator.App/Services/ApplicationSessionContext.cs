using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.Services;

public sealed class ApplicationSessionContext : IApplicationSessionContext
{
	public ApplicationOperatingMode? CurrentMode { get; private set; }

	public bool IsClassicSimulator => CurrentMode == ApplicationOperatingMode.ClassicSimulator;

	public bool IsVirtualMachine => CurrentMode == ApplicationOperatingMode.VirtualMachine;

	public void SetMode(ApplicationOperatingMode mode) => CurrentMode = mode;

	public void ClearMode() => CurrentMode = null;
}
