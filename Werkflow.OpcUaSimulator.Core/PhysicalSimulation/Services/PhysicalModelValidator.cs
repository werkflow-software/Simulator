using System;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class PhysicalModelValidator : IPhysicalModelValidator
{
	public void ValidateTick(PhysicalMachineSession session)
	{
		PerMachinePhysicsState physicsState = session.Simulation.PhysicsState;
		foreach (SignalDefinition signalDef in session.Profile.Signals.Where((SignalDefinition s) => s.IsEnabled))
		{
			SignalRuntimeState signalRuntimeState = session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId == signalDef.SignalId);
			if (signalRuntimeState != null)
			{
				PhysicalSignalDataType dataType = signalDef.DataType;
				if ((uint)dataType <= 1u)
				{
					ValidateNumeric(signalDef, signalRuntimeState, session, physicsState);
				}
				else if (signalDef.TechnicalBehavior == TechnicalSignalBehavior.Counter)
				{
					ValidateCounter(signalDef, signalRuntimeState, session, physicsState);
				}
			}
		}
	}

	private static void ValidateNumeric(SignalDefinition def, SignalRuntimeState runtime, PhysicalMachineSession session, PerMachinePhysicsState physics)
	{
		double currentValue = runtime.CurrentValue;
		if (currentValue < def.HardMinimum || currentValue > def.HardMaximum)
		{
			runtime.CurrentValue = Math.Clamp(currentValue, def.HardMinimum, def.HardMaximum);
			session.Simulation.Metrics.HardLimitPrevented++;
			LogViolation(session, "Hard Limit für " + def.SignalId + " verhindert.");
		}
		if (physics.PreviousSignalValues.TryGetValue(def.SignalId, out var value))
		{
			double num = (def.HardMaximum - def.HardMinimum) * 0.25;
			if (num > 0.0 && Math.Abs(currentValue - value) > num)
			{
				runtime.CurrentValue = value + (double)Math.Sign(currentValue - value) * num;
				LogViolation(session, "Sprungbegrenzung für " + def.SignalId + ".");
			}
		}
		physics.PreviousSignalValues[def.SignalId] = runtime.CurrentValue;
		if (def.SignalId.Contains("Quality", StringComparison.OrdinalIgnoreCase) && (currentValue < 0.0 || currentValue > 100.0))
		{
			runtime.CurrentValue = Math.Clamp(currentValue, 0.0, 100.0);
			LogViolation(session, "Qualitätsindex " + def.SignalId + " korrigiert.");
		}
		if (def.SignalId.Contains("Vibration", StringComparison.OrdinalIgnoreCase) && currentValue < 0.0)
		{
			runtime.CurrentValue = 0.0;
			LogViolation(session, "Negative Vibration " + def.SignalId + " korrigiert.");
		}
	}

	private static void ValidateCounter(SignalDefinition def, SignalRuntimeState runtime, PhysicalMachineSession session, PerMachinePhysicsState physics)
	{
		if (physics.PreviousCounterValues.TryGetValue(def.SignalId, out var value) && runtime.CurrentValue < value)
		{
			runtime.CurrentValue = value;
			LogViolation(session, "Counter " + def.SignalId + " darf nicht sinken.");
		}
		physics.PreviousCounterValues[def.SignalId] = runtime.CurrentValue;
	}

	private static void LogViolation(PhysicalMachineSession session, string message)
	{
		session.Simulation.Metrics.PlausibilityViolations++;
		session.Simulation.Metrics.LastPlausibilityError = message;
	}
}
