using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public sealed class FaultScenarioEventHub : IFaultScenarioEventSink
{
	public event EventHandler<FaultScenarioEvent>? EventPublished;

	public void Publish(FaultScenarioEvent eventArgs)
	{
		EventPublished?.Invoke(this, eventArgs);
	}
}
