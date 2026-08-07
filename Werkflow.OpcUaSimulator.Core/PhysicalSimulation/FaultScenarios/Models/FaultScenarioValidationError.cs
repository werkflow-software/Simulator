namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioValidationError
{
	public required string ScenarioId { get; init; }

	public required string FieldPath { get; init; }

	public required string Message { get; init; }
}
