namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Experiments;

public enum ExperimentRunnerState
{
	Created,
	Warmup,
	NormalLearning,
	Running,
	Recovering,
	Cooldown,
	Completed,
	Cancelled,
	Failed
}
