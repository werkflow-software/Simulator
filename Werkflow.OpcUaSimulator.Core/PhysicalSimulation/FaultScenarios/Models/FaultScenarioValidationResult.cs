using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioValidationResult
{
	public bool IsValid => Errors.Count == 0;

	public List<FaultScenarioValidationError> Errors { get; } = new List<FaultScenarioValidationError>();
}
