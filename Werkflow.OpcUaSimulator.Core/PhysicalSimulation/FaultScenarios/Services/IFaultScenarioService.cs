using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultScenarioService
{
	event EventHandler<FaultScenarioEvent>? ScenarioEvent;

	Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));

	IReadOnlyList<FaultScenarioDefinition> GetCatalog();

	FaultScenarioValidationResult ValidateCatalog();

	IReadOnlyList<FaultScenarioRuntimeInfo> GetActiveScenarios(Guid machineId);

	Task<FaultScenarioInstance> StartAsync(FaultScenarioStartRequest request, CancellationToken cancellationToken = default(CancellationToken));

	Task PauseAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken));

	Task ResumeAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken));

	Task StopAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken));

	Task CancelAsync(Guid machineId, string scenarioId, CancellationToken cancellationToken = default(CancellationToken));

	Task ResetMachineAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken));

	void SetIntensity(Guid machineId, string scenarioId, double intensity);

	void SetTimeFactor(Guid machineId, string scenarioId, double timeFactor);

	void SetAutoThresholdFault(Guid machineId, string scenarioId, bool enabled);

	void SetAutoScenarioEnd(Guid machineId, string scenarioId, bool enabled);

	void SetDiagnosisMode(Guid machineId, bool enabled);

	bool IsDiagnosisModeEnabled(Guid machineId);

	void RegisterSession(PhysicalMachineSession session);

	void UnregisterSession(Guid machineId);

	PhysicalMachineSession? GetSession(Guid machineId);
}
