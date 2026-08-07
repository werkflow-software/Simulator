using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PhysicalPhaseSegmentSnapshot
{
	public Guid MachineId { get; init; }

	public string MachineName { get; init; } = string.Empty;

	public string ProfileId { get; init; } = string.Empty;

	public string Phase { get; init; } = string.Empty;

	public DateTimeOffset StartedAtUtc { get; init; }

	public DateTimeOffset EndedAtUtc { get; init; }

	public double DurationSeconds { get; init; }

	public int JobId { get; init; }

	public string JobName { get; init; } = string.Empty;

	public string PartName { get; init; } = string.Empty;

	public int TargetCounter { get; init; }

	public int ActualCounterAtStart { get; init; }

	public int SampleCount { get; init; }

	public double? AverageLoad { get; init; }

	public double? AverageCurrent { get; init; }

	public double? AverageTemperature { get; init; }

	public double? AverageSpeed { get; init; }

	public double? AveragePressure { get; init; }

	public double? AverageProcessPower { get; init; }

	public double? MinimumLoad { get; init; }

	public double? MaximumLoad { get; init; }

	public double? MinimumTemperature { get; init; }

	public double? MaximumTemperature { get; init; }

	public bool IsValid { get; init; }
}
