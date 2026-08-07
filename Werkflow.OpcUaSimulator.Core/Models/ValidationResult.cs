using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class ValidationResult
{
	public bool IsValid => Errors.Count == 0;

	public List<string> Errors { get; set; } = new List<string>();

	public List<string> Warnings { get; set; } = new List<string>();

	public void AddError(string message)
	{
		Errors.Add(message);
	}

	public void AddWarning(string message)
	{
		Warnings.Add(message);
	}
}
