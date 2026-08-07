using System;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public static class SimulationErrorPolicy
{
	public const double MaxConcurrentDisruptedFraction = 0.25;

	public const int MaxDisruptedDurationMs = 60000;

	public const int MinDisruptedDurationMs = 3000;

	public const double DefaultMachineErrorProbabilityPercent = 0.5;

	public const double DefaultMachineDisconnectProbabilityPercent = 0.5;

	public const double DefaultEventErrorProbabilityPercent = 0.5;

	public const double DefaultEventWarningProbabilityPercent = 1.0;

	public const double DefaultEventDisconnectProbabilityPercent = 0.5;

	public static int GetMaxConcurrentDisrupted(int activeMachineCount)
	{
		if (activeMachineCount <= 0)
		{
			return 0;
		}
		return Math.Min(activeMachineCount, (int)Math.Ceiling((double)activeMachineCount * 0.25));
	}

	public static int GetMaxConcurrentErrors(int activeMachineCount)
	{
		return GetMaxConcurrentDisrupted(activeMachineCount);
	}

	public static int CapDisruptedDuration(int durationMs)
	{
		return Math.Clamp(durationMs, 3000, 60000);
	}

	public static int CapErrorDuration(int durationMs)
	{
		return CapDisruptedDuration(durationMs);
	}

	public static bool IsDisruptedState(MachineRuntimeState runtime)
	{
		MachineState state = runtime.State;
		bool flag = ((state == MachineState.Offline || (uint)(state - 3) <= 1u) ? true : false);
		return flag || runtime.ErrorActive;
	}
}
