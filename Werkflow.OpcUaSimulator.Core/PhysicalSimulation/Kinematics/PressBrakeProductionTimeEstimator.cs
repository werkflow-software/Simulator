using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class PressBrakeProductionTimeEstimator
{
	public static (double partRemainingSeconds, double jobRemainingSeconds) EstimateRemaining(
		PhysicalSimulationContext simulation,
		int seed)
	{
		PressBrakeKinematicsState pressBrake = simulation.PressBrake;
		if (!pressBrake.IsEnabled || pressBrake.ActiveProgram == null)
		{
			return (0.0, 0.0);
		}

		PressBrakePartDefinition? currentPart = GetCurrentPart(pressBrake);
		double currentPartRemaining = EstimateCurrentPartRemainingSeconds(pressBrake, currentPart, seed);
		int remainingParts = Math.Max(0, pressBrake.TargetParts - pressBrake.ProducedParts);
		double averagePart = EstimateAveragePartSeconds(pressBrake.ActiveProgram);
		double jobRemaining = currentPartRemaining + Math.Max(0, remainingParts - 1) * averagePart;
		return (currentPartRemaining, jobRemaining);
	}

	private static double EstimateCurrentPartRemainingSeconds(
		PressBrakeKinematicsState pressBrake,
		PressBrakePartDefinition? part,
		int seed)
	{
		if (part == null)
		{
			return 0.0;
		}

		double remaining = PressBrakeKinematicsEngine.GetPhaseRemainingSeconds(pressBrake, seed);
		for (int stepIndex = pressBrake.BendStepIndex + 1; stepIndex < part.BendSteps.Count; stepIndex++)
		{
			PressBrakeBendStepDefinition step = part.BendSteps[stepIndex];
			remaining += step.ApproachDurationSeconds
				+ step.FormingDurationSeconds
				+ step.HoldDurationSeconds
				+ step.ReturnDurationSeconds
				+ step.InterStepWaitSeconds;
		}

		return remaining;
	}

	private static double EstimateAveragePartSeconds(PressBrakeProgramDefinition program)
	{
		if (program.Parts.Count == 0)
		{
			return 30.0;
		}

		double total = 0.0;
		foreach (PressBrakePartDefinition part in program.Parts)
		{
			double bendSeconds = part.BendSteps.Sum(step =>
				step.ApproachDurationSeconds
				+ step.FormingDurationSeconds
				+ step.HoldDurationSeconds
				+ step.ReturnDurationSeconds
				+ step.InterStepWaitSeconds);
			total += bendSeconds + part.InterPartWaitSeconds;
		}

		return total / program.Parts.Count;
	}

	private static PressBrakePartDefinition? GetCurrentPart(PressBrakeKinematicsState pressBrake) =>
		pressBrake.ActiveProgram != null && pressBrake.ActiveProgram.Parts.Count > 0
			? pressBrake.ActiveProgram.Parts[pressBrake.PartIndex % pressBrake.ActiveProgram.Parts.Count]
			: null;
}
