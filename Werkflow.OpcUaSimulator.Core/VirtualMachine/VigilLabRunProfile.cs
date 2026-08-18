using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic VIGIL LAB run configuration (Run 001 baseline, Run 002 short profile).
/// </summary>
public static class VigilLabRunProfile
{
	public const int RandomSeed = 42;

	public static readonly int[] FixedJobCatalogIndices = [0, 1, 2, 3];

	public static readonly string[] FixedJobIds = ["JOB-001", "JOB-002", "JOB-003", "JOB-004"];

	/// <summary>RUN-002 short profile: JOB-001 (Halter_01, catalog index 0).</summary>
	public const int Run002Job1CatalogIndex = 0;

	/// <summary>RUN-002 short profile: JOB-002 (Flansch_02, catalog index 1).</summary>
	public const int Run002Job2CatalogIndex = 1;

	/// <summary>~11 min production at ~58 s/part (wall clock, physical simulation).</summary>
	public const int Run002Job1Quantity = 11;

	/// <summary>~10 min production at ~41 s/part (wall clock, physical simulation).</summary>
	public const int Run002Job2Quantity = 15;

	public static int ResolveSimulationSeed(Guid machineId, int globalSeed) =>
		machineId == VigilLabMachineContract.MachineId ? RandomSeed : globalSeed;

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
		if (machineId != VigilLabMachineContract.MachineId)
		{
			return definition;
		}

		int? shortQuantity = catalogIndex switch
		{
			Run002Job1CatalogIndex => Run002Job1Quantity,
			Run002Job2CatalogIndex => Run002Job2Quantity,
			_ => null
		};

		return shortQuantity.HasValue
			? FixedSimulationCatalog.WithTargetQuantity(definition, shortQuantity.Value)
			: definition;
	}

	public static void SynchronizeSimulationJob(SimulationJob job, Guid machineId)
	{
		ArgumentNullException.ThrowIfNull(job);
		if (job.CatalogIndex < 0 || job.CatalogIndex >= FixedSimulationCatalog.JobCount)
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
