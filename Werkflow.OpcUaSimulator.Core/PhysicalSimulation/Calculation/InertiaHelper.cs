using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;

public static class InertiaHelper
{
	public static double Approach(double current, double target, double inertiaSeconds, double deltaSeconds)
	{
		if (inertiaSeconds <= 0.0 || deltaSeconds <= 0.0)
		{
			return target;
		}
		double num = 1.0 - Math.Exp((0.0 - deltaSeconds) / inertiaSeconds);
		return current + (target - current) * num;
	}

	public static double ClampToRange(double value, double min, double max)
	{
		return Math.Clamp(value, min, max);
	}
}
