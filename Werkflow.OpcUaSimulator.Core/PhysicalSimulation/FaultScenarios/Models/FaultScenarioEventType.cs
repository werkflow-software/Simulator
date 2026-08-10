namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public enum FaultScenarioEventType
{
	ScenarioLoaded,
	ScenarioValidated,
	ScenarioStarted,
	ScenarioPaused,
	ScenarioResumed,
	ScenarioPhaseChanged,
	ThresholdApproaching,
	ThresholdReached,
	ThresholdEntered,
	ThresholdExited,
	ThresholdConfirmed,
	MachineFaulted,
	RecoveryStarted,
	RecoveryCompleted,
	ScenarioStopped,
	ScenarioCancelled,
	CombinationActivated,
	ConflictDetected,
	ScenarioFailed,
	DegradationBecameDetectable
}
