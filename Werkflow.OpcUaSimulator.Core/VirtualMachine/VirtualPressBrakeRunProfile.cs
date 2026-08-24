using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic baseline configuration for Virtual Press Brake (Machine 2).
/// </summary>
public static class VirtualPressBrakeRunProfile
{
	public const int RandomSeed = 194;

	public static readonly int[] BaselineProgramIndices = [0, 1, 2];

	public const int BaselineBatchQuantity = 10;

	public const bool UnattendedBaselineEnabled = true;

	public const bool DisableOperatorWaitsInUnattendedBaseline = true;

	/// <summary>Approximate baseline wall-clock duration at 1x simulation speed with 2x machine factor (~18-22 min).</summary>
	public const int ApproximateBaselineWallClockMinutes = 20;

	public static int ResolveSimulationSeed(Guid machineId, int globalSeed) =>
		machineId == VirtualPressBrakeContract.MachineId ? RandomSeed : globalSeed;

	public static void ApplyDeterministicSettings(SimulationSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.RandomSeed = RandomSeed;
		settings.GenerateNewSeedOnStart = false;
		settings.RandomModeEnabled = false;
	}

	public static FixedProductionJobDefinition ResolveJobDefinition(Guid machineId, int catalogIndex)
	{
		FixedProductionJobDefinition definition = FixedSimulationCatalog.GetDefinition(catalogIndex);
		if (machineId != VirtualPressBrakeContract.MachineId)
		{
			return definition;
		}

		int programIndex = BaselineProgramIndices[catalogIndex % BaselineProgramIndices.Length];
		return new FixedProductionJobDefinition
		{
			CatalogIndex = programIndex,
			JobName = $"PB-{programIndex + 1:00}",
			PartName = $"PB-PART-{programIndex + 1:00}",
			TargetQuantity = BaselineBatchQuantity,
			MaterialName = definition.MaterialName,
			MaterialThicknessMm = definition.MaterialThicknessMm,
			RecipeName = definition.RecipeName,
			ProgramName = $"PRG-{programIndex}"
		};
	}

	public static void SynchronizeSimulationJob(SimulationJob job, Guid machineId)
	{
		ArgumentNullException.ThrowIfNull(job);
		if (machineId != VirtualPressBrakeContract.MachineId || job.CatalogIndex < 0)
		{
			return;
		}

		FixedProductionJobDefinition definition = ResolveJobDefinition(machineId, job.CatalogIndex);
		job.JobName = definition.JobName;
		job.PartName = definition.PartName;
		job.TargetQuantity = definition.TargetQuantity;
		job.MaterialName = definition.MaterialName;
		job.MaterialThicknessMm = definition.MaterialThicknessMm;
		job.RecipeName = definition.RecipeName;
		job.ProgramName = definition.ProgramName;
	}

	public static void ResolveJobChangePauseRange(
		Guid machineId,
		int nextCatalogIndex,
		out int minPauseSeconds,
		out int maxPauseSeconds)
	{
		if (machineId != VirtualPressBrakeContract.MachineId)
		{
			minPauseSeconds = FixedSimulationCatalog.MinJobChangePauseSeconds;
			maxPauseSeconds = FixedSimulationCatalog.MaxJobChangePauseSeconds;
			return;
		}

		FixedProductionJobDefinition nextJob = ResolveJobDefinition(machineId, nextCatalogIndex);
		int pauseSeconds = (int)Math.Ceiling(
			PressBrakePhaseObservability.EstimateJobChangePauseSeconds(
				machineId,
				nextJob,
				RandomSeed));
		minPauseSeconds = pauseSeconds;
		maxPauseSeconds = pauseSeconds;
	}

	public static (double MinimumSeconds, double NominalSeconds, double MaximumSeconds) EstimateCompleteBaselineWallClockSeconds(
		double simulationSpeedFactor = 1.0,
		double productionSpeedFactor = 2.0)
	{
		double speedFactor = Math.Max(0.1, simulationSpeedFactor * productionSpeedFactor);
		double minimum = 0.0;
		double nominal = 0.0;
		double maximum = 0.0;
		for (int programIndex = 0; programIndex < BaselineProgramIndices.Length; programIndex++)
		{
			int catalogIndex = BaselineProgramIndices[programIndex];
			PressBrakeProgramDefinition program = PressBrakeProgramCatalog.GetProgram(catalogIndex);
			double partCycle = EstimateAveragePartCycleSeconds(program);
			double batch = partCycle * BaselineBatchQuantity;
			double transition = programIndex == 0
				? 0.0
				: PressBrakePhaseObservability.EstimateJobChangePauseSeconds(
					VirtualPressBrakeContract.MachineId,
					ResolveJobDefinition(VirtualPressBrakeContract.MachineId, catalogIndex),
					RandomSeed);
			minimum += (batch + transition) * 0.92;
			nominal += batch + transition;
			maximum += (batch + transition) * 1.08;
		}

		return (minimum / speedFactor, nominal / speedFactor, maximum / speedFactor);
	}

	private static double EstimateAveragePartCycleSeconds(PressBrakeProgramDefinition program)
	{
		if (program.Parts.Count == 0)
		{
			return 30.0;
		}

		double total = 0.0;
		foreach (PressBrakePartDefinition part in program.Parts)
		{
			double bendSeconds = part.BendSteps.Sum(step =>
				step.ApproachDurationSeconds
				+ step.FormingDurationSeconds
				+ step.HoldDurationSeconds
				+ step.ReturnDurationSeconds
				+ step.InterStepWaitSeconds);
			total += bendSeconds + part.InterPartWaitSeconds + program.SetupDurationSeconds / Math.Max(1, program.Parts.Count);
		}

		return total / program.Parts.Count;
	}
}
