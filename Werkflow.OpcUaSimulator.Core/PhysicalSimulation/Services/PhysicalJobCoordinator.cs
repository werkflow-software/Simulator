using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class PhysicalJobCoordinator
{
	private static readonly (string Job, string Part)[] VerificationJobs = new(string, string)[3]
	{
		("JOB-001", "PART-A"),
		("JOB-002", "PART-B"),
		("JOB-003", "PART-C")
	};

	public static void Initialize(PhysicalSimulationContext context)
	{
		context.Job = new PhysicalJobState
		{
			JobIndex = 1,
			JobName = VerificationJobs[0].Job,
			PartName = VerificationJobs[0].Part,
			TargetQuantity = ((context.VerificationMode == PhysicalVerificationMode.Short) ? 8 : 25),
			ProducedQuantity = 0,
			JobStartedAtUtc = DateTimeOffset.UtcNow
		};
	}

	public static void AdvanceJob(PhysicalSimulationContext context)
	{
		context.Job.JobIndex++;
		int num = (context.Job.JobIndex - 1) % VerificationJobs.Length;
		(string, string) tuple = VerificationJobs[num];
		context.Job.JobName = tuple.Item1;
		context.Job.PartName = tuple.Item2;
		context.Job.TargetQuantity = ((context.VerificationMode == PhysicalVerificationMode.Short) ? 8 : 25);
		context.Job.ProducedQuantity = 0;
		context.Job.JobStartedAtUtc = DateTimeOffset.UtcNow;
		context.Metrics.JobChanges++;
	}

	public static void TickProductionCounters(PhysicalSimulationContext context)
	{
		ProcessPhase currentPhase = context.CurrentPhase;
		if ((uint)(currentPhase - 3) <= 1u)
		{
			context.Job.ProducedQuantity++;
		}
	}
}
