using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PerMachinePhysicsState
{
	public Dictionary<string, DelayRingBuffer> DelayBuffers { get; } = new Dictionary<string, DelayRingBuffer>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> PreviousOutputs { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, TimeSpan> NextHiddenUpdate { get; } = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> PreviousSignalValues { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, double> PreviousCounterValues { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

	public SeededRandomStreams? Random { get; set; }

	public void Reset()
	{
		DelayBuffers.Clear();
		PreviousOutputs.Clear();
		NextHiddenUpdate.Clear();
		PreviousSignalValues.Clear();
		PreviousCounterValues.Clear();
		Random = null;
	}
}
