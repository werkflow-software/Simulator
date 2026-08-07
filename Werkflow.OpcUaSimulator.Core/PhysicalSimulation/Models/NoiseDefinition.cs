namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class NoiseDefinition
{
	public NoiseModel Model { get; init; } = NoiseModel.None;

	public double Amplitude { get; init; }

	public double FrequencyHz { get; init; }

	public double SeedOffset { get; init; }
}
