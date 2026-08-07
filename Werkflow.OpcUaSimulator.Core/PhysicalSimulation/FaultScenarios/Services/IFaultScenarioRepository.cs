using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultScenarioRepository
{
	Task LoadAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	IReadOnlyList<FaultScenarioDefinition> GetAll();

	FaultScenarioDefinition? GetById(string scenarioId);
}
