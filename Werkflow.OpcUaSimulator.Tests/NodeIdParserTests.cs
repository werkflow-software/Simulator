using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Utilities;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class NodeIdParserTests
{
	[Fact]
	public void ParsePath_SplitsHierarchy()
	{
		IReadOnlyList<string> actual = NodeIdParser.ParsePath("Production.CurrentPart");
		Assert.Equal(new[] { "Production", "CurrentPart" }, actual);
	}

	[Fact]
	public void ValidateNodeMappings_DetectsDuplicates()
	{
		List<NodeMapping> list = (from n in NodeSemanticDefaults.CreateDefaultMappings()
			select n.Clone()).ToList();
		list[1].NodeId = list[0].NodeId;
		ValidationResult validationResult = NodeIdParser.ValidateNodeMappings(list);
		Assert.False(validationResult.IsValid);
		Assert.Contains((IEnumerable<string>)validationResult.Errors, (Predicate<string>)((string e) => e.Contains("Doppelte")));
	}
}
