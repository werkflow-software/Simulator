using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Calculation;

public sealed class FaultEffectCalculator : IFaultEffectCalculator
{
	public double ComputeEffectContribution(FaultScenarioInstance instance, FaultEffectDefinition effect, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, TimeSpan deltaTime)
	{
		if (!effect.IsEnabled || !IsPhaseActive(instance.CurrentPhase, effect.StartPhase, effect.EndPhase))
		{
			return 0.0;
		}
		if (instance.ScenarioElapsedTime < effect.Delay)
		{
			return 0.0;
		}
		double intensity = instance.Intensity;
		double totalMinutes = deltaTime.TotalMinutes;
		string effectId = effect.EffectId;
		instance.EffectAccumulators.TryGetValue(effectId, out var value);
		double num;
		switch (effect.EffectType)
		{
		case FaultEffectType.AdditiveDrift:
			num = effect.RatePerSimulationMinute * totalMinutes * intensity * Sign(effect.Direction);
			break;
		case FaultEffectType.MultiplicativeDrift:
			num = value + effect.Magnitude * totalMinutes * 0.1 * intensity * Sign(effect.Direction);
			break;
		case FaultEffectType.TargetShift:
			num = effect.Magnitude * intensity * PhaseWeight(instance.CurrentPhase) * Sign(effect.Direction);
			break;
		case FaultEffectType.RateChange:
			num = effect.RatePerSimulationMinute * totalMinutes * intensity * 0.5 * Sign(effect.Direction);
			break;
		case FaultEffectType.EfficiencyLoss:
			num = (0.0 - effect.Magnitude) * totalMinutes * intensity * 0.15;
			break;
		case FaultEffectType.Oscillation:
			instance.OscillationPhase += deltaTime.TotalSeconds * effect.OscillationFrequencyHz * Math.PI * 2.0;
			num = Math.Sin(instance.OscillationPhase) * effect.Magnitude * intensity * 0.02;
			break;
		case FaultEffectType.IntermittentPulse:
			UpdateIntermittentPulse(instance, effect, deltaTime);
			num = (instance.IsIntermittentPulseActive ? (effect.Magnitude * intensity * 0.05 * Sign(effect.Direction)) : 0.0);
			break;
		case FaultEffectType.StepChange:
			num = ((instance.CurrentPhase >= effect.StartPhase) ? (effect.Magnitude * intensity * Sign(effect.Direction)) : 0.0);
			break;
		case FaultEffectType.NoiseIncrease:
			instance.NoiseModifiers[effect.TargetId] = 1.0 + effect.Magnitude * intensity;
			return 0.0;
		case FaultEffectType.SignalFreeze:
		case FaultEffectType.ConnectionDrop:
			return 0.0;
		default:
			num = effect.RatePerSimulationMinute * totalMinutes * intensity * 0.05 * Sign(effect.Direction);
			break;
		}
		value += num;
		value = Math.Clamp(value, effect.MinimumEffect, effect.MaximumEffect);
		instance.EffectAccumulators[effectId] = value;
		return num;
	}

	private static bool IsPhaseActive(FaultScenarioPhase current, FaultScenarioPhase start, FaultScenarioPhase end)
	{
		return current >= start && current <= end;
	}

	private static double Sign(FaultEffectDirection direction)
	{
		if (1 == 0)
		{
		}
		int num = direction switch
		{
			FaultEffectDirection.Decrease => -1, 
			FaultEffectDirection.Stabilize => 0, 
			_ => 1, 
		};
		if (1 == 0)
		{
		}
		return num;
	}

	private static double PhaseWeight(FaultScenarioPhase phase)
	{
		if (1 == 0)
		{
		}
		double result = phase switch
		{
			FaultScenarioPhase.Initiating => 0.2, 
			FaultScenarioPhase.Developing => 0.5, 
			FaultScenarioPhase.Degraded => 0.75, 
			FaultScenarioPhase.Critical => 0.9, 
			FaultScenarioPhase.Faulted => 1.0, 
			_ => 0.1, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static void UpdateIntermittentPulse(FaultScenarioInstance instance, FaultEffectDefinition effect, TimeSpan deltaTime)
	{
		instance.IntermittentPhase += deltaTime.TotalSeconds;
		double pulseIntervalSeconds = effect.PulseIntervalSeconds;
		double pulseDurationSeconds = effect.PulseDurationSeconds;
		if (!(pulseIntervalSeconds <= 0.0))
		{
			double num = instance.IntermittentPhase % pulseIntervalSeconds;
			bool isIntermittentPulseActive = instance.IsIntermittentPulseActive;
			instance.IsIntermittentPulseActive = num < pulseDurationSeconds;
			if (instance.IsIntermittentPulseActive && !isIntermittentPulseActive)
			{
				instance.IntermittentEpisodeCount++;
			}
		}
	}
}
