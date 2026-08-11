using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class HiddenProcessStateEngine : IHiddenProcessStateEngine
{
	public void Initialize(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random)
	{
		context.PhysicsState.Reset();
		context.PhysicsState.Random = random;
		context.ResetPhaseState();
		context.SimulationTime = TimeSpan.Zero;
		PhysicalJobCoordinator.Initialize(context);
		foreach (HiddenStateDependencyDefinition item in profile.HiddenStateDependencies.Where((HiddenStateDependencyDefinition d) => d.ResponseDelay > TimeSpan.Zero))
		{
			context.PhysicsState.DelayBuffers[item.DependencyId] = new DelayRingBuffer(item.ResponseDelay, TimeSpan.FromMilliseconds(100.0));
		}
		foreach (SignalDependencyDefinition item2 in profile.Dependencies.Where((SignalDependencyDefinition d) => d.ResponseDelay > TimeSpan.Zero))
		{
			context.PhysicsState.DelayBuffers[item2.DependencyId] = new DelayRingBuffer(item2.ResponseDelay, TimeSpan.FromMilliseconds(100.0));
		}
	}

	public void Tick(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, SeededRandomStreams random, TimeSpan deltaTime)
	{
		context.SimulationTime += deltaTime;
		context.PhaseElapsedSimulationTime += deltaTime;
		ProcessPhaseScheduler.TryAdvance(context, random, out ProcessPhaseTransition _);
		PhysicalJobCoordinator.TickProductionCounters(context);
		double phaseDemand = ProcessPhaseScheduler.GetPhaseDemand(context.CurrentPhase);
		Dictionary<string, HiddenProcessRuntimeState> dictionary = runtime.HiddenProcessStates.ToDictionary<HiddenProcessRuntimeState, string>((HiddenProcessRuntimeState s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HiddenProcessStateDefinition> dictionary2 = profile.HiddenProcessStates.ToDictionary<HiddenProcessStateDefinition, string>((HiddenProcessStateDefinition s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		foreach (HiddenProcessRuntimeState hiddenProcessState in runtime.HiddenProcessStates)
		{
			if (dictionary2.TryGetValue(hiddenProcessState.StateId, out var value))
			{
				double value2 = value.NominalValue;
				if (hiddenProcessState.StateId.Equals("ProcessDemand", StringComparison.OrdinalIgnoreCase))
				{
					value2 = Lerp(value.NormalMinimum, value.NormalMaximum, phaseDemand * context.Job.ProcessLoadFactor);
				}
				else if (hiddenProcessState.StateId.Equals("MechanicalLoad", StringComparison.OrdinalIgnoreCase))
				{
					HiddenProcessRuntimeState value3;
					double num = (dictionary.TryGetValue("ProcessDemand", out value3) ? value3.CurrentValue : phaseDemand);
					value2 = Lerp(value.NormalMinimum, value.NormalMaximum, num * 0.85 + phaseDemand * 0.15);
				}
				else if (hiddenProcessState.StateId.Equals("PressLoad", StringComparison.OrdinalIgnoreCase))
				{
					HiddenProcessRuntimeState value4;
					double num2 = (dictionary.TryGetValue("ValveResponse", out value4) ? value4.CurrentValue : 0.9);
					HiddenProcessRuntimeState value5;
					double num3 = (dictionary.TryGetValue("ProcessDemand", out value5) ? value5.CurrentValue : phaseDemand);
					double t = num3 * 0.5 + num2 * 0.28 + phaseDemand * 0.22;
					value2 = Lerp(value.NormalMinimum, value.NormalMaximum, t);
				}
				else if (hiddenProcessState.StateId.Equals("ThermalLoad", StringComparison.OrdinalIgnoreCase) || hiddenProcessState.StateId.Equals("StructuralThermalLoad", StringComparison.OrdinalIgnoreCase))
				{
					HiddenProcessRuntimeState value6;
					HiddenProcessRuntimeState value7;
					double num4 = (dictionary.TryGetValue("MechanicalLoad", out value6) ? value6.CurrentValue : (dictionary.TryGetValue("PressLoad", out value7) ? value7.CurrentValue : phaseDemand));
					value2 = Lerp(value.NormalMinimum, value.NormalMaximum, num4 * 0.7 + phaseDemand * 0.3);
				}
				else if (context.CurrentPhase == ProcessPhase.Cooling || context.CurrentPhase == ProcessPhase.Idle)
				{
					value2 = Math.Max(value.NormalMinimum, hiddenProcessState.CurrentValue - value.RecoveryRate * deltaTime.TotalSeconds);
				}
				hiddenProcessState.TargetValue = Math.Clamp(value2, value.HardMinimum, value.HardMaximum);
			}
		}
		ApplyHiddenDependencies(profile, runtime, context, deltaTime);
		TimeSpan simulationTime = context.SimulationTime;
		foreach (HiddenProcessRuntimeState hiddenProcessState2 in runtime.HiddenProcessStates)
		{
			if (dictionary2.TryGetValue(hiddenProcessState2.StateId, out var value8) && (!context.PhysicsState.NextHiddenUpdate.TryGetValue(hiddenProcessState2.StateId, out var value9) || !(simulationTime < value9)))
			{
				hiddenProcessState2.PreviousValue = hiddenProcessState2.CurrentValue;
				double inertiaSeconds = Math.Max(0.05, value8.ResponseInertia);
				double num5 = InertiaHelper.Approach(hiddenProcessState2.CurrentValue, hiddenProcessState2.TargetValue, inertiaSeconds, deltaTime.TotalSeconds);
				num5 += value8.NaturalDrift * deltaTime.TotalSeconds;
				num5 += random.ProcessNoise(value8.NoiseAmplitude);
				num5 = Math.Clamp(num5, value8.HardMinimum, value8.HardMaximum);
				hiddenProcessState2.CurrentValue = num5;
				hiddenProcessState2.LastUpdatedAt = DateTimeOffset.UtcNow;
				context.PhysicsState.NextHiddenUpdate[hiddenProcessState2.StateId] = simulationTime.Add((value8.UpdateInterval > TimeSpan.Zero) ? value8.UpdateInterval : TimeSpan.FromSeconds(1.0));
			}
		}
	}

	private static void ApplyHiddenDependencies(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, PhysicalSimulationContext context, TimeSpan deltaTime)
	{
		Dictionary<string, HiddenProcessRuntimeState> dictionary = runtime.HiddenProcessStates.ToDictionary<HiddenProcessRuntimeState, string>((HiddenProcessRuntimeState s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HiddenProcessStateDefinition> dictionary2 = profile.HiddenProcessStates.ToDictionary<HiddenProcessStateDefinition, string>((HiddenProcessStateDefinition s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		PerMachinePhysicsState physicsState = context.PhysicsState;
		foreach (HiddenStateDependencyDefinition item in profile.HiddenStateDependencies.Where((HiddenStateDependencyDefinition d) => d.IsEnabled))
		{
			if (dictionary.TryGetValue(item.SourceStateId, out var value) && dictionary.TryGetValue(item.TargetStateId, out var value2) && dictionary2.TryGetValue(item.TargetStateId, out var value3))
			{
				double num = value.CurrentValue;
				if (physicsState.DelayBuffers.TryGetValue(item.DependencyId, out DelayRingBuffer value4))
				{
					value4.Push(num);
					num = value4.GetDelayed(item.ResponseDelay);
				}
				physicsState.PreviousOutputs.TryGetValue(item.DependencyId, out var value5);
				double num2 = DependencyEvaluator.Evaluate(item.DependencyType, num, item.Weight, item.Offset, item.MinimumEffect, item.MaximumEffect, item.ThresholdValue, value5);
				physicsState.PreviousOutputs[item.DependencyId] = num2;
				double num3 = num2 * deltaTime.TotalSeconds;
				value2.TargetValue = Math.Clamp(value2.TargetValue + num3, value3.HardMinimum, value3.HardMaximum);
			}
		}
	}

	private static double Lerp(double min, double max, double t)
	{
		return min + (max - min) * Math.Clamp(t, 0.0, 1.0);
	}
}
