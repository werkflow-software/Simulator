using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public static class AutonomousCellProductionTimeEstimator
{
	private static readonly Dictionary<AutonomousCellMotionPhase, double> NominalPhaseDurations = new()
	{
		[AutonomousCellMotionPhase.HiddenInboundDelivery] = 2.0,
		[AutonomousCellMotionPhase.LoadPick] = 8.0,
		[AutonomousCellMotionPhase.LoadTransfer] = 6.0,
		[AutonomousCellMotionPhase.FixtureClamp] = 4.0,
		[AutonomousCellMotionPhase.ProcessApproach] = 3.0,
		[AutonomousCellMotionPhase.ProcessPressFit] = 12.0,
		[AutonomousCellMotionPhase.ProcessRetract] = 3.0,
		[AutonomousCellMotionPhase.FixtureRelease] = 2.0,
		[AutonomousCellMotionPhase.TransferPickup] = 5.0,
		[AutonomousCellMotionPhase.TransferToVision] = 5.0,
		[AutonomousCellMotionPhase.VisionInspect] = 3.0,
		[AutonomousCellMotionPhase.SortOutput] = 2.0,
		[AutonomousCellMotionPhase.ContainerFill] = 1.0,
		[AutonomousCellMotionPhase.WaitReplenishment] = 30.0,
		[AutonomousCellMotionPhase.WaitContainerExchange] = 35.0,
		[AutonomousCellMotionPhase.HiddenOutboundExchange] = 3.0
	};

	public static double EstimateAveragePartCycleSeconds(int seed) =>
		AutonomousCellPhaseObservability.GetStandardPartCycle()
			.Sum(phase => ResolveDuration(seed, phase, Machine3SeedArchitecture.PhysicalProcessSeed));

	public static double EstimateCurrentPartRemainingSeconds(AutonomousCellKinematicsState cell, int seed)
	{
		if (cell.MotionPhase == AutonomousCellMotionPhase.Complete)
		{
			return 0.0;
		}

		double remaining = Math.Max(0.0, cell.PhaseDurationSeconds - cell.PhaseElapsedSeconds);
		if (IsLogisticsWait(cell.MotionPhase))
		{
			return remaining;
		}

		IReadOnlyList<AutonomousCellMotionPhase> cycle = AutonomousCellPhaseObservability.GetStandardPartCycle();
		int currentIndex = Array.IndexOf(cycle.ToArray(), cell.MotionPhase);
		if (currentIndex >= 0)
		{
			for (int i = currentIndex + 1; i < cycle.Count; i++)
			{
				remaining += ResolveDuration(seed, cycle[i], ResolveStreamSeed(cycle[i]));
			}

			return remaining;
		}

		if (cell.MotionPhase == AutonomousCellMotionPhase.HiddenInboundDelivery)
		{
			remaining += SumCycleDuration(seed);
		}

		return remaining;
	}

	public static double EstimateJobRemainingSeconds(AutonomousCellKinematicsState cell, int seed)
	{
		if (cell.MotionPhase == AutonomousCellMotionPhase.Complete)
		{
			return 0.0;
		}

		int targetParts = cell.TargetParts > 0 ? cell.TargetParts : VirtualAutonomousCellRunProfile.TotalParts;
		double remaining = EstimateCurrentPartRemainingSeconds(cell, seed);
		int partsAfterCurrent = Math.Max(0, targetParts - cell.CompletedParts - 1);
		double averagePart = EstimateAveragePartCycleSeconds(seed);
		remaining += partsAfterCurrent * averagePart;
		remaining += EstimateFutureLogisticsSeconds(cell);
		return remaining;
	}

	public static double EstimateImplementedBaselineWallClockSeconds(int seed)
	{
		double perPart = EstimateAveragePartCycleSeconds(seed);
		double logistics = 2.0 * (ResolveDuration(seed, AutonomousCellMotionPhase.WaitReplenishment, Machine3SeedArchitecture.LogisticsAmrSeed)
			+ ResolveDuration(seed, AutonomousCellMotionPhase.HiddenInboundDelivery, Machine3SeedArchitecture.LogisticsAmrSeed))
			+ 2.0 * (ResolveDuration(seed, AutonomousCellMotionPhase.WaitContainerExchange, Machine3SeedArchitecture.LogisticsAmrSeed)
			+ ResolveDuration(seed, AutonomousCellMotionPhase.HiddenOutboundExchange, Machine3SeedArchitecture.LogisticsAmrSeed));
		return VirtualAutonomousCellRunProfile.TotalParts * perPart
			+ ResolveDuration(seed, AutonomousCellMotionPhase.HiddenInboundDelivery, Machine3SeedArchitecture.LogisticsAmrSeed)
			+ logistics;
	}

	private static double EstimateFutureLogisticsSeconds(AutonomousCellKinematicsState cell)
	{
		double seconds = 0.0;
		int completed = cell.CompletedParts;
		if (completed < VirtualAutonomousCellRunProfile.ExchangeAfterPart1)
		{
			seconds += NominalPhaseDurations[AutonomousCellMotionPhase.WaitContainerExchange]
				+ NominalPhaseDurations[AutonomousCellMotionPhase.HiddenOutboundExchange];
		}

		if (completed < VirtualAutonomousCellRunProfile.ExchangeAfterPart2)
		{
			seconds += NominalPhaseDurations[AutonomousCellMotionPhase.WaitContainerExchange]
				+ NominalPhaseDurations[AutonomousCellMotionPhase.HiddenOutboundExchange];
		}

		if (completed < VirtualAutonomousCellRunProfile.ReplenishmentAfterPart1)
		{
			seconds += NominalPhaseDurations[AutonomousCellMotionPhase.WaitReplenishment]
				+ NominalPhaseDurations[AutonomousCellMotionPhase.HiddenInboundDelivery];
		}

		if (completed < VirtualAutonomousCellRunProfile.ReplenishmentAfterPart2)
		{
			seconds += NominalPhaseDurations[AutonomousCellMotionPhase.WaitReplenishment]
				+ NominalPhaseDurations[AutonomousCellMotionPhase.HiddenInboundDelivery];
		}

		return seconds;
	}

	private static double SumCycleDuration(int seed) =>
		AutonomousCellPhaseObservability.GetStandardPartCycle()
			.Sum(phase => ResolveDuration(seed, phase, ResolveStreamSeed(phase)));

	private static bool IsLogisticsWait(AutonomousCellMotionPhase phase) =>
		phase is AutonomousCellMotionPhase.WaitReplenishment
			or AutonomousCellMotionPhase.WaitContainerExchange
			or AutonomousCellMotionPhase.HiddenInboundDelivery
			or AutonomousCellMotionPhase.HiddenOutboundExchange
			or AutonomousCellMotionPhase.WaitRawMaterial;

	private static int ResolveStreamSeed(AutonomousCellMotionPhase phase) =>
		phase switch
		{
			AutonomousCellMotionPhase.LoadPick or AutonomousCellMotionPhase.LoadTransfer
				or AutonomousCellMotionPhase.TransferPickup or AutonomousCellMotionPhase.TransferToVision
				=> Machine3SeedArchitecture.RobotVariabilitySeed,
			AutonomousCellMotionPhase.VisionInspect => Machine3SeedArchitecture.VisionSeed,
			AutonomousCellMotionPhase.WaitReplenishment or AutonomousCellMotionPhase.WaitContainerExchange
				or AutonomousCellMotionPhase.HiddenInboundDelivery or AutonomousCellMotionPhase.HiddenOutboundExchange
				=> Machine3SeedArchitecture.LogisticsAmrSeed,
			_ => Machine3SeedArchitecture.PhysicalProcessSeed
		};

	private static double ResolveDuration(int seed, AutonomousCellMotionPhase phase, int streamSeed)
	{
		if (!NominalPhaseDurations.TryGetValue(phase, out double nominal))
		{
			nominal = 2.0;
		}

		Random random = new(streamSeed ^ (int)phase ^ seed);
		double factor = 0.92 + random.NextDouble() * 0.16;
		return nominal * factor;
	}
}
