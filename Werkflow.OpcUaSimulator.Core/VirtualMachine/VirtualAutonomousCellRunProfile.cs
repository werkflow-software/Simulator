using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Frozen baseline scenario for Machine 3 autonomous production cell.
/// </summary>
public static class VirtualAutonomousCellRunProfile
{
	public static readonly int MasterSeed = Machine3SeedArchitecture.MasterScenarioSeed;

	public static readonly char[] ProductSequence =
	[
		'A', 'A', 'B', 'A', 'C', 'B', 'A', 'A', 'B', 'C',
		'A', 'B', 'B', 'A', 'C', 'A', 'B', 'A', 'C', 'B',
		'A', 'A', 'C', 'B', 'A', 'B', 'C', 'A'
	];

	public const int TotalParts = 28;
	public const int PalletCapacity = 12;
	public const int ContainerCapacity = 10;
	public const int ReplenishmentAfterPart1 = 12;
	public const int ReplenishmentAfterPart2 = 24;
	public const int ExchangeAfterPart1 = 10;
	public const int ExchangeAfterPart2 = 20;
	public const double NominalPartCycleSeconds = 55.0;
	public const bool UnattendedBaselineEnabled = true;

	public const string BaselineScenarioDisplayName = "Machine-3 Baseline";

	public const int ApproximateBaselineWallClockMinutesMin = 25;

	public const int ApproximateBaselineWallClockMinutesMax = 30;

	public static int ResolveSimulationSeed(Guid machineId, int globalSeed) =>
		machineId == VirtualAutonomousProductionCellContract.MachineId ? MasterSeed : globalSeed;

	public static void ApplyDeterministicSettings(SimulationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.RandomSeed = MasterSeed;
		settings.GenerateNewSeedOnStart = false;
		settings.RandomModeEnabled = false;
	}

	public static FixedProductionJobDefinition ResolveJobDefinition(Guid machineId, int catalogIndex)
	{
		FixedProductionJobDefinition definition = FixedSimulationCatalog.GetDefinition(catalogIndex);
		if (machineId != VirtualAutonomousProductionCellContract.MachineId)
		{
			return definition;
		}

		return new FixedProductionJobDefinition
		{
			CatalogIndex = 0,
			JobName = BaselineScenarioDisplayName,
			PartName = "M3-PART",
			TargetQuantity = TotalParts,
			MaterialName = definition.MaterialName,
			MaterialThicknessMm = definition.MaterialThicknessMm,
			RecipeName = "PF-BASELINE",
			ProgramName = "M3-CELL-01"
		};
	}

	public static char GetVariantForPartIndex(int partIndex)
	{
		if (partIndex < 0 || partIndex >= ProductSequence.Length)
		{
			return 'A';
		}

		return ProductSequence[partIndex];
	}

	public static bool RequiresReplenishmentAfterPart(int completedParts) =>
		completedParts == ReplenishmentAfterPart1 || completedParts == ReplenishmentAfterPart2;

	public static bool RequiresExchangeAfterPart(int completedParts) =>
		completedParts == ExchangeAfterPart1 || completedParts == ExchangeAfterPart2;

	public static void SynchronizeSimulationJob(SimulationJob job, Guid machineId)
	{
		ArgumentNullException.ThrowIfNull(job);
		if (!VirtualAutonomousCellMachineRegistry.IsVirtualAutonomousCellMachine(machineId))
		{
			return;
		}

		FixedProductionJobDefinition definition = ResolveJobDefinition(machineId, 0);
		job.JobName = definition.JobName;
		job.PartName = definition.PartName;
		job.TargetQuantity = definition.TargetQuantity;
		job.MaterialName = definition.MaterialName;
		job.MaterialThicknessMm = definition.MaterialThicknessMm;
		job.RecipeName = definition.RecipeName;
		job.ProgramName = definition.ProgramName;
	}
}
