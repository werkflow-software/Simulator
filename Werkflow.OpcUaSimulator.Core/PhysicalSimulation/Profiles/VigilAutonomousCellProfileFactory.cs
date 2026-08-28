using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class VigilAutonomousCellProfileFactory
{
	public const string ProfileIdCore24 = VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24;
	public const string ProfileIdExpanded48 = VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48;

	public static PhysicalMachineProfile CreateCore24() => Create(AutonomousCellSignalProfileTier.Core24);

	public static PhysicalMachineProfile CreateExpanded48() => Create(AutonomousCellSignalProfileTier.Expanded48);

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

		string profileId = tier switch
		{
			AutonomousCellSignalProfileTier.Core24 => ProfileIdCore24,
			AutonomousCellSignalProfileTier.Expanded48 => ProfileIdExpanded48,
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
		CreateNumeric("Vision.AlignmentDeviationIntermittent", "Alignment Intermittent", SignalCategory.Process, "mm", -3, 3, 0, 0.5)
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
