using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Registry;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalSignalRegistryTests
{
	[Fact]
	public void Registry_SeparatesMachineInstances()
	{
		PhysicalSignalNodeRegistry physicalSignalNodeRegistry = new PhysicalSignalNodeRegistry();
		PhysicalSignalNodeRegistry physicalSignalNodeRegistry2 = new PhysicalSignalNodeRegistry();
		Assert.Equal(0, physicalSignalNodeRegistry.Count);
		Assert.Equal(0, physicalSignalNodeRegistry2.Count);
	}

	[Fact]
	public void Registry_Clear_RemovesAllEntries()
	{
		PhysicalSignalNodeRegistry physicalSignalNodeRegistry = new PhysicalSignalNodeRegistry();
		physicalSignalNodeRegistry.Clear();
		Assert.Equal(0, physicalSignalNodeRegistry.Count);
	}
}
