using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic VIGIL LAB run configuration (Run 001 baseline, Run 002/003 short profile, Run 004 validation profile).
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

	/// <summary>RUN-004 short validation profile: JOB-001 (Halter_01, catalog index 0).</summary>
	public const int Run004Job1CatalogIndex = 0;

	/// <summary>RUN-004 short validation profile: JOB-002 (Flansch_02, catalog index 1).</summary>
	public const int Run004Job2CatalogIndex = 1;

	/// <summary>~4 min production at ~40.5 s/part (evaluator wall clock).</summary>
	public const int Run004Job1Quantity = 6;

	/// <summary>~9.8 min production at ~97.8 s/part (evaluator wall clock).</summary>
	public const int Run004Job2Quantity = 6;

	/// <summary>Active VIGIL LAB short-profile quantities for simulator runtime.</summary>
	public const int ActiveJob1Quantity = Run004Job1Quantity;

	/// <summary>Active VIGIL LAB short-profile quantities for simulator runtime.</summary>
	public const int ActiveJob2Quantity = Run004Job2Quantity;

	/// <summary>
	/// RUN-004 job-change pause lower bound in simulation seconds.
	/// With default speed factors (1.0 x 2.0) this yields ~30 s wall clock.
	/// </summary>
	public const int Run004MinJobChangePauseSeconds = 60;

	/// <summary>
	/// RUN-004 job-change pause upper bound in simulation seconds.
	/// With default speed factors (1.0 x 2.0) this yields ~60 s wall clock.
	/// </summary>
	public const int Run004MaxJobChangePauseSeconds = 120;

	public static int ResolveSimulationSeed(Guid machineId, int globalSeed) =>
		machineId == VigilLabMachineContract.MachineId ? RandomSeed : globalSeed;

	public static void ApplyDeterministicSettings(SimulationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.RandomSeed = RandomSeed;
		settings.GenerateNewSeedOnStart = false;
		settings.RandomModeEnabled = false;
	}

	public static void ResolveJobChangePauseRange(
		Guid machineId,
		out int minPauseSeconds,
		out int maxPauseSeconds)
	{
		if (machineId == VigilLabMachineContract.MachineId)
		{
			minPauseSeconds = Run004MinJobChangePauseSeconds;
			maxPauseSeconds = Run004MaxJobChangePauseSeconds;
			return;
		}

		minPauseSeconds = FixedSimulationCatalog.MinJobChangePauseSeconds;
		maxPauseSeconds = FixedSimulationCatalog.MaxJobChangePauseSeconds;
	}

	public static (double MinWallSeconds, double MaxWallSeconds) ResolveExpectedJobChangeWallClock(
		double simulationSpeedFactor,
		double productionSpeedFactor)
	{
		double speedFactor = Math.Max(0.1, simulationSpeedFactor * productionSpeedFactor);
		return (
			Run004MinJobChangePauseSeconds / speedFactor,
			Run004MaxJobChangePauseSeconds / speedFactor);
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
			Run004Job1CatalogIndex => ActiveJob1Quantity,
			Run004Job2CatalogIndex => ActiveJob2Quantity,
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
