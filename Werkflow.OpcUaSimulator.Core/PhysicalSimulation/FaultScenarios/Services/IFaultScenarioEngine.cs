using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public interface IFaultScenarioEngine
{
	void Tick(PhysicalMachineSession session, TimeSpan deltaTime, IFaultScenarioSimulationBridge? bridge);

	void EvaluateThresholdsAfterSignals(PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge);

	void ApplySignalOverrides(PhysicalMachineSession session, IFaultScenarioSimulationBridge? bridge);
}
