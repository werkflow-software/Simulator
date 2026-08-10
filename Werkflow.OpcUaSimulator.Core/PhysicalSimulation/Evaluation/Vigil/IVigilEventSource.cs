using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;

public interface IVigilEventSource
{
	bool IsConnected { get; }

	IReadOnlyList<VigilEvent> GetEvents(string experimentId);
}
