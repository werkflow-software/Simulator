using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalMachineSession
{
	public required Guid MachineId { get; init; }

	public required string MachineName { get; init; }

	public required PhysicalMachineProfile Profile { get; init; }

	public required PhysicalMachineRuntime Runtime { get; init; }

	public PhysicalPublisherMetrics Metrics { get; } = new PhysicalPublisherMetrics();

	public PhysicalSimulationContext Simulation { get; } = new PhysicalSimulationContext();

	public int OpcUaNodeCount { get; set; }

	public bool IsPublisherManualOverride { get; set; }
}
