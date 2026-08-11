using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IPhysicalSignalPublishingCoordinator
{
	void PrepareMachine(MachineConfiguration machine, int simulationSeed);

	Task StartForMachineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task StopForMachineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task StopAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task PauseAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task ResumeAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	PhysicalMachineSession? GetSession(Guid machineId);

	IReadOnlyList<PhysicalMachineSession> GetSessions();

	Task<bool> SetManualValueAsync(Guid machineId, string signalId, object value, CancellationToken cancellationToken = default(CancellationToken));

	void EnableManualOverride(Guid machineId, bool enabled);

	bool TrySetGenerationMode(Guid machineId, SignalGenerationMode mode);

	SignalGenerationMode GetGenerationMode(Guid machineId);

	void BeginJobChange(Guid machineId, int pauseSimulationSeconds, FixedProductionJobDefinition nextJob);

	void ApplyProductionJob(Guid machineId, FixedProductionJobDefinition job);

	void SyncProductionCounters(Guid machineId, int actualCounter, int targetCounter);

	int ConsumePendingPartCompletions(Guid machineId);

	Task PauseProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	Task ResumeProductionAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	void StopProduction(Guid machineId);

	void AbortProductionForJobChange(Guid machineId, FixedProductionJobDefinition nextJob);

	(double partRemainingSeconds, double jobRemainingSeconds) GetProductionTimeEstimates(Guid machineId);

	double GetSetupRemainingSeconds(Guid machineId);

	double GetNozzleChangeRemainingSeconds(Guid machineId);
}
