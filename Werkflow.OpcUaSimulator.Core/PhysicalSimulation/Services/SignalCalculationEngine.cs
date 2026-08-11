using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class SignalCalculationEngine : ISignalCalculationEngine
{
	public void Initialize(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random)
	{
		context.PhysicsState.PreviousOutputs.Clear();
		foreach (SignalDependencyDefinition item in profile.Dependencies.Where((SignalDependencyDefinition d) => d.ResponseDelay > TimeSpan.Zero))
		{
			context.PhysicsState.DelayBuffers[item.DependencyId] = new DelayRingBuffer(item.ResponseDelay, TimeSpan.FromMilliseconds(100.0));
		}
	}

	public void CalculateSignals(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random, TimeSpan deltaTime)
	{
		Dictionary<string, HiddenProcessRuntimeState> dictionary = runtime.HiddenProcessStates.ToDictionary<HiddenProcessRuntimeState, string>((HiddenProcessRuntimeState s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, SignalRuntimeState> dictionary2 = runtime.Signals.ToDictionary<SignalRuntimeState, string>((SignalRuntimeState s) => s.SignalId, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<SignalDependencyDefinition>> dictionary3 = profile.Dependencies.Where((SignalDependencyDefinition d) => d.IsEnabled).GroupBy<SignalDependencyDefinition, string>((SignalDependencyDefinition d) => d.TargetSignalId, StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, SignalDependencyDefinition>, string, List<SignalDependencyDefinition>>((IGrouping<string, SignalDependencyDefinition> g) => g.Key, (IGrouping<string, SignalDependencyDefinition> g) => g.ToList(), StringComparer.OrdinalIgnoreCase);
		PerMachinePhysicsState physicsState = context.PhysicsState;
		foreach (SignalDefinition item in profile.Signals.Where((SignalDefinition s) => s.IsEnabled))
		{
			if (!dictionary2.TryGetValue(item.SignalId, out var value))
			{
				continue;
			}
			if (context.Kinematics.ControlsSignal(item.SignalId))
			{
				continue;
			}
			PhysicalSignalDataType dataType = item.DataType;
			if ((uint)dataType > 3u)
			{
				UpdateNonNumericSignal(item, value, context, random);
				continue;
			}
			if (!dictionary3.TryGetValue(item.SignalId, out var value2) || value2.Count == 0)
			{
				UpdateIndependentSignal(item, value, random, deltaTime);
				continue;
			}
			List<double> list = new List<double>();
			List<string> list2 = new List<string>();
			foreach (SignalDependencyDefinition item2 in value2)
			{
				if (dictionary.TryGetValue(item2.SourceStateId, out var value3))
				{
					double num = value3.CurrentValue;
					if (physicsState.DelayBuffers.TryGetValue(item2.DependencyId, out DelayRingBuffer value4))
					{
						value4.Push(num);
						num = value4.GetDelayed(item2.ResponseDelay);
					}
					physicsState.PreviousOutputs.TryGetValue(item2.DependencyId, out var value5);
					double num2 = DependencyEvaluator.Evaluate(item2.DependencyType, num, item2.Weight, item2.Offset, item2.MinimumEffect, item2.MaximumEffect, item2.ThresholdValue, value5);
					physicsState.PreviousOutputs[item2.DependencyId] = num2;
					list.Add(num2);
					list2.Add(item2.SourceStateId);
				}
			}
			if (list.Count == 0)
			{
				UpdateIndependentSignal(item, value, random, deltaTime);
				continue;
			}
			double dependencyTarget = CombineDependencyEffects(item, value2, list, context);
			HiddenProcessRuntimeState value6;
			double processDemand = (dictionary.TryGetValue("ProcessDemand", out value6) ? value6.CurrentValue : 0.55);
			HiddenProcessRuntimeState value7;
			HiddenProcessRuntimeState value8;
			double pressLoad = (dictionary.TryGetValue("PressLoad", out value7) ? value7.CurrentValue : (dictionary.TryGetValue("MechanicalLoad", out value8) ? value8.CurrentValue : 0.5));
			HiddenProcessRuntimeState value9;
			double pumpEfficiency = (dictionary.TryGetValue("PumpEfficiency", out value9) ? value9.CurrentValue : 0.88);
			HiddenProcessRuntimeState value10;
			double valveResponse = (dictionary.TryGetValue("ValveResponse", out value10) ? value10.CurrentValue : 0.9);
			dependencyTarget = PhysicalSignalPhaseCalibration.Apply(item.SignalId, dependencyTarget, item, context.CurrentPhase, processDemand, pressLoad, pumpEfficiency, valveResponse);
			if (item.SignalId.Contains("MotorTemperature", StringComparison.OrdinalIgnoreCase))
			{
				dependencyTarget += ProcessPhaseScheduler.GetTemperaturePhaseOffset(context.CurrentPhase);
			}
			if (item.SignalId.EndsWith(".Load", StringComparison.OrdinalIgnoreCase) && context.CurrentPhase == ProcessPhase.PeakLoad)
			{
				dependencyTarget += (item.NormalMaximum - item.NormalMinimum) * 0.05;
			}
			if (random.ShouldTriggerOutlier(0.002))
			{
				double num3 = item.NormalMaximum - item.NormalMinimum;
				dependencyTarget += random.OutlierMagnitude(num3 * 0.08);
				context.Metrics.HarmlessOutliersTriggered++;
			}
			value.TargetValue = dependencyTarget;
			lock (value.ActiveInfluences)
			{
				value.ActiveInfluences.Clear();
				value.ActiveInfluences.AddRange(list2);
			}
			value.PreviousValue = value.CurrentValue;
			double inertiaSeconds = Math.Max(0.05, item.ResponseInertia);
			double num4 = InertiaHelper.Approach(value.CurrentValue, dependencyTarget, inertiaSeconds, deltaTime.TotalSeconds);
			num4 += random.SignalNoise(item.NoiseAmplitude);
			if (item.SignalId.Contains("MotorCurrent", StringComparison.OrdinalIgnoreCase) || item.SignalId.Contains(".Load", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(item.NoiseAmplitude * 0.65);
			}
			if (item.SignalId.Contains(".Speed", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(item.NoiseAmplitude * 0.55);
			}
			if (item.SignalId.Contains("SupplyPressure", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(0.7);
			}
			if (item.SignalId.Equals("Bending.PressForce", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(item.NoiseAmplitude * 0.7);
			}
			if (item.SignalId.Equals("Process.PowerDemand", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(item.NoiseAmplitude * 0.75);
			}
			if (item.SignalId.Contains("Cooling.PrimaryCircuit.Temperature", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(item.NoiseAmplitude * 0.55);
			}
			if (item.SignalId.Contains("PumpSpeed", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(item.NoiseAmplitude * 0.5);
			}
			if (item.SignalId.Contains("QualityIndex", StringComparison.OrdinalIgnoreCase))
			{
				num4 += random.SignalNoise(0.25);
				num4 = Math.Min(item.NormalMaximum - 0.05, num4);
			}
			num4 = (value.CurrentValue = Math.Clamp(num4, item.HardMinimum, item.HardMaximum));
			value.IsWithinNormalRange = num4 >= item.NormalMinimum && num4 <= item.NormalMaximum;
			value.IsWithinHardLimits = true;
			value.Quality = SignalQuality.Good;
			value.LastUpdatedAt = DateTimeOffset.UtcNow;
			if (Math.Abs(num4 - value.PreviousValue) > 1E-09)
			{
				value.LastChangedAt = value.LastUpdatedAt;
			}
		}
	}

	private static void UpdateIndependentSignal(SignalDefinition def, SignalRuntimeState state, SeededRandomStreams random, TimeSpan deltaTime)
	{
		double num = random.SignalNoise(def.NoiseAmplitude * 0.5);
		double target = (state.TargetValue = Math.Clamp(state.CurrentValue + num, def.NormalMinimum, def.NormalMaximum));
		state.PreviousValue = state.CurrentValue;
		state.CurrentValue = InertiaHelper.Approach(state.CurrentValue, target, Math.Max(0.5, def.ResponseInertia), deltaTime.TotalSeconds);
		state.IsWithinNormalRange = state.CurrentValue >= def.NormalMinimum && state.CurrentValue <= def.NormalMaximum;
		state.LastUpdatedAt = DateTimeOffset.UtcNow;
	}

	private static double CombineDependencyEffects(SignalDefinition signal, IReadOnlyList<SignalDependencyDefinition> deps, IReadOnlyList<double> effects, PhysicalSimulationContext context)
	{
		if (effects.Count == 1)
		{
			return effects[0];
		}
		double num = signal.NominalValue;
		for (int i = 0; i < effects.Count; i++)
		{
			SignalDependencyDefinition dep = deps[i];
			double num2 = effects[i];
			double blendStrength = GetBlendStrength(dep, signal);
			num += (num2 - signal.NominalValue) * blendStrength;
		}
		return num;
	}

	private static double GetBlendStrength(SignalDependencyDefinition dep, SignalDefinition signal)
	{
		double num = Math.Abs(dep.Weight);
		if (dep.SourceStateId.Equals("PressLoad", StringComparison.OrdinalIgnoreCase) && signal.SignalId.Equals("Hydraulic.SupplyPressure", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Clamp(num / 90.0, 0.08, 0.14);
		}
		if (dep.SourceStateId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase) && signal.SignalId.Equals("Hydraulic.SupplyPressure", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Clamp(num / 50.0, 0.65, 0.85);
		}
		if (dep.SourceStateId.Equals("CoolingEfficiency", StringComparison.OrdinalIgnoreCase) && signal.SignalId.Contains("Cooling.PrimaryCircuit", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Clamp(num / 10.0, 0.55, 0.8);
		}
		if (dep.SourceStateId.Equals("Friction", StringComparison.OrdinalIgnoreCase) && signal.SignalId.Contains("MotorCurrent", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Clamp(num / 8.0, 0.45, 0.7);
		}
		DependencyType dependencyType = dep.DependencyType;
		if (1 == 0)
		{
		}
		double result = dependencyType switch
		{
			DependencyType.InverseLinear => Math.Clamp(num / 120.0, 0.15, 0.38), 
			DependencyType.DelayedLinear => Math.Clamp(num / 40.0, 0.35, 0.65), 
			DependencyType.Polynomial => Math.Clamp(num / 14.0, 0.2, 0.42), 
			DependencyType.Saturating => Math.Clamp(num / 35.0, 0.25, 0.48), 
			_ => Math.Clamp(num / 22.0, 0.28, 0.52), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static void UpdateNonNumericSignal(SignalDefinition def, SignalRuntimeState state, PhysicalSimulationContext context, SeededRandomStreams random)
	{
		if (def.TechnicalBehavior == TechnicalSignalBehavior.Counter)
		{
			int num = Math.Max(1, def.CounterStepSize);
			ProcessPhase currentPhase = context.CurrentPhase;
			if ((uint)(currentPhase - 3) <= 1u)
			{
				state.CurrentValue += num;
				context.Job.ProducedQuantity = (int)Math.Min(context.Job.TargetQuantity, state.CurrentValue);
			}
		}
		else if (def.TechnicalBehavior == TechnicalSignalBehavior.BooleanState)
		{
			bool? currentBooleanValue = state.CurrentBooleanValue;
			bool valueOrDefault = currentBooleanValue == true;
			if (!currentBooleanValue.HasValue)
			{
				valueOrDefault = def.InitialValue >= 0.5;
				bool? currentBooleanValue2 = valueOrDefault;
				state.CurrentBooleanValue = currentBooleanValue2;
			}
		}
		else if (def.TechnicalBehavior == TechnicalSignalBehavior.Timestamp)
		{
			state.CurrentDateTimeUtc = DateTime.UtcNow;
		}
		state.LastUpdatedAt = DateTimeOffset.UtcNow;
	}
}
