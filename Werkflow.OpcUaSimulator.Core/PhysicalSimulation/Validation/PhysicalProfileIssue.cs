namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

public sealed class PhysicalProfileIssue
{
	public required string Code { get; init; }

	public required string Message { get; init; }

	public string FieldPath { get; init; } = string.Empty;

	public PhysicalProfileIssueSeverity Severity { get; init; } = PhysicalProfileIssueSeverity.Error;
}
