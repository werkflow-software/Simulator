using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Recording;

public sealed class SignalRecordingConfiguration
{
	public TimeSpan RecordingInterval { get; init; } = TimeSpan.FromSeconds(1);

	public IReadOnlyList<string> SignalIds { get; init; } = [];

	public bool RecordStandardStatus { get; init; } = true;
}

public sealed class SignalSample
{
	public DateTimeOffset RealTimestampUtc { get; init; }

	public TimeSpan SimulationTimestamp { get; init; }

	public required string RunId { get; init; }

	public Dictionary<string, double> Signals { get; init; } = new(StringComparer.OrdinalIgnoreCase);

	public bool ErrorActive { get; set; }

	public string? MachineState { get; set; }

	public string? ErrorMessage { get; set; }
}

public interface ISignalRecorder
{
	void BeginRun(string runId);

	void Record(PhysicalMachineSession session, TimeSpan simulationTime);

	IReadOnlyList<SignalSample> GetSamples(string runId);

	void CompleteRun();
}
