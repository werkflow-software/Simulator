using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

/// <summary>
/// Maps internal press-brake simulation state to OPC-exposed categorical tokens without
/// encoding MotionPhase identity, BendStepIndex, or ground-truth semantics.
/// </summary>
public static class PressBrakeExposedSignalSemantics
{
	/// <summary>
	/// Stamps <see cref="PressBrakeKinematicsState.LastProductionChangeUtc"/> on genuine
	/// production-context changes (aligned with legacy Laser counter-increment semantics).
	/// </summary>
	public static void StampLastProductionChange(PressBrakeKinematicsState pressBrake) =>
		pressBrake.LastProductionChangeUtc = DateTime.UtcNow;

	public static int ResolveMachineStateIndex(
		PressBrakeMotionPhase phase,
		bool isProductionPaused,
		bool isProductionMotionActive)
	{
		if (isProductionPaused)
		{
			return 4;
		}

		if (!isProductionMotionActive)
		{
			return 0;
		}

		return phase switch
		{
			PressBrakeMotionPhase.Idle => 0,
			PressBrakeMotionPhase.Setup
				or PressBrakeMotionPhase.OperatorWait
				or PressBrakeMotionPhase.ToolChange
				or PressBrakeMotionPhase.ProgramTransition => 1,
			PressBrakeMotionPhase.InterruptRecovery => 5,
			PressBrakeMotionPhase.BackgaugeMove
				or PressBrakeMotionPhase.RamApproach
				or PressBrakeMotionPhase.Forming
				or PressBrakeMotionPhase.Hold
				or PressBrakeMotionPhase.RamReturn
				or PressBrakeMotionPhase.InterStepWait
				or PressBrakeMotionPhase.InterPartWait => 2,
			_ => 0
		};
	}

	public static int ResolveActivityStateIndex(
		PressBrakeMotionPhase phase,
		bool isProductionPaused,
		bool isProductionMotionActive)
	{
		if (isProductionPaused || !isProductionMotionActive)
		{
			return 0;
		}

		return phase switch
		{
			PressBrakeMotionPhase.Forming or PressBrakeMotionPhase.Hold => 2,
			PressBrakeMotionPhase.BackgaugeMove
				or PressBrakeMotionPhase.RamApproach
				or PressBrakeMotionPhase.RamReturn => 1,
			PressBrakeMotionPhase.InterruptRecovery => 3,
			_ => 0
		};
	}

	public static void ApplyExposedTokens(
		PressBrakeKinematicsState pressBrake,
		PressBrakeMotionPhase phase,
		PhysicalSimulationContext context)
	{
		int machineIndex = ResolveMachineStateIndex(
			phase,
			context.IsProductionPaused,
			context.IsProductionMotionActive);
		int activityIndex = ResolveActivityStateIndex(
			phase,
			context.IsProductionPaused,
			context.IsProductionMotionActive);
		int toolIndex = (pressBrake.ProgramIndex + pressBrake.PartIndex)
			% VirtualPressBrakeKinematicsConfig.ToolStationTokens.Length;

		pressBrake.MachineStateToken = VirtualPressBrakeKinematicsConfig.MachineStateTokens[machineIndex];
		pressBrake.ActivityStateToken = VirtualPressBrakeKinematicsConfig.ActivityStateTokens[activityIndex];
		pressBrake.ToolStationToken = VirtualPressBrakeKinematicsConfig.ToolStationTokens[toolIndex];
	}
}
