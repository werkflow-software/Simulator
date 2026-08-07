using System;
using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Nodes;

public sealed class PhysicalSignalNodeFactory : IPhysicalSignalNodeFactory
{
	private readonly IPhysicalSignalTypeMapper _typeMapper;

	public PhysicalSignalNodeFactory(IPhysicalSignalTypeMapper typeMapper)
	{
		_typeMapper = typeMapper;
	}

	public PhysicalSignalNodeEntry CreateVariable(FolderState parent, SignalDefinition signal, ushort namespaceIndex, object? initialValue)
	{
		object value = _typeMapper.ConvertToOpcValue(signal.DataType, initialValue);
		string text = (string.IsNullOrWhiteSpace(signal.EngineeringUnit) ? signal.DisplayName : (signal.DisplayName + " [" + signal.EngineeringUnit + "]"));
		BaseDataVariableState variable = new BaseDataVariableState(parent)
		{
			SymbolicName = signal.BrowseName,
			ReferenceTypeId = 47u,
			TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
			NodeId = new NodeId(signal.NodeId, namespaceIndex),
			BrowseName = new QualifiedName(signal.BrowseName, namespaceIndex),
			DisplayName = new LocalizedText(text),
			Description = new LocalizedText(signal.Description),
			DataType = _typeMapper.MapDataType(signal.DataType),
			ValueRank = -1,
			AccessLevel = 1,
			UserAccessLevel = 1,
			Historizing = false,
			Value = value,
			Timestamp = DateTime.UtcNow,
			StatusCode = 0u
		};
		return new PhysicalSignalNodeEntry
		{
			SignalId = signal.SignalId,
			NodeIdPath = signal.NodeId,
			Variable = variable,
			Definition = signal
		};
	}
}
