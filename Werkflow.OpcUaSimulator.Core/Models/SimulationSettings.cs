using System;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class SimulationSettings
{
	public bool RandomModeEnabled { get; set; } = true;

	public int RandomSeed { get; set; } = Environment.TickCount;

	public bool GenerateNewSeedOnStart { get; set; } = true;

	public double SimulationSpeedFactor { get; set; } = 1.0;

	public int JobCount { get; set; } = 20;

	public int MinBatchSize { get; set; } = 25;

	public int MaxBatchSize { get; set; } = 1000;

	public string PartNamePrefix { get; set; } = "Part";

	public string JobNamePrefix { get; set; } = "Job";

	public bool DistributeJobsRandomly { get; set; } = true;

	public bool ChangeJobOnlyAfterCompletion { get; set; } = true;

	public bool ReuseCompletedJobs { get; set; } = true;

	public int SetupTimeMs { get; set; } = 3000;

	public int PauseBetweenJobsMs { get; set; } = 1000;

	public bool AutoRestartCompletedJobs { get; set; } = true;

	public int HeartbeatIntervalMs { get; set; } = 1000;

	public int LogMaxEntries { get; set; } = 5000;

	public bool UseRealisticPartNames { get; set; } = true;
}
