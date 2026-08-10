using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class OpcUaGroundTruthLeakageScanner
{
	public static readonly string[] ForbiddenTerms =
	[
		"ScenarioId",
		"ExperimentId",
		"RunId",
		"RunSeed",
		"FaultRepetitionIndex",
		"GroundTruth",
		"HiddenState",
		"Detectable",
		"FaultScenario",
		"laser-overheating-axis-drive",
		"hydraulic-leak"
	];

	public static Ap5OpcUaLeakageReport ScanSession(PhysicalMachineSession session, string endpoint)
	{
		var report = new Ap5OpcUaLeakageReport
		{
			MachineId = session.MachineId,
			Endpoint = endpoint,
			ForbiddenTerms = ForbiddenTerms.ToList()
		};

		foreach (var signal in session.Profile.Signals.Where(s => s.IsEnabled))
		{
			report.NodesInspected++;
			InspectText(signal.SignalId, report);
			InspectText(signal.BrowseName, report);
			InspectText(signal.DisplayName, report);
			InspectText(signal.Description, report);
			InspectText(signal.NodeId, report);

			var runtime = session.Runtime.Signals.FirstOrDefault(r =>
				r.SignalId.Equals(signal.SignalId, StringComparison.OrdinalIgnoreCase));
			if (runtime != null)
			{
				report.ValuesInspected++;
				InspectText(runtime.CurrentValue.ToString(), report);
			}
		}

		report.Passed = report.Matches.Count == 0;
		return report;
	}

	private static void InspectText(string? text, Ap5OpcUaLeakageReport report)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		if (IsAllowedMachineErrorMessage(text))
		{
			report.AllowedMachineErrorMessages.Add(text);
			return;
		}

		foreach (var term in ForbiddenTerms)
		{
			if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
			{
				report.Matches.Add(new Ap5LeakageMatch(term, text));
			}
		}
	}

	private static bool IsAllowedMachineErrorMessage(string text) =>
		text.Contains("Temperaturgrenzwert", StringComparison.OrdinalIgnoreCase)
		|| text.Contains("Hydraulikleck", StringComparison.OrdinalIgnoreCase)
		|| text.Contains("Vordruck unter", StringComparison.OrdinalIgnoreCase)
		|| text.Contains("Achsmotor", StringComparison.OrdinalIgnoreCase);
}

public sealed class Ap5OpcUaLeakageReport
{
	public Guid MachineId { get; set; }
	public string Endpoint { get; set; } = "";
	public int NodesInspected { get; set; }
	public int ValuesInspected { get; set; }
	public List<string> ForbiddenTerms { get; set; } = [];
	public List<Ap5LeakageMatch> Matches { get; set; } = [];
	public List<string> AllowedMachineErrorMessages { get; set; } = [];
	public bool Passed { get; set; }
}

public sealed record Ap5LeakageMatch(string Term, string Context);
