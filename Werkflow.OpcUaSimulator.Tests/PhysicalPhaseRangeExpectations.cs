using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalPhaseRangeExpectations
{
	private static readonly HashSet<string> ProcessingCriticalSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Process.SpindleSpeed", "Hydraulic.SupplyPressure", "Process.QualityIndex", "Quality.ProcessQualityIndex", "Axis01.MotorTemperature" };

	private static readonly Dictionary<string, Dictionary<ProcessPhase, (double Min, double Max)>> PhaseRanges = new Dictionary<string, Dictionary<ProcessPhase, (double, double)>>(StringComparer.OrdinalIgnoreCase)
	{
		["Axis01.MotorTemperature"] = new Dictionary<ProcessPhase, (double, double)>
		{
			[ProcessPhase.Idle] = (40.0, 48.0),
			[ProcessPhase.Setup] = (44.0, 50.0),
			[ProcessPhase.RampUp] = (48.0, 54.0),
			[ProcessPhase.Processing] = (49.0, 56.0),
			[ProcessPhase.PeakLoad] = (52.0, 59.0),
			[ProcessPhase.RampDown] = (48.0, 54.0),
			[ProcessPhase.Cooling] = (45.0, 52.0),
			[ProcessPhase.Waiting] = (42.0, 50.0)
		},
		["Process.SpindleSpeed"] = new Dictionary<ProcessPhase, (double, double)>
		{
			[ProcessPhase.Idle] = (400.0, 1200.0),
			[ProcessPhase.Setup] = (900.0, 1800.0),
			[ProcessPhase.RampUp] = (2200.0, 2900.0),
			[ProcessPhase.Processing] = (2950.0, 3050.0),
			[ProcessPhase.PeakLoad] = (2980.0, 3080.0),
			[ProcessPhase.RampDown] = (1200.0, 2500.0),
			[ProcessPhase.Cooling] = (400.0, 1100.0),
			[ProcessPhase.Waiting] = (350.0, 1000.0)
		},
		["Hydraulic.SupplyPressure"] = new Dictionary<ProcessPhase, (double, double)>
		{
			[ProcessPhase.Idle] = (100.0, 130.0),
			[ProcessPhase.Setup] = (130.0, 168.0),
			[ProcessPhase.RampUp] = (155.0, 180.0),
			[ProcessPhase.Processing] = (168.0, 188.0),
			[ProcessPhase.PeakLoad] = (175.0, 192.0),
			[ProcessPhase.RampDown] = (140.0, 175.0),
			[ProcessPhase.Cooling] = (110.0, 145.0),
			[ProcessPhase.Waiting] = (105.0, 135.0)
		},
		["Process.QualityIndex"] = new Dictionary<ProcessPhase, (double, double)>
		{
			[ProcessPhase.Processing] = (95.5, 99.5),
			[ProcessPhase.PeakLoad] = (95.0, 99.2),
			[ProcessPhase.Setup] = (95.0, 99.5),
			[ProcessPhase.RampUp] = (95.5, 99.5)
		},
		["Quality.ProcessQualityIndex"] = new Dictionary<ProcessPhase, (double, double)>
		{
			[ProcessPhase.Processing] = (95.5, 99.5),
			[ProcessPhase.PeakLoad] = (95.0, 99.2),
			[ProcessPhase.Setup] = (95.0, 99.5),
			[ProcessPhase.RampUp] = (95.5, 99.5)
		}
	};

	public static bool TryGetExpectedRange(string signalId, ProcessPhase phase, out double min, out double max)
	{
		if (!PhaseRanges.TryGetValue(signalId, out Dictionary<ProcessPhase, (double, double)> value) || !value.TryGetValue(phase, out var value2))
		{
			min = 0.0;
			max = 0.0;
			return false;
		}
		(min, max) = value2;
		return true;
	}

	public static bool IsProcessingCritical(string signalId)
	{
		return ProcessingCriticalSignals.Contains(signalId);
	}
}
