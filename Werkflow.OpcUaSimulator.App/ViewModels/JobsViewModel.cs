using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class JobsViewModel : ObservableObject
{
	private readonly IConfigurationService _configurationService;

	public ObservableCollection<JobListItemViewModel> Jobs { get; } = new ObservableCollection<JobListItemViewModel>();

	public string InfoText => "Fester Auftragspool (Part-001 bis Part-020). Teile, Mengen und AuftrÃ¤ge sind fÃ¼r alle Tests gleich â€” nur die Zykluszeiten variieren zufÃ¤llig.";

	public JobsViewModel(IConfigurationService configurationService)
	{
		_configurationService = configurationService;
		Refresh();
	}

	public void Refresh()
	{
		Jobs.Clear();
		foreach (SimulationJob job in _configurationService.Configuration.Jobs.OrderBy((SimulationJob j) => j.PartName))
		{
			string machineName = (job.AssignedMachineId.HasValue ? (_configurationService.Configuration.Machines.FirstOrDefault(delegate(MachineConfiguration m)
			{
				Guid id = m.Id;
				Guid? assignedMachineId = job.AssignedMachineId;
				return id == assignedMachineId;
			})?.Name ?? "â€”") : "â€”");
			Jobs.Add(new JobListItemViewModel(job, machineName));
		}
	}
}
