using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class SignalRuntimeState
{
	public required string SignalId { get; init; }

	public double CurrentValue { get; set; }

	public double TargetValue { get; set; }

	public double PreviousValue { get; set; }

	public DateTimeOffset LastUpdatedAt { get; set; }

	public DateTimeOffset LastChangedAt { get; set; }

	public bool IsWithinNormalRange { get; set; }

	public bool IsWithinHardLimits { get; set; }

	public SignalQuality Quality { get; set; } = SignalQuality.Good;

	public long UpdateSequence { get; set; }

	public string? CurrentStringValue { get; set; }

	public bool? CurrentBooleanValue { get; set; }

	public DateTime? CurrentDateTimeUtc { get; set; }

	public List<string> ActiveInfluences { get; init; } = new List<string>();
}
