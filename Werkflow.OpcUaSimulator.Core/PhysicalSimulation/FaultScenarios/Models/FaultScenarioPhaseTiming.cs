using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultScenarioPhaseTiming
{
	public FaultScenarioPhase Phase { get; set; }

	public TimeSpan Duration { get; set; }

	public double DurationFraction { get; set; }
}
