using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class JobDispatcher : IJobDispatcher
{
	public void AssignJobs(AppConfiguration config, Random random)
	{
		List<MachineConfiguration> list = config.Machines.Where((MachineConfiguration m) => m.IsActive).ToList();
		List<SimulationJob> list2 = (from j in config.Jobs
			where j.Status == JobState.Pending
			orderby j.Priority descending
			select j).ToList();
		if (config.Settings.DistributeJobsRandomly)
		{
			list2 = list2.OrderBy((SimulationJob _) => random.Next()).ToList();
		}
		int num = 0;
		foreach (SimulationJob item in list2)
		{
			if (list.Count == 0)
			{
				break;
			}
			MachineConfiguration machineConfiguration = list[num % list.Count];
			item.AssignedMachineId = machineConfiguration.Id;
			item.Status = JobState.Assigned;
			num++;
		}
	}

	public SimulationJob? GetNextJobForMachine(Guid machineId, AppConfiguration config, Random random)
	{
		SimulationJob simulationJob = config.Jobs.FirstOrDefault(delegate(SimulationJob j)
		{
			bool flag = j.AssignedMachineId == machineId;
			bool flag2 = flag;
			if (flag2)
			{
				JobState status = j.Status;
				bool flag3 = (uint)(status - 1) <= 1u;
				flag2 = flag3;
			}
			return flag2;
		});
		if (simulationJob != null)
		{
			return simulationJob;
		}
		SimulationJob simulationJob2 = (from j in config.Jobs
			where j.Status == JobState.Pending || (config.Settings.ReuseCompletedJobs && j.Status == JobState.Completed)
			orderby j.Priority descending, j.CreatedAt
			select j).FirstOrDefault();
		if (simulationJob2 == null)
		{
			return null;
		}
		if (simulationJob2.Status == JobState.Completed)
		{
			simulationJob2 = simulationJob2.Clone();
			config.Jobs.Add(simulationJob2);
		}
		simulationJob2.AssignedMachineId = machineId;
		simulationJob2.Status = JobState.Assigned;
		simulationJob2.ActualCounter = 0;
		simulationJob2.StartedAt = null;
		simulationJob2.CompletedAt = null;
		return simulationJob2;
	}

	public void CompleteJob(SimulationJob job, MachineRuntimeState runtime)
	{
		job.ActualCounter = runtime.ActualCounter;
		job.Status = JobState.Completed;
		job.CompletedAt = DateTime.UtcNow;
		runtime.AssignedJobId = null;
	}
}
