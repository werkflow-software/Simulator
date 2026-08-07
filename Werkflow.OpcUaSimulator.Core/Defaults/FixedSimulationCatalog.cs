using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public static class FixedSimulationCatalog
{
	public const int JobCount = 20;

	public const int DefaultRandomSeed = 42;

	public static readonly int[] BatchSizes = new int[20]
	{
		125, 250, 500, 75, 1000, 300, 400, 600, 50, 800,
		200, 450, 350, 900, 100, 275, 725, 175, 625, 525
	};

	public static List<SimulationJob> CreateJobs()
	{
		List<SimulationJob> list = new List<SimulationJob>();
		for (int i = 1; i <= 20; i++)
		{
			list.Add(new SimulationJob
			{
				PartName = $"Part-{i:D3}",
				JobName = $"Job-{i:D3}",
				TargetQuantity = BatchSizes[i - 1],
				Priority = i % 5 + 1,
				Status = JobState.Pending,
				CreatedAt = DateTime.UtcNow
			});
		}
		return list;
	}

	public static SimulationSettings CreateDefaultSettings()
	{
		return new SimulationSettings
		{
			RandomModeEnabled = true,
			RandomSeed = 42,
			GenerateNewSeedOnStart = true,
			SimulationSpeedFactor = 1.0,
			JobCount = 20,
			MinBatchSize = 25,
			MaxBatchSize = 1000,
			PartNamePrefix = "Part",
			JobNamePrefix = "Job",
			DistributeJobsRandomly = true,
			ChangeJobOnlyAfterCompletion = true,
			ReuseCompletedJobs = true,
			SetupTimeMs = 3000,
			PauseBetweenJobsMs = 1000,
			AutoRestartCompletedJobs = true,
			HeartbeatIntervalMs = 1000,
			LogMaxEntries = 5000,
			UseRealisticPartNames = false
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
}
