using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public sealed class FaultScenarioEngine : IFaultScenarioEngine
{
	private readonly IFaultEffectCalculator _effectCalculator;

	private readonly IFaultRecoveryEngine _recoveryEngine;

	public FaultScenarioEngine(IFaultEffectCalculator effectCalculator, IFaultRecoveryEngine recoveryEngine)
	{
		_effectCalculator = effectCalculator;
		_recoveryEngine = recoveryEngine;
	}

	public void Tick(PhysicalMachineSession session, TimeSpan deltaTime, IFaultScenarioSimulationBridge? bridge)
	{
		FaultScenarioMachineContext faultScenarios = session.Simulation.FaultScenarios;
		if (faultScenarios.ActiveInstances.Count == 0)
		{
			return;
		}
		TimeSpan timeSpan = TimeSpan.FromTicks((long)((double)deltaTime.Ticks * session.Simulation.TimeFactor));
		foreach (FaultScenarioInstance item in faultScenarios.ActiveInstances.Values.ToList())
		{
			if (item.LifecycleState == FaultScenarioLifecycleState.Paused)
			{
				continue;
			}
			FaultScenarioLifecycleState lifecycleState = item.LifecycleState;
			if (((uint)(lifecycleState - 1) <= 1u || lifecycleState == FaultScenarioLifecycleState.Faulted) ? true : false)
			{
				TimeSpan timeSpan2 = TimeSpan.FromTicks((long)((double)timeSpan.Ticks * item.TimeFactor));
				item.ScenarioElapsedTime += timeSpan2;
				AdvancePhase(item);
				ApplyEffects(item, session, timeSpan2);
				EvaluateThresholds(item, session, bridge);
				CheckScenarioEnd(item);
			}
			if (item.LifecycleState == FaultScenarioLifecycleState.Recovering)
			{
				TimeSpan deltaTime2 = TimeSpan.FromTicks((long)((double)timeSpan.Ticks * item.TimeFactor));
				_recoveryEngine.TickRecovery(item, session.Profile, session.Runtime, session.Simulation, deltaTime2);
				if (_recoveryEngine.IsRecoveryComplete(item))
				{
					CompleteInstance(item, session, bridge, clearFault: true);
				}
			}
		}
	}

	public void ApplySignalOverrides(PhysicalMachineSession session)
	{
		Dictionary<string, SignalRuntimeState> dictionary = session.Runtime.Signals.ToDictionary<SignalRuntimeState, string>((SignalRuntimeState s) => s.SignalId, StringComparer.OrdinalIgnoreCase);
		foreach (FaultScenarioInstance value4 in session.Simulation.FaultScenarios.ActiveInstances.Values)
		{
			foreach (FaultEffectDefinition item in value4.Definition.Effects.Where((FaultEffectDefinition e) => e.EffectType == FaultEffectType.SignalFreeze && e.IsEnabled))
			{
				if (!value4.FrozenSignalValues.TryGetValue(item.TargetId, out var value) && dictionary.TryGetValue(item.TargetId, out var value2))
				{
					value4.FrozenSignalValues[item.TargetId] = value2.CurrentValue;
					value = value2.CurrentValue;
				}
				if (dictionary.TryGetValue(item.TargetId, out var value3))
				{
					value3.CurrentValue = value;
				}
			}
		}
	}

	private void ApplyEffects(FaultScenarioInstance instance, PhysicalMachineSession session, TimeSpan deltaTime)
	{
		if (instance.LifecycleState == FaultScenarioLifecycleState.Starting)
		{
			instance.LifecycleState = FaultScenarioLifecycleState.Running;
		}
		PhysicalMachineProfile profile = session.Profile;
		PhysicalMachineRuntime runtime = session.Runtime;
		PhysicalSimulationContext simulation = session.Simulation;
		Dictionary<string, HiddenProcessRuntimeState> dictionary = runtime.HiddenProcessStates.ToDictionary<HiddenProcessRuntimeState, string>((HiddenProcessRuntimeState s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HiddenProcessStateDefinition> dictionary2 = profile.HiddenProcessStates.ToDictionary<HiddenProcessStateDefinition, string>((HiddenProcessStateDefinition s) => s.StateId, StringComparer.OrdinalIgnoreCase);
		foreach (FaultEffectDefinition item in instance.Definition.Effects.Where((FaultEffectDefinition e) => e.IsEnabled))
		{
			if (item.EffectType != FaultEffectType.ConnectionDrop)
			{
				double num = _effectCalculator.ComputeEffectContribution(instance, item, profile, runtime, simulation, deltaTime);
				if (item.TargetType == FaultEffectTargetType.HiddenState && dictionary.TryGetValue(item.TargetId, out var value) && dictionary2.TryGetValue(value.StateId, out var value2))
				{
					string targetId = item.TargetId;
					double valueOrDefault = instance.HiddenStateOffsets.GetValueOrDefault(targetId);
					double min = ((item.Direction == FaultEffectDirection.Decrease) ? (0.0 - item.MaximumEffect) : item.MinimumEffect);
					double max = ((item.Direction == FaultEffectDirection.Decrease) ? (0.0 - item.MinimumEffect) : item.MaximumEffect);
					double num2 = Math.Clamp(valueOrDefault + num, min, max);
					instance.HiddenStateOffsets[targetId] = num2;
					value.TargetValue = Math.Clamp(value.TargetValue + num2, value2.HardMinimum, value2.HardMaximum);
					value.CurrentValue = Math.Clamp(value.CurrentValue + num2 * 0.35, value2.HardMinimum, value2.HardMaximum);
				}
			}
		}
	}

	private static void AdvancePhase(FaultScenarioInstance instance)
	{
		List<FaultScenarioPhaseTiming> phases = instance.Definition.Phases;
		if (phases.Count == 0)
		{
			return;
		}
		TimeSpan defaultDuration = instance.Definition.DefaultDuration;
		TimeSpan scenarioElapsedTime = instance.ScenarioElapsedTime;
		TimeSpan zero = TimeSpan.Zero;
		foreach (FaultScenarioPhaseTiming item in phases)
		{
			TimeSpan timeSpan = ((item.Duration > TimeSpan.Zero) ? item.Duration : TimeSpan.FromTicks((long)((double)defaultDuration.Ticks * item.DurationFraction)));
			zero += timeSpan;
			if (scenarioElapsedTime < zero)
			{
				if (instance.CurrentPhase != item.Phase)
				{
					instance.CurrentPhase = item.Phase;
					instance.NextPhaseChangeAt = instance.StartedAt + zero;
				}
				return;
			}
		}
		if (instance.CurrentPhase < FaultScenarioPhase.Faulted)
		{
			instance.CurrentPhase = FaultScenarioPhase.Faulted;
		}
	}

	private void EvaluateThresholds(FaultScenarioInstance instance, PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge)
	{
		if (!instance.AutoThresholdFaultEnabled || instance.ThresholdFaultTriggered || instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun)
		{
			return;
		}
		foreach (FaultThresholdRule item in instance.Definition.ThresholdRules.Where((FaultThresholdRule r) => r.IsEnabled))
		{
			if (instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun && item.DisabledInControlRun)
			{
				continue;
			}
			double value = ReadThresholdSource(item, session);
			if (!EvaluateComparison(item, value))
			{
				if (instance.ActiveThresholdRuleId == item.RuleId)
				{
					instance.ThresholdConditionStartedAt = null;
					instance.ActiveThresholdRuleId = null;
				}
				continue;
			}
			if (instance.ActiveThresholdRuleId != item.RuleId)
			{
				instance.ActiveThresholdRuleId = item.RuleId;
				instance.ThresholdConditionStartedAt = DateTimeOffset.UtcNow;
			}
			if (!instance.ThresholdConditionStartedAt.HasValue || !(DateTimeOffset.UtcNow - instance.ThresholdConditionStartedAt.Value >= item.MinimumDuration))
			{
				continue;
			}
			TriggerThresholdFault(instance, item, session, bridge);
			break;
		}
	}

	private static double ReadThresholdSource(FaultThresholdRule rule, PhysicalMachineSession session)
	{
		switch (rule.SourceType)
		{
		case FaultThresholdSourceType.HiddenState:
			return session.Runtime.HiddenProcessStates.FirstOrDefault((HiddenProcessRuntimeState s) => s.StateId.Equals(rule.SourceId, StringComparison.OrdinalIgnoreCase))?.CurrentValue ?? 0.0;
		case FaultThresholdSourceType.Signal:
			return session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId.Equals(rule.SourceId, StringComparison.OrdinalIgnoreCase))?.CurrentValue ?? 0.0;
		case FaultThresholdSourceType.ScenarioPhase:
		{
			FaultScenarioInstance faultScenarioInstance = session.Simulation.FaultScenarios.ActiveInstances.Values.FirstOrDefault((FaultScenarioInstance i) => i.ScenarioId.Equals(rule.SourceId, StringComparison.OrdinalIgnoreCase));
			return (faultScenarioInstance != null) ? ((double)faultScenarioInstance.CurrentPhase) : 0.0;
		}
		default:
			return 0.0;
		}
	}

	private static bool EvaluateComparison(FaultThresholdRule rule, double value)
	{
		FaultThresholdComparison comparison = rule.Comparison;
		if (1 == 0)
		{
		}
		bool result = comparison switch
		{
			FaultThresholdComparison.GreaterThan => value > rule.ThresholdValue, 
			FaultThresholdComparison.GreaterThanOrEqual => value >= rule.ThresholdValue, 
			FaultThresholdComparison.LessThan => value < rule.ThresholdValue, 
			FaultThresholdComparison.LessThanOrEqual => value <= rule.ThresholdValue, 
			FaultThresholdComparison.OutsideRange => value < rule.ThresholdValue || value > rule.ThresholdValueSecondary.GetValueOrDefault(rule.ThresholdValue), 
			FaultThresholdComparison.InsideRange => value >= rule.ThresholdValue && value <= rule.ThresholdValueSecondary.GetValueOrDefault(rule.ThresholdValue), 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void TriggerThresholdFault(FaultScenarioInstance instance, FaultThresholdRule rule, PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge)
	{
		instance.ThresholdFaultTriggered = true;
		instance.ActiveFaultCode = rule.FaultCode;
		instance.CurrentPhase = FaultScenarioPhase.Faulted;
		instance.LifecycleState = FaultScenarioLifecycleState.Faulted;
		session.Simulation.FaultScenarios.ActiveFaultCodes.Add(rule.FaultCode);
		if (bridge != null && rule.SetErrorActive)
		{
			bridge.SetMachineFault(instance.MachineId, rule.FaultCode, rule.FaultMessage, rule.StopProduction, rule.KeepServerOnline, instance.Definition.Priority);
		}
	}

	private static void CheckScenarioEnd(FaultScenarioInstance instance)
	{
		if (instance.AutoScenarioEndEnabled && instance.ScenarioElapsedTime >= instance.Definition.DefaultDuration && instance.CurrentPhase >= FaultScenarioPhase.Faulted && instance.LifecycleState != FaultScenarioLifecycleState.Recovering)
		{
			instance.LifecycleState = FaultScenarioLifecycleState.Recovering;
			instance.CurrentPhase = FaultScenarioPhase.Recovering;
		}
	}

	private void CompleteInstance(FaultScenarioInstance instance, PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge, bool clearFault)
	{
		instance.LifecycleState = FaultScenarioLifecycleState.Completed;
		instance.CurrentPhase = FaultScenarioPhase.Completed;
		if (clearFault && instance.ActiveFaultCode != null && bridge != null)
		{
			session.Simulation.FaultScenarios.ActiveFaultCodes.Remove(instance.ActiveFaultCode);
			bridge.ClearMachineFault(instance.MachineId, instance.ActiveFaultCode);
			if (instance.Definition.Recovery.ResumeProductionAfterRecovery)
			{
				bridge.ResumeProduction(instance.MachineId);
			}
		}
		session.Simulation.FaultScenarios.ActiveInstances.Remove(instance.InstanceId);
		session.Simulation.FaultScenarios.ScenarioIdToInstance.Remove(instance.ScenarioId);
	}
}
