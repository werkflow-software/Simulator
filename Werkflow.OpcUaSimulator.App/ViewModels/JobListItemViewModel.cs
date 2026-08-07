using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class JobListItemViewModel
{
	public string PartName { get; }

	public string JobName { get; }

	public int TargetQuantity { get; }

	public int ActualCounter { get; }

	public int Priority { get; }

	public JobState Status { get; }

	public string MachineName { get; }

	public string StatusLabel => Status.ToGermanLabel();

	public JobListItemViewModel(SimulationJob job, string machineName)
	{
		PartName = job.PartName;
		JobName = job.JobName;
		TargetQuantity = job.TargetQuantity;
		ActualCounter = job.ActualCounter;
		Priority = job.Priority;
		Status = job.Status;
		MachineName = machineName;
	}
}
