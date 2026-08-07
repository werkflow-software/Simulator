namespace Werkflow.OpcUaSimulator.Core.Models;

public enum SimulationEventType
{
	Error,
	Warning,
	ProductionStop,
	OpcUaDisconnect,
	SlowProduction,
	FastProductionJump,
	CounterFreeze,
	CounterJump,
	JobChange,
	TargetQuantityChange,
	SetupState
}
