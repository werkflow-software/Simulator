using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class DataChangeSample
{
	public string NodePath { get; set; } = "";

	public string DataType { get; set; } = "";

	public string? InitialValue { get; set; }

	public string? LaterValue { get; set; }

	public DateTime InitialSourceTimestamp { get; set; }

	public DateTime LaterSourceTimestamp { get; set; }

	public bool TypeMatches { get; set; }

	public bool SubscriptionReceived { get; set; }

	public bool SourceTimestampUpdated { get; set; }

	public bool CounterMonotonic { get; set; }
}
