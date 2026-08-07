using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public static class NodeMappingPresets
{
	public static IReadOnlyList<NodeMappingPresetInfo> All { get; } = new _003C_003Ez__ReadOnlyArray<NodeMappingPresetInfo>(new NodeMappingPresetInfo[4]
	{
		new NodeMappingPresetInfo("standard", "Standard (Machine.*)", CreateStandard),
		new NodeMappingPresetInfo("production", "Produktion (Production.*)", CreateProductionStyle),
		new NodeMappingPresetInfo("line", "Linie (Line1.*)", CreateLineStyle),
		new NodeMappingPresetInfo("vendor", "Anlage (Plant.*)", CreatePlantStyle)
	});

	public static List<NodeMapping> CreateStandard()
	{
		return (from n in NodeSemanticDefaults.CreateDefaultMappings()
			select n.Clone()).ToList();
	}

	public static List<NodeMapping> CreateProductionStyle()
	{
		return CreateFromPairs(new(NodeSemanticType, string, string, string)[9]
		{
			(NodeSemanticType.PartName, "CurrentPart", "CurrentPart", "Production.CurrentPart"),
			(NodeSemanticType.JobName, "OrderName", "OrderName", "Production.OrderName"),
			(NodeSemanticType.ErrorActive, "FaultActive", "FaultActive", "Production.FaultActive"),
			(NodeSemanticType.ErrorMessage, "FaultText", "FaultText", "Production.FaultText"),
			(NodeSemanticType.ActualCounter, "PiecesDone", "PiecesDone", "Production.PiecesDone"),
			(NodeSemanticType.TargetCounter, "PiecesTarget", "PiecesTarget", "Production.PiecesTarget"),
			(NodeSemanticType.MachineState, "MachineStatus", "MachineStatus", "Production.MachineStatus"),
			(NodeSemanticType.Heartbeat, "Alive", "Alive", "Production.Alive"),
			(NodeSemanticType.LastProductionChange, "LastPieceTime", "LastPieceTime", "Production.LastPieceTime")
		});
	}

	public static List<NodeMapping> CreateLineStyle()
	{
		return CreateFromPairs(new(NodeSemanticType, string, string, string)[9]
		{
			(NodeSemanticType.PartName, "Part", "Part", "Line1.Part"),
			(NodeSemanticType.JobName, "WorkOrder", "WorkOrder", "Line1.WorkOrder"),
			(NodeSemanticType.ErrorActive, "Error", "Error", "Line1.Error"),
			(NodeSemanticType.ErrorMessage, "ErrorText", "ErrorText", "Line1.ErrorText"),
			(NodeSemanticType.ActualCounter, "CountActual", "CountActual", "Line1.CountActual"),
			(NodeSemanticType.TargetCounter, "CountTarget", "CountTarget", "Line1.CountTarget"),
			(NodeSemanticType.MachineState, "State", "State", "Line1.State"),
			(NodeSemanticType.Heartbeat, "LinkOk", "LinkOk", "Line1.LinkOk"),
			(NodeSemanticType.LastProductionChange, "LastCountChange", "LastCountChange", "Line1.LastCountChange")
		});
	}

	public static List<NodeMapping> CreatePlantStyle()
	{
		return CreateFromPairs(new(NodeSemanticType, string, string, string)[9]
		{
			(NodeSemanticType.PartName, "Product", "Product", "Plant.Unit.Product"),
			(NodeSemanticType.JobName, "Batch", "Batch", "Plant.Unit.Batch"),
			(NodeSemanticType.ErrorActive, "AlarmActive", "AlarmActive", "Plant.Unit.AlarmActive"),
			(NodeSemanticType.ErrorMessage, "AlarmMessage", "AlarmMessage", "Plant.Unit.AlarmMessage"),
			(NodeSemanticType.ActualCounter, "ActualQty", "ActualQty", "Plant.Unit.ActualQty"),
			(NodeSemanticType.TargetCounter, "TargetQty", "TargetQty", "Plant.Unit.TargetQty"),
			(NodeSemanticType.MachineState, "RunState", "RunState", "Plant.Unit.RunState"),
			(NodeSemanticType.Heartbeat, "CommOk", "CommOk", "Plant.Unit.CommOk"),
			(NodeSemanticType.LastProductionChange, "LastOutput", "LastOutput", "Plant.Unit.LastOutput")
		});
	}

	public static List<NodeMapping> GetById(string presetId)
	{
		return All.FirstOrDefault((NodeMappingPresetInfo p) => p.Id == presetId)?.Factory() ?? CreateStandard();
	}

	public static List<NodeMapping> GetDefaultForMachine(int machineIndex)
	{
		if (1 == 0)
		{
		}
		List<NodeMapping> result = machineIndex switch
		{
			1 => CreateStandard(), 
			2 => CreateProductionStyle(), 
			3 => CreateLineStyle(), 
			4 => CreatePlantStyle(), 
			_ => CreateStandard(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static List<NodeMapping> CreateFromPairs((NodeSemanticType Semantic, string Display, string Browse, string NodeId)[] pairs)
	{
		Dictionary<NodeSemanticType, NodeMapping> defaults = NodeSemanticDefaults.CreateDefaultMappings().ToDictionary((NodeMapping n) => n.SemanticType);
		return pairs.Select(delegate((NodeSemanticType Semantic, string Display, string Browse, string NodeId) p)
		{
			NodeMapping nodeMapping = defaults[p.Semantic].Clone();
			nodeMapping.DisplayName = p.Display;
			nodeMapping.BrowseName = p.Browse;
			nodeMapping.NodeId = p.NodeId;
			return nodeMapping;
		}).ToList();
	}
}
