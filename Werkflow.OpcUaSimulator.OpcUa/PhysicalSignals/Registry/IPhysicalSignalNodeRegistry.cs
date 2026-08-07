using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

public interface IPhysicalSignalNodeRegistry
{
	int Count { get; }

	void Register(PhysicalSignalNodeEntry entry);

	bool TryGetBySignalId(string signalId, out PhysicalSignalNodeEntry entry);

	bool TryGetByNodeId(string nodeIdPath, out PhysicalSignalNodeEntry entry);

	IReadOnlyList<PhysicalSignalNodeEntry> GetAll();

	void Clear();
}
