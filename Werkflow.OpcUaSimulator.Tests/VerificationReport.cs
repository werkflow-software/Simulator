using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class VerificationReport
{
	public DateTime StartedAtUtc { get; set; }

	public DateTime EndedAtUtc { get; set; }

	public TimeSpan Duration { get; set; }

	public VerificationOptions Options { get; set; } = new VerificationOptions();

	public List<string> Endpoints { get; set; } = new List<string>();

	public List<string> MachineNames { get; set; } = new List<string>();

	public List<int> SignalCountPerMachine { get; set; } = new List<int>();

	public List<int> RegisteredNodesPerMachine { get; set; } = new List<int>();

	public List<double> UpdatesPerSecondPerMachine { get; set; } = new List<double>();

	public List<double> AveragePublishDurationMsPerMachine { get; set; } = new List<double>();

	public List<double> MaxPublishDurationMsPerMachine { get; set; } = new List<double>();

	public List<int> FailedUpdatesPerMachine { get; set; } = new List<int>();

	public List<int> SkippedIdenticalPerMachine { get; set; } = new List<int>();

	public int ActiveServers { get; set; }

	public int ActivePublishers { get; set; }

	public double MemoryStartMb { get; set; }

	public double MemoryEndMb { get; set; }

	public bool Machine1StoppedSuccessfully { get; set; }

	public bool Machine2StillUpdatingWhileMachine1Stopped { get; set; }

	public double Machine2UpdatesWhileMachine1Stopped { get; set; }

	public int Machine1RestartNodeCount { get; set; }

	public bool Machine1RestartSameNodeCount { get; set; }

	public int PublisherCountAfterMachine1Restart { get; set; }

	public bool ValuesStableDuringPause { get; set; }

	public bool ServerReachableDuringPause { get; set; }

	public int PublishersDuringPause { get; set; }

	public int PublisherCountAfterResume { get; set; }

	public bool NoDuplicatePublishersAfterResume { get; set; }

	public List<StopRestartCycleResult> StopRestartCycles { get; set; } = new List<StopRestartCycleResult>();

	public List<DataChangeSample> DataChangeResults { get; set; } = new List<DataChangeSample>();

	public List<string> Exceptions { get; set; } = new List<string>();

	public string ToJson()
	{
		return JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}
}
