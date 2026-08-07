using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalPublisherMetrics
{
	public PhysicalPublisherState State { get; set; } = PhysicalPublisherState.Stopped;

	public DateTimeOffset? LastPublishAt { get; set; }

	public double UpdatesPerSecond { get; set; }

	public double AveragePublishDurationMs { get; set; }

	public double MaxPublishDurationMs { get; set; }

	public int FailedUpdates { get; set; }

	public int SkippedIdenticalValues { get; set; }

	public long TotalPublishedUpdates { get; set; }

	public int PublishedSignalCount { get; set; }

	public string? LastError { get; set; }
}
