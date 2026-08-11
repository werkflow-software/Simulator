using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class LaserKinematicsState
{
	private static readonly HashSet<string> ControlledSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"Axis01.Position", "Axis02.Position", "Axis03.Position",
		"Axis01.TargetPosition", "Axis02.TargetPosition", "Axis03.TargetPosition",
		"Axis01.Speed", "Axis02.Speed", "Axis03.Speed",
		"Axis01.TargetSpeed", "Axis02.TargetSpeed", "Axis03.TargetSpeed",
		"Axis01.MotionActive", "Axis02.MotionActive", "Axis03.MotionActive",
		"Process.FeedRate", "Process.CuttingSpeed", "Process.FocusPosition", "Process.PierceTime",
		"Process.LaserPowerActual", "Process.LaserPowerSetpoint", "Process.PowerDemand"
	};

	public bool IsEnabled { get; set; }

	public double X { get; set; }

	public double Y { get; set; }

	public double Z { get; set; }

	public double Vx { get; set; }

	public double Vy { get; set; }

	public LaserMotionPhase MotionPhase { get; set; } = LaserMotionPhase.Idle;

	public LaserToolpathPlan? CurrentPlan { get; set; }

	public int SegmentIndex { get; set; }

	public double PierceElapsedSeconds { get; set; }

	public int PartIndex { get; set; }

	public int PendingPartCompletions { get; set; }

	public bool NozzleChangeRequired { get; set; }

	public bool NozzleChangeActive { get; set; }

	public double NozzleChangeElapsedSeconds { get; set; }

	public bool MovingToService { get; set; }

	public double CutFeedMmPerMin { get; set; }

	public double Vz { get; set; }

	public double PathSpeedMmPerS { get; set; }

	public double LaserPowerKw { get; set; }

	public double DistanceAlongSegmentMm { get; set; }

	public double SegmentStartX { get; set; }

	public double SegmentStartY { get; set; }

	public string NextActionHint { get; set; } = "—";

	public double MinX { get; set; } = double.MaxValue;

	public double MaxX { get; set; } = double.MinValue;

	public double MinY { get; set; } = double.MaxValue;

	public double MaxY { get; set; } = double.MinValue;

	public bool ControlsSignal(string signalId) => IsEnabled && ControlledSignals.Contains(signalId);

	public void TrackPosition()
	{
		if (X < MinX)
		{
			MinX = X;
		}
		if (X > MaxX)
		{
			MaxX = X;
		}
		if (Y < MinY)
		{
			MinY = Y;
		}
		if (Y > MaxY)
		{
			MaxY = Y;
		}
	}
}
