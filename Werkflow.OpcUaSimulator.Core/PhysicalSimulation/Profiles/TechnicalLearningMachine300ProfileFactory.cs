using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class TechnicalLearningMachine300ProfileFactory
{
	public const string ProfileId = "technical-learning-machine-300";

	public const string ProfileVersion = "1.1.0";

	public static PhysicalMachineProfile Create()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		list.AddRange(CreateProductionSignals());
		list.AddRange(CreateAxisSignals());
		list.AddRange(CreateDriveSignals());
		list.AddRange(CreateThermalSignals());
		list.AddRange(CreateProcessSignals());
		list.AddRange(CreateCoolingSignals());
		list.AddRange(CreateFluidSignals());
		list.AddRange(CreateElectricalSignals());
		list.AddRange(CreateQualitySignals());
		list = TechnicalLearningMachine300SemanticCorrector.Apply(list);
		return new PhysicalMachineProfile
		{
			ProfileId = "technical-learning-machine-300",
			ProfileVersion = "1.1.0",
			DisplayName = "Technical Learning Machine 300",
			Description = "Technisches Last-, Publishing- und Skalierungsprofil (AP 2). Kein VIGIL-Physikmodell.",
			MachineType = "TechnicalLearningCell",
			Manufacturer = "Werkflow",
			DefaultUpdateInterval = TimeSpan.FromSeconds(1.0),
			Metadata = new Dictionary<string, string>
			{
				["ap"] = "2",
				["purpose"] = "load-test-scaling",
				["profileKind"] = "technical-load-test",
				["signalCount"] = list.Count.ToString()
			},
			Signals = list,
			HiddenProcessStates = ReferenceHiddenStates(),
			Dependencies = Array.Empty<SignalDependencyDefinition>()
		};
	}

	private static IEnumerable<SignalDefinition> CreateProductionSignals()
	{
		(string, string, SignalCategory, string, double, double, double, int, int, int)[] source = new(string, string, SignalCategory, string, double, double, double, int, int, int)[20]
		{
			("Production.CycleCounter", "Cycle Counter", SignalCategory.Production, "1", 0.0, 999999.0, 120.0, 0, 9999999, 1),
			("Production.ActiveProgram", "Active Program", SignalCategory.Production, "1", 1.0, 99.0, 12.0, 0, 200, 5),
			("Production.SetupTime", "Setup Time", SignalCategory.Production, "s", 30.0, 180.0, 75.0, 0, 600, 10),
			("Production.IdleTime", "Idle Time", SignalCategory.Production, "s", 0.0, 120.0, 15.0, 0, 3600, 30),
			("Production.Throughput", "Throughput", SignalCategory.Production, "1/h", 40.0, 120.0, 85.0, 0, 500, 5),
			("Production.OeeAvailability", "OEE Availability", SignalCategory.Production, "%", 88.0, 99.0, 95.0, 0, 100, 30),
			("Production.OeePerformance", "OEE Performance", SignalCategory.Production, "%", 85.0, 98.0, 93.0, 0, 100, 30),
			("Production.OeeQuality", "OEE Quality", SignalCategory.Production, "%", 92.0, 99.5, 97.0, 0, 100, 30),
			("Production.ToolLifeRemaining", "Tool Life Remaining", SignalCategory.Production, "%", 20.0, 95.0, 65.0, 0, 100, 60),
			("Production.PartCounterShift", "Part Counter Shift", SignalCategory.Production, "1", 0.0, 5000.0, 850.0, 0, 50000, 10),
			("Production.BatchProgress", "Batch Progress", SignalCategory.Production, "%", 0.0, 100.0, 45.0, 0, 100, 5),
			("Production.QueueLength", "Queue Length", SignalCategory.Production, "1", 0.0, 8.0, 2.0, 0, 20, 10),
			("Production.ChangeoverActive", "Changeover Active", SignalCategory.Production, "1", 0.0, 1.0, 0.0, 0, 1, 60),
			("Production.LastCycleDuration", "Last Cycle Duration", SignalCategory.Production, "s", 12.0, 28.0, 18.0, 1, 120, 2),
			("Production.NextMaintenanceHours", "Next Maintenance Hours", SignalCategory.Production, "h", 50.0, 500.0, 220.0, 0, 2000, 3600),
			("Production.EnergyPerPart", "Energy Per Part", SignalCategory.Production, "kWh", 0.2, 1.5, 0.65, 0, 5, 30),
			("Production.ScrapRate", "Scrap Rate", SignalCategory.Production, "%", 0.0, 2.0, 0.4, 0, 10, 60),
			("Production.ReworkRate", "Rework Rate", SignalCategory.Production, "%", 0.0, 3.0, 0.8, 0, 15, 60),
			("Production.OperatorPresence", "Operator Presence", SignalCategory.Production, "1", 0.0, 1.0, 1.0, 0, 1, 5),
			("Production.AutomaticMode", "Automatic Mode", SignalCategory.Production, "1", 0.0, 1.0, 1.0, 0, 1, 5)
		};
		return source.Select(((string, string, SignalCategory Production, string, double, double, double, int, int, int) n) => CreateNumeric(n.Item1, n.Item2, n.Production, n.Item4, n.Item5, n.Item6, n.Item7, n.Rest.Item1, n.Rest.Item2, n.Rest.Item3));
	}

	private static IEnumerable<SignalDefinition> CreateAxisSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		for (int i = 1; i <= 6; i++)
		{
			string text = $"Axis{i:D2}";
			list.Add(CreateNumeric(text + ".Position", $"Axis {i} Position", SignalCategory.Axis, "mm", 0.0, 1200.0, 600.0, -50.0, 1300.0, 0.2));
			list.Add(CreateNumeric(text + ".TargetPosition", $"Axis {i} Target Position", SignalCategory.Axis, "mm", 0.0, 1200.0, 600.0, -50.0, 1300.0, 0.2));
			list.Add(CreateNumeric(text + ".Speed", $"Axis {i} Speed", SignalCategory.Axis, "mm/s", 920.0, 980.0, 950.0, 0.0, 1200.0, 1.0));
			list.Add(CreateNumeric(text + ".TargetSpeed", $"Axis {i} Target Speed", SignalCategory.Axis, "mm/s", 920.0, 980.0, 950.0, 0.0, 1200.0, 1.0));
			list.Add(CreateNumeric(text + ".Acceleration", $"Axis {i} Acceleration", SignalCategory.Axis, "mm/s²", 100.0, 800.0, 400.0, 0.0, 2000.0, 0.5));
			list.Add(CreateNumeric(text + ".MotorCurrent", $"Axis {i} Motor Current", SignalCategory.Drive, "A", 4.0, 14.0, 8.5, 0.0, 25.0, 0.2));
			list.Add(CreateNumeric(text + ".MotorTemperature", $"Axis {i} Motor Temperature", SignalCategory.Thermal, "°C", 50.0, 54.0, 52.0, 20.0, 90.0, 10.0));
			list.Add(CreateNumeric(text + ".Load", $"Axis {i} Load", SignalCategory.Drive, "%", 25.0, 75.0, 45.0, 0.0, 110.0, 0.5));
			list.Add(CreateNumeric(text + ".FollowingError", $"Axis {i} Following Error", SignalCategory.Axis, "mm", 0.0, 0.15, 0.04, 0.0, 2.0, 0.2));
			list.Add(CreateNumeric(text + ".ControlDeviation", $"Axis {i} Control Deviation", SignalCategory.Axis, "mm", 0.0, 0.08, 0.02, 0.0, 1.0, 0.2));
			list.Add(CreateNumeric(text + ".VibrationRms", $"Axis {i} Vibration RMS", SignalCategory.Vibration, "mm/s", 0.8, 1.4, 1.1, 0.0, 12.0, 0.5));
			list.Add(CreateNumeric(text + ".Torque", $"Axis {i} Torque", SignalCategory.Drive, "Nm", 2.0, 18.0, 9.0, 0.0, 40.0, 0.5));
			list.Add(CreateNumeric(text + ".OperatingHours", $"Axis {i} Operating Hours", SignalCategory.Diagnostic, "h", 1000.0, 20000.0, 8500.0, 0.0, 50000.0, 3600.0));
			list.Add(CreateNumeric(text + ".MotionActive", $"Axis {i} Motion Active", SignalCategory.Axis, "1", 0.0, 1.0, 0.0, 0.0, 1.0, 0.2));
			list.Add(CreateNumeric(text + ".ReferenceValid", $"Axis {i} Reference Valid", SignalCategory.Axis, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 60.0));
			list.Add(CreateNumeric(text + ".BrakeEngaged", $"Axis {i} Brake Engaged", SignalCategory.Safety, "1", 0.0, 1.0, 0.0, 0.0, 1.0, 1.0));
			list.Add(CreateNumeric(text + ".EncoderTemperature", $"Axis {i} Encoder Temperature", SignalCategory.Thermal, "°C", 35.0, 55.0, 42.0, 0.0, 80.0, 30.0));
			list.Add(CreateNumeric(text + ".DriveVoltage", $"Axis {i} Drive Voltage", SignalCategory.Electrical, "V", 380.0, 420.0, 400.0, 300.0, 480.0, 2.0));
		}
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateDriveSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		for (int i = 1; i <= 4; i++)
		{
			string text = $"Drive{i:D2}";
			list.Add(CreateNumeric(text + ".Speed", $"Drive {i} Speed", SignalCategory.Drive, "1/min", 1400.0, 3200.0, 2200.0, 0.0, 6000.0, 1.0));
			list.Add(CreateNumeric(text + ".TargetSpeed", $"Drive {i} Target Speed", SignalCategory.Drive, "1/min", 1400.0, 3200.0, 2200.0, 0.0, 6000.0, 1.0));
			list.Add(CreateNumeric(text + ".Current", $"Drive {i} Current", SignalCategory.Electrical, "A", 3.0, 16.0, 8.0, 0.0, 30.0, 0.5));
			list.Add(CreateNumeric(text + ".Voltage", $"Drive {i} Voltage", SignalCategory.Electrical, "V", 390.0, 410.0, 400.0, 300.0, 480.0, 2.0));
			list.Add(CreateNumeric(text + ".Power", $"Drive {i} Power", SignalCategory.Electrical, "kW", 1.0, 12.0, 5.5, 0.0, 25.0, 1.0));
			list.Add(CreateNumeric(text + ".Temperature", $"Drive {i} Temperature", SignalCategory.Thermal, "°C", 42.0, 58.0, 48.0, 0.0, 95.0, 10.0));
			list.Add(CreateNumeric(text + ".Load", $"Drive {i} Load", SignalCategory.Drive, "%", 20.0, 80.0, 50.0, 0.0, 110.0, 1.0));
			list.Add(CreateNumeric(text + ".Torque", $"Drive {i} Torque", SignalCategory.Drive, "Nm", 5.0, 35.0, 18.0, 0.0, 80.0, 1.0));
			list.Add(CreateNumeric(text + ".Frequency", $"Drive {i} Frequency", SignalCategory.Electrical, "Hz", 45.0, 65.0, 55.0, 0.0, 100.0, 2.0));
			list.Add(CreateNumeric(text + ".OperatingState", $"Drive {i} Operating State", SignalCategory.Diagnostic, "1", 0.0, 3.0, 1.0, 0.0, 10.0, 5.0));
		}
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateThermalSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[20]
		{
			CreateNumeric("Thermal.CabinetTemperature", "Cabinet Temperature", SignalCategory.Thermal, "°C", 29.0, 34.0, 31.0, 10.0, 70.0, 10.0),
			CreateNumeric("Thermal.AmbientTemperature", "Ambient Temperature", SignalCategory.Environment, "°C", 18.0, 26.0, 22.0, -10.0, 45.0, 30.0),
			CreateNumeric("Thermal.SpindleBearingTemp", "Spindle Bearing Temperature", SignalCategory.Thermal, "°C", 38.0, 48.0, 42.0, 0.0, 90.0, 10.0),
			CreateNumeric("Thermal.HydraulicOilTemp", "Hydraulic Oil Temperature", SignalCategory.Thermal, "°C", 35.0, 50.0, 42.0, 0.0, 90.0, 15.0),
			CreateNumeric("Thermal.ElectronicsModule1", "Electronics Module 1 Temperature", SignalCategory.Thermal, "°C", 32.0, 48.0, 38.0, 0.0, 85.0, 15.0),
			CreateNumeric("Thermal.ElectronicsModule2", "Electronics Module 2 Temperature", SignalCategory.Thermal, "°C", 32.0, 48.0, 39.0, 0.0, 85.0, 15.0),
			CreateNumeric("Thermal.OpticsHousingTemp", "Optics Housing Temperature", SignalCategory.Optical, "°C", 22.0, 30.0, 25.0, 0.0, 60.0, 10.0),
			CreateNumeric("Thermal.CoolantReturnTemp", "Coolant Return Temperature", SignalCategory.Cooling, "°C", 22.0, 30.0, 26.0, 5.0, 55.0, 10.0),
			CreateNumeric("Thermal.CoolantSupplyTemp", "Coolant Supply Temperature", SignalCategory.Cooling, "°C", 18.0, 26.0, 22.0, 5.0, 50.0, 10.0),
			CreateNumeric("Thermal.MotorBearingTempX", "Motor Bearing Temp X", SignalCategory.Thermal, "°C", 40.0, 55.0, 48.0, 0.0, 95.0, 15.0),
			CreateNumeric("Thermal.MotorBearingTempY", "Motor Bearing Temp Y", SignalCategory.Thermal, "°C", 40.0, 55.0, 48.0, 0.0, 95.0, 15.0),
			CreateNumeric("Thermal.LinearGuideTemp", "Linear Guide Temperature", SignalCategory.Thermal, "°C", 25.0, 38.0, 30.0, 0.0, 70.0, 20.0),
			CreateNumeric("Thermal.ControlCabinetDeltaT", "Control Cabinet Delta T", SignalCategory.Thermal, "°C", 2.0, 12.0, 6.0, 0.0, 40.0, 30.0),
			CreateNumeric("Thermal.HeatSinkTemp", "Heat Sink Temperature", SignalCategory.Thermal, "°C", 35.0, 55.0, 44.0, 0.0, 90.0, 10.0),
			CreateNumeric("Thermal.AirInletTemp", "Air Inlet Temperature", SignalCategory.Environment, "°C", 18.0, 28.0, 23.0, -10.0, 45.0, 30.0),
			CreateNumeric("Thermal.AirOutletTemp", "Air Outlet Temperature", SignalCategory.Environment, "°C", 24.0, 36.0, 29.0, -5.0, 55.0, 30.0),
			CreateNumeric("Thermal.SpindleMotorTemp", "Spindle Motor Temperature", SignalCategory.Thermal, "°C", 45.0, 58.0, 52.0, 0.0, 100.0, 10.0),
			CreateNumeric("Thermal.GearboxTemp", "Gearbox Temperature", SignalCategory.Thermal, "°C", 38.0, 52.0, 44.0, 0.0, 90.0, 15.0),
			CreateNumeric("Thermal.ServoAmplifierTemp", "Servo Amplifier Temperature", SignalCategory.Thermal, "°C", 40.0, 58.0, 48.0, 0.0, 95.0, 10.0),
			CreateNumeric("Thermal.PanelSurfaceTemp", "Panel Surface Temperature", SignalCategory.Thermal, "°C", 25.0, 35.0, 29.0, 0.0, 60.0, 30.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateProcessSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[30]
		{
			CreateNumeric("Process.FeedRate", "Process Feed Rate", SignalCategory.Process, "mm/min", 1800.0, 2400.0, 2100.0, 0.0, 6000.0, 0.5),
			CreateNumeric("Process.SpindleSpeed", "Process Spindle Speed", SignalCategory.Process, "1/min", 2950.0, 3050.0, 3000.0, 0.0, 15000.0, 0.5),
			CreateNumeric("Process.FocusPosition", "Process Focus Position", SignalCategory.Optical, "mm", -0.1, 0.1, 0.0, -2.0, 2.0, 0.2),
			CreateNumeric("Process.PowerDemand", "Process Power Demand", SignalCategory.Electrical, "kW", 8.0, 18.0, 12.0, 0.0, 40.0, 0.5),
			CreateNumeric("Process.CycleTime", "Process Cycle Time", SignalCategory.Process, "s", 14.0, 22.0, 18.0, 1.0, 120.0, 1.0),
			CreateNumeric("Process.Pressure", "Process Pressure", SignalCategory.Process, "bar", 3.5, 6.0, 4.8, 0.0, 15.0, 1.0),
			CreateNumeric("Process.FlowRate", "Process Flow Rate", SignalCategory.Process, "l/min", 12.0, 22.0, 17.0, 0.0, 50.0, 1.0),
			CreateNumeric("Process.MaterialThickness", "Material Thickness", SignalCategory.Process, "mm", 0.8, 3.2, 2.0, 0.0, 20.0, 5.0),
			CreateNumeric("Process.QualityIndex", "Process Quality Index", SignalCategory.Quality, "%", 96.0, 99.5, 98.0, 0.0, 100.0, 5.0),
			CreateNumeric("Process.RegulationError", "Process Regulation Error", SignalCategory.Process, "mm", 0.0, 0.05, 0.01, 0.0, 1.0, 0.5),
			CreateNumeric("Process.LaserPowerSetpoint", "Laser Power Setpoint", SignalCategory.Process, "kW", 2.0, 8.0, 4.5, 0.0, 20.0, 1.0),
			CreateNumeric("Process.LaserPowerActual", "Laser Power Actual", SignalCategory.Process, "kW", 2.0, 8.0, 4.4, 0.0, 20.0, 0.5),
			CreateNumeric("Process.GasFlow", "Process Gas Flow", SignalCategory.Process, "l/min", 8.0, 18.0, 12.0, 0.0, 40.0, 1.0),
			CreateNumeric("Process.GasPressure", "Process Gas Pressure", SignalCategory.Process, "bar", 8.0, 14.0, 11.0, 0.0, 25.0, 1.0),
			CreateNumeric("Process.NozzleDistance", "Nozzle Distance", SignalCategory.Optical, "mm", 0.5, 2.5, 1.2, 0.0, 10.0, 1.0),
			CreateNumeric("Process.PierceTime", "Pierce Time", SignalCategory.Process, "s", 0.2, 1.5, 0.6, 0.0, 5.0, 2.0),
			CreateNumeric("Process.CuttingSpeed", "Cutting Speed", SignalCategory.Process, "mm/min", 800.0, 2200.0, 1400.0, 0.0, 8000.0, 1.0),
			CreateNumeric("Process.PathDeviation", "Path Deviation", SignalCategory.Quality, "mm", 0.0, 0.08, 0.02, 0.0, 1.0, 1.0),
			CreateNumeric("Process.SurfaceRoughness", "Surface Roughness", SignalCategory.Quality, "µm", 0.8, 3.5, 1.8, 0.0, 20.0, 10.0),
			CreateNumeric("Process.BeamAlignment", "Beam Alignment", SignalCategory.Optical, "mrad", 0.0, 0.5, 0.12, 0.0, 5.0, 5.0),
			CreateNumeric("Process.ChamberPressure", "Chamber Pressure", SignalCategory.Process, "mbar", 900.0, 1100.0, 1000.0, 500.0, 2000.0, 10.0),
			CreateNumeric("Process.ExhaustFlow", "Exhaust Flow", SignalCategory.Process, "m³/h", 200.0, 600.0, 380.0, 0.0, 2000.0, 30.0),
			CreateNumeric("Process.FilterLoad", "Filter Load", SignalCategory.Process, "%", 10.0, 60.0, 28.0, 0.0, 100.0, 60.0),
			CreateNumeric("Process.CoolantConcentration", "Coolant Concentration", SignalCategory.Process, "%", 4.0, 8.0, 6.0, 0.0, 15.0, 300.0),
			CreateNumeric("Process.ToolWearIndex", "Tool Wear Index", SignalCategory.Process, "%", 0.0, 40.0, 12.0, 0.0, 100.0, 60.0),
			CreateNumeric("Process.PartTemperature", "Part Temperature", SignalCategory.Thermal, "°C", 22.0, 45.0, 32.0, 0.0, 120.0, 5.0),
			CreateNumeric("Process.WorkpieceFlatness", "Workpiece Flatness", SignalCategory.Quality, "mm", 0.0, 0.12, 0.04, 0.0, 2.0, 10.0),
			CreateNumeric("Process.AlignmentOffset", "Alignment Offset", SignalCategory.Process, "mm", -0.05, 0.05, 0.0, -1.0, 1.0, 2.0),
			CreateNumeric("Process.StageVacuum", "Stage Vacuum", SignalCategory.Process, "mbar", 50.0, 200.0, 110.0, 0.0, 1000.0, 10.0),
			CreateNumeric("Process.GasPurity", "Gas Purity", SignalCategory.Process, "%", 99.5, 99.99, 99.9, 90.0, 100.0, 60.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateCoolingSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[20]
		{
			CreateNumeric("Cooling.PrimaryCircuit.Flow", "Primary Cooling Flow", SignalCategory.Cooling, "l/min", 18.0, 22.0, 20.0, 0.0, 50.0, 1.0),
			CreateNumeric("Cooling.PrimaryCircuit.Pressure", "Primary Cooling Pressure", SignalCategory.Cooling, "bar", 4.7, 5.2, 5.0, 0.0, 10.0, 1.0),
			CreateNumeric("Cooling.PrimaryCircuit.Temperature", "Primary Cooling Temperature", SignalCategory.Cooling, "°C", 20.0, 28.0, 24.0, 5.0, 55.0, 10.0),
			CreateNumeric("Cooling.SecondaryCircuit.Flow", "Secondary Cooling Flow", SignalCategory.Cooling, "l/min", 10.0, 18.0, 14.0, 0.0, 40.0, 1.0),
			CreateNumeric("Cooling.SecondaryCircuit.Pressure", "Secondary Cooling Pressure", SignalCategory.Cooling, "bar", 3.5, 5.0, 4.2, 0.0, 10.0, 1.0),
			CreateNumeric("Cooling.PumpSpeed", "Cooling Pump Speed", SignalCategory.Cooling, "1/min", 1400.0, 2800.0, 2100.0, 0.0, 4000.0, 2.0),
			CreateNumeric("Cooling.PumpCurrent", "Cooling Pump Current", SignalCategory.Electrical, "A", 1.5, 6.5, 3.8, 0.0, 15.0, 1.0),
			CreateNumeric("Cooling.ValvePosition", "Cooling Valve Position", SignalCategory.Cooling, "%", 30.0, 80.0, 55.0, 0.0, 100.0, 2.0),
			CreateNumeric("Cooling.FilterDifferentialPressure", "Filter Differential Pressure", SignalCategory.Cooling, "bar", 0.1, 0.8, 0.35, 0.0, 3.0, 10.0),
			CreateNumeric("Cooling.ReservoirLevel", "Cooling Reservoir Level", SignalCategory.Cooling, "%", 60.0, 95.0, 82.0, 0.0, 100.0, 30.0),
			CreateNumeric("Cooling.ChillerPower", "Chiller Power", SignalCategory.Electrical, "kW", 0.5, 4.5, 2.2, 0.0, 15.0, 5.0),
			CreateNumeric("Cooling.HeatExchangerEfficiency", "Heat Exchanger Efficiency", SignalCategory.Cooling, "%", 70.0, 95.0, 88.0, 0.0, 100.0, 30.0),
			CreateNumeric("Cooling.ReturnFlow", "Cooling Return Flow", SignalCategory.Cooling, "l/min", 16.0, 24.0, 20.0, 0.0, 50.0, 1.0),
			CreateNumeric("Cooling.SupplyFlow", "Cooling Supply Flow", SignalCategory.Cooling, "l/min", 18.0, 24.0, 21.0, 0.0, 50.0, 1.0),
			CreateNumeric("Cooling.InletTemperature", "Cooling Inlet Temperature", SignalCategory.Cooling, "°C", 18.0, 26.0, 22.0, 5.0, 50.0, 10.0),
			CreateNumeric("Cooling.OutletTemperature", "Cooling Outlet Temperature", SignalCategory.Cooling, "°C", 22.0, 30.0, 26.0, 5.0, 55.0, 10.0),
			CreateNumeric("Cooling.FanSpeed", "Cooling Fan Speed", SignalCategory.Cooling, "1/min", 800.0, 2200.0, 1400.0, 0.0, 4000.0, 5.0),
			CreateNumeric("Cooling.CabinetAirflow", "Cabinet Airflow", SignalCategory.Cooling, "m³/h", 80.0, 250.0, 160.0, 0.0, 500.0, 10.0),
			CreateNumeric("Cooling.CondensateLevel", "Condensate Level", SignalCategory.Cooling, "%", 0.0, 30.0, 8.0, 0.0, 100.0, 60.0),
			CreateNumeric("Cooling.CircuitConductivity", "Cooling Circuit Conductivity", SignalCategory.Cooling, "µS/cm", 5.0, 25.0, 12.0, 0.0, 100.0, 300.0)
		});
	}

	private static IEnumerable<SignalDefinition> CreateFluidSignals()
	{
		List<SignalDefinition> list = new List<SignalDefinition>();
		list.Add(CreateNumeric("Pneumatic.SupplyPressure", "Pneumatic Supply Pressure", SignalCategory.Pneumatic, "bar", 5.5, 7.0, 6.2, 0.0, 12.0, 1.0));
		list.Add(CreateNumeric("Pneumatic.Consumption", "Pneumatic Consumption", SignalCategory.Pneumatic, "l/min", 20.0, 80.0, 45.0, 0.0, 200.0, 5.0));
		list.Add(CreateNumeric("Hydraulic.SupplyPressure", "Hydraulic Supply Pressure", SignalCategory.Hydraulic, "bar", 120.0, 180.0, 150.0, 0.0, 250.0, 1.0));
		list.Add(CreateNumeric("Hydraulic.ReturnPressure", "Hydraulic Return Pressure", SignalCategory.Hydraulic, "bar", 2.0, 12.0, 6.0, 0.0, 30.0, 1.0));
		list.Add(CreateNumeric("Hydraulic.OilLevel", "Hydraulic Oil Level", SignalCategory.Hydraulic, "%", 55.0, 95.0, 78.0, 0.0, 100.0, 60.0));
		list.Add(CreateNumeric("Hydraulic.PumpSpeed", "Hydraulic Pump Speed", SignalCategory.Hydraulic, "1/min", 1200.0, 2400.0, 1800.0, 0.0, 4000.0, 2.0));
		for (int i = 1; i <= 10; i++)
		{
			list.Add(CreateNumeric($"Pneumatic.Valve{i:D2}.Position", $"Pneumatic Valve {i} Position", SignalCategory.Pneumatic, "%", 0.0, 100.0, 50.0, 0.0, 100.0, 2.0));
			list.Add(CreateNumeric($"Hydraulic.Valve{i:D2}.Position", $"Hydraulic Valve {i} Position", SignalCategory.Hydraulic, "%", 0.0, 100.0, 45.0, 0.0, 100.0, 2.0));
		}
		list.Add(CreateNumeric("Hydraulic.AccumulatorPressure", "Hydraulic Accumulator Pressure", SignalCategory.Hydraulic, "bar", 80.0, 160.0, 120.0, 0.0, 250.0, 5.0));
		list.Add(CreateNumeric("Pneumatic.DryerDewPoint", "Pneumatic Dryer Dew Point", SignalCategory.Pneumatic, "°C", -40.0, -10.0, -25.0, -60.0, 10.0, 300.0));
		list.Add(CreateNumeric("Hydraulic.FilterLoad", "Hydraulic Filter Load", SignalCategory.Hydraulic, "%", 5.0, 70.0, 22.0, 0.0, 100.0, 60.0));
		return list;
	}

	private static IEnumerable<SignalDefinition> CreateElectricalSignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[20]
		{
			CreateNumeric("Electrical.MainsVoltage", "Mains Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.DcBusVoltage", "DC Bus Voltage", SignalCategory.Electrical, "V", 540.0, 580.0, 560.0, 400.0, 700.0, 1.0),
			CreateNumeric("Electrical.TotalCurrent", "Total Current", SignalCategory.Electrical, "A", 10.0, 45.0, 22.0, 0.0, 120.0, 1.0),
			CreateNumeric("Electrical.Frequency", "Grid Frequency", SignalCategory.Electrical, "Hz", 49.5, 50.5, 50.0, 45.0, 55.0, 5.0),
			CreateNumeric("Electrical.PowerConsumption", "Power Consumption", SignalCategory.Electrical, "kW", 5.0, 28.0, 14.0, 0.0, 60.0, 1.0),
			CreateNumeric("Electrical.PowerFactor", "Power Factor", SignalCategory.Electrical, "1", 0.85, 0.99, 0.94, 0.0, 1.0, 10.0),
			CreateNumeric("Electrical.EnergyCounter", "Energy Counter", SignalCategory.Electrical, "kWh", 1000.0, 50000.0, 12000.0, 0.0, 999999.0, 60.0),
			CreateNumeric("Electrical.CabinetHumidity", "Cabinet Humidity", SignalCategory.Electrical, "%", 20.0, 55.0, 35.0, 0.0, 100.0, 60.0),
			CreateNumeric("Electrical.UpsBatteryLevel", "UPS Battery Level", SignalCategory.Electrical, "%", 80.0, 100.0, 95.0, 0.0, 100.0, 300.0),
			CreateNumeric("Electrical.GroundingResistance", "Grounding Resistance", SignalCategory.Electrical, "Ω", 0.1, 2.0, 0.6, 0.0, 10.0, 300.0),
			CreateNumeric("Electrical.PhaseL1Voltage", "Phase L1 Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.PhaseL2Voltage", "Phase L2 Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.PhaseL3Voltage", "Phase L3 Voltage", SignalCategory.Electrical, "V", 395.0, 405.0, 400.0, 300.0, 480.0, 2.0),
			CreateNumeric("Electrical.HarmonicDistortion", "Harmonic Distortion", SignalCategory.Electrical, "%", 1.0, 8.0, 3.5, 0.0, 20.0, 30.0),
			CreateNumeric("Electrical.BusbarTemperature", "Busbar Temperature", SignalCategory.Thermal, "°C", 30.0, 50.0, 38.0, 0.0, 90.0, 15.0),
			CreateNumeric("Electrical.ControlVoltage24V", "Control Voltage 24V", SignalCategory.Electrical, "V", 23.5, 24.5, 24.0, 20.0, 30.0, 5.0),
			CreateNumeric("Electrical.SafetyCircuitOk", "Safety Circuit OK", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 1.0),
			CreateNumeric("Electrical.EmergencyStopReleased", "Emergency Stop Released", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 1.0),
			CreateNumeric("Electrical.DoorInterlockClosed", "Door Interlock Closed", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 1.0),
			CreateNumeric("Electrical.LightCurtainClear", "Light Curtain Clear", SignalCategory.Safety, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 0.5)
		});
	}

	private static IEnumerable<SignalDefinition> CreateQualitySignals()
	{
		return new _003C_003Ez__ReadOnlyArray<SignalDefinition>(new SignalDefinition[20]
		{
			CreateNumeric("Quality.ProcessQualityIndex", "Process Quality Index", SignalCategory.Quality, "%", 96.0, 99.5, 98.0, 0.0, 100.0, 5.0),
			CreateNumeric("Quality.PositionAccuracy", "Position Accuracy", SignalCategory.Quality, "mm", 0.0, 0.05, 0.015, 0.0, 1.0, 5.0),
			CreateNumeric("Quality.Repeatability", "Repeatability", SignalCategory.Quality, "mm", 0.0, 0.03, 0.01, 0.0, 1.0, 10.0),
			CreateNumeric("Quality.SurfaceInspectionScore", "Surface Inspection Score", SignalCategory.Quality, "%", 90.0, 100.0, 97.0, 0.0, 100.0, 5.0),
			CreateNumeric("Quality.DimensionCheckPassed", "Dimension Check Passed", SignalCategory.Quality, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 10.0),
			CreateNumeric("Quality.VisionSystemConfidence", "Vision System Confidence", SignalCategory.Optical, "%", 85.0, 99.0, 94.0, 0.0, 100.0, 2.0),
			CreateNumeric("Quality.SensorHealthIndex", "Sensor Health Index", SignalCategory.Diagnostic, "%", 90.0, 100.0, 98.0, 0.0, 100.0, 30.0),
			CreateNumeric("Quality.CommunicationErrorCount", "Communication Error Count", SignalCategory.Diagnostic, "1", 0.0, 3.0, 0.0, 0.0, 1000.0, 60.0),
			CreateNumeric("Quality.CalibrationAgeDays", "Calibration Age Days", SignalCategory.Diagnostic, "d", 0.0, 30.0, 8.0, 0.0, 365.0, 3600.0),
			CreateNumeric("Quality.LastInspectionResult", "Last Inspection Result", SignalCategory.Quality, "1", 0.0, 1.0, 1.0, 0.0, 1.0, 10.0),
			CreateNumeric("Diagnostic.CpuLoad", "CPU Load", SignalCategory.Diagnostic, "%", 5.0, 45.0, 18.0, 0.0, 100.0, 5.0),
			CreateNumeric("Diagnostic.MemoryUsage", "Memory Usage", SignalCategory.Diagnostic, "%", 20.0, 70.0, 42.0, 0.0, 100.0, 10.0),
			CreateNumeric("Diagnostic.NetworkLatency", "Network Latency", SignalCategory.Diagnostic, "ms", 0.5, 5.0, 1.8, 0.0, 100.0, 5.0),
			CreateNumeric("Diagnostic.DiskFreeSpace", "Disk Free Space", SignalCategory.Diagnostic, "%", 20.0, 90.0, 55.0, 0.0, 100.0, 300.0),
			CreateNumeric("Diagnostic.ControllerTemperature", "Controller Temperature", SignalCategory.Diagnostic, "°C", 35.0, 55.0, 42.0, 0.0, 90.0, 15.0),
			CreateNumeric("Diagnostic.WatchdogCounter", "Watchdog Counter", SignalCategory.Diagnostic, "1", 0.0, 5.0, 0.0, 0.0, 10000.0, 60.0),
			CreateNumeric("Diagnostic.FieldbusCycleTime", "Fieldbus Cycle Time", SignalCategory.Diagnostic, "ms", 1.0, 8.0, 4.0, 0.0, 50.0, 2.0),
			CreateNumeric("Diagnostic.AlarmCountActive", "Active Alarm Count", SignalCategory.Diagnostic, "1", 0.0, 5.0, 0.0, 0.0, 100.0, 10.0),
			CreateNumeric("Diagnostic.WarningCountActive", "Active Warning Count", SignalCategory.Diagnostic, "1", 0.0, 8.0, 1.0, 0.0, 100.0, 10.0),
			CreateNumeric("Diagnostic.UptimeHours", "Uptime Hours", SignalCategory.Diagnostic, "h", 100.0, 20000.0, 4500.0, 0.0, 100000.0, 3600.0)
		});
	}

	private static IReadOnlyList<HiddenProcessStateDefinition> ReferenceHiddenStates()
	{
		return new _003C_003Ez__ReadOnlyArray<HiddenProcessStateDefinition>(new HiddenProcessStateDefinition[5]
		{
			new HiddenProcessStateDefinition
			{
				StateId = "MechanicalLoad",
				DisplayName = "Mechanical Load",
				NormalMinimum = 0.2,
				NormalMaximum = 0.8,
				NominalValue = 0.45,
				HardMinimum = 0.0,
				HardMaximum = 1.2,
				InitialValue = 0.45
			},
			new HiddenProcessStateDefinition
			{
				StateId = "ThermalLoad",
				DisplayName = "Thermal Load",
				NormalMinimum = 0.15,
				NormalMaximum = 0.75,
				NominalValue = 0.4,
				HardMinimum = 0.0,
				HardMaximum = 1.1,
				InitialValue = 0.4
			},
			new HiddenProcessStateDefinition
			{
				StateId = "CoolingEfficiency",
				DisplayName = "Cooling Efficiency",
				NormalMinimum = 0.7,
				NormalMaximum = 1.0,
				NominalValue = 0.92,
				HardMinimum = 0.3,
				HardMaximum = 1.0,
				InitialValue = 0.92
			},
			new HiddenProcessStateDefinition
			{
				StateId = "Friction",
				DisplayName = "Friction",
				NormalMinimum = 0.1,
				NormalMaximum = 0.6,
				NominalValue = 0.28,
				HardMinimum = 0.0,
				HardMaximum = 1.0,
				InitialValue = 0.28
			},
			new HiddenProcessStateDefinition
			{
				StateId = "ProcessDemand",
				DisplayName = "Process Demand",
				NormalMinimum = 0.2,
				NormalMaximum = 0.9,
				NominalValue = 0.55,
				HardMinimum = 0.0,
				HardMaximum = 1.2,
				InitialValue = 0.55
			}
		});
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
			IsEnabled = true
		};
	}
}
