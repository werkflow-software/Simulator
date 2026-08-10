using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public interface IExperimentRunner
{
	ExperimentRunnerState State { get; }

	ExperimentResult? LastResult { get; }

	Task<ExperimentResult> RunAsync(
		ExperimentDefinition definition,
		PhysicalMachineSession session,
		CancellationToken cancellationToken = default);

	void Pause();

	void Resume();

	void Cancel();
}

public sealed class ExperimentResult
{
	public required string ExperimentId { get; init; }

	public required string ExperimentHash { get; init; }

	public string ProfileHash { get; init; } = "";

	public string ScenarioHash { get; init; } = "";

	public DateTime StartedAtUtc { get; init; }

	public DateTime CompletedAtUtc { get; init; }

	public ExperimentRunnerState FinalState { get; init; }

	public List<RunManifestEntry> Runs { get; init; } = [];

	public string ExportPath { get; set; } = "";

	public bool Passed { get; init; }

	public List<string> FailedCriteria { get; init; } = [];
}
