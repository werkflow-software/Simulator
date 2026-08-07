using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IScenarioService
{
	IReadOnlyList<ScenarioDefinition> Scenarios { get; }

	event EventHandler? ScenariosChanged;

	Task StartScenarioAsync(string scenarioId, Guid? targetMachineId, int? durationMs, CancellationToken cancellationToken = default(CancellationToken));

	Task StopScenarioAsync(string scenarioId, CancellationToken cancellationToken = default(CancellationToken));

	Task StopAllScenariosAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task TriggerEventAsync(SimulationEventType eventType, Guid machineId, CancellationToken cancellationToken = default(CancellationToken));
}
