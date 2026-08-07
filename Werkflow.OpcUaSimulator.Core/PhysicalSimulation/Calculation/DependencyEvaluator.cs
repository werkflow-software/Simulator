using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;

public static class DependencyEvaluator
{
	public static double Evaluate(DependencyType type, double sourceValue, double weight, double offset, double? minEffect, double? maxEffect, double threshold = 0.0, double? previousOutput = null)
	{
		double min = minEffect ?? double.NegativeInfinity;
		double max = maxEffect ?? double.PositiveInfinity;
		double valueOrDefault = maxEffect.GetValueOrDefault();
		if (1 == 0)
		{
		}
		double num = type switch
		{
			DependencyType.Linear => sourceValue * weight + offset, 
			DependencyType.InverseLinear => offset - sourceValue * weight, 
			DependencyType.Threshold => (sourceValue >= threshold) ? (sourceValue * weight + offset) : offset, 
			DependencyType.Saturating => Saturate(sourceValue * weight + offset, min, max), 
			DependencyType.Polynomial => Math.Pow(sourceValue, 2.0) * weight + offset, 
			DependencyType.Sigmoid => Sigmoid(sourceValue) * weight + offset, 
			DependencyType.PiecewiseLinear => Piecewise(sourceValue, weight, offset), 
			DependencyType.RateLimited => RateLimit(sourceValue * weight + offset, previousOutput ?? sourceValue, valueOrDefault), 
			DependencyType.Hysteresis => Hysteresis(sourceValue, threshold, weight, offset, previousOutput ?? sourceValue), 
			DependencyType.DelayedLinear => sourceValue * weight + offset, 
			_ => sourceValue * weight + offset, 
		};
		if (1 == 0)
		{
		}
		double num2 = num;
		if (type != DependencyType.Saturating && type != DependencyType.RateLimited && type != DependencyType.Hysteresis)
		{
			num2 = ApplyEffectLimits(num2, minEffect, maxEffect);
		}
		return num2;
	}

	public static double ApplyEffectLimits(double value, double? minEffect, double? maxEffect)
	{
		if (minEffect.HasValue && maxEffect.HasValue)
		{
			if (minEffect.Value > maxEffect.Value)
			{
				return value;
			}
			return Math.Clamp(value, minEffect.Value, maxEffect.Value);
		}
		if (minEffect.HasValue)
		{
			return Math.Max(value, minEffect.Value);
		}
		if (maxEffect.HasValue)
		{
			return Math.Min(value, maxEffect.Value);
		}
		return value;
	}

	private static double Saturate(double value, double min, double max)
	{
		return (max > min) ? Math.Clamp(value, min, max) : value;
	}

	private static double Sigmoid(double x)
	{
		return 1.0 / (1.0 + Math.Exp(-6.0 * (x - 0.5)));
	}

	private static double Piecewise(double x, double weight, double offset)
	{
		return (x < 0.33) ? (x * weight * 0.5 + offset) : ((x < 0.66) ? (x * weight + offset) : (x * weight * 1.3 + offset));
	}

	private static double RateLimit(double target, double previous, double maxRate)
	{
		return (maxRate <= 0.0) ? target : Math.Clamp(target, previous - maxRate, previous + maxRate);
	}

	private static double Hysteresis(double source, double threshold, double weight, double offset, double previous)
	{
		return (source >= threshold) ? (source * weight + offset) : (previous * 0.95 + offset * 0.05);
	}
}
