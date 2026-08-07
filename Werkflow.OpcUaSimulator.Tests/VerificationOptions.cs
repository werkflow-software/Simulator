using System;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class VerificationOptions
{
	public int MachineCount { get; init; } = 1;

	public TimeSpan PublishDuration { get; init; } = TimeSpan.FromMinutes(10.0);

	public int Seed { get; init; } = 42;

	public bool TestPauseResume { get; init; }

	public bool TestDataChange { get; init; }

	public bool TestMachineIsolation { get; init; }

	public int TestStopRestartCycles { get; init; }
}
