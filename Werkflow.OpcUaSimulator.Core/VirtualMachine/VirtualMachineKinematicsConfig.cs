namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Kinematic workspace and fixed positions for Werkflow Virtual Laser 01 (generic 2D flatbed model).
/// </summary>
public static class VirtualMachineKinematicsConfig
{
	public const string MachineType = "2D Flatbed Fiber Laser";

	public const double WorkingAreaXMin = 0.0;
	public const double WorkingAreaXMax = 3000.0;
	public const double WorkingAreaYMin = 0.0;
	public const double WorkingAreaYMax = 1500.0;

	public const double SafeXMin = 50.0;
	public const double SafeXMax = 2950.0;
	public const double SafeYMin = 50.0;
	public const double SafeYMax = 1450.0;

	public const double HomeX = 30.0;
	public const double HomeY = 50.0;

	public const double ParkX = 30.0;
	public const double ParkY = 50.0;

	public const double NozzleServiceX = 100.0;
	public const double NozzleServiceY = 80.0;

	public const double ZRapid = 25.0;
	public const double ZCutBase = 2.0;
	public const double ZService = 20.0;

	public const double RapidSpeedMmPerS = 1800.0;
	public const double MaxAccelMmPerS2 = 2500.0;

	public const double NozzleChangeDurationSeconds = 12.0;
}
