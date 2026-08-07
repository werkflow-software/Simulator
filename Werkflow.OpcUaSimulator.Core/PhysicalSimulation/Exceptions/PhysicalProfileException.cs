using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Exceptions;

public class PhysicalProfileException : Exception
{
	public string? ProfilePath { get; }

	public string? FieldPath { get; }

	public PhysicalProfileException(string message, string? profilePath = null, string? fieldPath = null)
		: base(FormatMessage(message, profilePath, fieldPath))
	{
		ProfilePath = profilePath;
		FieldPath = fieldPath;
	}

	public PhysicalProfileException(string message, Exception innerException, string? profilePath = null, string? fieldPath = null)
		: base(FormatMessage(message, profilePath, fieldPath), innerException)
	{
		ProfilePath = profilePath;
		FieldPath = fieldPath;
	}

	private static string FormatMessage(string message, string? profilePath, string? fieldPath)
	{
		List<string> list = new List<string> { message };
		if (!string.IsNullOrWhiteSpace(profilePath))
		{
			list.Add("Pfad: " + profilePath);
		}
		if (!string.IsNullOrWhiteSpace(fieldPath))
		{
			list.Add("Feld: " + fieldPath);
		}
		return string.Join(" | ", list);
	}
}
