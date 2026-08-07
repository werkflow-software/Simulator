using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class PhysicalMachineRuntimeFactory : IPhysicalMachineRuntimeFactory
{
	public PhysicalMachineRuntime Create(PhysicalMachineProfile profile, DateTimeOffset? createdAt = null)
	{
		ArgumentNullException.ThrowIfNull(profile, "profile");
		DateTimeOffset timestamp = createdAt ?? DateTimeOffset.UtcNow;
		List<SignalRuntimeState> signals = profile.Signals.OrderBy<SignalDefinition, string>((SignalDefinition s) => s.SignalId, StringComparer.Ordinal).Select(delegate(SignalDefinition signal)
		{
			SignalRuntimeState signalRuntimeState = new SignalRuntimeState
			{
				SignalId = signal.SignalId,
				Quality = SignalQuality.Good,
				UpdateSequence = 0L,
				ActiveInfluences = new List<string>()
			};
			SignalRuntimeValueHelper.Initialize(signal, signalRuntimeState, timestamp);
			return signalRuntimeState;
		}).ToList();
		List<HiddenProcessRuntimeState> hiddenProcessStates = profile.HiddenProcessStates.OrderBy<HiddenProcessStateDefinition, string>((HiddenProcessStateDefinition s) => s.StateId, StringComparer.Ordinal).Select(delegate(HiddenProcessStateDefinition state)
		{
			double initialValue = state.InitialValue;
			return new HiddenProcessRuntimeState
			{
				StateId = state.StateId,
				CurrentValue = initialValue,
				TargetValue = initialValue,
				PreviousValue = initialValue,
				LastUpdatedAt = timestamp,
				ActiveInfluences = new List<string>()
			};
		}).ToList();
		return new PhysicalMachineRuntime
		{
			ProfileId = profile.ProfileId,
			ProfileVersion = profile.ProfileVersion,
			CreatedAt = timestamp,
			Signals = signals,
			HiddenProcessStates = hiddenProcessStates
		};
	}
}
