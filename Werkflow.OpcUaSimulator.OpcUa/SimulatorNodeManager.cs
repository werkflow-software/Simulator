using System;
using System.Collections.Generic;
using System.Linq;
using Opc.Ua;
using Opc.Ua.Server;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Utilities;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.OpcUa;

internal sealed class SimulatorNodeManager : CustomNodeManager2
{
	private readonly MachineConfiguration _machine;

	private readonly MachineRuntimeState _runtime;

	private readonly ILogService _logService;

	private readonly PhysicalSignalPublishingCoordinator? _physicalCoordinator;

	private readonly Dictionary<NodeSemanticType, BaseDataVariableState> _variables = new Dictionary<NodeSemanticType, BaseDataVariableState>();

	private readonly Dictionary<NodeSemanticType, object?> _liveValues = new Dictionary<NodeSemanticType, object>();

	private readonly List<NodeState> _registeredNodes = new List<NodeState>();

	public ushort CustomNamespaceIndex => base.NamespaceIndex;

	public SimulatorNodeManager(IServerInternal server, ApplicationConfiguration configuration, MachineConfiguration machine, MachineRuntimeState runtime, ILogService logService, PhysicalSignalPublishingCoordinator? physicalCoordinator = null)
		: base(server, configuration, machine.NamespaceUri)
	{
		_machine = machine;
		_runtime = runtime;
		_logService = logService;
		_physicalCoordinator = physicalCoordinator;
	}

