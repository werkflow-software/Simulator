using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic Run 001 configuration for the VIGIL LAB laser.
/// </summary>
public static class VigilLabRunProfile
{
	public const int RandomSeed = 42;

	public static readonly int[] FixedJobCatalogIndices = [0, 1, 2, 3];

	public static readonly string[] FixedJobIds = ["JOB-001", "JOB-002", "JOB-003", "JOB-004"];

	public static int ResolveSimulationSeed(Guid machineId, int globalSeed) =>
		machineId == VigilLabMachineContract.MachineId ? RandomSeed : globalSeed;

	public static void ApplyDeterministicSettings(SimulationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.RandomSeed = RandomSeed;
		settings.GenerateNewSeedOnStart = false;
		settings.RandomModeEnabled = false;
	}
}
