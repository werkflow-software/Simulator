using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public sealed class FaultRecoveryEngine : IFaultRecoveryEngine
{
	public void BeginRecovery(FaultScenarioInstance instance)
	{
		instance.LifecycleState = FaultScenarioLifecycleState.Recovering;
		instance.CurrentPhase = FaultScenarioPhase.Recovering;
		instance.RecoveryElapsedTime = TimeSpan.Zero;
		instance.RecoveryProgress = 0.0;
	}

	public void TickRecovery(FaultScenarioInstance instance, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, TimeSpan deltaTime)
	{
		FaultRecoveryDefinition recovery = instance.Definition.Recovery;
		instance.RecoveryElapsedTime += deltaTime;
		double totalSeconds = recovery.Duration.TotalSeconds;
		if (totalSeconds <= 0.0)
		{
			instance.RecoveryProgress = 1.0;
			return;
		}
		double value = instance.RecoveryElapsedTime.TotalSeconds / totalSeconds;
		instance.RecoveryProgress = Math.Clamp(value, 0.0, 1.0);
		double factor = ComputeRecoveryFactor(recovery, instance.RecoveryProgress);
		ApplyRecoveryToEffects(instance, factor);
		ApplyRecoveryToHiddenStates(instance, profile, runtime, factor);
	}

	public bool IsRecoveryComplete(FaultScenarioInstance instance)
	{
		FaultRecoveryDefinition recovery = instance.Definition.Recovery;
		return instance.RecoveryProgress >= 1.0 && instance.RecoveryElapsedTime >= recovery.MinimumStableDuration;
	}

	private static double ComputeRecoveryFactor(FaultRecoveryDefinition recovery, double progress)
	{
		FaultRecoveryType recoveryType = recovery.RecoveryType;
		if (1 == 0)
		{
		}
		double result = recoveryType switch
		{
			FaultRecoveryType.Linear => 1.0 - progress, 
			FaultRecoveryType.Exponential => Math.Pow(1.0 - progress, 2.0), 
			FaultRecoveryType.RateLimited => Math.Max(0.0, 1.0 - progress * recovery.Rate * 2.0), 
			FaultRecoveryType.ThermalCooldown => Math.Pow(1.0 - progress, 3.0), 
			FaultRecoveryType.PressureRecovery => Math.Pow(1.0 - progress, 1.5), 
			FaultRecoveryType.OscillationDecay => (1.0 - progress) * (0.5 + 0.5 * Math.Cos(progress * Math.PI)), 
			FaultRecoveryType.ManualHold => 1.0, 
			_ => 1.0 - progress, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static void ApplyRecoveryToEffects(FaultScenarioInstance instance, double factor)
	{
		foreach (string item in instance.EffectAccumulators.Keys.ToList())
		{
			instance.EffectAccumulators[item] *= factor;
		}
	}

	private static void ApplyRecoveryToHiddenStates(FaultScenarioInstance instance, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, double factor)
	{
		Dictionary<string, HiddenProcessStateDefinition> dictionary = profile.HiddenProcessStates.ToDictionary<HiddenProcessStateDefinition, string>((HiddenProcessStateDefinition s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		foreach (string item in instance.HiddenStateOffsets.Keys.ToList())
		{
			instance.HiddenStateOffsets[item] *= factor;
		}
		foreach (FaultEffectDefinition effect in instance.Definition.Effects.Where((FaultEffectDefinition e) => e.TargetType == FaultEffectTargetType.HiddenState))
		{
			if (instance.HiddenStateOffsets.TryGetValue(effect.TargetId, out var value) && value != 0.0)
			{
				HiddenProcessRuntimeState hiddenProcessRuntimeState = runtime.HiddenProcessStates.FirstOrDefault((HiddenProcessRuntimeState s) => s.StateId.Equals(effect.TargetId, StringComparison.OrdinalIgnoreCase));
				if (hiddenProcessRuntimeState != null && dictionary.TryGetValue(hiddenProcessRuntimeState.StateId, out var value2))
				{
					double num = value * (1.0 - factor);
					hiddenProcessRuntimeState.TargetValue = Math.Clamp(hiddenProcessRuntimeState.TargetValue - num, value2.HardMinimum, value2.HardMaximum);
					hiddenProcessRuntimeState.CurrentValue = Math.Clamp(hiddenProcessRuntimeState.CurrentValue - num * 0.5, value2.HardMinimum, value2.HardMaximum);
				}
			}
		}
	}
}
