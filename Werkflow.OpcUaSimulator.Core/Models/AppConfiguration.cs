using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class AppConfiguration
{
	public SimulationSettings Settings { get; set; } = new SimulationSettings();

	public List<MachineConfiguration> Machines { get; set; } = new List<MachineConfiguration>();

	public List<SimulationJob> Jobs { get; set; } = new List<SimulationJob>();

	public EventSettings Events { get; set; } = new EventSettings();
}
