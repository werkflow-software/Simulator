using System.Globalization;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

public static class HmiSignalCatalog
{
	public static readonly IReadOnlyList<(string TabKey, string Title, SignalCategory[] Categories)> TabDefinitions =
	[
		("axes", "Achsen", [SignalCategory.Axis]),
		("motors", "Motoren", [SignalCategory.Drive]),
		("temperatures", "Temperaturen", [SignalCategory.Thermal]),
		("process", "Prozess", [SignalCategory.Process]),
		("cooling", "Kühlung", [SignalCategory.Cooling]),
		("power", "Leistung", [SignalCategory.Electrical]),
		("vibration", "Vibration", [SignalCategory.Vibration]),
		("production", "Produktion", [SignalCategory.Production]),
		("other", "Weitere Signale", [
			SignalCategory.Pneumatic,
			SignalCategory.Hydraulic,
			SignalCategory.Quality,
			SignalCategory.Optical,
			SignalCategory.Safety,
			SignalCategory.Environment,
			SignalCategory.Diagnostic,
			SignalCategory.Auxiliary
		])
	];

	public static string FormatDisplayName(SignalDefinition definition)
	{
		if (!string.IsNullOrWhiteSpace(definition.DisplayName))
		{
			return definition.DisplayName;
		}

		string lastSegment = definition.SignalId.Split('.').LastOrDefault() ?? definition.SignalId;
		return Humanize(lastSegment);
	}

	public static string FormatValue(SignalDefinition definition, double value)
	{
		int decimals = definition.DecimalPlaces > 0 ? definition.DecimalPlaces : 2;
		string formatted = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
		if (!string.IsNullOrWhiteSpace(definition.EngineeringUnit))
		{
			return $"{formatted} {definition.EngineeringUnit}";
		}

		return formatted;
	}

	public static string? ExtractAxisKey(string signalId)
	{
		if (signalId.StartsWith("Axis", StringComparison.OrdinalIgnoreCase))
		{
			int dot = signalId.IndexOf('.');
			return dot > 0 ? signalId[..dot] : signalId;
		}

		return null;
	}

	private static string Humanize(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		return System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
	}
}
