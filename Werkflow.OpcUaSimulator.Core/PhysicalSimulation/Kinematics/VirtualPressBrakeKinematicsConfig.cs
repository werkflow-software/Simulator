namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class VirtualPressBrakeKinematicsConfig
{
	public const double RamOpenPositionMm = 185.0;

	public const double RamApproachSpeedMmPerS = 22.0;

	public const double RamFormingSpeedMmPerS = 6.5;

	public const double RamReturnSpeedMmPerS = 28.0;

	public const double BackgaugeSpeedMmPerS = 45.0;

	public const double BaseHydraulicOilTempC = 38.5;

	public const double OilTempRisePerFormingSecond = 0.018;

	public const double OilTempCoolPerIdleSecond = 0.004;

	public static readonly string[] MachineStateTokens =
	[
		"PB_ST_00", "PB_ST_01", "PB_ST_02", "PB_ST_03", "PB_ST_04", "PB_ST_05"
	];

	public static readonly string[] ActivityStateTokens =
	[
		"CY_AC_00", "CY_AC_01", "CY_AC_02", "CY_AC_03", "CY_AC_04",
		"CY_AC_05", "CY_AC_06", "CY_AC_07", "CY_AC_08", "CY_AC_09"
	];

	public static readonly string[] ToolStationTokens =
	[
		"TL_ST_00", "TL_ST_01", "TL_ST_02", "TL_ST_03"
	];
}
