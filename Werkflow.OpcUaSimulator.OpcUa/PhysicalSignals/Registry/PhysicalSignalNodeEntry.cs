using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

public sealed class PhysicalSignalNodeEntry
{
	public required string SignalId { get; init; }

	public required string NodeIdPath { get; init; }

	public required BaseDataVariableState Variable { get; init; }

	public required SignalDefinition Definition { get; init; }
}
