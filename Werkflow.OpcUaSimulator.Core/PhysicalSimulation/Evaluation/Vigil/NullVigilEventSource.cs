using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;

public sealed class NullVigilEventSource : IVigilEventSource
{
	public bool IsConnected => false;

	public IReadOnlyList<VigilEvent> GetEvents(string experimentId) => [];
}
