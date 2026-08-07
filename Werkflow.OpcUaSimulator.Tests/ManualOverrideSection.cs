using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class ManualOverrideSection
{
	public string SignalId { get; set; } = "";

	public string NodeId { get; set; } = "";

	public string? InitialValue { get; set; }

	public string? SetValue { get; set; }

	public DateTime ActionAtUtc { get; set; }

	public DateTime NotificationAtUtc { get; set; }

	public double LatencyMilliseconds { get; set; }

	public DateTime SourceTimestamp { get; set; }

	public string StatusCode { get; set; } = "";

	public string Result { get; set; } = "Failed";
}
