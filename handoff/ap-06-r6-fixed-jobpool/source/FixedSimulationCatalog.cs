using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public static class FixedSimulationCatalog
{
	public const int JobCount = 20;

	public const int DefaultRandomSeed = 42;

	public const int MinJobChangePauseSeconds = 60;

	public const int MaxJobChangePauseSeconds = 300;

	private static readonly FixedProductionJobDefinition[] Definitions = BuildDefinitions();

	public static IReadOnlyList<FixedProductionJobDefinition> GetDefinitions() => Definitions;

	public static FixedProductionJobDefinition GetDefinition(int catalogIndex)
	{
		if (catalogIndex < 0 || catalogIndex >= JobCount)
		{
			throw new ArgumentOutOfRangeException(nameof(catalogIndex));
		}

		return Definitions[catalogIndex];
	}

	public static int GetNextCatalogIndex(int currentCatalogIndex) => (currentCatalogIndex + 1) % JobCount;

	public static List<SimulationJob> CreateJobs()
	{
		List<SimulationJob> list = new List<SimulationJob>();
		foreach (FixedProductionJobDefinition definition in Definitions)
		{
			list.Add(CreateSimulationJob(definition));
		}

		return list;
	}

	public static SimulationJob CreateSimulationJob(FixedProductionJobDefinition definition)
	{
		return new SimulationJob
		{
			CatalogIndex = definition.CatalogIndex,
			PartName = definition.PartName,
			JobName = definition.JobName,
			TargetQuantity = definition.TargetQuantity,
			MaterialName = definition.MaterialName,
			MaterialThicknessMm = definition.MaterialThicknessMm,
			RecipeName = definition.RecipeName,
			ProgramName = definition.ProgramName,
			Priority = definition.CatalogIndex % 5 + 1,
			Status = JobState.Pending,
			CreatedAt = DateTime.UtcNow
		};
	}

	public static SimulationSettings CreateDefaultSettings()
	{
		return new SimulationSettings
		{
			RandomModeEnabled = true,
			RandomSeed = DefaultRandomSeed,
			GenerateNewSeedOnStart = true,
			SimulationSpeedFactor = 1.0,
			JobCount = JobCount,
			MinBatchSize = 50,
			MaxBatchSize = 1000,
			PartNamePrefix = "Part",
			JobNamePrefix = "Job",
			DistributeJobsRandomly = false,
			ChangeJobOnlyAfterCompletion = true,
			ReuseCompletedJobs = false,
			SetupTimeMs = 3000,
			PauseBetweenJobsMs = 1000,
			AutoRestartCompletedJobs = true,
			HeartbeatIntervalMs = 1000,
			LogMaxEntries = 5000,
			UseRealisticPartNames = true
		};
	}

	public static EventSettings CreateDefaultEvents()
	{
		return new EventSettings
		{
			Events = EventSettings.CreateDefaults(),
			ErrorMessages = DefaultErrorMessages.Create()
		};
	}

	private static FixedProductionJobDefinition[] BuildDefinitions()
	{
		return new FixedProductionJobDefinition[]
		{
			Def(0, "JOB-001", "Halter_01", 50, "S235JR", 1.0, "LaserCut-Standard-A", "PRG-12045"),
			Def(1, "JOB-002", "Flansch_02", 75, "1.4301", 1.5, "LaserCut-Fine-B", "PRG-12046"),
			Def(2, "JOB-003", "Abdeckung_03", 100, "AlMg3-EN-AW5754", 2.0, "LaserCut-Standard-A", "PRG-12045"),
			Def(3, "JOB-004", "Grundplatte_04", 140, "CuZn37", 2.0, "Mill-Contour-C", "PRG-22010"),
			Def(4, "JOB-005", "Seitenblech_05", 180, "S235JR", 3.0, "LaserCut-Standard-A", "PRG-12046"),
			Def(5, "JOB-006", "Traeger_06", 220, "1.4301", 3.0, "LaserCut-Fine-B", "PRG-33008"),
			Def(6, "JOB-007", "Winkel_07", 275, "AlMg3-EN-AW5754", 4.0, "LaserCut-Standard-A", "PRG-12045"),
			Def(7, "JOB-008", "Konsole_08", 320, "CuZn37", 4.0, "Mill-Contour-C", "PRG-22010"),
			Def(8, "JOB-009", "Rahmenplatte_09", 400, "S235JR", 5.0, "LaserCut-Fine-B", "PRG-12046"),
			Def(9, "JOB-010", "Verstaerkung_10", 500, "1.4301", 5.0, "LaserCut-Standard-A", "PRG-33008"),
			Def(10, "JOB-011", "Gehaeuseteil_11", 600, "AlMg3-EN-AW5754", 6.0, "LaserCut-Fine-B", "PRG-12045"),
			Def(11, "JOB-012", "Montageplatte_12", 750, "CuZn37", 6.0, "Mill-Contour-C", "PRG-22010"),
			Def(12, "JOB-013", "Halter_13", 850, "S235JR", 8.0, "LaserCut-Standard-A", "PRG-12046"),
			Def(13, "JOB-014", "Flansch_14", 900, "1.4301", 8.0, "LaserCut-Fine-B", "PRG-33008"),
			Def(14, "JOB-015", "Abdeckung_15", 1000, "AlMg3-EN-AW5754", 10.0, "LaserCut-Standard-A", "PRG-12045"),
			Def(15, "JOB-016", "Grundplatte_16", 125, "CuZn37", 1.5, "Mill-Contour-C", "PRG-22010"),
			Def(16, "JOB-017", "Seitenblech_17", 250, "S235JR", 2.5, "LaserCut-Fine-B", "PRG-12046"),
			Def(17, "JOB-018", "Traeger_18", 350, "1.4301", 4.5, "LaserCut-Standard-A", "PRG-33008"),
			Def(18, "JOB-019", "Winkel_19", 625, "AlMg3-EN-AW5754", 7.0, "LaserCut-Fine-B", "PRG-12045"),
			Def(19, "JOB-020", "Konsole_20", 525, "CuZn37", 9.0, "Mill-Contour-C", "PRG-22010")
		};
	}

	private static FixedProductionJobDefinition Def(
		int index,
		string jobName,
		string partName,
		int quantity,
		string material,
		double thicknessMm,
		string recipe,
		string program) =>
		new()
		{
			CatalogIndex = index,
			JobName = jobName,
			PartName = partName,
			TargetQuantity = quantity,
			MaterialName = material,
			MaterialThicknessMm = thicknessMm,
			RecipeName = recipe,
			ProgramName = program
		};
}
