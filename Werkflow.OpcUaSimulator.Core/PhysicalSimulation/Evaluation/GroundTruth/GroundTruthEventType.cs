namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public enum GroundTruthEventType
{
	ExperimentStarted,
	NormalObservationStarted,
	ScenarioStarted,
	ScenarioPhaseChanged,
	DegradationBecameDetectable,
	ThresholdApproaching,
	ThresholdFirstReached,
	ThresholdEntered,
	ThresholdExited,
	ThresholdConfirmed,
	MachineFaulted,
	RecoveryStarted,
	RecoveryCompleted,
	ScenarioStopped,
	ExperimentCompleted
}
