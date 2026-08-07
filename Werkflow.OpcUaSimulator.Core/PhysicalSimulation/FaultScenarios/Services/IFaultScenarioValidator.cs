using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultScenarioValidator
{
	FaultScenarioValidationResult ValidateCatalog(IReadOnlyList<FaultScenarioDefinition> scenarios);

	FaultScenarioValidationResult ValidateForProfile(FaultScenarioDefinition scenario, PhysicalMachineProfile profile);
}
