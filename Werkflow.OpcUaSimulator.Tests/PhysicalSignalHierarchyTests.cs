using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Utilities;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalSignalHierarchyTests
{
	[Fact]
	public void NodeIdPath_BuildsStableHierarchySegments()
	{
		IReadOnlyList<string> actual = NodeIdParser.ParsePath("Axis01.MotorTemperature");
		Assert.Equal(new[] { "Axis01", "MotorTemperature" }, actual);
	}

	[Fact]
	public void NodeIdPath_DeepHierarchy_IsStable()
	{
		IReadOnlyList<string> actual = NodeIdParser.ParsePath("Cooling.PrimaryCircuit.Flow");
		Assert.Equal(new[] { "Cooling", "PrimaryCircuit", "Flow" }, actual);
	}
}
