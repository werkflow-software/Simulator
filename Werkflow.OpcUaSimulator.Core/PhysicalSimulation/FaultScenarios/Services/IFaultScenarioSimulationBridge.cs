using System;
using System.Threading;
using System.Threading.Tasks;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultScenarioSimulationBridge
{
	void SetMachineFault(Guid machineId, string faultCode, string message, bool stopProduction, bool keepServerOnline, int priority);

	void ClearMachineFault(Guid machineId, string faultCode);

	Task StopServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task StartServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	void StopProduction(Guid machineId);

	void ResumeProduction(Guid machineId);
}
