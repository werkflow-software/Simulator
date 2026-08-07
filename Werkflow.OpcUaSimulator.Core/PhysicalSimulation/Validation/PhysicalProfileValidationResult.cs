using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

public sealed class PhysicalProfileValidationResult
{
	public bool IsValid => Errors.Count == 0;

	public List<PhysicalProfileIssue> Errors { get; } = new List<PhysicalProfileIssue>();

	public List<PhysicalProfileIssue> Warnings { get; } = new List<PhysicalProfileIssue>();

	public void AddError(string code, string message, string fieldPath = "")
	{
		Errors.Add(new PhysicalProfileIssue
		{
			Code = code,
			Message = message,
			FieldPath = fieldPath,
			Severity = PhysicalProfileIssueSeverity.Error
		});
	}

	public void AddWarning(string code, string message, string fieldPath = "")
	{
		Warnings.Add(new PhysicalProfileIssue
		{
			Code = code,
			Message = message,
			FieldPath = fieldPath,
			Severity = PhysicalProfileIssueSeverity.Warning
		});
	}
}
