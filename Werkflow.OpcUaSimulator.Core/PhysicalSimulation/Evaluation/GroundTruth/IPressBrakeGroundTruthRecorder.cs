namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;

public interface IPressBrakeGroundTruthRecorder
{
	void BeginSession(Guid machineId, int seed, string? artifactPath = null);

	void Record(PressBrakeGroundTruthEvent evt);

	IReadOnlyList<PressBrakeGroundTruthEvent> GetEvents();

	string? ArtifactPath { get; }

	void Flush();
}
