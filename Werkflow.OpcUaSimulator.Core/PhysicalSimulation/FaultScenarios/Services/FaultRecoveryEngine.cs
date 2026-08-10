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
		instance.RecoveryStableElapsedTime = TimeSpan.Zero;
		instance.RecoveryStartedAtUtc ??= DateTimeOffset.UtcNow;
	}

	public void TickRecovery(FaultScenarioInstance instance, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, TimeSpan deltaTime)
	{
		FaultRecoveryDefinition recovery = instance.Definition.Recovery;
		instance.RecoveryElapsedTime += deltaTime;
		double totalSeconds = recovery.Duration.TotalSeconds;
		if (totalSeconds <= 0.0)
		{
			instance.RecoveryProgress = 1.0;
		}
		else
		{
			double value = instance.RecoveryElapsedTime.TotalSeconds / totalSeconds;
			instance.RecoveryProgress = Math.Clamp(value, 0.0, 1.0);
		}

		double factor = ComputeRecoveryFactor(recovery, instance.RecoveryProgress);
		ApplyRecoveryToEffects(instance, factor);
		ApplyRecoveryToHiddenStates(instance, profile, runtime, factor);
		ApplyRecoveryToSignalOffsets(instance, factor);
		PullHiddenStatesTowardNormal(instance, profile, runtime, instance.RecoveryProgress);
		instance.LastScenarioDeltaTime = deltaTime;
	}

	public bool IsRecoveryComplete(FaultScenarioInstance instance, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime)
	{
		FaultRecoveryDefinition recovery = instance.Definition.Recovery;
		if (instance.RecoveryProgress < 1.0)
		{
			return false;
		}

		if (recovery.HasSafeRecoveryThreshold)
		{
			if (instance.RecoveryStableElapsedTime < recovery.MinimumStableDuration)
			{
				return false;
			}

			double value = ReadSafeRecoverySource(recovery, profile, runtime);
			return EvaluateSafeRecoveryCondition(recovery, profile, runtime, value);
		}

		return instance.RecoveryElapsedTime >= recovery.MinimumStableDuration;
	}

	public void ApplyRecoverySignalOverrides(FaultScenarioInstance instance, PhysicalMachineRuntime runtime)
	{
		if (instance.LifecycleState != FaultScenarioLifecycleState.Recovering)
		{
			return;
		}

		FaultRecoveryDefinition recovery = instance.Definition.Recovery;
		double factor = ComputeRecoveryFactor(recovery, instance.RecoveryProgress);
		TimeSpan deltaTime = instance.LastScenarioDeltaTime > TimeSpan.Zero
			? instance.LastScenarioDeltaTime
			: TimeSpan.FromMilliseconds(200);
		ApplySafeRecoverySignalAdjustment(recovery, runtime, factor, deltaTime);
	}

	private static void PullHiddenStatesTowardNormal(
		FaultScenarioInstance instance,
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime,
		double progress)
	{
		if (progress < 0.4)
		{
			return;
		}

		double pull = Math.Clamp((progress - 0.35) * 0.55, 0.12, 0.55);
		foreach (HiddenProcessRuntimeState state in runtime.HiddenProcessStates)
		{
			HiddenProcessStateDefinition? definition = profile.HiddenProcessStates.FirstOrDefault(d =>
				d.StateId.Equals(state.StateId, StringComparison.OrdinalIgnoreCase));
			if (definition == null)
			{
				continue;
			}

			double normal = (definition.NormalMinimum + definition.NormalMaximum) * 0.5;
			state.TargetValue += (normal - state.TargetValue) * pull;
			state.CurrentValue += (normal - state.CurrentValue) * pull * 0.6;
			state.TargetValue = Math.Clamp(state.TargetValue, definition.HardMinimum, definition.HardMaximum);
			state.CurrentValue = Math.Clamp(state.CurrentValue, definition.HardMinimum, definition.HardMaximum);
		}
	}

	private static void ApplySafeRecoverySignalAdjustment(
		FaultRecoveryDefinition recovery,
		PhysicalMachineRuntime runtime,
		double factor,
		TimeSpan deltaTime)
	{
		if (!recovery.HasSafeRecoveryThreshold || recovery.SafeRecoverySourceType != FaultThresholdSourceType.Signal)
		{
			return;
		}

		SignalRuntimeState? signal = runtime.Signals.FirstOrDefault(s =>
			s.SignalId.Equals(recovery.SafeRecoverySourceId, StringComparison.OrdinalIgnoreCase));
		if (signal == null || !recovery.SafeRecoveryThreshold.HasValue || !recovery.SafeRecoveryComparison.HasValue)
		{
			return;
		}

		double progress = 1.0 - factor;
		double step = deltaTime.TotalSeconds * recovery.Rate * 10.0 * progress;
		double threshold = recovery.SafeRecoveryThreshold.Value;
		double target = threshold - recovery.SafeRecoveryTolerance * 0.5;

		switch (recovery.SafeRecoveryComparison.Value)
		{
		case FaultThresholdComparison.LessThan:
		case FaultThresholdComparison.LessThanOrEqual:
			if (signal.CurrentValue > target)
			{
				signal.CurrentValue = Math.Max(target, signal.CurrentValue - step);
			}
			break;
		case FaultThresholdComparison.GreaterThan:
		case FaultThresholdComparison.GreaterThanOrEqual:
			double raiseTarget = threshold + recovery.SafeRecoveryTolerance * 0.25;
			if (signal.CurrentValue < raiseTarget)
			{
				signal.CurrentValue = Math.Min(raiseTarget, signal.CurrentValue + step);
			}
			break;
		}
	}

	public void UpdateRecoveryStableTimer(
		FaultScenarioInstance instance,
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime,
		TimeSpan deltaTime)
	{
		FaultRecoveryDefinition recovery = instance.Definition.Recovery;
		if (!recovery.HasSafeRecoveryThreshold)
		{
			return;
		}

		double value = ReadSafeRecoverySource(recovery, profile, runtime);
		if (EvaluateSafeRecoveryCondition(recovery, profile, runtime, value))
		{
			instance.RecoveryStableElapsedTime += deltaTime;
		}
		else
		{
			instance.RecoveryStableElapsedTime = TimeSpan.Zero;
		}
	}

	private static double ReadSafeRecoverySource(
		FaultRecoveryDefinition recovery,
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime)
	{
		if (string.IsNullOrWhiteSpace(recovery.SafeRecoverySourceId))
		{
			return 0.0;
		}

		if (recovery.SafeRecoverySourceType == FaultThresholdSourceType.HiddenState)
		{
			return runtime.HiddenProcessStates.FirstOrDefault(s =>
				s.StateId.Equals(recovery.SafeRecoverySourceId, StringComparison.OrdinalIgnoreCase))?.CurrentValue ?? 0.0;
		}

		return runtime.Signals.FirstOrDefault(s =>
			s.SignalId.Equals(recovery.SafeRecoverySourceId, StringComparison.OrdinalIgnoreCase))?.CurrentValue ?? 0.0;
	}

	private static bool EvaluateSafeRecoveryCondition(
		FaultRecoveryDefinition recovery,
		PhysicalMachineProfile profile,
		PhysicalMachineRuntime runtime,
		double signalValue)
	{
		return EvaluateSafeComparison(recovery, signalValue);
	}

	private static bool EvaluateSafeComparison(FaultRecoveryDefinition recovery, double value)
	{
		if (!recovery.SafeRecoveryThreshold.HasValue || !recovery.SafeRecoveryComparison.HasValue)
		{
			return true;
		}

		double threshold = recovery.SafeRecoveryThreshold.Value;
		return recovery.SafeRecoveryComparison.Value switch
		{
			FaultThresholdComparison.GreaterThan => value > threshold,
			FaultThresholdComparison.GreaterThanOrEqual => value >= threshold,
			FaultThresholdComparison.LessThan => value < threshold,
			FaultThresholdComparison.LessThanOrEqual => value <= threshold,
			FaultThresholdComparison.OutsideRange => value < threshold || value > threshold,
			FaultThresholdComparison.InsideRange => value >= threshold && value <= threshold,
			_ => false
		};
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

	private static void ApplyRecoveryToSignalOffsets(FaultScenarioInstance instance, double factor)
	{
		foreach (string item in instance.SignalOffsets.Keys.ToList())
		{
			instance.SignalOffsets[item] *= factor;
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
