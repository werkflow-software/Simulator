using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class HiddenProcessRuntimeState
{
	public required string StateId { get; init; }

	public double CurrentValue { get; set; }

	public double TargetValue { get; set; }

	public double PreviousValue { get; set; }

	public DateTimeOffset LastUpdatedAt { get; set; }

	public List<string> ActiveInfluences { get; init; } = new List<string>();
}
