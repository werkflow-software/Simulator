using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalMachineRuntime
{
	public required string ProfileId { get; init; }

	public required string ProfileVersion { get; init; }

	public DateTimeOffset CreatedAt { get; init; }

	public IReadOnlyList<SignalRuntimeState> Signals { get; init; } = Array.Empty<SignalRuntimeState>();

	public IReadOnlyList<HiddenProcessRuntimeState> HiddenProcessStates { get; init; } = Array.Empty<HiddenProcessRuntimeState>();
}
