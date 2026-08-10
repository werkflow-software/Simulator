using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public interface IGroundTruthRecorder
{
	void BeginExperiment(string experimentId, Guid machineId, int baseSeed);

	void BeginRun(string runId, string runType, int runSeed, int faultRepetitionIndex);

	void RecordEvent(
		GroundTruthEventType eventType,
		TimeSpan simulationTime,
		TimeSpan relativeSinceRunStart,
		string? scenarioId = null,
		string? scenarioPhase = null,
		string? severity = null,
		double intensity = 0,
		int seed = 0,
		IReadOnlyDictionary<string, string>? metadata = null);

	void CompleteRun();

	void CompleteExperiment();

	IReadOnlyList<GroundTruthEvent> GetEventsForExperiment(string experimentId);

	IReadOnlyList<GroundTruthEvent> GetEventsForMachine(Guid machineId);

	GroundTruthTimeline BuildTimeline(string experimentId, string runId);
}
