using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public sealed class FaultScenarioRuntimeFactory : IFaultScenarioRuntimeFactory
{
	public FaultScenarioInstance CreateInstance(FaultScenarioDefinition definition, FaultScenarioStartRequest request, int baseSeed)
	{
		int seed = request.Seed ?? (baseSeed ^ definition.ScenarioId.GetHashCode());
		Random random = new Random(seed);
		double num = 1.0 + (random.NextDouble() - 0.5) * 0.08;
		double num2 = 1.0 + (random.NextDouble() - 0.5) * 0.1;
		return new FaultScenarioInstance
		{
			MachineId = request.MachineId,
			ScenarioId = definition.ScenarioId,
			Definition = definition,
			LifecycleState = FaultScenarioLifecycleState.Starting,
			CurrentPhase = FaultScenarioPhase.Initiating,
			RunMode = request.RunMode,
			Intensity = Math.Clamp(request.Intensity * num, definition.MinimumIntensity, definition.MaximumIntensity),
			TimeFactor = ((request.TimeFactor > 0.0) ? request.TimeFactor : 1.0),
			Seed = seed,
			AutoThresholdFaultEnabled = request.AutoThresholdFaultEnabled,
			AutoScenarioEndEnabled = request.AutoScenarioEndEnabled,
			StartedAt = DateTimeOffset.UtcNow
		};
	}
}
