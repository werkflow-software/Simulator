using System.Collections.Frozen;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

public static class HmiSemanticRegistry
{
	private static readonly FrozenDictionary<HmiSemantic, string[]> LaserSignalIds = new Dictionary<HmiSemantic, string[]>
	{
		[HmiSemantic.XPosition] = ["Axis01.Position"],
		[HmiSemantic.YPosition] = ["Axis02.Position"],
		[HmiSemantic.ZPosition] = ["Axis03.Position"],
		[HmiSemantic.FeedRate] = ["Process.FeedRate", "Axis01.Speed"],
		[HmiSemantic.MotorCurrent] = ["Axis01.MotorCurrent", "Drive.Axis01.Current"],
		[HmiSemantic.MotorTemperature] = ["Axis01.MotorTemperature"],
		[HmiSemantic.CoolingTemperature] = [
			"Cooling.PrimaryCircuit.Temperature",
			"Thermal.CoolantSupplyTemp",
			"Thermal.CoolantReturnTemp"
		],
		[HmiSemantic.PowerDemand] = ["Process.PowerDemand", "Electrical.PowerDemand"],
		[HmiSemantic.VibrationRms] = ["Mechanical.VibrationRms", "Axis01.VibrationRms"],
		[HmiSemantic.ProcessPhase] = ["Process.Phase", "Production.ProcessPhase"],
		[HmiSemantic.ProcessDemand] = ["Process.PowerDemand", "Process.LaserPowerDemand"],
		[HmiSemantic.ProcessSpeed] = ["Process.CuttingSpeed", "Spindle.Speed"],
		[HmiSemantic.FocusPosition] = ["Optics.FocusPosition", "Process.FocusPosition"],
		[HmiSemantic.MaterialThickness] = ["Process.MaterialThickness", "Production.MaterialThickness"],
		[HmiSemantic.CycleTime] = ["Production.CycleTime", "Process.CycleTime"],
		[HmiSemantic.QualityIndex] = ["Quality.Index", "Process.QualityIndex"],
		[HmiSemantic.CoolingFlow] = ["Cooling.PrimaryCircuit.Flow"],
		[HmiSemantic.CoolingPressure] = ["Cooling.PrimaryCircuit.Pressure"],
		[HmiSemantic.CoolingPumpCurrent] = ["Cooling.Pump.Current", "Drive.CoolingPump.Current"],
		[HmiSemantic.CoolingPumpSpeed] = ["Cooling.Pump.Speed"],
		[HmiSemantic.CoolingFanSpeed] = ["Cooling.Fan.Speed", "Drive.CoolingFan.Speed"],
		[HmiSemantic.CoolingStatus] = ["Cooling.Status", "Cooling.PrimaryCircuit.Status"],
		[HmiSemantic.Voltage] = ["Electrical.Voltage", "Electrical.LineVoltage"],
		[HmiSemantic.Current] = ["Electrical.Current", "Electrical.TotalCurrent"],
		[HmiSemantic.VibrationPeak] = ["Mechanical.VibrationPeak", "Axis01.VibrationPeak"],
		[HmiSemantic.ActualCounter] = ["Production.ActualCounter"],
		[HmiSemantic.TargetCounter] = ["Production.TargetCounter"],
		[HmiSemantic.JobName] = ["Production.JobName"],
		[HmiSemantic.PartName] = ["Production.PartName"],
		[HmiSemantic.ProductionRunning] = ["Production.Running"],
		[HmiSemantic.LastProductionChange] = ["Production.LastProductionChange"]
	}.ToFrozenDictionary();

	public static IReadOnlyList<string> GetCandidateSignalIds(HmiSemantic semantic) =>
		LaserSignalIds.TryGetValue(semantic, out string[]? ids) ? ids : [];

	public static IReadOnlyList<HmiSemantic> OverviewSemantics { get; } =
	[
		HmiSemantic.XPosition,
		HmiSemantic.YPosition,
		HmiSemantic.ZPosition,
		HmiSemantic.FeedRate,
		HmiSemantic.MotorCurrent,
		HmiSemantic.MotorTemperature,
		HmiSemantic.CoolingTemperature,
		HmiSemantic.PowerDemand,
		HmiSemantic.VibrationRms,
		HmiSemantic.ActualCounter,
		HmiSemantic.TargetCounter,
		HmiSemantic.JobName,
		HmiSemantic.PartName,
		HmiSemantic.MachineState,
		HmiSemantic.ErrorActive
	];
}
