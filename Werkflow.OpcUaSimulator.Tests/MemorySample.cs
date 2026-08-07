using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class MemorySample
{
	public DateTime TimestampUtc { get; set; }

	public double WorkingSetMb { get; set; }

	public double PrivateMemoryMb { get; set; }

	public double GcHeapMb { get; set; }

	public int Gen0Collections { get; set; }

	public int Gen1Collections { get; set; }

	public int Gen2Collections { get; set; }

	public int ActiveEngines { get; set; }

	public int ActivePublishers { get; set; }

	public int RegisteredNodes { get; set; }
}
