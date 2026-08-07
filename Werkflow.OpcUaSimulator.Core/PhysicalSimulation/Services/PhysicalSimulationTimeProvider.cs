using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class PhysicalSimulationTimeProvider : IPhysicalTimeProvider
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

	public TimeSpan ElapsedSince(DateTimeOffset since)
	{
		return UtcNow - since;
	}
}