	public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
	{
		lock (base.Lock)
		{
			base.Server.NamespaceUris.GetIndexOrAppend(NamespaceUris.First());
			FolderState folderState = CreateFolder(null, "Simulation", "Simulation");
			folderState.AddReference(35u, isInverse: true, ObjectIds.ObjectsFolder);
			if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference> value))
			{
				value = (externalReferences[ObjectIds.ObjectsFolder] = new List<IReference>());
			}
			value.Add(new NodeStateReference(35u, isInverse: false, folderState.NodeId));
			RegisterNode(folderState);
			Dictionary<string, FolderState> dictionary = new Dictionary<string, FolderState>(StringComparer.OrdinalIgnoreCase) { ["Simulation"] = folderState };
			foreach (NodeMapping item in _machine.Nodes.Where((NodeMapping n) => n.IsEnabled))
			{
				IReadOnlyList<string> readOnlyList = NodeIdParser.ParsePath(item.NodeId);
				if (readOnlyList.Count == 0)
				{
					continue;
				}
				FolderState folderState2 = folderState;
				string text = "Simulation";
				for (int i = 0; i < readOnlyList.Count - 1; i++)
				{
					text = text + "." + readOnlyList[i];
					if (!dictionary.TryGetValue(text, out var value2))
					{
						value2 = CreateFolder(folderState2, readOnlyList[i], readOnlyList[i]);
						folderState2.AddChild(value2);
						RegisterNode(value2);
						dictionary[text] = value2;
					}
					folderState2 = value2;
				}
				string browseName = readOnlyList[readOnlyList.Count - 1];
				BaseDataVariableState baseDataVariableState = CreateVariable(folderState2, item, browseName);
				folderState2.AddChild(baseDataVariableState);
				RegisterNode(baseDataVariableState);
				_variables[item.SemanticType] = baseDataVariableState;
				_liveValues[item.SemanticType] = baseDataVariableState.Value;
			}
			_physicalCoordinator?.BuildAddressSpace(_machine.Id, base.SystemContext, folderState, CustomNamespaceIndex, RegisterNode);
			AddReverseReferences(externalReferences);
			ApplyRuntimeValues();
		}
	}

	public void PublishAll(MachineRuntimeState state, IReadOnlyList<NodeMapping> nodes)
	{
		PublishValue(NodeSemanticType.PartName, state.PartName, nodes);
		PublishValue(NodeSemanticType.JobName, state.JobName, nodes);
		PublishValue(NodeSemanticType.ErrorActive, state.ErrorActive, nodes);
		PublishValue(NodeSemanticType.ErrorMessage, state.ErrorMessage, nodes);
		PublishValue(NodeSemanticType.ActualCounter, state.ActualCounter, nodes);
		PublishValue(NodeSemanticType.TargetCounter, state.TargetCounter, nodes);
		PublishValue(NodeSemanticType.MachineState, (int)state.State, nodes);
		PublishValue(NodeSemanticType.Heartbeat, state.Heartbeat, nodes);
		PublishValue(NodeSemanticType.LastProductionChange, state.LastProductionChange, nodes);
	}

	public void PublishValue(NodeSemanticType semanticType, object? value, IReadOnlyList<NodeMapping> nodes)
	{
		NodeMapping nodeMapping = nodes.FirstOrDefault((NodeMapping n) => n.SemanticType == semanticType && n.IsEnabled);
		if (nodeMapping == null)
		{
			return;
		}
		lock (base.Lock)
		{
			_liveValues[semanticType] = value;
			_runtime.LiveNodeValues[semanticType] = value;
			if (_variables.TryGetValue(semanticType, out BaseDataVariableState value2))
			{
				SetVariableValue(value2, ConvertValue(value, nodeMapping.DataType));
			}
		}
	}

	public object? GetLiveValue(NodeSemanticType semanticType)
	{
		lock (base.Lock)
		{
			object value;
			return _liveValues.TryGetValue(semanticType, out value) ? value : null;
		}
	}

	private void RegisterNode(NodeState node)
	{
		AddPredefinedNode(base.SystemContext, node);
		_registeredNodes.Add(node);
	}

	private void ApplyRuntimeValues()
	{
		foreach (NodeMapping item in _machine.Nodes.Where((NodeMapping n) => n.IsEnabled))
		{
			if (_variables.TryGetValue(item.SemanticType, out BaseDataVariableState value))
			{
				NodeSemanticType semanticType = item.SemanticType;
				if (1 == 0)
				{
				}
				object obj = semanticType switch
				{
					NodeSemanticType.PartName => _runtime.PartName, 
					NodeSemanticType.JobName => _runtime.JobName, 
					NodeSemanticType.ErrorActive => _runtime.ErrorActive, 
					NodeSemanticType.ErrorMessage => _runtime.ErrorMessage, 
					NodeSemanticType.ActualCounter => _runtime.ActualCounter, 
					NodeSemanticType.TargetCounter => _runtime.TargetCounter, 
					NodeSemanticType.MachineState => (int)_runtime.State, 
					NodeSemanticType.Heartbeat => _runtime.Heartbeat, 
					NodeSemanticType.LastProductionChange => _runtime.LastProductionChange, 
					_ => ParseInitialValue(item), 
				};
				if (1 == 0)
				{
				}
				object value2 = obj;
				SetVariableValue(value, ConvertValue(value2, item.DataType));
			}
		}
	}

	private void SetVariableValue(BaseDataVariableState variable, object? value)
	{
		variable.Value = value;
		variable.Timestamp = DateTime.UtcNow;
		variable.StatusCode = 0u;
		variable.ClearChangeMasks(base.SystemContext, includeChildren: false);
	}

	private FolderState CreateFolder(NodeState? parent, string browseName, string displayName)
	{
		return new FolderState(parent)
		{
			SymbolicName = browseName,
			ReferenceTypeId = 35u,
			TypeDefinitionId = ObjectTypeIds.FolderType,
			NodeId = new NodeId(browseName, CustomNamespaceIndex),
			BrowseName = new QualifiedName(browseName, CustomNamespaceIndex),
			DisplayName = new LocalizedText(displayName),
			WriteMask = AttributeWriteMask.None,
			UserWriteMask = AttributeWriteMask.None,
			EventNotifier = 0
		};
	}

	private BaseDataVariableState CreateVariable(FolderState parent, NodeMapping mapping, string browseName)
	{
		NodeId nodeId = new NodeId(mapping.NodeId, CustomNamespaceIndex);
		NodeId opcDataType = GetOpcDataType(mapping.DataType);
		object value = ParseInitialValue(mapping);
		return new BaseDataVariableState(parent)
		{
			SymbolicName = mapping.BrowseName,
			ReferenceTypeId = 47u,
			TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
			NodeId = nodeId,
			BrowseName = new QualifiedName(mapping.BrowseName, CustomNamespaceIndex),
			DisplayName = new LocalizedText(mapping.DisplayName),
			DataType = opcDataType,
			ValueRank = -1,
			AccessLevel = 1,
			UserAccessLevel = 1,
			Historizing = false,
			Value = value,
			Timestamp = DateTime.UtcNow,
			StatusCode = 0u
		};
	}

	private static NodeId GetOpcDataType(OpcUaDataType dataType)
	{
		if (1 == 0)
		{
		}
		NodeId result = dataType switch
		{
			OpcUaDataType.String => DataTypeIds.String, 
			OpcUaDataType.Boolean => DataTypeIds.Boolean, 
			OpcUaDataType.Int32 => DataTypeIds.Int32, 
			OpcUaDataType.UInt64 => DataTypeIds.UInt64, 
			OpcUaDataType.DateTime => DataTypeIds.DateTime, 
			_ => DataTypeIds.String, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static object? ParseInitialValue(NodeMapping mapping)
	{
		OpcUaDataType dataType = mapping.DataType;
		if (1 == 0)
		{
		}
		bool result;
		int result2;
		ulong result3;
		DateTime result4;
		object result5 = dataType switch
		{
			OpcUaDataType.String => mapping.InitialValue, 
			OpcUaDataType.Boolean => bool.TryParse(mapping.InitialValue, out result) && result, 
			OpcUaDataType.Int32 => int.TryParse(mapping.InitialValue, out result2) ? result2 : 0, 
			OpcUaDataType.UInt64 => ulong.TryParse(mapping.InitialValue, out result3) ? result3 : 0, 
			OpcUaDataType.DateTime => DateTime.TryParse(mapping.InitialValue, out result4) ? result4.ToUniversalTime() : DateTime.UtcNow, 
			_ => mapping.InitialValue, 
		};
		if (1 == 0)
		{
		}
		return result5;
	}

	private static object? ConvertValue(object? value, OpcUaDataType dataType)
	{
		if (value == null)
		{
			return null;
		}
		if (1 == 0)
		{
		}
		object result = dataType switch
		{
			OpcUaDataType.String => value.ToString(), 
			OpcUaDataType.Boolean => Convert.ToBoolean(value), 
			OpcUaDataType.Int32 => Convert.ToInt32(value), 
			OpcUaDataType.UInt64 => Convert.ToUInt64(value), 
			OpcUaDataType.DateTime => (value is DateTime dateTime) ? dateTime.ToUniversalTime() : DateTime.Parse(value.ToString()).ToUniversalTime(), 
			_ => value, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
