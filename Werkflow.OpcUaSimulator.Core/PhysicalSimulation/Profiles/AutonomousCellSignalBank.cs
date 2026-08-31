using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class AutonomousCellSignalBank
{
	public static IReadOnlyList<string> ResolveEnabledSignalIds(AutonomousCellSignalProfileTier tier)
	{
		int target = (int)tier;
		if (target <= AutonomousCellScale96SignalIds.All.Count)
		{
			return AutonomousCellScale96SignalIds.All.Take(target).ToList();
		}

		List<string> ids = AutonomousCellScale96SignalIds.All.ToList();
		for (int slot = ids.Count; slot < target; slot++)
		{
			ids.Add(GenerateBankSignalKey(slot));
		}

		return ids;
	}

	public static IReadOnlyList<SignalDefinition> GenerateDefinitions(AutonomousCellSignalProfileTier tier)
	{
		int target = (int)tier;
		if (target <= AutonomousCellScale96SignalIds.All.Count)
		{
			return [];
		}

		List<SignalDefinition> definitions = [];
		for (int slot = AutonomousCellScale96SignalIds.All.Count; slot < target; slot++)
		{
			definitions.Add(CreateBankDefinition(slot));
		}

		return definitions;
	}

	public static string GetRelevanceClass(int slotIndex) =>
		(slotIndex % 12) switch
		{
			0 or 1 => "RELEVANT_SECONDARY",
			2 => "REDUNDANT",
			3 => "DERIVED_CORRELATED",
			4 => "WEAK_RELATION",
			5 => "SLOW_DRIFT",
			6 => "SPARSE",
			7 => "NOISY",
			8 => "CONSTANT_OR_NEAR_CONSTANT",
			9 => "IRRELEVANT_INDEPENDENT",
			10 => "DELAYED_COPY",
			_ => "INTERMITTENT_AVAILABILITY"
		};

	public static string GenerateBankSignalKey(int slotIndex) =>
		$"Bank.Slot{slotIndex:D4}.Value";

	private static SignalDefinition CreateBankDefinition(int slotIndex) =>
		new()
		{
			SignalId = GenerateBankSignalKey(slotIndex),
			NodeId = GenerateBankSignalKey(slotIndex),
			BrowseName = GenerateBankSignalKey(slotIndex),
			DisplayName = $"Bank Slot {slotIndex}",
			Category = SignalCategory.Process,
			DataType = PhysicalSignalDataType.Double,
			EngineeringUnit = "unitless",
			NormalMinimum = 0,
			NormalMaximum = 100,
			NominalValue = 0,
			HardMinimum = -10,
			HardMaximum = 110,
			InitialValue = 0,
			UpdateInterval = TimeSpan.FromSeconds(1),
			IsEnabled = false
		};
}
