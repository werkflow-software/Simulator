using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed record NotificationRecord
{
	public int NotificationNumber { get; init; }

	public uint? SequenceNumber { get; init; }

	public string SignalId { get; init; } = "";

	public string NodeId { get; init; } = "";

	public string? Value { get; init; }

	public string DataType { get; init; } = "";

	public string StatusCode { get; init; } = "";

	public DateTime SourceTimestamp { get; init; }

	public DateTime ServerTimestamp { get; init; }

	public DateTime ReceivedAtUtc { get; init; }

	public string? ValueDelta { get; init; }

	public double? SourceTimestampDeltaMs { get; init; }
}
