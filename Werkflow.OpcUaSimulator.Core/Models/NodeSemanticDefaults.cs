using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.Models;

public static class NodeSemanticDefaults
{
	public static IReadOnlyList<NodeMapping> CreateDefaultMappings()
	{
		return new _003C_003Ez__ReadOnlyArray<NodeMapping>(new NodeMapping[9]
		{
			new NodeMapping
			{
				SemanticType = NodeSemanticType.PartName,
				DisplayName = "PartName",
				BrowseName = "PartName",
				NodeId = "Machine.PartName",
				DataType = OpcUaDataType.String,
				InitialValue = "—",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.JobName,
				DisplayName = "JobName",
				BrowseName = "JobName",
				NodeId = "Machine.JobName",
				DataType = OpcUaDataType.String,
				InitialValue = "—",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.ErrorActive,
				DisplayName = "ErrorActive",
				BrowseName = "ErrorActive",
				NodeId = "Machine.ErrorActive",
				DataType = OpcUaDataType.Boolean,
				InitialValue = "false",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.ErrorMessage,
				DisplayName = "ErrorMessage",
				BrowseName = "ErrorMessage",
				NodeId = "Machine.ErrorMessage",
				DataType = OpcUaDataType.String,
				InitialValue = "",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.ActualCounter,
				DisplayName = "ActualCounter",
				BrowseName = "ActualCounter",
				NodeId = "Machine.ActualCounter",
				DataType = OpcUaDataType.Int32,
				InitialValue = "0",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.TargetCounter,
				DisplayName = "TargetCounter",
				BrowseName = "TargetCounter",
				NodeId = "Machine.TargetCounter",
				DataType = OpcUaDataType.Int32,
				InitialValue = "100",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.MachineState,
				DisplayName = "MachineState",
				BrowseName = "MachineState",
				NodeId = "Machine.MachineState",
				DataType = OpcUaDataType.Int32,
				InitialValue = "1",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.Heartbeat,
				DisplayName = "Heartbeat",
				BrowseName = "Heartbeat",
				NodeId = "Machine.Heartbeat",
				DataType = OpcUaDataType.UInt64,
				InitialValue = "0",
				IsEnabled = true
			},
			new NodeMapping
			{
				SemanticType = NodeSemanticType.LastProductionChange,
				DisplayName = "LastProductionChange",
				BrowseName = "LastProductionChange",
				NodeId = "Machine.LastProductionChange",
				DataType = OpcUaDataType.DateTime,
				InitialValue = DateTime.UtcNow.ToString("O"),
				IsEnabled = true
			}
		});
	}

	public static string GetSemanticLabel(NodeSemanticType type)
	{
		if (1 == 0)
		{
		}
		string result = type switch
		{
			NodeSemanticType.PartName => "Teilename", 
			NodeSemanticType.JobName => "Jobname", 
			NodeSemanticType.ErrorActive => "Fehler aktiv", 
			NodeSemanticType.ErrorMessage => "Fehlermeldung", 
			NodeSemanticType.ActualCounter => "Istzähler", 
			NodeSemanticType.TargetCounter => "Sollzähler", 
			NodeSemanticType.MachineState => "Maschinenstatus", 
			NodeSemanticType.Heartbeat => "Verbindungssignal", 
			NodeSemanticType.LastProductionChange => "Produktionsfortschritt", 
			_ => type.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static OpcUaDataType GetDefaultDataType(NodeSemanticType type)
	{
		if (1 == 0)
		{
		}
		OpcUaDataType result;
		switch (type)
		{
		case NodeSemanticType.PartName:
		case NodeSemanticType.JobName:
		case NodeSemanticType.ErrorMessage:
			result = OpcUaDataType.String;
			break;
		case NodeSemanticType.ErrorActive:
			result = OpcUaDataType.Boolean;
			break;
		case NodeSemanticType.ActualCounter:
		case NodeSemanticType.TargetCounter:
		case NodeSemanticType.MachineState:
			result = OpcUaDataType.Int32;
			break;
		case NodeSemanticType.Heartbeat:
			result = OpcUaDataType.UInt64;
			break;
		case NodeSemanticType.LastProductionChange:
			result = OpcUaDataType.DateTime;
			break;
		default:
			result = OpcUaDataType.String;
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}
}
