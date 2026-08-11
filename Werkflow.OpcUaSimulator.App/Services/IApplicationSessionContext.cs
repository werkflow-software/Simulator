using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.Services;

public interface IApplicationSessionContext
{
	ApplicationOperatingMode? CurrentMode { get; }

	bool IsClassicSimulator { get; }

	bool IsVirtualMachine { get; }

	void SetMode(ApplicationOperatingMode mode);

	void ClearMode();
}
