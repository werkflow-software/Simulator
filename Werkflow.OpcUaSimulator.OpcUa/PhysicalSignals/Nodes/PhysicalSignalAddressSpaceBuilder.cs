using System;
using System.Collections.Generic;
using System.Linq;
using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.Utilities;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Nodes;

public sealed class PhysicalSignalAddressSpaceBuilder
{
	private readonly IPhysicalSignalNodeFactory _nodeFactory;

	public PhysicalSignalAddressSpaceBuilder(IPhysicalSignalNodeFactory nodeFactory)
	{
		_nodeFactory = nodeFactory;
	}

	public int Build(ISystemContext context, FolderState simulationRoot, ushort namespaceIndex, PhysicalMachineProfile profile, PhysicalMachineRuntime runtime, IPhysicalSignalNodeRegistry registry, Action<NodeState> registerNode)
	{
		Dictionary<string, FolderState> cache = new Dictionary<string, FolderState>(StringComparer.OrdinalIgnoreCase) { ["Simulation"] = simulationRoot };
		FolderState parent = EnsureFolder(simulationRoot, cache, namespaceIndex, "Machine", "Machine", registerNode);
		FolderState folderState = EnsureFolder(parent, cache, namespaceIndex, "Simulation.Machine.Physical", "Physical", registerNode);
		int num = 0;
		foreach (SignalDefinition signal in profile.Signals.Where((SignalDefinition s) => s.IsEnabled).OrderBy<SignalDefinition, string>((SignalDefinition s) => s.SignalId, StringComparer.Ordinal))
		{
			IReadOnlyList<string> readOnlyList = NodeIdParser.ParsePath(signal.NodeId);
			if (readOnlyList.Count != 0)
			{
				FolderState folderState2 = folderState;
				string text = "Simulation.Machine.Physical";
				for (int i = 0; i < readOnlyList.Count - 1; i++)
				{
					text = text + "." + readOnlyList[i];
					folderState2 = EnsureFolder(folderState2, cache, namespaceIndex, text, readOnlyList[i], registerNode);
				}
				SignalRuntimeState state = runtime.Signals.First((SignalRuntimeState s) => s.SignalId == signal.SignalId);
				PhysicalSignalNodeEntry physicalSignalNodeEntry = _nodeFactory.CreateVariable(folderState2, signal, namespaceIndex, SignalRuntimeValueHelper.GetCurrentValue(signal, state));
				folderState2.AddChild(physicalSignalNodeEntry.Variable);
				registerNode(physicalSignalNodeEntry.Variable);
				registry.Register(physicalSignalNodeEntry);
				num++;
			}
		}
		return num;
	}

	private static FolderState EnsureFolder(FolderState parent, Dictionary<string, FolderState> cache, ushort namespaceIndex, string cacheKey, string browseName, Action<NodeState> registerNode)
	{
		if (cache.TryGetValue(cacheKey, out FolderState value))
		{
			return value;
		}
		FolderState folderState = new FolderState(parent)
		{
			SymbolicName = browseName,
			ReferenceTypeId = 35u,
			TypeDefinitionId = ObjectTypeIds.FolderType,
			NodeId = new NodeId(cacheKey.Replace("Simulation.", string.Empty), namespaceIndex),
			BrowseName = new QualifiedName(browseName, namespaceIndex),
			DisplayName = new LocalizedText(browseName),
			WriteMask = AttributeWriteMask.None,
			UserWriteMask = AttributeWriteMask.None,
			EventNotifier = 0
		};
		parent.AddChild(folderState);
		registerNode(folderState);
		cache[cacheKey] = folderState;
		return folderState;
	}
}
