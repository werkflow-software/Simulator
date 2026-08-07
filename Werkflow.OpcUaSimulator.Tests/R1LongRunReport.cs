using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class R1LongRunReport
{
	public DateTime StartedAtUtc { get; set; }

	public DateTime EndedAtUtc { get; set; }

	public TimeSpan Duration { get; set; }

	public bool FullMode { get; set; }

	public int MachineCount { get; set; }

	public int SeedMachine1 { get; set; }

	public int SeedMachine2 { get; set; }

	public bool Passed { get; set; }

	public int HardLimitViolations { get; set; }

	public int PlausibilityViolations { get; set; }

	public int PhaseChanges { get; set; }

	public List<R1MachineReport> Machines { get; } = new List<R1MachineReport>();

	public List<SignalStatisticsSnapshot> Statistics { get; set; } = new List<SignalStatisticsSnapshot>();

	public List<CorrelationGroupResult> Correlations { get; set; } = new List<CorrelationGroupResult>();

	public List<UncorrelatedPairResult> UncorrelatedPairs { get; set; } = new List<UncorrelatedPairResult>();

	public List<MemorySample> MemorySamples { get; set; } = new List<MemorySample>();

	public List<string> Exceptions { get; set; } = new List<string>();
}
