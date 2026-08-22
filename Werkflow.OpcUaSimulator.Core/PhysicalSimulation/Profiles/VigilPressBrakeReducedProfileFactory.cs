using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class VigilPressBrakeReducedProfileFactory
{
	public const string ProfileId = "vigil-press-brake-reduced";

	public const string ProfileVersion = "1.0.0";

	public static readonly IReadOnlyList<string> ContractSignalIds =
	[
		"Machine.MachineState",
		"Machine.ProgramId",
		"Machine.PartId",
		"Machine.ActualCounter",
		"Machine.TargetCounter",
		"Machine.LastProductionChange",
		"Ram.Position",
		"Ram.Velocity",
		"Backgauge.Position",
		"Process.BendAngle",
		"Process.FormingForce",
		"Tool.StationState",
		"Thermal.HydraulicOilTemp",
		"Cycle.ActivityState"
	];

	public static PhysicalMachineProfile Create()
	{
		List<SignalDefinition> signals =
		[
			CreateString("Machine.MachineState", "Machine State", SignalCategory.Production, "PB_ST_00"),
			CreateString("Machine.ProgramId", "Program Id", SignalCategory.Production, "—"),
			CreateString("Machine.PartId", "Part Id", SignalCategory.Production, "—"),
			CreateNumeric("Machine.ActualCounter", "Actual Counter", SignalCategory.Production, "1", 0, 99999, 0, 1.0),
			CreateNumeric("Machine.TargetCounter", "Target Counter", SignalCategory.Production, "1", 0, 99999, 12, 1.0),
			CreateDateTime("Machine.LastProductionChange", "Last Production Change"),
			CreateNumeric("Ram.Position", "Ram Position", SignalCategory.Process, "mm", 0, 200, 185, 0.2),
			CreateNumeric("Ram.Velocity", "Ram Velocity", SignalCategory.Process, "mm/s", -40, 40, 0, 0.2),
			CreateNumeric("Backgauge.Position", "Backgauge Position", SignalCategory.Axis, "mm", 0, 900, 400, 0.5),
			CreateNumeric("Process.BendAngle", "Bend Angle", SignalCategory.Process, "°", 0, 180, 0, 0.5),
			CreateNumeric("Process.FormingForce", "Forming Force", SignalCategory.Process, "kN", 0, 500, 0, 0.5),
			CreateString("Tool.StationState", "Tool Station State", SignalCategory.Process, "TL_ST_00"),
			CreateNumeric("Thermal.HydraulicOilTemp", "Hydraulic Oil Temp", SignalCategory.Thermal, "°C", 20, 80, 38.5, 5.0),
			CreateString("Cycle.ActivityState", "Cycle Activity State", SignalCategory.Production, "CY_AC_00")
		];

		return new PhysicalMachineProfile
		{
			ProfileId = ProfileId,
			ProfileVersion = ProfileVersion,
			DisplayName = "VIGIL Press Brake Reduced",
			Description = "Reduced 14-signal OPC contract for VIGIL cross-machine generalization validation (Machine 2).",
			MachineType = "VirtualPressBrake",
			Manufacturer = "Werkflow",
			DefaultUpdateInterval = TimeSpan.FromMilliseconds(200),
			Metadata = new Dictionary<string, string>
			{
				["purpose"] = "vigil-press-brake-machine-2",
				["profileKind"] = "physical-simulation-reduced",
				["signalCount"] = signals.Count.ToString(),
				["enabledSignalCount"] = signals.Count.ToString()
			},
			Signals = signals,
			HiddenProcessStates = CreateHiddenStates(),
			Dependencies = [],
			HiddenStateDependencies = []
		};
	}

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
		},
		new HiddenProcessStateDefinition
		{
			StateId = "PressLoad",
			DisplayName = "Press Load",
			NormalMinimum = 0.0,
			NormalMaximum = 1.0,
			NominalValue = 0.15,
			HardMinimum = 0.0,
			HardMaximum = 1.2,
			ResponseInertia = 0.6,
			RecoveryRate = 0.04,
			UpdateInterval = TimeSpan.FromSeconds(1)
		},
		new HiddenProcessStateDefinition
		{
			StateId = "ThermalLoad",
			DisplayName = "Thermal Load",
			NormalMinimum = 0.0,
			NormalMaximum = 1.0,
			NominalValue = 0.25,
			HardMinimum = 0.0,
			HardMaximum = 1.2,
			ResponseInertia = 2.5,
			RecoveryRate = 0.02,
			UpdateInterval = TimeSpan.FromSeconds(2)
		}
	];

	private static SignalDefinition CreateNumeric(
		string id,
		string name,
		SignalCategory category,
		string unit,
		double nMin,
		double nMax,
		double nominal,
		double intervalSeconds) =>
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
			HardMinimum = nMin - Math.Abs(nMin) * 0.2 - 1,
			HardMaximum = nMax + Math.Abs(nMax) * 0.2 + 1,
			InitialValue = nominal,
			UpdateInterval = TimeSpan.FromSeconds(intervalSeconds),
			DecimalPlaces = 2,
			ResponseInertia = 0.2
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

	private static SignalDefinition CreateDateTime(string id, string name) =>
		new()
		{
			SignalId = id,
			NodeId = id,
			BrowseName = id,
			DisplayName = name,
			Category = SignalCategory.Production,
			DataType = PhysicalSignalDataType.DateTime,
			InitialDateTimeUtc = DateTime.UtcNow,
			UpdateInterval = TimeSpan.FromSeconds(1)
		};
}
