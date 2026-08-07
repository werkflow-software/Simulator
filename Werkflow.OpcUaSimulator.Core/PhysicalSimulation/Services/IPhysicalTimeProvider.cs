using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IPhysicalTimeProvider
{
	DateTimeOffset UtcNow { get; }

	TimeSpan ElapsedSince(DateTimeOffset since);
}
