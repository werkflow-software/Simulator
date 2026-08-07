using System;
using System.Collections.Generic;
using System.Linq;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

public sealed class PhysicalSignalNodeRegistry : IPhysicalSignalNodeRegistry
{
	private readonly Dictionary<string, PhysicalSignalNodeEntry> _bySignalId = new Dictionary<string, PhysicalSignalNodeEntry>(StringComparer.Ordinal);

	private readonly Dictionary<string, PhysicalSignalNodeEntry> _byNodeId = new Dictionary<string, PhysicalSignalNodeEntry>(StringComparer.Ordinal);

	public int Count => _bySignalId.Count;

	public void Register(PhysicalSignalNodeEntry entry)
	{
		if (_bySignalId.ContainsKey(entry.SignalId))
		{
			throw new InvalidOperationException("Signal '" + entry.SignalId + "' ist bereits registriert.");
		}
		if (_byNodeId.ContainsKey(entry.NodeIdPath))
		{
			throw new InvalidOperationException("NodeId '" + entry.NodeIdPath + "' ist bereits registriert.");
		}
		_bySignalId[entry.SignalId] = entry;
		_byNodeId[entry.NodeIdPath] = entry;
	}

	public bool TryGetBySignalId(string signalId, out PhysicalSignalNodeEntry entry)
	{
		return _bySignalId.TryGetValue(signalId, out entry);
	}

	public bool TryGetByNodeId(string nodeIdPath, out PhysicalSignalNodeEntry entry)
	{
		return _byNodeId.TryGetValue(nodeIdPath, out entry);
	}

	public IReadOnlyList<PhysicalSignalNodeEntry> GetAll()
	{
		return _bySignalId.Values.ToList();
	}

	public void Clear()
	{
		_bySignalId.Clear();
		_byNodeId.Clear();
	}
}
