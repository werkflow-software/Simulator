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

	private readonly IFaultScenarioEventSink? _eventSink;

	public FaultScenarioEngine(
		IFaultEffectCalculator effectCalculator,
		IFaultRecoveryEngine recoveryEngine,
		IFaultScenarioEventSink? eventSink = null)
	{
		_effectCalculator = effectCalculator;
		_recoveryEngine = recoveryEngine;
		_eventSink = eventSink;
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
				item.LastScenarioDeltaTime = timeSpan2;
				AdvancePhase(item, session.MachineId);
				ApplyEffects(item, session, timeSpan2);
				CheckDetectability(item, session);
				CheckScenarioEnd(item, session.MachineId);
			}
			if (item.LifecycleState == FaultScenarioLifecycleState.Recovering)
			{
				TimeSpan deltaTime2 = TimeSpan.FromTicks((long)((double)timeSpan.Ticks * item.TimeFactor));
				_recoveryEngine.TickRecovery(item, session.Profile, session.Runtime, session.Simulation, deltaTime2);
			}
		}
	}

	public void ApplySignalOverrides(PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge)
	{
		Dictionary<string, SignalRuntimeState> dictionary = session.Runtime.Signals.ToDictionary<SignalRuntimeState, string>((SignalRuntimeState s) => s.SignalId, StringComparer.OrdinalIgnoreCase);
		foreach (FaultScenarioInstance value4 in session.Simulation.FaultScenarios.ActiveInstances.Values)
		{
			foreach (KeyValuePair<string, double> offset in value4.SignalOffsets)
			{
				if (dictionary.TryGetValue(offset.Key, out var signalState))
				{
					signalState.CurrentValue += offset.Value;
				}
			}

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

			if (value4.LifecycleState == FaultScenarioLifecycleState.Recovering)
			{
				_recoveryEngine.ApplyRecoverySignalOverrides(value4, session.Runtime);
				TimeSpan deltaTime = value4.LastScenarioDeltaTime > TimeSpan.Zero
					? value4.LastScenarioDeltaTime
					: TimeSpan.FromMilliseconds(200);
				_recoveryEngine.UpdateRecoveryStableTimer(value4, session.Profile, session.Runtime, deltaTime);
				if (_recoveryEngine.IsRecoveryComplete(value4, session.Profile, session.Runtime))
				{
					_recoveryEngine.FinalizeRecoveryState(value4, session.Profile, session.Runtime);
					CompleteInstance(value4, session, bridge, clearFault: true);
				}
			}
		}
	}

	public void EvaluateThresholdsAfterSignals(PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge)
	{
		FaultScenarioMachineContext faultScenarios = session.Simulation.FaultScenarios;
		if (faultScenarios.ActiveInstances.Count == 0)
		{
			return;
		}

		foreach (FaultScenarioInstance item in faultScenarios.ActiveInstances.Values.ToList())
		{
			if (item.LifecycleState == FaultScenarioLifecycleState.Paused)
			{
				continue;
			}

			FaultScenarioLifecycleState lifecycleState = item.LifecycleState;
			if (((uint)(lifecycleState - 1) <= 1u || lifecycleState == FaultScenarioLifecycleState.Faulted) ? true : false)
			{
				EvaluateThresholds(item, session, bridge);
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
				else if (item.TargetType == FaultEffectTargetType.SignalQuality
					&& item.EffectType != FaultEffectType.SignalFreeze
					&& item.EffectType != FaultEffectType.ConnectionDrop)
				{
					string targetId = item.TargetId;
					double valueOrDefault = instance.SignalOffsets.GetValueOrDefault(targetId);
					double min = ((item.Direction == FaultEffectDirection.Decrease) ? (0.0 - item.MaximumEffect) : item.MinimumEffect);
					double max = ((item.Direction == FaultEffectDirection.Decrease) ? (0.0 - item.MinimumEffect) : item.MaximumEffect);
					instance.SignalOffsets[targetId] = Math.Clamp(valueOrDefault + num, min, max);
				}
			}
		}
	}

	private void AdvancePhase(FaultScenarioInstance instance, Guid machineId)
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
			if (instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun
				&& item.Phase >= FaultScenarioPhase.Faulted)
			{
				continue;
			}

			TimeSpan timeSpan = ((item.Duration > TimeSpan.Zero) ? item.Duration : TimeSpan.FromTicks((long)((double)defaultDuration.Ticks * item.DurationFraction)));
			zero += timeSpan;
			if (scenarioElapsedTime < zero)
			{
				if (instance.CurrentPhase != item.Phase)
				{
					instance.CurrentPhase = item.Phase;
					instance.NextPhaseChangeAt = instance.StartedAt + zero;
					PublishEvent(FaultScenarioEventType.ScenarioPhaseChanged, machineId, instance, item.Phase);
				}
				return;
			}
		}

		if (instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun)
		{
			if (instance.CurrentPhase < FaultScenarioPhase.Degraded)
			{
				instance.CurrentPhase = FaultScenarioPhase.Degraded;
				PublishEvent(FaultScenarioEventType.ScenarioPhaseChanged, machineId, instance, FaultScenarioPhase.Degraded);
			}
			else if (instance.LifecycleState != FaultScenarioLifecycleState.Recovering
				&& instance.LifecycleState != FaultScenarioLifecycleState.Completed
				&& instance.CurrentPhase < FaultScenarioPhase.Recovering)
			{
				instance.CurrentPhase = FaultScenarioPhase.Recovering;
				instance.LifecycleState = FaultScenarioLifecycleState.Recovering;
				PublishEvent(FaultScenarioEventType.ScenarioPhaseChanged, machineId, instance, FaultScenarioPhase.Recovering);
			}
			return;
		}

		if (instance.CurrentPhase < FaultScenarioPhase.Faulted)
		{
			instance.CurrentPhase = FaultScenarioPhase.Faulted;
			PublishEvent(FaultScenarioEventType.ScenarioPhaseChanged, machineId, instance, FaultScenarioPhase.Faulted);
		}
	}

	private void CheckDetectability(FaultScenarioInstance instance, PhysicalMachineSession session)
	{
		if (instance.DetectabilityEmitted)
		{
			return;
		}

		FaultDetectabilityDefinition? detectability = instance.Definition.Detectability;
		if (detectability == null)
		{
			return;
		}

		double progress = instance.Definition.DefaultDuration.TotalSeconds > 0
			? instance.ScenarioElapsedTime.TotalSeconds / instance.Definition.DefaultDuration.TotalSeconds
			: 0.0;
		double maxEffect = instance.HiddenStateOffsets.Count == 0
			? 0.0
			: instance.HiddenStateOffsets.Values.Max(v => Math.Abs(v));
		bool progressOk = progress >= detectability.MinimumProgress;
		bool effectOk = maxEffect >= detectability.MinimumEffectMagnitude;
		bool phaseOk = instance.CurrentPhase >= FaultScenarioPhase.Degraded;

		if (!progressOk || !effectOk || !phaseOk)
		{
			instance.DetectabilityConditionStarted = null;
			return;
		}

		if (!instance.DetectabilityConditionStarted.HasValue)
		{
			instance.DetectabilityConditionStarted = instance.ScenarioElapsedTime;
			return;
		}

		if (instance.ScenarioElapsedTime - instance.DetectabilityConditionStarted.Value < detectability.MinimumDuration)
		{
			return;
		}

		instance.DetectabilityEmitted = true;
		instance.DetectableAtUtc = DateTimeOffset.UtcNow;
		instance.DetectableSimulationTime = instance.ScenarioElapsedTime;
		PublishEvent(FaultScenarioEventType.DegradationBecameDetectable, session.MachineId, instance);
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
					instance.ThresholdConditionStartedSimulationTime = null;
					instance.ActiveThresholdRuleId = null;
				}
				continue;
			}
			if (instance.ActiveThresholdRuleId != item.RuleId)
			{
				instance.ActiveThresholdRuleId = item.RuleId;
				instance.ThresholdConditionStartedAt = DateTimeOffset.UtcNow;
				instance.ThresholdConditionStartedSimulationTime = instance.ScenarioElapsedTime;
				if (instance.ThresholdFirstReachedAtUtc == null)
				{
					instance.ThresholdFirstReachedAtUtc = DateTimeOffset.UtcNow;
					instance.ThresholdValueAtFirstReached = value;
				}
			}
			if (!instance.ThresholdConditionStartedSimulationTime.HasValue
				|| instance.ScenarioElapsedTime - instance.ThresholdConditionStartedSimulationTime.Value < item.MinimumDuration)
			{
				if (!instance.ThresholdApproachingEmitted)
				{
					instance.ThresholdApproachingEmitted = true;
					PublishEvent(FaultScenarioEventType.ThresholdApproaching, session.MachineId, instance);
				}
				continue;
			}
			instance.ThresholdValueAtConfirmed = value;
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
		if (!instance.DetectabilityEmitted)
		{
			instance.DetectabilityEmitted = true;
			instance.DetectableAtUtc = DateTimeOffset.UtcNow;
			instance.DetectableSimulationTime = instance.ScenarioElapsedTime;
			PublishEvent(FaultScenarioEventType.DegradationBecameDetectable, session.MachineId, instance);
		}

		instance.ThresholdFaultTriggered = true;
		instance.ThresholdConfirmedAtUtc = DateTimeOffset.UtcNow;
		instance.MachineFaultedAtUtc = DateTimeOffset.UtcNow;
		instance.ActiveFaultCode = rule.FaultCode;
		instance.CurrentPhase = FaultScenarioPhase.Faulted;
		instance.LifecycleState = FaultScenarioLifecycleState.Faulted;
		session.Simulation.FaultScenarios.ActiveFaultCodes.Add(rule.FaultCode);
		if (!instance.ThresholdFirstReachedEmitted)
		{
			instance.ThresholdFirstReachedEmitted = true;
			PublishEvent(FaultScenarioEventType.ThresholdReached, session.MachineId, instance, value: instance.ThresholdValueAtFirstReached);
		}
		if (!instance.MachineFaultedEventEmitted)
		{
			instance.MachineFaultedEventEmitted = true;
			PublishEvent(FaultScenarioEventType.MachineFaulted, session.MachineId, instance, detail: rule.FaultCode);
		}
		if (bridge != null && rule.SetErrorActive)
		{
			bridge.SetMachineFault(instance.MachineId, rule.FaultCode, rule.FaultMessage, rule.StopProduction, rule.KeepServerOnline, instance.Definition.Priority);
		}
	}

	private static void CheckScenarioEnd(FaultScenarioInstance instance, Guid machineId)
	{
		if (!instance.AutoScenarioEndEnabled || instance.ScenarioElapsedTime < instance.Definition.DefaultDuration)
		{
			return;
		}

		if (instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun)
		{
			if (instance.LifecycleState != FaultScenarioLifecycleState.Recovering
				&& instance.LifecycleState != FaultScenarioLifecycleState.Completed)
			{
				instance.LifecycleState = FaultScenarioLifecycleState.Recovering;
				instance.CurrentPhase = FaultScenarioPhase.Recovering;
			}
			return;
		}

		if (instance.CurrentPhase >= FaultScenarioPhase.Faulted && instance.LifecycleState != FaultScenarioLifecycleState.Recovering)
		{
			instance.LifecycleState = FaultScenarioLifecycleState.Recovering;
			instance.CurrentPhase = FaultScenarioPhase.Recovering;
		}
	}

	private void CompleteInstance(FaultScenarioInstance instance, PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge, bool clearFault)
	{
		instance.RecoveryCompletedAtUtc = DateTimeOffset.UtcNow;
		instance.LifecycleState = FaultScenarioLifecycleState.Completed;
		instance.CurrentPhase = FaultScenarioPhase.Completed;
		session.Simulation.FaultScenarios.LastRecoveryCompletedAtUtc = instance.RecoveryCompletedAtUtc;
		session.Simulation.FaultScenarios.LastCompletedScenarioId = instance.ScenarioId;
		if (!instance.RecoveryCompletedEventEmitted)
		{
			instance.RecoveryCompletedEventEmitted = true;
			PublishEvent(FaultScenarioEventType.RecoveryCompleted, session.MachineId, instance);
			PublishEvent(FaultScenarioEventType.ScenarioStopped, session.MachineId, instance);
		}
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

	private void PublishEvent(
		FaultScenarioEventType type,
		Guid machineId,
		FaultScenarioInstance instance,
		FaultScenarioPhase? phase = null,
		double? value = null,
		string? detail = null)
	{
		if (_eventSink == null)
		{
			return;
		}

		_eventSink.Publish(new FaultScenarioEvent
		{
			EventType = type,
			MachineId = machineId,
			ScenarioId = instance.ScenarioId,
			InstanceId = instance.InstanceId,
			Phase = phase ?? instance.CurrentPhase,
			Value = value,
			Detail = detail
		});
	}
}
