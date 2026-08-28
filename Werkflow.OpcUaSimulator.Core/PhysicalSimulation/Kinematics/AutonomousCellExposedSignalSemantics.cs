namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class AutonomousCellExposedSignalSemantics
{
	public static void ApplyTokens(AutonomousCellKinematicsState cell)
	{
		cell.CellOperationalStateCode = cell.MotionPhase switch
		{
			AutonomousCellMotionPhase.Complete => 3,
			AutonomousCellMotionPhase.WaitRawMaterial or AutonomousCellMotionPhase.WaitReplenishment or AutonomousCellMotionPhase.WaitContainerExchange => 2,
			AutonomousCellMotionPhase.Idle => 0,
			_ => 1
		};
		cell.CellOperationalToken = $"CELL_ST_{cell.CellOperationalStateCode:D2}";
		cell.LoadActivityToken = cell.MotionPhase is AutonomousCellMotionPhase.LoadPick or AutonomousCellMotionPhase.LoadTransfer
			? "LR_AC_01" : "LR_AC_00";
		cell.FixtureClampToken = cell.MotionPhase is AutonomousCellMotionPhase.FixtureClamp ? "FX_ST_01" : "FX_ST_00";
		cell.ProcessActivityToken = cell.MotionPhase switch
		{
			AutonomousCellMotionPhase.ProcessApproach => "PR_AC_01",
			AutonomousCellMotionPhase.ProcessPressFit => "PR_AC_02",
			AutonomousCellMotionPhase.ProcessRetract => "PR_AC_03",
			_ => "PR_AC_00"
		};
		cell.TransferActivityToken = cell.MotionPhase is AutonomousCellMotionPhase.TransferPickup or AutonomousCellMotionPhase.TransferToVision
			? "TR_AC_01" : "TR_AC_00";
	}
}
