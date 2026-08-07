using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;

public sealed class SeededRandomStreams
{
	private readonly Random _process;

	private readonly Random _noise;

	private readonly Random _outlier;

	private readonly Random _phase;

	public SeededRandomStreams(int seed)
	{
		_process = new Random(seed ^ 0x51ED);
		_noise = new Random(seed ^ 0xBEEF);
		_outlier = new Random(seed ^ 0xCAFE);
		_phase = new Random(seed ^ 0xFACE);
	}

	public double ProcessNoise(double amplitude)
	{
		return (amplitude <= 0.0) ? 0.0 : ((_process.NextDouble() * 2.0 - 1.0) * amplitude);
	}

	public double SignalNoise(double amplitude)
	{
		return (amplitude <= 0.0) ? 0.0 : ((_noise.NextDouble() * 2.0 - 1.0) * amplitude);
	}

	public bool ShouldTriggerOutlier(double probability)
	{
		return probability > 0.0 && _outlier.NextDouble() < probability;
	}

	public double OutlierMagnitude(double maxMagnitude)
	{
		return (maxMagnitude <= 0.0) ? 0.0 : ((_outlier.NextDouble() * 2.0 - 1.0) * maxMagnitude);
	}

	public double PhaseDurationFactor(double minFactor, double maxFactor)
	{
		return minFactor + _phase.NextDouble() * (maxFactor - minFactor);
	}
}
