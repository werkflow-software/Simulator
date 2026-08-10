namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Stable semantic keys for Virtual Machine HMI binding (no NodeIds in UI).
/// </summary>
public enum HmiSemantic
{
	XPosition,
	YPosition,
	ZPosition,
	FeedRate,
	MotorCurrent,
	MotorTemperature,
	CoolingTemperature,
	PowerDemand,
	VibrationRms,
	JobName,
	PartName,
	ActualCounter,
	TargetCounter,
	MachineState,
	ErrorActive,
	ErrorMessage,
	ProcessPhase,
	ProcessDemand,
	ProcessSpeed,
	FocusPosition,
	MaterialThickness,
	CycleTime,
	QualityIndex,
	CoolingFlow,
	CoolingPressure,
	CoolingPumpCurrent,
	CoolingPumpSpeed,
	CoolingFanSpeed,
	CoolingStatus,
	Voltage,
	Current,
	VibrationPeak,
	RemainingCounter,
	ProductionRunning,
	LastProductionChange
}
