using System;

namespace Werkflow.OpcUaSimulator.Core.Utilities;

public static class SimulationRandom
{
	public static Random Create(int seed)
	{
		return new Random(seed);
	}

	public static bool Roll(Random random, double probabilityPercent)
	{
		return random.NextDouble() * 100.0 < probabilityPercent;
	}

	public static int NextInRange(Random random, int min, int max)
	{
		if (min > max)
		{
			int num = max;
			max = min;
			min = num;
		}
		return random.Next(min, max + 1);
	}

	public static int ScaleInterval(int intervalMs, double speedFactor)
	{
		if (speedFactor <= 0.0)
		{
			return intervalMs;
		}
		return Math.Max(50, (int)((double)intervalMs / speedFactor));
	}
}
