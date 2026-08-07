using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Nodes;

public interface IPhysicalSignalNodeFactory
{
	PhysicalSignalNodeEntry CreateVariable(FolderState parent, SignalDefinition signal, ushort namespaceIndex, object? initialValue);
}
