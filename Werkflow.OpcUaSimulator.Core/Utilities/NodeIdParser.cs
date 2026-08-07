using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Utilities;

public static class NodeIdParser
{
	public static IReadOnlyList<string> ParsePath(string nodeId)
	{
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			return Array.Empty<string>();
		}
		return nodeId.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	public static ValidationResult ValidateNodeMappings(IEnumerable<NodeMapping> nodes)
	{
		ValidationResult validationResult = new ValidationResult();
		List<NodeMapping> list = nodes.Where((NodeMapping n) => n.IsEnabled).ToList();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NodeMapping item in list)
		{
			if (string.IsNullOrWhiteSpace(item.NodeId))
			{
				validationResult.AddError("Node '" + NodeSemanticDefaults.GetSemanticLabel(item.SemanticType) + "': NodeId fehlt.");
				continue;
			}
			if (string.IsNullOrWhiteSpace(item.BrowseName))
			{
				validationResult.AddError("Node '" + item.NodeId + "': BrowseName fehlt.");
			}
			if (!hashSet.Add(item.NodeId))
			{
				validationResult.AddError("Doppelte NodeId: '" + item.NodeId + "'.");
			}
		}
		return validationResult;
	}
}
