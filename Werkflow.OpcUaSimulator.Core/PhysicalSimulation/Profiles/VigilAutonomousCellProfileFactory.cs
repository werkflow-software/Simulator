using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class VigilAutonomousCellProfileFactory
{
	public const string ProfileIdCore24 = VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24;
	public const string ProfileIdExpanded48 = VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48;
	public const string ProfileIdScale96 = VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96;

	public static PhysicalMachineProfile CreateCore24() => Create(AutonomousCellSignalProfileTier.Core24);

	public static PhysicalMachineProfile CreateExpanded48() => Create(AutonomousCellSignalProfileTier.Expanded48);

	public static PhysicalMachineProfile CreateScale96() => Create(AutonomousCellSignalProfileTier.Scale96);

	public static PhysicalMachineProfile Create(AutonomousCellSignalProfileTier tier)
	{
		List<SignalDefinition> signals = BuildBaseSignals();
		foreach (SignalDefinition bankSignal in AutonomousCellSignalBank.GenerateDefinitions(tier))
		{
			if (signals.All(s => !string.Equals(s.SignalId, bankSignal.SignalId, StringComparison.OrdinalIgnoreCase)))
			{
				signals.Add(bankSignal);
			}
		}

		HashSet<string> enabled = AutonomousCellSignalBank.ResolveEnabledSignalIds(tier)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		signals = signals
			.Select(signal => SignalDefinitionMutator.Copy(signal, m => m.IsEnabled = enabled.Contains(signal.SignalId)))
			.ToList();
		IReadOnlyList<string> orderedEnabled = AutonomousCellSignalBank.ResolveEnabledSignalIds(tier);
		Dictionary<string, int> enabledOrder = orderedEnabled
			.Select((id, index) => (id, index))
			.ToDictionary(x => x.id, x => x.index, StringComparer.OrdinalIgnoreCase);
		signals = signals
			.Where(signal => enabled.Contains(signal.SignalId))
			.OrderBy(signal => enabledOrder.GetValueOrDefault(signal.SignalId, int.MaxValue))
			.ToList();



		string profileId = tier switch
		{
			AutonomousCellSignalProfileTier.Core24 => ProfileIdCore24,
			AutonomousCellSignalProfileTier.Expanded48 => ProfileIdExpanded48,
			AutonomousCellSignalProfileTier.Scale96 => ProfileIdScale96,
			_ => $"vigil-autonomous-cell-scale{(int)tier}"
		};

		return new PhysicalMachineProfile
		{
			ProfileId = profileId,
			ProfileVersion = "1.0.0",
			DisplayName = $"VIGIL Autonomous Cell ({(int)tier} signals)",
			Description = "Machine 3 autonomous production cell OPC contract.",
			MachineType = "VirtualAutonomousProductionCell",
			Manufacturer = "Werkflow",
			DefaultUpdateInterval = TimeSpan.FromMilliseconds(200),
			Metadata = new Dictionary<string, string>
			{
				["purpose"] = "vigil-autonomous-cell-machine-3",
				["profileKind"] = "physical-simulation-reduced",
				["signalProfileTier"] = ((int)tier).ToString(),
				["signalCount"] = enabled.Count.ToString()
			},
			Signals = signals,
			HiddenProcessStates = CreateHiddenStates(),
			Dependencies = [],
			HiddenStateDependencies = []
		};
	}

	private static List<SignalDefinition> BuildBaseSignals() =>
	[
		CreateInt32("Cell.OperationalState", "Cell Operational State", SignalCategory.Production, 0, 3, 0, 0, 5),
		CreateString("Cell.CurrentProductId", "Current Product Id", SignalCategory.Production, "A"),
		CreateInt64("Cell.CompletedPartCount", "Completed Part Count", SignalCategory.Production, 0, 28, 0, 0, 35),
		CreateBool("Inbound.RawMaterialPresent", "Raw Material Present", SignalCategory.Production, false),
		CreateInt32("Inbound.PalletQuantityRemaining", "Pallet Quantity Remaining", SignalCategory.Production, 0, 12, 0, 0, 15),
		CreateString("LoadRobot.ActivityState", "Load Robot Activity", SignalCategory.Axis, "LR_AC_00"),
		CreateNumeric("LoadRobot.AxisPosition", "Load Axis Position", SignalCategory.Axis, "mm", 0, 1200, 0, 0.1),
		CreateNumeric("LoadRobot.GripperPressure", "Gripper Pressure", SignalCategory.Process, "bar", 0, 8, 0, 0.1),
		CreateNumeric("LoadRobot.VelocityActual", "Load Velocity", SignalCategory.Axis, "mm/s", 0, 500, 0, 0.1),
		CreateNumeric("Fixture.ClampForce", "Fixture Clamp Force", SignalCategory.Process, "N", 0, 5000, 0, 0.2),
		CreateString("Fixture.ClampState", "Fixture Clamp State", SignalCategory.Process, "FX_ST_00"),
		CreateString("Process.ActivityState", "Process Activity", SignalCategory.Process, "PR_AC_00"),
		CreateNumeric("Process.ForceActual", "Process Force", SignalCategory.Process, "kN", 0, 80, 0, 0.1),
		CreateNumeric("Process.StrokePosition", "Stroke Position", SignalCategory.Process, "mm", 0, 120, 0, 0.1),
		CreateNumeric("Process.ServoDriveTemperature", "Servo Drive Temperature", SignalCategory.Thermal, "C", 20, 90, 32, 2.0),
		CreateString("TransferRobot.ActivityState", "Transfer Activity", SignalCategory.Axis, "TR_AC_00"),
		CreateNumeric("TransferRobot.AxisPosition", "Transfer Axis Position", SignalCategory.Axis, "mm", 0, 1200, 0, 0.1),
		CreateNumeric("TransferRobot.GripperVacuum", "Gripper Vacuum", SignalCategory.Process, "kPa", -100, 0, -80, 0.1),
		CreateNumeric("Vision.DimensionOffset", "Dimension Offset", SignalCategory.Process, "mm", -5, 5, 0, 0.5),
		CreateNumeric("Vision.SurfaceScore", "Surface Score", SignalCategory.Process, "score", 0, 100, 50, 0.5),
		CreateNumeric("Vision.AlignmentDeviation", "Alignment Deviation", SignalCategory.Process, "mm", -3, 3, 0, 0.5),
		CreateInt32("Sorting.PositionIndex", "Sorting Position", SignalCategory.Production, 0, 4, 0, 0, 10),
		CreateNumeric("Output.ContainerFillLevel", "Container Fill Level", SignalCategory.Production, "ratio", 0, 1, 0, 0.5),
		CreateBool("Output.ContainerExchangeRequested", "Container Exchange Requested", SignalCategory.Production, false),
		CreateNumeric("Process.EnergyIntegral", "Energy Integral", SignalCategory.Process, "kJ", 0, 500, 0, 0.5),
		CreateNumeric("Vision.EdgeDeviation", "Edge Deviation", SignalCategory.Process, "mm", -3, 3, 0, 0.5),
		CreateNumeric("Fixture.PartSeatForce", "Part Seat Force", SignalCategory.Process, "N", 0, 2000, 0, 0.2),
		CreateNumeric("LoadRobot.JointTorqueActual", "Joint Torque", SignalCategory.Axis, "Nm", 0, 200, 0, 0.1),
		CreateNumeric("TransferRobot.PathProgress", "Path Progress", SignalCategory.Axis, "ratio", 0, 1, 0, 0.1),
		CreateBool("Output.EmptyContainerPresent", "Empty Container Present", SignalCategory.Production, true),
		CreateNumeric("Inbound.MaterialWidthRaw", "Material Width", SignalCategory.Production, "mm", 30, 70, 42, 0.5),
		CreateNumeric("Process.RamVelocityActual", "Ram Velocity", SignalCategory.Process, "mm/s", 0, 80, 0, 0.1),
		CreateNumeric("Process.ForceActualMirror", "Force Mirror", SignalCategory.Process, "kN", 0, 80, 0, 0.1),
		CreateNumeric("LoadRobot.AxisPositionSecondary", "Load Axis Secondary", SignalCategory.Axis, "mm", 0, 1200, 0, 0.1),
		CreateNumeric("Vision.DimensionOffsetFiltered", "Dimension Offset Filtered", SignalCategory.Process, "mm", -5, 5, 0, 0.5),
		CreateNumeric("Output.ContainerFillPercent", "Container Fill Percent", SignalCategory.Production, "%", 0, 100, 0, 0.5),
		CreateNumeric("Cell.AmbientHumidity", "Ambient Humidity", SignalCategory.Thermal, "%RH", 0, 100, 45, 5.0),
		CreateNumeric("Fixture.VibrationRms", "Fixture Vibration", SignalCategory.Process, "mm/s", 0, 20, 0, 0.5),
		CreateInt32("Vision.CameraExposureIndex", "Camera Exposure", SignalCategory.Process, 90, 105, 100, 80, 120),
		CreateNumeric("Process.ToolWearIndex", "Tool Wear Index", SignalCategory.Process, "ratio", 0, 1, 0, 10.0),
		CreateNumeric("Cell.EnclosureTemperature", "Enclosure Temperature", SignalCategory.Thermal, "C", 15, 45, 22, 5.0),
		CreateBool("Vision.CalibrationPulse", "Calibration Pulse", SignalCategory.Process, false),
		CreateInt64("Fixture.MaintenanceCounter", "Maintenance Counter", SignalCategory.Production, 0, 28, 0, 0, 100),
		CreateNumeric("Process.ForceSensorNoiseChannel", "Force Noise Channel", SignalCategory.Process, "kN", 0, 80, 0, 0.1),
		CreateNumeric("Vision.RawPixelContrast", "Raw Pixel Contrast", SignalCategory.Process, "score", 0, 100, 50, 0.5),
		CreateNumeric("Auxiliary.FacilityPowerRipple", "Facility Power Ripple", SignalCategory.Thermal, "V", 220, 240, 230, 0.2),
		CreateInt64("Auxiliary.UnrelatedConveyorEncoder", "Unrelated Conveyor Encoder", SignalCategory.Production, 0, 10_000, 0, 0, 20_000),
		CreateNumeric("Vision.AlignmentDeviationIntermittent", "Alignment Intermittent", SignalCategory.Process, "mm", -3, 3, 0, 0.5),
		CreateNumeric("Process.HydraulicPressureSupply", "Hydraulic Supply Pressure", SignalCategory.Process, "bar", 0, 200, 0, 0.2),
		CreateNumeric("Process.HydraulicPressureReturn", "Hydraulic Return Pressure", SignalCategory.Process, "bar", 0, 180, 0, 0.2),
		CreateNumeric("Fixture.AlignmentPinPosition", "Alignment Pin Position", SignalCategory.Process, "mm", 0, 12, 0, 0.1),
		CreateNumeric("LoadRobot.MotorCurrentPhaseA", "Load Motor Current A", SignalCategory.Axis, "A", 0, 12, 0, 0.1),
		CreateNumeric("LoadRobot.MotorCurrentPhaseB", "Load Motor Current B", SignalCategory.Axis, "A", 0, 12, 0, 0.1),
		CreateNumeric("TransferRobot.MotorCurrentPhaseA", "Transfer Motor Current A", SignalCategory.Axis, "A", 0, 10, 0, 0.1),
		CreateInt32("Sorting.LaneOccupancyIndex", "Lane Occupancy Index", SignalCategory.Production, 0, 3, 0, 0, 5),
		CreateNumeric("Output.ConveyorSpeedActual", "Conveyor Speed", SignalCategory.Production, "mm/s", 0, 150, 0, 0.1),
		CreateNumeric("Inbound.ScaleGrossWeight", "Scale Gross Weight", SignalCategory.Production, "kg", 40, 60, 48, 0.5),
		CreateNumeric("TransferRobot.VacuumPumpCurrent", "Vacuum Pump Current", SignalCategory.Process, "A", 0, 5, 0, 0.1),
		CreateNumeric("Process.ClampApproachVelocity", "Clamp Approach Velocity", SignalCategory.Process, "mm/s", 0, 50, 0, 0.1),
		CreateNumeric("Vision.PartEdgeGradient", "Part Edge Gradient", SignalCategory.Process, "score", 0, 20, 0, 0.5),
		CreateNumeric("Process.ForcePeakFiltered", "Force Peak Filtered", SignalCategory.Process, "kN", 0, 80, 0, 0.1),
		CreateNumeric("Fixture.ClampPressureSecondary", "Clamp Pressure Secondary", SignalCategory.Process, "bar", 0, 2, 0, 0.1),
		CreateNumeric("TransferRobot.AxisPositionEncoder", "Transfer Encoder Position", SignalCategory.Axis, "mm", 0, 1200, 0, 0.1),
		CreateNumeric("Vision.DimensionOffsetDuplicate", "Dimension Offset Duplicate", SignalCategory.Process, "mm", -5, 5, 0, 0.5),
		CreateNumeric("Cell.LineFrequencyHz", "Line Frequency", SignalCategory.Thermal, "Hz", 49, 51, 50, 0.5),
		CreateNumeric("Output.ContainerFillLevelSmoothed", "Container Fill Smoothed", SignalCategory.Production, "ratio", 0, 1, 0, 0.5),
		CreateNumeric("TransferRobot.BeltTensionActual", "Belt Tension", SignalCategory.Process, "N", 100, 200, 120, 0.5),
		CreateNumeric("Cell.CompressedAirPressure", "Compressed Air Pressure", SignalCategory.Process, "bar", 5, 7, 6.2, 0.5),
		CreateNumeric("Cell.CoolantFlowRate", "Coolant Flow Rate", SignalCategory.Process, "L/min", 3, 6, 4.5, 0.5),
		CreateNumeric("Cell.LineVoltageRms", "Line Voltage RMS", SignalCategory.Thermal, "V", 220, 240, 230, 0.2),
		CreateNumeric("LoadRobot.BrakeReleasePressure", "Brake Release Pressure", SignalCategory.Process, "bar", 0, 6, 0, 0.1),
		CreateNumeric("Process.FilterDifferentialPressure", "Filter Differential Pressure", SignalCategory.Process, "bar", 0, 1, 0.15, 5.0),
		CreateNumeric("Process.DieTemperatureZoneA", "Die Temperature Zone A", SignalCategory.Thermal, "C", 20, 60, 28, 5.0),
		CreateNumeric("Fixture.GuideWearIndicator", "Guide Wear Indicator", SignalCategory.Process, "ratio", 0, 1, 0.01, 10.0),
		CreateNumeric("Vision.LensTemperature", "Lens Temperature", SignalCategory.Thermal, "C", 15, 40, 24, 5.0),
		CreateNumeric("Process.OilTemperatureSump", "Oil Temperature Sump", SignalCategory.Thermal, "C", 20, 70, 30, 5.0),
		CreateNumeric("Vision.FocusDrivePosition", "Focus Drive Position", SignalCategory.Process, "mm", 8, 16, 12, 0.5),
		CreateInt32("Sorting.DiverterActuationCount", "Diverter Actuation Count", SignalCategory.Production, 0, 28, 0, 0, 100),
		CreateNumeric("Cell.GroundLeakageMilliamp", "Ground Leakage", SignalCategory.Thermal, "mA", 0, 2, 0.8, 10.0),
		CreateBool("Output.LabelApplicatorReady", "Label Applicator Ready", SignalCategory.Production, false),
		CreateNumeric("Cell.PowerFactorInstantaneous", "Power Factor", SignalCategory.Thermal, "ratio", 0.7, 1.0, 0.92, 0.2),
		CreateNumeric("Process.ServoBusUtilization", "Servo Bus Utilization", SignalCategory.Process, "%", 0, 100, 0, 0.2),
		CreateNumeric("LoadRobot.FollowingError", "Following Error", SignalCategory.Axis, "mm", -0.2, 0.2, 0, 0.1),
		CreateNumeric("Vision.SurfaceReflectanceIndex", "Surface Reflectance Index", SignalCategory.Process, "score", 0, 2, 0.5, 0.5),
		CreateNumeric("Auxiliary.PlantChilledWaterSupplyTemp", "Chilled Water Supply", SignalCategory.Thermal, "C", 8, 16, 12, 5.0),
		CreateNumeric("Auxiliary.NeighborPressVibration", "Neighbor Press Vibration", SignalCategory.Process, "mm/s", 0, 5, 0.2, 0.5),
		CreateNumeric("Auxiliary.BuildingHvacDamperPosition", "HVAC Damper Position", SignalCategory.Thermal, "%", 0, 100, 42, 5.0),
		CreateInt32("Auxiliary.UnrelatedStackLightState", "Stack Light State", SignalCategory.Production, 0, 3, 0, 0, 5),
		CreateNumeric("Vision.BarcodeReadConfidence", "Barcode Read Confidence", SignalCategory.Process, "score", 0, 1, 0, 0.5),
		CreateNumeric("Output.RejectChutePosition", "Reject Chute Position", SignalCategory.Production, "mm", 0, 50, 0, 0.5),
		CreateBool("Inbound.BarcodeScannerTrigger", "Barcode Scanner Trigger", SignalCategory.Production, false),
		CreateNumeric("Vision.BacklightIntensity", "Backlight Intensity", SignalCategory.Process, "%", 0, 100, 65, 0.5),
		CreateInt32("Cell.DoorInterlockState", "Door Interlock State", SignalCategory.Production, 0, 2, 1, 0, 3),
		CreateBool("Cell.ShiftMaintenanceModeActive", "Maintenance Mode Active", SignalCategory.Production, false),
		CreateNumeric("Process.CycleTimeSlidingAverage", "Cycle Time Sliding Average", SignalCategory.Process, "s", 0, 120, 0, 1.0),
		CreateNumeric("Process.PeakForceLastStroke", "Peak Force Last Stroke", SignalCategory.Process, "kN", 0, 80, 0, 0.1)
	];

	private static List<HiddenProcessStateDefinition> CreateHiddenStates() =>
	[
		new HiddenProcessStateDefinition
		{
			StateId = "ProcessDemand",
			DisplayName = "Process Demand",
			NormalMinimum = 0.0,
			NormalMaximum = 1.0,
			NominalValue = 0.2,
			HardMinimum = 0.0,
			HardMaximum = 1.2,
			ResponseInertia = 0.8,
			RecoveryRate = 0.05,
			UpdateInterval = TimeSpan.FromSeconds(1)
		}
	];

	private static SignalDefinition CreateNumeric(string id, string name, SignalCategory category, string unit, double nMin, double nMax, double nominal, double intervalSeconds) =>
		new()
		{
			SignalId = id,
			NodeId = id,
			BrowseName = id,
			DisplayName = name,
			Category = category,
			DataType = PhysicalSignalDataType.Double,
			EngineeringUnit = unit,
			NormalMinimum = nMin,
			NormalMaximum = nMax,
			NominalValue = nominal,
			HardMinimum = nMin - 1,
			HardMaximum = nMax + 1,
			InitialValue = nominal,
			UpdateInterval = TimeSpan.FromSeconds(intervalSeconds),
			DecimalPlaces = 2
		};

	private static SignalDefinition CreateInt32(
		string id,
		string name,
		SignalCategory category,
		int normalMin,
		int normalMax,
		int initial,
		int hardMin,
		int hardMax) =>
		new()
		{
			SignalId = id,
			NodeId = id,
			BrowseName = id,
			DisplayName = name,
			Category = category,
			DataType = PhysicalSignalDataType.Int32,
			NormalMinimum = normalMin,
			NormalMaximum = normalMax,
			NominalValue = initial,
			HardMinimum = hardMin,
			HardMaximum = hardMax,
			InitialValue = initial,
			UpdateInterval = TimeSpan.FromSeconds(0.5),
			DecimalPlaces = 0
		};

	private static SignalDefinition CreateInt64(
		string id,
		string name,
		SignalCategory category,
		long normalMin,
		long normalMax,
		long initial,
		long hardMin,
		long hardMax) =>
		new()
		{
			SignalId = id,
			NodeId = id,
			BrowseName = id,
			DisplayName = name,
			Category = category,
			DataType = PhysicalSignalDataType.Int64,
			NormalMinimum = normalMin,
			NormalMaximum = normalMax,
			NominalValue = initial,
			HardMinimum = hardMin,
			HardMaximum = hardMax,
			InitialValue = initial,
			UpdateInterval = TimeSpan.FromSeconds(0.5),
			DecimalPlaces = 0
		};

	private static SignalDefinition CreateString(string id, string name, SignalCategory category, string initial) =>
		new()
		{
			SignalId = id,
			NodeId = id,
			BrowseName = id,
			DisplayName = name,
			Category = category,
			DataType = PhysicalSignalDataType.String,
			InitialStringValue = initial,
			UpdateInterval = TimeSpan.FromSeconds(0.5)
		};

	private static SignalDefinition CreateBool(string id, string name, SignalCategory category, bool initial) =>
		new()
		{
			SignalId = id,
			NodeId = id,
			BrowseName = id,
			DisplayName = name,
			Category = category,
			DataType = PhysicalSignalDataType.Boolean,
			InitialValue = initial ? 1.0 : 0.0,
			UpdateInterval = TimeSpan.FromSeconds(0.5)
		};
}
