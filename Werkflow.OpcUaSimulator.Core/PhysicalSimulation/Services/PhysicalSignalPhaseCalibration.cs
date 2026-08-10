using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class PhysicalSignalPhaseCalibration
{
	public static double Apply(string signalId, double dependencyTarget, SignalDefinition signal, ProcessPhase phase, double processDemand, double pressLoad, double pumpEfficiency = 0.88, double valveResponse = 0.9)
	{
		if (signalId.Equals("Process.SpindleSpeed", StringComparison.OrdinalIgnoreCase))
		{
			double spindlePhaseFactor = GetSpindlePhaseFactor(phase);
			double num = 650.0;
			double num2 = signal.NominalValue * (0.98 + 0.02 * Math.Clamp(processDemand, 0.0, 1.0));
			return num + (num2 - num) * spindlePhaseFactor;
		}
		if (signalId.Equals("Hydraulic.SupplyPressure", StringComparison.OrdinalIgnoreCase))
		{
			double hydraulicPhaseFactor = GetHydraulicPhaseFactor(phase);
			double num3 = 96.0;
			double num4 = signal.NominalValue + 2.0;
			double num5 = num3 + (num4 - num3) * hydraulicPhaseFactor;
			double num6 = (pumpEfficiency - 0.86) * 30.0;
			double num7 = (valveResponse - 0.88) * 14.0;
			double num8 = num5 * 0.65 + num6 + num7 + 33.0;
			double num9 = (Math.Clamp(pressLoad, 0.0, 1.0) - 0.35) * 26.0;
			return dependencyTarget * 0.25 + num8 + num9 * 0.36;
		}
		if (signalId.Equals("Hydraulic.PumpCurrent", StringComparison.OrdinalIgnoreCase))
		{
			double hydraulicPhaseFactor = GetHydraulicPhaseFactor(phase);
			if (hydraulicPhaseFactor <= 0.55)
			{
				return dependencyTarget;
			}

			double idleTarget = signal.NormalMinimum + 0.35;
			double runTarget = signal.NominalValue;
			double phaseTarget = idleTarget + (runTarget - idleTarget) * hydraulicPhaseFactor;
			return dependencyTarget * 0.35 + phaseTarget * 0.65;
		}
		if (signalId.Equals("Process.QualityIndex", StringComparison.OrdinalIgnoreCase) || signalId.Equals("Quality.ProcessQualityIndex", StringComparison.OrdinalIgnoreCase))
		{
			double phaseDemand = ProcessPhaseScheduler.GetPhaseDemand(phase);
			double num10 = ((phase == ProcessPhase.PeakLoad) ? 1.2 : 0.0);
			return Math.Clamp(dependencyTarget - num10, signal.NormalMinimum, signal.NormalMaximum - 0.2);
		}
		return dependencyTarget;
	}

	public static double GetHydraulicPhaseFactor(ProcessPhase phase)
	{
		if (1 == 0)
		{
		}
		double result = phase switch
		{
			ProcessPhase.Idle => 0.18, 
			ProcessPhase.Setup => 0.35, 
			ProcessPhase.RampUp => 0.62, 
			ProcessPhase.Processing => 1.0, 
			ProcessPhase.PeakLoad => 1.06, 
			ProcessPhase.RampDown => 0.55, 
			ProcessPhase.Cooling => 0.22, 
			ProcessPhase.Waiting => 0.2, 
			_ => 0.3, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static double GetSpindlePhaseFactor(ProcessPhase phase)
	{
		if (1 == 0)
		{
		}
		double result = phase switch
		{
			ProcessPhase.Idle => 0.22, 
			ProcessPhase.Setup => 0.42, 
			ProcessPhase.RampUp => 0.72, 
			ProcessPhase.Processing => 1.0, 
			ProcessPhase.PeakLoad => 1.04, 
			ProcessPhase.RampDown => 0.58, 
			ProcessPhase.Cooling => 0.28, 
			ProcessPhase.Waiting => 0.24, 
			_ => 0.35, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
