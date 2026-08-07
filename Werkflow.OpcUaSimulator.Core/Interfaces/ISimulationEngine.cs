using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface ISimulationEngine
{
	SimulationState State { get; }

	DateTime? StartedAt { get; }

	int TotalProducedParts { get; }

	int ActiveErrorCount { get; }

	int RunningServerCount { get; }

	int TotalConnectedClients { get; }

	int CurrentSeed { get; }

	event EventHandler? StateChanged;

	event EventHandler<MachineRuntimeState>? MachineStateChanged;

	IReadOnlyDictionary<Guid, MachineRuntimeState> GetRuntimeStates();

	MachineRuntimeState? GetRuntimeState(Guid machineId);

	Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task PauseAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task ResumeAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task ResetAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task StartMachineServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task StopMachineServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	void ApplyManualValues(Guid machineId, string partName, string jobName, int actualCounter, int targetCounter, MachineState state, bool errorActive, string errorMessage, int productionIntervalMs, int stepSize);

	Task StartProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task PauseProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task ProduceNextPartAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task TriggerErrorAsync(Guid machineId, string? message = null, CancellationToken cancellationToken = default(CancellationToken));

	Task ClearErrorAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task SetMachineOfflineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task SetMachineOnlineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task CompleteJobAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task ResetCountersAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	void SetCounterFrozen(Guid machineId, bool frozen);

	Task SetMachineStateManualAsync(Guid machineId, MachineState state, CancellationToken cancellationToken = default(CancellationToken));
}
