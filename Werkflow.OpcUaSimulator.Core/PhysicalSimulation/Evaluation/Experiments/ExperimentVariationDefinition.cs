using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public sealed class ExperimentVariationDefinition
{
	public double IntensityVariationFraction { get; set; } = 0.15;

	public TimeSpan StartOffsetVariation { get; set; } = TimeSpan.FromSeconds(30);

	public double DriftRateVariationFraction { get; set; } = 0.1;

	public double DurationVariationFraction { get; set; } = 0.1;

	public double BackgroundNoiseVariation { get; set; } = 0.05;

	public bool VaryJobBetweenRuns { get; set; } = true;

	public bool VaryNormalPhaseBetweenRuns { get; set; } = true;
}
