using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class ProcessPhaseScheduler
{
	public static TimeSpan GetMinDuration(ProcessPhase phase, PhysicalVerificationMode mode, SeededRandomStreams random)
	{
		TimeSpan result;
		if (mode == PhysicalVerificationMode.Short)
		{
			if (1 == 0)
			{
			}
			result = phase switch
			{
				ProcessPhase.Idle => TimeSpan.FromSeconds(15.0 * random.PhaseDurationFactor(0.85, 1.1)), 
				ProcessPhase.Setup => TimeSpan.FromSeconds(15.0), 
				ProcessPhase.RampUp => TimeSpan.FromSeconds(15.0), 
				ProcessPhase.Processing => TimeSpan.FromSeconds(45.0), 
				ProcessPhase.PeakLoad => TimeSpan.FromSeconds(20.0), 
				ProcessPhase.RampDown => TimeSpan.FromSeconds(15.0), 
				ProcessPhase.Cooling => TimeSpan.FromSeconds(45.0), 
				ProcessPhase.Waiting => TimeSpan.FromSeconds(15.0), 
				_ => TimeSpan.FromSeconds(15.0), 
			};
			if (1 == 0)
			{
			}
			return result;
		}
		if (1 == 0)
		{
		}
		result = phase switch
		{
			ProcessPhase.Idle => TimeSpan.FromSeconds(30.0 * random.PhaseDurationFactor(0.8, 1.2)), 
			ProcessPhase.Setup => TimeSpan.FromSeconds(20.0), 
			ProcessPhase.RampUp => TimeSpan.FromSeconds(25.0), 
			ProcessPhase.Processing => TimeSpan.FromSeconds(90.0 * random.PhaseDurationFactor(0.7, 1.4)), 
			ProcessPhase.PeakLoad => TimeSpan.FromSeconds(40.0), 
			ProcessPhase.RampDown => TimeSpan.FromSeconds(20.0), 
			ProcessPhase.Cooling => TimeSpan.FromSeconds(35.0), 
			ProcessPhase.Waiting => TimeSpan.FromSeconds(15.0), 
			_ => TimeSpan.FromSeconds(30.0), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static ProcessPhase GetNextPhase(ProcessPhase current)
	{
		if (1 == 0)
		{
		}
		ProcessPhase result = current switch
		{
			ProcessPhase.Idle => ProcessPhase.Setup, 
			ProcessPhase.Setup => ProcessPhase.RampUp, 
			ProcessPhase.RampUp => ProcessPhase.Processing, 
			ProcessPhase.Processing => ProcessPhase.PeakLoad, 
			ProcessPhase.PeakLoad => ProcessPhase.RampDown, 
			ProcessPhase.RampDown => ProcessPhase.Cooling, 
			ProcessPhase.Cooling => ProcessPhase.Waiting, 
			ProcessPhase.Waiting => ProcessPhase.Idle, 
			_ => ProcessPhase.Idle, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static bool TryAdvance(PhysicalSimulationContext context, SeededRandomStreams random, out ProcessPhaseTransition? transition)
	{
		transition = null;
		TimeSpan minDuration = GetMinDuration(context.CurrentPhase, context.VerificationMode, random);
		if (context.PhaseElapsedSimulationTime < minDuration)
		{
			return false;
		}
		ProcessPhase currentPhase = context.CurrentPhase;
		ProcessPhase nextPhase = GetNextPhase(currentPhase);
		transition = new ProcessPhaseTransition
		{
			FromPhase = currentPhase,
			ToPhase = nextPhase,
			TimestampUtc = DateTimeOffset.UtcNow,
			PhaseDuration = context.PhaseElapsedSimulationTime,
			JobIndex = context.Job.JobIndex
		};
		context.CurrentPhase = nextPhase;
		context.PhaseStartedAt = DateTimeOffset.UtcNow;
		context.PhaseElapsedSimulationTime = TimeSpan.Zero;
		context.PhaseTransitions.Add(transition);
		context.Metrics.PhaseChanges++;
		bool flag = (uint)nextPhase <= 1u;
		bool flag2 = flag;
		bool flag3 = flag2;
		if (flag3)
		{
			bool flag4 = (uint)(currentPhase - 6) <= 1u;
			flag3 = flag4;
		}
		if (flag3)
		{
			PhysicalJobCoordinator.AdvanceJob(context);
		}
		return true;
	}

	public static double GetPhaseDemand(ProcessPhase phase)
	{
		if (1 == 0)
		{
		}
		double result = phase switch
		{
			ProcessPhase.Idle => 0.15, 
			ProcessPhase.Setup => 0.25, 
			ProcessPhase.RampUp => 0.55, 
			ProcessPhase.Processing => 0.65, 
			ProcessPhase.PeakLoad => 0.95, 
			ProcessPhase.RampDown => 0.4, 
			ProcessPhase.Cooling => 0.2, 
			ProcessPhase.Waiting => 0.18, 
			_ => 0.3, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static double GetTemperaturePhaseOffset(ProcessPhase phase)
	{
		if (1 == 0)
		{
		}
		int num = phase switch
		{
			ProcessPhase.Idle => -10, 
			ProcessPhase.Setup => -6, 
			ProcessPhase.RampUp => -2, 
			ProcessPhase.Processing => 0, 
			ProcessPhase.PeakLoad => 2, 
			ProcessPhase.RampDown => 1, 
			ProcessPhase.Cooling => -4, 
			ProcessPhase.Waiting => -8, 
			_ => 0, 
		};
		if (1 == 0)
		{
		}
		return num;
	}
}
