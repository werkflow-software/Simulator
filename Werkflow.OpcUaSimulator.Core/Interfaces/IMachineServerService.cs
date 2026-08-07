using System;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IMachineServerService
{
	event EventHandler<(Guid MachineId, bool IsOnline, int ClientCount)>? ServerStatusChanged;

	bool IsRunning(Guid machineId);

	int GetConnectedClients(Guid machineId);

	Task StartServerAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken = default(CancellationToken));

	Task StopServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task StopAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	ushort? GetNamespaceIndex(Guid machineId);
}
