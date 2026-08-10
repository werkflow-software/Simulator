using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Metrics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Export;

public sealed class ExperimentExporter
{
	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() }
	};

	private readonly JsonSerializerOptions _jsonLinesOptions = new()
	{
		Converters = { new JsonStringEnumConverter() }
	};

	public string BaseDirectory { get; init; } = Path.Combine("experiments");

	public async Task<string> ExportAsync(
		Experiments.ExperimentDefinition definition,
		Experiments.ExperimentResult result,
		IReadOnlyList<GroundTruthEvent> groundTruth,
		IReadOnlyList<VigilEvent> vigilEvents,
		EvaluationMetrics metrics,
		CancellationToken cancellationToken = default)
	{
		string dir = Path.Combine(BaseDirectory, definition.ExperimentId);
		Directory.CreateDirectory(dir);

		await File.WriteAllTextAsync(
			Path.Combine(dir, "experiment-definition.json"),
			JsonSerializer.Serialize(definition, _jsonOptions),
			cancellationToken);

		var manifest = new
		{
			ExperimentId = definition.ExperimentId,
			ExperimentVersion = definition.ExperimentVersion,
			ExperimentType = definition.ExperimentType.ToString(),
			MachineProfileId = definition.MachineProfileId,
			ProfileHash = result.ProfileHash,
			ScenarioId = definition.ScenarioId,
			ScenarioHash = result.ScenarioHash,
			BaseSeed = definition.BaseSeed,
			StartedAtUtc = result.StartedAtUtc,
			CompletedAtUtc = result.CompletedAtUtc,
			TimeFactor = definition.TimeFactor,
			Runs = result.Runs
		};

		await File.WriteAllTextAsync(
			Path.Combine(dir, "run-manifest.json"),
			JsonSerializer.Serialize(manifest, _jsonOptions),
			cancellationToken);

		await WriteJsonLinesAsync(Path.Combine(dir, "ground-truth.jsonl"), groundTruth, cancellationToken);
		await WriteJsonLinesAsync(Path.Combine(dir, "vigil-events.jsonl"), vigilEvents, cancellationToken);

		await File.WriteAllTextAsync(
			Path.Combine(dir, "metrics.json"),
			JsonSerializer.Serialize(metrics, _jsonOptions),
			cancellationToken);

		var summary = BuildSummary(definition, result, metrics);
		await File.WriteAllTextAsync(Path.Combine(dir, "summary.md"), summary, cancellationToken);

		return Path.GetFullPath(dir);
	}

	private static async Task WriteJsonLinesAsync<T>(string path, IReadOnlyList<T> items, CancellationToken token)
	{
		var sb = new StringBuilder();
		var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
		foreach (var item in items)
		{
			sb.AppendLine(JsonSerializer.Serialize(item, options));
		}
		await File.WriteAllTextAsync(path, sb.ToString(), token);
	}

	private static string BuildSummary(
		Experiments.ExperimentDefinition definition,
		Experiments.ExperimentResult result,
		EvaluationMetrics metrics)
	{
		return $"""
# Experiment {definition.ExperimentId}

- Display: {definition.DisplayName}
- Runs: {result.Runs.Count}
- VigilEvaluationAvailable: {metrics.VigilEvaluationAvailable}
- DetectionRate: {metrics.DetectionRate}
- FalsePositiveRate: {metrics.FalsePositiveRate}
- Export: {result.ExportPath}
""";
	}
}
