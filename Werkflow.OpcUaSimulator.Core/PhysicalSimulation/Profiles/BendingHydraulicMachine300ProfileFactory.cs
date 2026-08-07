using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class BendingHydraulicMachine300ProfileFactory
{
	public const string ProfileId = "bending-hydraulic-machine-300";

	public const string ProfileVersion = "1.1.0";

	public static PhysicalMachineProfile Create()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		list.AddRange(CreateProductionSignals());
		list.AddRange(CreateBendingSignals());
		list.AddRange(CreateAxisSignals());
		list.AddRange(CreateHydraulicSignals());
		list.AddRange(CreateThermalSignals());
		list.AddRange(CreateElectricalSignals());
		list.AddRange(CreateQualitySignals());
		list.AddRange(CreateDiagnosticSignals());
		list.AddRange(CreatePneumaticSignals());
		list.AddRange(CreateCoolingSignals());
		list.AddRange(CreateSupplementarySignals());
		List<SignalDependencyDefinition> list2 = PhysicalProfileDependencyBuilder.CreateBendingSignalDependencies();
		PhysicalProfileDependencyBuilder.ApplyHiddenInputs(list, list2);
		return new PhysicalMachineProfile
		{
			ProfileId = "bending-hydraulic-machine-300",
			ProfileVersion = "1.1.0",
			DisplayName = "Bending Hydraulic Machine 300",
			Description = "Physikalisches Biege-/Hydraulikmaschinenprofil mit eigenen Hidden States und Abhängigkeiten (AP 3).",
			MachineType = "BendingHydraulicPress",
			Manufacturer = "Werkflow",
			DefaultUpdateInterval = TimeSpan.FromSeconds(1.0),
			Metadata = new Dictionary<string, string>
			{
				["ap"] = "3",
				["purpose"] = "physical-simulation",
				["profileKind"] = "physical-simulation",
				["signalCount"] = list.Count.ToString(),
				["hiddenStateCount"] = "12",
				["dependencyCount"] = list2.Count.ToString()
			},
			Signals = list,
			HiddenProcessStates = PhysicalProfileDependencyBuilder.CreateBendingHiddenStates(),
			Dependencies = list2,
			HiddenStateDependencies = PhysicalProfileDependencyBuilder.CreateBendingHiddenDependencies()
		};
	}

	private static IEnumerable<SignalDefinition> CreateProductionSignals()
	{
		(string, string, SignalCategory, string, double, double, double, double, double, double)[] source = new(string, string, SignalCategory, string, double, double, double, double, double, double)[18]
		{
			("Production.CycleCounter", "Cycle Counter", SignalCategory.Production, "1", 0.0, 999999.0, 120.0, 0.0, 9999999.0, 1.0),
			("Production.ActiveProgram", "Active Program", SignalCategory.Production, "1", 1.0, 99.0, 12.0, 0.0, 200.0, 5.0),
			("Production.SetupTime", "Setup Time", SignalCategory.Production, "s", 30.0, 180.0, 75.0, 0.0, 600.0, 10.0),
			("Production.Throughput", "Throughput", SignalCategory.Production, "1/h", 30.0, 90.0, 55.0, 0.0, 300.0, 5.0),
			("Production.OeeAvailability", "OEE Availability", SignalCategory.Production, "%", 88.0, 99.0, 95.0, 0.0, 100.0, 30.0),
			("Production.OeePerformance", "OEE Performance", SignalCategory.Production, "%", 85.0, 98.0, 93.0, 0.0, 100.0, 30.0),
			("Production.OeeQuality", "OEE Quality", SignalCategory.Production, "%", 92.0, 99.5, 97.0, 0.0, 100.0, 30.0),
			("Production.ToolLifeRemaining", "Tool Life Remaining", SignalCategory.Production, "%", 20.0, 95.0, 65.0, 0.0, 100.0, 60.0),
			("Production.PartCounterShift", "Part Counter Shift", SignalCategory.Production, "1", 0.0, 5000.0, 850.0, 0.0, 50000.0, 10.0),
			("Production.BatchProgress", "Batch Progress", SignalCategory.Production, "%", 0.0, 100.0, 45.0, 0.0, 100.0, 5.0),
			("Production.LastCycleDuration", "Last Cycle Duration", SignalCategory.Production, "s", 14.0, 28.0, 20.0, 1.0, 120.0, 2.0),
			("Production.NextMaintenanceHours", "Next Maintenance Hours", SignalCategory.Production, "h", 50.0, 500.0, 220.0, 0.0, 2000.0, 3600.0),
			("Production.ScrapRate", "Scrap Rate", SignalCategory.Production, "%", 0.0, 2.0, 0.4, 0.0, 10.0, 60.0),
			("Production.AutomaticMode", "Automatic Mode", SignalCategory.Production, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 5.0),
			("Production.ShiftTarget", "Shift Target", SignalCategory.Production, "1", 200.0, 1200.0, 650.0, 0.0, 5000.0, 60.0),
			("Production.ReworkRate", "Rework Rate", SignalCategory.Production, "%", 0.0, 3.0, 0.8, 0.0, 15.0, 60.0),
			("Production.ChangeoverActive", "Changeover Active", SignalCategory.Production, "1", 0.0, 1.0, 0.0, 0.0, 1.0, 5.0),
			("Production.OperatorPresence", "Operator Presence", SignalCategory.Production, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 10.0)
		};
		return source.Select(((string Id, string Name, SignalCategory Cat, string Unit, double NMin, double NMax, double Nom, double HMin, double HMax, double Interval) n) => CreateNumeric(n.Id, n.Name, n.Cat, n.Unit, n.NMin, n.NMax, n.Nom, n.Rest.Item1, n.Rest.Item2, n.Rest.Item3));
	}

	private static IEnumerable<SignalDefinition> CreateBendingSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[27]
		{
			CreateNumeric("Bending.PressForce", "Press Force", SignalCategory.Process, "kN", 180.0, 320.0, 250.0, 0.0, 500.0, 0.5),
			CreateNumeric("Bending.RamPosition", "Ram Position", SignalCategory.Process, "mm", 0.0, 120.0, 60.0, 0.0, 150.0, 0.2),
			CreateNumeric("Bending.ToolPosition", "Tool Position", SignalCategory.Process, "mm", 0.0, 80.0, 40.0, 0.0, 100.0, 0.2),
			CreateNumeric("Bending.AngleMeasured", "Angle Measured", SignalCategory.Quality, "°", 85.0, 95.0, 90.0, 0.0, 180.0, 1.0),
			CreateNumeric("Bending.AngleTarget", "Angle Target", SignalCategory.Process, "°", 85.0, 95.0, 90.0, 0.0, 180.0, 5.0),
			CreateNumeric("Bending.SpringbackCompensation", "Springback Compensation", SignalCategory.Process, "°", -2.0, 2.0, 0.0, -5.0, 5.0, 2.0),
			CreateNumeric("Bending.BendAngleError", "Bend Angle Error", SignalCategory.Quality, "°", 0.0, 0.5, 0.1, 0.0, 3.0, 2.0),
			CreateNumeric("Bending.FrameDeflection", "Frame Deflection", SignalCategory.Process, "mm", 0.0, 0.3, 0.08, 0.0, 2.0, 1.0),
			CreateNumeric("Bending.DieWearIndex", "Die Wear Index", SignalCategory.Process, "%", 0.0, 40.0, 12.0, 0.0, 100.0, 60.0),
			CreateNumeric("Bending.BackgaugePosition", "Backgauge Position", SignalCategory.Axis, "mm", 0.0, 800.0, 400.0, 0.0, 900.0, 0.5),
			CreateNumeric("Bending.MaterialThickness", "Material Thickness", SignalCategory.Process, "mm", 0.8, 6.0, 2.5, 0.0, 20.0, 5.0),
			CreateNumeric("Bending.BendLength", "Bend Length", SignalCategory.Process, "mm", 100.0, 3000.0, 800.0, 0.0, 4000.0, 5.0),
			CreateNumeric("Bending.CycleTime", "Bending Cycle Time", SignalCategory.Process, "s", 12.0, 28.0, 18.0, 1.0, 120.0, 1.0),
			CreateNumeric("Bending.HoldTime", "Hold Time", SignalCategory.Process, "s", 0.5, 3.0, 1.2, 0.0, 10.0, 1.0),
			CreateNumeric("Bending.RamSpeed", "Ram Speed", SignalCategory.Process, "mm/s", 5.0, 25.0, 14.0, 0.0, 50.0, 0.5),
			CreateNumeric("Bending.Crowning", "Crowning", SignalCategory.Process, "mm", 0.0, 0.5, 0.1, 0.0, 2.0, 5.0),
			CreateNumeric("Bending.TonnagePerMeter", "Tonnage Per Meter", SignalCategory.Process, "t/m", 5.0, 20.0, 12.0, 0.0, 40.0, 5.0),
			CreateNumeric("Bending.PressureSetpoint", "Pressure Setpoint", SignalCategory.Process, "bar", 150.0, 220.0, 185.0, 0.0, 280.0, 2.0),
			CreateNumeric("Bending.RamForceActual", "Ram Force Actual", SignalCategory.Process, "kN", 180.0, 320.0, 250.0, 0.0, 500.0, 0.5),
			CreateNumeric("Bending.BackgaugeForce", "Backgauge Force", SignalCategory.Process, "kN", 2.0, 18.0, 8.0, 0.0, 40.0, 2.0),
			CreateNumeric("Bending.PunchWear", "Punch Wear", SignalCategory.Process, "%", 0.0, 35.0, 10.0, 0.0, 100.0, 60.0),
			CreateNumeric("Bending.DieOpening", "Die Opening", SignalCategory.Process, "mm", 2.0, 20.0, 8.0, 0.0, 40.0, 1.0),
			CreateNumeric("Bending.SheetLength", "Sheet Length", SignalCategory.Process, "mm", 200.0, 3000.0, 900.0, 0.0, 4000.0, 10.0),
			CreateNumeric("Bending.Parallelism", "Parallelism", SignalCategory.Quality, "mm", 0.0, 0.08, 0.02, 0.0, 1.0, 10.0),
			CreateNumeric("Bending.SheetSupportHeight", "Sheet Support Height", SignalCategory.Process, "mm", 0.0, 200.0, 80.0, 0.0, 300.0, 2.0),
			CreateNumeric("Process.PowerDemand", "Process Power Demand", SignalCategory.Electrical, "kW", 8.0, 22.0, 14.0, 0.0, 40.0, 0.5),
			CreateNumeric("Process.CycleTime", "Process Cycle Time", SignalCategory.Process, "s", 14.0, 28.0, 20.0, 1.0, 120.0, 1.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateAxisSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		for (int i = 1; i <= 4; i++)
		{
			string text = $"Axis{i:D2}";
			list.Add(CreateNumeric(text + ".Position", $"Axis {i} Position", SignalCategory.Axis, "mm", 0.0, 900.0, 450.0, -40.0, 980.0, 0.2));
			list.Add(CreateNumeric(text + ".Speed", $"Axis {i} Speed", SignalCategory.Axis, "mm/s", 350.0, 450.0, 400.0, 0.0, 600.0, 1.0));
			list.Add(CreateNumeric(text + ".MotorCurrent", $"Axis {i} Motor Current", SignalCategory.Drive, "A", 4.0, 14.0, 8.0, 0.0, 25.0, 0.2));
			list.Add(CreateNumeric(text + ".MotorTemperature", $"Axis {i} Motor Temperature", SignalCategory.Thermal, "°C", 42.0, 48.0, 45.0, 20.0, 90.0, 10.0));
			list.Add(CreateNumeric(text + ".Load", $"Axis {i} Load", SignalCategory.Drive, "%", 25.0, 75.0, 45.0, 0.0, 110.0, 0.5));
			list.Add(CreateNumeric(text + ".VibrationRms", $"Axis {i} Vibration RMS", SignalCategory.Vibration, "mm/s", 0.8, 1.4, 1.1, 0.0, 12.0, 0.5));
			list.Add(CreateNumeric(text + ".Torque", $"Axis {i} Torque", SignalCategory.Drive, "Nm", 5.0, 25.0, 14.0, 0.0, 50.0, 0.5));
			list.Add(CreateNumeric(text + ".FollowingError", $"Axis {i} Following Error", SignalCategory.Axis, "mm", 0.0, 0.12, 0.03, 0.0, 2.0, 0.2));
			list.Add(CreateNumeric(text + ".Acceleration", $"Axis {i} Acceleration", SignalCategory.Axis, "mm/s²", 80.0, 500.0, 280.0, 0.0, 1500.0, 0.5));
			list.Add(CreateNumeric(text + ".BrakeEngaged", $"Axis {i} Brake Engaged", SignalCategory.Safety, "1", 0.0, 1.0, 0.0, 0.0, 1.0, 1.0));
			list.Add(CreateNumeric(text + ".MotionActive", $"Axis {i} Motion Active", SignalCategory.Axis, "1", 0.0, 1.0, 0.0, 0.0, 1.0, 0.2));
			list.Add(CreateNumeric(text + ".DriveVoltage", $"Axis {i} Drive Voltage", SignalCategory.Electrical, "V", 380.0, 420.0, 400.0, 300.0, 480.0, 2.0));
			list.Add(CreateNumeric(text + ".ControlDeviation", $"Axis {i} Control Deviation", SignalCategory.Axis, "mm", 0.0, 0.06, 0.015, 0.0, 1.0, 0.2));
		}
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateHydraulicSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>
		{
			CreateNumeric("Hydraulic.SupplyPressure", "Hydraulic Supply Pressure", SignalCategory.Hydraulic, "bar", 175.0, 185.0, 180.0, 0.0, 250.0, 0.5),
			CreateNumeric("Hydraulic.ReturnPressure", "Hydraulic Return Pressure", SignalCategory.Hydraulic, "bar", 2.0, 12.0, 6.0, 0.0, 30.0, 1.0),
			CreateNumeric("Hydraulic.OilLevel", "Hydraulic Oil Level", SignalCategory.Hydraulic, "%", 55.0, 95.0, 78.0, 0.0, 100.0, 60.0),
			CreateNumeric("Hydraulic.OilTemperature", "Hydraulic Oil Temperature", SignalCategory.Thermal, "°C", 42.0, 48.0, 45.0, 0.0, 90.0, 10.0),
			CreateNumeric("Hydraulic.PumpSpeed", "Hydraulic Pump Speed", SignalCategory.Hydraulic, "1/min", 1200.0, 2400.0, 1800.0, 0.0, 4000.0, 2.0),
			CreateNumeric("Hydraulic.AccumulatorPressure", "Hydraulic Accumulator Pressure", SignalCategory.Hydraulic, "bar", 80.0, 160.0, 120.0, 0.0, 250.0, 5.0),
			CreateNumeric("Hydraulic.FilterLoad", "Hydraulic Filter Load", SignalCategory.Hydraulic, "%", 5.0, 70.0, 22.0, 0.0, 100.0, 60.0),
			CreateNumeric("Hydraulic.PumpCurrent", "Hydraulic Pump Current", SignalCategory.Electrical, "A", 3.0, 16.0, 8.0, 0.0, 30.0, 0.5),
			CreateNumeric("Hydraulic.CylinderPressureA", "Cylinder Pressure A", SignalCategory.Hydraulic, "bar", 100.0, 200.0, 160.0, 0.0, 280.0, 0.5),
			CreateNumeric("Hydraulic.CylinderPressureB", "Cylinder Pressure B", SignalCategory.Hydraulic, "bar", 20.0, 80.0, 45.0, 0.0, 150.0, 0.5),
			CreateNumeric("Hydraulic.FlowRate", "Hydraulic Flow Rate", SignalCategory.Hydraulic, "l/min", 18.0, 35.0, 26.0, 0.0, 60.0, 1.0),
			CreateNumeric("Hydraulic.ReservoirTemperature", "Reservoir Temperature", SignalCategory.Thermal, "°C", 38.0, 50.0, 44.0, 0.0, 85.0, 15.0),
			CreateNumeric("Hydraulic.TankBreatherPressure", "Tank Breather Pressure", SignalCategory.Hydraulic, "mbar", 0.5, 3.0, 1.5, 0.0, 10.0, 30.0),
			CreateNumeric("Hydraulic.LineFilterDeltaP", "Line Filter Delta P", SignalCategory.Hydraulic, "bar", 0.1, 2.5, 0.8, 0.0, 8.0, 10.0),
			CreateNumeric("Hydraulic.ServoValveDriveCurrent", "Servo Valve Drive Current", SignalCategory.Hydraulic, "mA", 200.0, 900.0, 520.0, 0.0, 1500.0, 1.0)
		};
		for (int i = 1; i <= 6; i++)
		{
			list.Add(CreateNumeric($"Hydraulic.Valve{i:D2}.Position", $"Hydraulic Valve {i} Position", SignalCategory.Hydraulic, "%", 0.0, 100.0, 45.0, 0.0, 100.0, 2.0));
			list.Add(CreateNumeric($"Hydraulic.Valve{i:D2}.ResponseTime", $"Hydraulic Valve {i} Response Time", SignalCategory.Hydraulic, "ms", 10.0, 80.0, 35.0, 0.0, 200.0, 5.0));
		}
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateThermalSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[10]
		{
			CreateNumeric("Thermal.CabinetTemperature", "Cabinet Temperature", SignalCategory.Thermal, "°C", 29.0, 34.0, 31.0, 10.0, 70.0, 10.0),
			CreateNumeric("Thermal.AmbientTemperature", "Ambient Temperature", SignalCategory.Environment, "°C", 20.0, 24.0, 22.0, -10.0, 45.0, 30.0),
			CreateNumeric("Thermal.FrameTemperature", "Frame Temperature", SignalCategory.Thermal, "°C", 28.0, 38.0, 32.0, 0.0, 70.0, 15.0),
			CreateNumeric("Thermal.HydraulicManifoldTemp", "Hydraulic Manifold Temperature", SignalCategory.Thermal, "°C", 35.0, 50.0, 42.0, 0.0, 90.0, 10.0),
			CreateNumeric("Thermal.ServoAmplifierTemp", "Servo Amplifier Temperature", SignalCategory.Thermal, "°C", 40.0, 55.0, 48.0, 0.0, 95.0, 10.0),
			CreateNumeric("Thermal.ControlCabinetDeltaT", "Control Cabinet Delta T", SignalCategory.Thermal, "°C", 2.0, 12.0, 6.0, 0.0, 40.0, 30.0),
			CreateNumeric("Thermal.AirInletTemp", "Air Inlet Temperature", SignalCategory.Environment, "°C", 18.0, 26.0, 22.0, -10.0, 45.0, 30.0),
			CreateNumeric("Thermal.AirOutletTemp", "Air Outlet Temperature", SignalCategory.Environment, "°C", 24.0, 34.0, 28.0, -5.0, 55.0, 30.0),
			CreateNumeric("Thermal.BusbarTemperature", "Busbar Temperature", SignalCategory.Thermal, "°C", 30.0, 48.0, 38.0, 0.0, 90.0, 15.0),
			CreateNumeric("Thermal.PanelSurfaceTemp", "Panel Surface Temperature", SignalCategory.Thermal, "°C", 25.0, 35.0, 29.0, 0.0, 60.0, 30.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateElectricalSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>
		{
			CreateNumeric("Electrical.MainsVoltage", "Mains Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.TotalCurrent", "Total Current", SignalCategory.Electrical, "A", 8.0, 14.0, 11.0, 0.0, 120.0, 1.0),
			CreateNumeric("Electrical.PowerConsumption", "Power Consumption", SignalCategory.Electrical, "kW", 5.0, 22.0, 12.0, 0.0, 60.0, 1.0),
			CreateNumeric("Electrical.Frequency", "Grid Frequency", SignalCategory.Electrical, "Hz", 49.5, 50.5, 50.0, 45.0, 55.0, 5.0),
			CreateNumeric("Electrical.PowerFactor", "Power Factor", SignalCategory.Electrical, "1", 0.85, 0.99, 0.94, 0.0, 1.0, 10.0),
			CreateNumeric("Electrical.PhaseL1Voltage", "Phase L1 Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.PhaseL2Voltage", "Phase L2 Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.PhaseL3Voltage", "Phase L3 Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.ControlVoltage24V", "Control Voltage 24V", SignalCategory.Electrical, "V", 23.5, 24.5, 24.0, 20.0, 30.0, 5.0),
			CreateNumeric("Electrical.SafetyCircuitOk", "Safety Circuit OK", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 1.0),
			CreateNumeric("Electrical.EmergencyStopReleased", "Emergency Stop Released", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 1.0)
		};
		for (int i = 1; i <= 4; i++)
		{
			string text = $"Drive{i:D2}";
			list.Add(CreateNumeric(text + ".Speed", $"Drive {i} Speed", SignalCategory.Drive, "1/min", 1200.0, 2800.0, 2000.0, 0.0, 5000.0, 1.0));
			list.Add(CreateNumeric(text + ".Current", $"Drive {i} Current", SignalCategory.Electrical, "A", 3.0, 14.0, 8.0, 0.0, 30.0, 0.5));
			list.Add(CreateNumeric(text + ".Temperature", $"Drive {i} Temperature", SignalCategory.Thermal, "°C", 42.0, 55.0, 48.0, 0.0, 95.0, 10.0));
			list.Add(CreateNumeric(text + ".Load", $"Drive {i} Load", SignalCategory.Drive, "%", 20.0, 80.0, 50.0, 0.0, 110.0, 1.0));
			list.Add(CreateNumeric(text + ".Torque", $"Drive {i} Torque", SignalCategory.Drive, "Nm", 8.0, 40.0, 22.0, 0.0, 100.0, 1.0));
			list.Add(CreateNumeric(text + ".Voltage", $"Drive {i} Voltage", SignalCategory.Electrical, "V", 390.0, 410.0, 400.0, 300.0, 480.0, 2.0));
			list.Add(CreateNumeric(text + ".Power", $"Drive {i} Power", SignalCategory.Electrical, "kW", 2.0, 10.0, 5.0, 0.0, 25.0, 1.0));
		}
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateQualitySignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[10]
		{
			CreateNumeric("Quality.ProcessQualityIndex", "Process Quality Index", SignalCategory.Quality, "%", 96.0, 99.5, 98.0, 0.0, 100.0, 5.0),
			CreateNumeric("Quality.PositionAccuracy", "Position Accuracy", SignalCategory.Quality, "mm", 0.0, 0.05, 0.015, 0.0, 1.0, 5.0),
			CreateNumeric("Quality.Repeatability", "Repeatability", SignalCategory.Quality, "mm", 0.0, 0.03, 0.01, 0.0, 1.0, 10.0),
			CreateNumeric("Quality.AngleAccuracy", "Angle Accuracy", SignalCategory.Quality, "°", 0.0, 0.2, 0.05, 0.0, 2.0, 5.0),
			CreateNumeric("Quality.SurfaceInspectionScore", "Surface Inspection Score", SignalCategory.Quality, "%", 90.0, 100.0, 97.0, 0.0, 100.0, 5.0),
			CreateNumeric("Quality.DimensionCheckPassed", "Dimension Check Passed", SignalCategory.Quality, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 10.0),
			CreateNumeric("Quality.SensorHealthIndex", "Sensor Health Index", SignalCategory.Diagnostic, "%", 90.0, 100.0, 98.0, 0.0, 100.0, 30.0),
			CreateNumeric("Quality.CalibrationAgeDays", "Calibration Age Days", SignalCategory.Diagnostic, "d", 0.0, 30.0, 8.0, 0.0, 365.0, 3600.0),
			CreateNumeric("Quality.SurfaceRoughness", "Surface Roughness", SignalCategory.Quality, "µm", 0.2, 2.5, 0.9, 0.0, 10.0, 15.0),
			CreateNumeric("Quality.BendRadiusError", "Bend Radius Error", SignalCategory.Quality, "mm", 0.0, 0.4, 0.08, 0.0, 3.0, 10.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateDiagnosticSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		for (int i = 1; i <= 8; i++)
		{
			list.Add(CreateNumeric($"Diagnostic.Sensor{i:D2}.Health", $"Sensor {i} Health", SignalCategory.Diagnostic, "%", 85.0, 100.0, 97.0, 0.0, 100.0, 30.0));
			list.Add(CreateNumeric($"Diagnostic.Sensor{i:D2}.Drift", $"Sensor {i} Drift", SignalCategory.Diagnostic, "%", 0.0, 5.0, 0.5, 0.0, 20.0, 60.0));
		}
		list.AddRange(new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[5]
		{
			CreateNumeric("Diagnostic.CpuLoad", "CPU Load", SignalCategory.Diagnostic, "%", 5.0, 45.0, 18.0, 0.0, 100.0, 5.0),
			CreateNumeric("Diagnostic.MemoryUsage", "Memory Usage", SignalCategory.Diagnostic, "%", 20.0, 70.0, 42.0, 0.0, 100.0, 10.0),
			CreateNumeric("Diagnostic.NetworkLatency", "Network Latency", SignalCategory.Diagnostic, "ms", 0.5, 5.0, 1.8, 0.0, 100.0, 5.0),
			CreateNumeric("Diagnostic.ControllerTemperature", "Controller Temperature", SignalCategory.Diagnostic, "°C", 35.0, 55.0, 42.0, 0.0, 90.0, 15.0),
			CreateNumeric("Diagnostic.UptimeHours", "Uptime Hours", SignalCategory.Diagnostic, "h", 100.0, 20000.0, 4500.0, 0.0, 100000.0, 3600.0)
		}));
		return list;
	}

	private static IEnumerable<SignalDefinition> CreatePneumaticSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>
		{
			CreateNumeric("Pneumatic.SupplyPressure", "Pneumatic Supply Pressure", SignalCategory.Pneumatic, "bar", 5.5, 7.0, 6.2, 0.0, 12.0, 1.0),
			CreateNumeric("Pneumatic.Consumption", "Pneumatic Consumption", SignalCategory.Pneumatic, "l/min", 20.0, 80.0, 45.0, 0.0, 200.0, 5.0),
			CreateNumeric("Pneumatic.DryerDewPoint", "Pneumatic Dryer Dew Point", SignalCategory.Pneumatic, "°C", -40.0, -10.0, -25.0, -60.0, 10.0, 300.0)
		};
		for (int i = 1; i <= 5; i++)
		{
			list.Add(CreateNumeric($"Pneumatic.Valve{i:D2}.Position", $"Pneumatic Valve {i} Position", SignalCategory.Pneumatic, "%", 0.0, 100.0, 50.0, 0.0, 100.0, 2.0));
		}
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateCoolingSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[10]
		{
			CreateNumeric("Cooling.PrimaryCircuit.Flow", "Primary Cooling Flow", SignalCategory.Cooling, "l/min", 18.0, 22.0, 20.0, 0.0, 50.0, 1.0),
			CreateNumeric("Cooling.PrimaryCircuit.Pressure", "Primary Cooling Pressure", SignalCategory.Cooling, "bar", 4.7, 5.2, 5.0, 0.0, 10.0, 1.0),
			CreateNumeric("Cooling.PrimaryCircuit.Temperature", "Primary Cooling Temperature", SignalCategory.Cooling, "°C", 20.0, 28.0, 24.0, 5.0, 55.0, 10.0),
			CreateNumeric("Cooling.PumpSpeed", "Cooling Pump Speed", SignalCategory.Cooling, "1/min", 1400.0, 2800.0, 2100.0, 0.0, 4000.0, 2.0),
			CreateNumeric("Cooling.PumpCurrent", "Cooling Pump Current", SignalCategory.Electrical, "A", 1.5, 6.5, 3.8, 0.0, 15.0, 1.0),
			CreateNumeric("Cooling.ValvePosition", "Cooling Valve Position", SignalCategory.Cooling, "%", 30.0, 80.0, 55.0, 0.0, 100.0, 2.0),
			CreateNumeric("Cooling.ReservoirLevel", "Cooling Reservoir Level", SignalCategory.Cooling, "%", 60.0, 95.0, 82.0, 0.0, 100.0, 30.0),
			CreateNumeric("Cooling.FanSpeed", "Cooling Fan Speed", SignalCategory.Cooling, "1/min", 800.0, 2200.0, 1400.0, 0.0, 4000.0, 5.0),
			CreateNumeric("Cooling.InletTemperature", "Cooling Inlet Temperature", SignalCategory.Cooling, "°C", 18.0, 26.0, 22.0, 5.0, 50.0, 10.0),
			CreateNumeric("Cooling.OutletTemperature", "Cooling Outlet Temperature", SignalCategory.Cooling, "°C", 22.0, 30.0, 26.0, 5.0, 55.0, 10.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateSupplementarySignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		for (int i = 1; i <= 6; i++)
		{
			list.Add(CreateNumeric($"Vibration.Point{i:D2}.Rms", $"Vibration Point {i} RMS", SignalCategory.Vibration, "mm/s", 0.8, 1.4, 1.1, 0.0, 12.0, 0.5));
			list.Add(CreateNumeric($"Vibration.Point{i:D2}.Peak", $"Vibration Point {i} Peak", SignalCategory.Vibration, "mm/s", 1.2, 2.5, 1.8, 0.0, 20.0, 0.5));
			list.Add(CreateNumeric($"Safety.Zone{i:D2}.Clear", $"Safety Zone {i} Clear", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 0.5));
			list.Add(CreateNumeric($"Mechanical.Clamp{i:D2}.Force", $"Clamp {i} Force", SignalCategory.Process, "kN", 5.0, 40.0, 18.0, 0.0, 80.0, 1.0));
			list.Add(CreateNumeric($"Mechanical.Support{i:D2}.Height", $"Support {i} Height", SignalCategory.Process, "mm", 0.0, 200.0, 80.0, 0.0, 300.0, 2.0));
			list.Add(CreateNumeric($"Bending.Segment{i:D2}.Angle", $"Bend Segment {i} Angle", SignalCategory.Quality, "°", 80.0, 100.0, 90.0, 0.0, 180.0, 2.0));
		}
		for (int j = 1; j <= 4; j++)
		{
			list.Add(CreateNumeric($"Hydraulic.Cylinder{j:D2}.Position", $"Cylinder {j} Position", SignalCategory.Hydraulic, "mm", 0.0, 200.0, 80.0, 0.0, 300.0, 0.5));
			list.Add(CreateNumeric($"Hydraulic.Cylinder{j:D2}.Pressure", $"Cylinder {j} Pressure", SignalCategory.Hydraulic, "bar", 20.0, 200.0, 120.0, 0.0, 280.0, 0.5));
			list.Add(CreateNumeric($"Quality.Checkpoint{j:D2}.Score", $"Quality Checkpoint {j} Score", SignalCategory.Quality, "%", 90.0, 100.0, 97.0, 0.0, 100.0, 5.0));
			list.Add(CreateNumeric($"Process.Station{j:D2}.Load", $"Process Station {j} Load", SignalCategory.Process, "%", 10.0, 85.0, 45.0, 0.0, 110.0, 2.0));
			list.Add(CreateNumeric($"Diagnostic.IO.Module{j:D2}.Status", $"IO Module {j} Status", SignalCategory.Diagnostic, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 10.0));
			list.Add(CreateNumeric($"Diagnostic.Fieldbus.Node{j:D2}.Latency", $"Fieldbus Node {j} Latency", SignalCategory.Diagnostic, "ms", 0.5, 6.0, 2.0, 0.0, 50.0, 5.0));
		}
		list.AddRange(new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[25]
		{
			CreateNumeric("Mechanical.FrameStress", "Frame Stress", SignalCategory.Process, "MPa", 50.0, 180.0, 110.0, 0.0, 300.0, 5.0),
			CreateNumeric("Mechanical.BedDeflection", "Bed Deflection", SignalCategory.Process, "mm", 0.0, 0.2, 0.05, 0.0, 2.0, 2.0),
			CreateNumeric("Mechanical.RamParallelism", "Ram Parallelism", SignalCategory.Quality, "mm", 0.0, 0.08, 0.02, 0.0, 1.0, 5.0),
			CreateNumeric("Mechanical.DieGap", "Die Gap", SignalCategory.Process, "mm", 0.5, 4.0, 2.0, 0.0, 10.0, 1.0),
			CreateNumeric("Mechanical.SheetSupportLoad", "Sheet Support Load", SignalCategory.Process, "kN", 1.0, 15.0, 6.0, 0.0, 40.0, 2.0),
			CreateNumeric("Environment.Humidity", "Environment Humidity", SignalCategory.Environment, "%", 35.0, 65.0, 48.0, 0.0, 100.0, 60.0),
			CreateNumeric("Environment.AirPressure", "Environment Air Pressure", SignalCategory.Environment, "hPa", 990.0, 1025.0, 1013.0, 900.0, 1100.0, 300.0),
			CreateNumeric("Environment.LightIntensity", "Cabinet Light Intensity", SignalCategory.Environment, "%", 20.0, 90.0, 55.0, 0.0, 100.0, 30.0),
			CreateNumeric("Production.ProgramRevision", "Program Revision", SignalCategory.Production, "1", 1.0, 20.0, 7.0, 0.0, 100.0, 120.0),
			CreateNumeric("Production.SetupProgress", "Setup Progress", SignalCategory.Production, "%", 0.0, 100.0, 35.0, 0.0, 100.0, 10.0),
			CreateNumeric("Production.OrderQueueDepth", "Order Queue Depth", SignalCategory.Production, "1", 0.0, 8.0, 2.0, 0.0, 50.0, 30.0),
			CreateNumeric("Production.EnergyPerPart", "Energy Per Part", SignalCategory.Production, "kWh", 0.2, 1.5, 0.6, 0.0, 5.0, 60.0),
			CreateNumeric("Quality.AngleDeviation", "Angle Deviation", SignalCategory.Quality, "°", 0.0, 0.3, 0.06, 0.0, 2.0, 5.0),
			CreateNumeric("Quality.EdgeQualityIndex", "Edge Quality Index", SignalCategory.Quality, "%", 88.0, 100.0, 96.0, 0.0, 100.0, 10.0),
			CreateNumeric("Quality.FlatnessDeviation", "Flatness Deviation", SignalCategory.Quality, "mm", 0.0, 0.15, 0.04, 0.0, 2.0, 10.0),
			CreateNumeric("Hydraulic.CaseDrainFlow", "Case Drain Flow", SignalCategory.Hydraulic, "l/min", 0.5, 4.0, 1.8, 0.0, 10.0, 5.0),
			CreateNumeric("Hydraulic.CoolerEfficiency", "Hydraulic Cooler Efficiency", SignalCategory.Hydraulic, "%", 70.0, 98.0, 88.0, 0.0, 100.0, 30.0),
			CreateNumeric("Electrical.HarmonicDistortion", "Harmonic Distortion", SignalCategory.Electrical, "%", 1.0, 8.0, 3.5, 0.0, 20.0, 30.0),
			CreateNumeric("Electrical.CabinetHumidity", "Cabinet Humidity", SignalCategory.Electrical, "%", 25.0, 55.0, 38.0, 0.0, 100.0, 60.0),
			CreateNumeric("Thermal.LeftColumnTemperature", "Left Column Temperature", SignalCategory.Thermal, "°C", 28.0, 38.0, 32.0, 0.0, 70.0, 15.0),
			CreateNumeric("Thermal.RightColumnTemperature", "Right Column Temperature", SignalCategory.Thermal, "°C", 28.0, 38.0, 32.0, 0.0, 70.0, 15.0),
			CreateNumeric("Process.MaterialWidth", "Material Width", SignalCategory.Process, "mm", 200.0, 2500.0, 800.0, 0.0, 4000.0, 60.0),
			CreateNumeric("Process.BendSequenceStep", "Bend Sequence Step", SignalCategory.Process, "1", 1.0, 12.0, 4.0, 0.0, 50.0, 5.0),
			CreateNumeric("Process.BackgaugeSpeed", "Backgauge Speed", SignalCategory.Process, "mm/s", 80.0, 350.0, 200.0, 0.0, 600.0, 1.0),
			CreateNumeric("Process.ClampPressure", "Clamp Pressure", SignalCategory.Process, "bar", 20.0, 80.0, 45.0, 0.0, 120.0, 2.0)
		}));
		return list;
	}

	private static SignalDefinition CreateNumeric(string signalId, string displayName, SignalCategory category, string unit, double normalMin, double normalMax, double nominal, double hardMin, double hardMax, double intervalSeconds)
	{
		return new SignalDefinition
		{
			SignalId = signalId,
			NodeId = signalId,
			BrowseName = signalId.Split('.')[^1],
			DisplayName = displayName,
			Description = displayName,
			Category = category,
			DataType = PhysicalSignalDataType.Double,
			EngineeringUnit = unit,
			NormalMinimum = normalMin,
			NormalMaximum = normalMax,
			NominalValue = nominal,
			HardMinimum = hardMin,
			HardMaximum = hardMax,
			NoiseModel = NoiseModel.Gaussian,
			NoiseAmplitude = Math.Max(0.01, (normalMax - normalMin) * 0.02),
			UpdateInterval = TimeSpan.FromSeconds(intervalSeconds),
			DecimalPlaces = 2,
			ResponseInertia = 0.2,
			InitialValue = nominal,
			IsEnabled = true,
			TechnicalBehavior = TechnicalSignalBehavior.Continuous
		};
	}
}
