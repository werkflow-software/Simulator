using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic baseline configuration for Virtual Press Brake (Machine 2).
/// </summary>
public static class VirtualPressBrakeRunProfile
{
	public const int RandomSeed = 194;

	public static readonly int[] BaselineProgramIndices = [0, 1, 2];

	public const int BaselineBatchQuantity = 10;

	/// <summary>Approximate baseline wall-clock duration at 1x simulation speed with 2x machine factor (~18-22 min).</summary>
	public const int ApproximateBaselineWallClockMinutes = 20;

	public static int ResolveSimulationSeed(Guid machineId, int globalSeed) =>
		machineId == VirtualPressBrakeContract.MachineId ? RandomSeed : globalSeed;

	public static void ApplyDeterministicSettings(SimulationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.RandomSeed = RandomSeed;
		settings.GenerateNewSeedOnStart = false;
		settings.RandomModeEnabled = false;
	}

	public static FixedProductionJobDefinition ResolveJobDefinition(Guid machineId, int catalogIndex)
	{
		FixedProductionJobDefinition definition = FixedSimulationCatalog.GetDefinition(catalogIndex);
		if (machineId != VirtualPressBrakeContract.MachineId)
		{
			return definition;
		}

		int programIndex = BaselineProgramIndices[catalogIndex % BaselineProgramIndices.Length];
		return new FixedProductionJobDefinition
		{
			CatalogIndex = programIndex,
			JobName = $"PB-{programIndex + 1:00}",
			PartName = $"PB-PART-{programIndex + 1:00}",
			TargetQuantity = BaselineBatchQuantity,
			MaterialName = definition.MaterialName,
			MaterialThicknessMm = definition.MaterialThicknessMm,
			RecipeName = definition.RecipeName,
			ProgramName = $"PRG-{programIndex}"
		};
	}

	public static void SynchronizeSimulationJob(SimulationJob job, Guid machineId)
	{
		ArgumentNullException.ThrowIfNull(job);
		if (machineId != VirtualPressBrakeContract.MachineId || job.CatalogIndex < 0)
		{
			return;
		}

		FixedProductionJobDefinition definition = ResolveJobDefinition(machineId, job.CatalogIndex);
		job.JobName = definition.JobName;
		job.PartName = definition.PartName;
		job.TargetQuantity = definition.TargetQuantity;
		job.MaterialName = definition.MaterialName;
		job.MaterialThicknessMm = definition.MaterialThicknessMm;
		job.RecipeName = definition.RecipeName;
		job.ProgramName = definition.ProgramName;
	}
}
