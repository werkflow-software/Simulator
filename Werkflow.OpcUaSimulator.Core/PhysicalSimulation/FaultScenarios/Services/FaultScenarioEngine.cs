using System;
using System.Collections.Generic;
using System.Globalization;
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
		TimeSpan cumulative = TimeSpan.Zero;
		foreach (FaultScenarioPhaseTiming item in phases)
		{
			if (instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun
				&& item.Phase >= FaultScenarioPhase.Faulted)
			{
				continue;
			}

			if (item.Phase == FaultScenarioPhase.Faulted
				&& instance.RunMode != FaultScenarioRunMode.NonFaultingControlRun
				&& !instance.ThresholdFaultTriggered)
			{
				continue;
			}

			TimeSpan phaseDuration = item.Duration > TimeSpan.Zero
				? item.Duration
				: TimeSpan.FromTicks((long)(defaultDuration.Ticks * item.DurationFraction));
			cumulative += phaseDuration;
			if (scenarioElapsedTime < cumulative)
			{
				if (instance.CurrentPhase != item.Phase)
				{
					instance.CurrentPhase = item.Phase;
					instance.NextPhaseChangeAt = instance.StartedAt + cumulative;
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

		if (!instance.ThresholdFaultTriggered)
		{
			FaultScenarioPhase holdPhase = phases
				.Where(p => p.Phase < FaultScenarioPhase.Faulted)
				.Select(p => p.Phase)
				.DefaultIfEmpty(FaultScenarioPhase.Critical)
				.Max();
			if (instance.CurrentPhase < holdPhase)
			{
				instance.CurrentPhase = holdPhase;
				PublishEvent(FaultScenarioEventType.ScenarioPhaseChanged, machineId, instance, holdPhase);
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
		bool phaseOk = instance.CurrentPhase >= FaultScenarioPhase.Developing;

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

		bool canEmitThresholdEvents = CanEmitThresholdEvents(instance);

		foreach (FaultThresholdRule rule in instance.Definition.ThresholdRules.Where(r => r.IsEnabled))
		{
			if (instance.RunMode == FaultScenarioRunMode.NonFaultingControlRun && rule.DisabledInControlRun)
			{
				continue;
			}

			double value = ReadThresholdSource(rule, session);
			FaultThresholdRuleRuntimeState state = GetRuleState(instance, rule.RuleId);
			bool satisfied = EvaluateComparison(rule, value);
			bool approaching = IsThresholdApproaching(rule, value);

			TrackPhysicalThresholdCrossing(instance, rule, state, value, satisfied);

			if (canEmitThresholdEvents)
			{
				UpdateThresholdApproaching(instance, rule, state, approaching, satisfied);
			}

			if (!canEmitThresholdEvents)
			{
				continue;
			}

			if (satisfied && !state.IsCurrentlySatisfied)
			{
				OnThresholdEntered(instance, rule, state, value, session);
			}
			else if (!satisfied && state.IsCurrentlySatisfied)
			{
				OnThresholdExited(instance, rule, state, value, session);
			}
			else if (satisfied && state.IsCurrentlySatisfied && !state.IsConfirmed && !instance.ThresholdFaultTriggered)
			{
				if (state.CurrentSatisfiedSinceSimulationTime.HasValue
					&& instance.ScenarioElapsedTime - state.CurrentSatisfiedSinceSimulationTime.Value >= rule.MinimumDuration)
				{
					TriggerThresholdFault(instance, rule, state, session, bridge);
					break;
				}
			}
		}
	}

	private static bool CanEmitThresholdEvents(FaultScenarioInstance instance)
	{
		if (instance.Definition.Detectability == null)
		{
			return true;
		}

		return instance.DetectabilityEmitted
			&& instance.DetectableSimulationTime.HasValue
			&& instance.ScenarioElapsedTime > instance.DetectableSimulationTime.Value;
	}

	private static FaultThresholdRuleRuntimeState GetRuleState(FaultScenarioInstance instance, string ruleId)
	{
		if (!instance.ThresholdRuleStates.TryGetValue(ruleId, out var state))
		{
			state = new FaultThresholdRuleRuntimeState();
			instance.ThresholdRuleStates[ruleId] = state;
		}
		return state;
	}

	private static void TrackPhysicalThresholdCrossing(
		FaultScenarioInstance instance,
		FaultThresholdRule rule,
		FaultThresholdRuleRuntimeState state,
		double value,
		bool satisfied)
	{
		if (satisfied && !state.PhysicallySatisfied)
		{
			state.PhysicallySatisfied = true;
			if (!state.HasEverBeenSatisfied)
			{
				state.HasEverBeenSatisfied = true;
				state.FirstEverReachedSimulationTime = instance.ScenarioElapsedTime;
				instance.ThresholdFirstReachedAtUtc = DateTimeOffset.UtcNow;
				instance.ThresholdValueAtFirstReached = value;
			}
		}
		else if (!satisfied && state.PhysicallySatisfied)
		{
			state.PhysicallySatisfied = false;
		}
	}

	private void UpdateThresholdApproaching(
		FaultScenarioInstance instance,
		FaultThresholdRule rule,
		FaultThresholdRuleRuntimeState state,
		bool approaching,
		bool satisfied)
	{
		if (satisfied)
		{
			state.IsApproaching = false;
			return;
		}

		if (approaching && !state.IsApproaching)
		{
			state.IsApproaching = true;
			PublishEvent(FaultScenarioEventType.ThresholdApproaching, instance.MachineId, instance, detail: rule.RuleId);
		}
		else if (!approaching && state.IsApproaching)
		{
			state.IsApproaching = false;
		}
	}

	private void OnThresholdEntered(
		FaultScenarioInstance instance,
		FaultThresholdRule rule,
		FaultThresholdRuleRuntimeState state,
		double value,
		PhysicalMachineSession session)
	{
		state.IsCurrentlySatisfied = true;
		state.CurrentSatisfiedSinceSimulationTime = instance.ScenarioElapsedTime;
		state.LastEnteredAtUtc = DateTimeOffset.UtcNow;
		state.LastEnteredSimulationTime = instance.ScenarioElapsedTime;
		state.EnterCount++;
		instance.ThresholdEnterCount = state.EnterCount;

		PublishEvent(
			FaultScenarioEventType.ThresholdEntered,
			session.MachineId,
			instance,
			value: value,
			detail: rule.RuleId);

		if (!instance.ThresholdFirstReachedEmitted)
		{
			instance.ThresholdFirstReachedEmitted = true;
			instance.ThresholdValueAtFirstReached ??= value;
			PublishEvent(
				FaultScenarioEventType.ThresholdReached,
				session.MachineId,
				instance,
				value: instance.ThresholdValueAtFirstReached);
		}
	}

	private void OnThresholdExited(
		FaultScenarioInstance instance,
		FaultThresholdRule rule,
		FaultThresholdRuleRuntimeState state,
		double value,
		PhysicalMachineSession session)
	{
		state.IsCurrentlySatisfied = false;
		state.CurrentSatisfiedSinceSimulationTime = null;
		state.LastExitedAtUtc = DateTimeOffset.UtcNow;
		state.LastExitedSimulationTime = instance.ScenarioElapsedTime;
		state.ExitCount++;
		instance.ThresholdExitCount = state.ExitCount;
		state.IsApproaching = false;

		PublishEvent(
			FaultScenarioEventType.ThresholdExited,
			session.MachineId,
			instance,
			value: value,
			detail: rule.RuleId);
	}

	private static bool IsThresholdApproaching(FaultThresholdRule rule, double value)
	{
		if (EvaluateComparison(rule, value))
		{
			return false;
		}

		double threshold = rule.ThresholdValue;
		double margin = Math.Max(Math.Abs(threshold) * 0.1, 5.0);

		switch (rule.Comparison)
		{
		case FaultThresholdComparison.LessThan:
			return value < threshold + margin;
		case FaultThresholdComparison.LessThanOrEqual:
			return value <= threshold + margin;
		case FaultThresholdComparison.GreaterThan:
			return value > threshold - margin;
		case FaultThresholdComparison.GreaterThanOrEqual:
			return value >= threshold - margin;
		case FaultThresholdComparison.OutsideRange:
			double secondary = rule.ThresholdValueSecondary.GetValueOrDefault(rule.ThresholdValue);
			return value < threshold + margin || value > secondary - margin;
		case FaultThresholdComparison.InsideRange:
			double upper = rule.ThresholdValueSecondary.GetValueOrDefault(rule.ThresholdValue);
			return value >= threshold - margin && value <= upper + margin;
		default:
			return false;
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

	private void TriggerThresholdFault(
		FaultScenarioInstance instance,
		FaultThresholdRule rule,
		FaultThresholdRuleRuntimeState state,
		PhysicalMachineSession session,
		IFaultScenarioSimulationBridge? bridge)
	{
		state.IsConfirmed = true;
		instance.ThresholdFaultTriggered = true;
		instance.ThresholdConfirmedAtUtc = DateTimeOffset.UtcNow;
		instance.ThresholdValueAtConfirmed = ReadThresholdSource(rule, session);
		instance.MachineFaultedAtUtc = DateTimeOffset.UtcNow;
		instance.ConfirmedThresholdStreakStartedSimulationTime = state.CurrentSatisfiedSinceSimulationTime;
		instance.ThresholdEnterCount = state.EnterCount;
		instance.ThresholdExitCount = state.ExitCount;
		instance.ActiveFaultCode = rule.FaultCode;
		instance.LifecycleState = FaultScenarioLifecycleState.Faulted;
		session.Simulation.FaultScenarios.ActiveFaultCodes.Add(rule.FaultCode);

		if (!instance.ThresholdFirstReachedEmitted)
		{
			instance.ThresholdFirstReachedEmitted = true;
			instance.ThresholdValueAtFirstReached ??= instance.ThresholdValueAtConfirmed;
			PublishEvent(FaultScenarioEventType.ThresholdReached, session.MachineId, instance, value: instance.ThresholdValueAtFirstReached);
		}

		if (!instance.ThresholdConfirmedEmitted)
		{
			instance.ThresholdConfirmedEmitted = true;
			string streakStart = state.CurrentSatisfiedSinceSimulationTime?.ToString("c", CultureInfo.InvariantCulture) ?? "00:00:00";
			PublishEvent(
				FaultScenarioEventType.ThresholdConfirmed,
				session.MachineId,
				instance,
				value: instance.ThresholdValueAtConfirmed,
				detail: $"{rule.MinimumDuration.ToString("c", CultureInfo.InvariantCulture)}|{streakStart}");
		}

		if (!instance.MachineFaultedEventEmitted)
		{
			instance.MachineFaultedEventEmitted = true;
			PublishEvent(FaultScenarioEventType.MachineFaulted, session.MachineId, instance, detail: rule.FaultCode);
		}

		if (instance.CurrentPhase != FaultScenarioPhase.Faulted)
		{
			instance.CurrentPhase = FaultScenarioPhase.Faulted;
			PublishEvent(FaultScenarioEventType.ScenarioPhaseChanged, session.MachineId, instance, FaultScenarioPhase.Faulted);
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

		if (instance.ThresholdFaultTriggered
			&& instance.LifecycleState == FaultScenarioLifecycleState.Faulted)
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
