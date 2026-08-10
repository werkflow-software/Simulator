using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public static class ExperimentSeedDeriver
{
	public static int DeriveRunSeed(int baseSeed, int runIndex, string runType)
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 31 + baseSeed;
			hash = hash * 31 + runIndex;
			hash = hash * 31 + runType.GetHashCode(StringComparison.Ordinal);
			return hash == 0 ? baseSeed : hash;
		}
	}

	public static double DeriveIntensity(double baseIntensity, int runIndex, int baseSeed, ExperimentVariationDefinition variation)
	{
		var rng = new Random(DeriveRunSeed(baseSeed, runIndex, "intensity"));
		double delta = (rng.NextDouble() * 2.0 - 1.0) * variation.IntensityVariationFraction;
		return Math.Clamp(baseIntensity + delta, 0.25, 1.5);
	}

	public static TimeSpan DeriveStartOffset(int runIndex, int baseSeed, ExperimentVariationDefinition variation)
	{
		var rng = new Random(DeriveRunSeed(baseSeed, runIndex, "start-offset"));
		double seconds = rng.NextDouble() * variation.StartOffsetVariation.TotalSeconds;
		return TimeSpan.FromSeconds(seconds);
	}
}
