using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class JobGenerator : IJobGenerator
{
	public List<SimulationJob> GenerateJobs(SimulationSettings settings, Random random)
	{
		return (from j in FixedSimulationCatalog.CreateJobs()
			select j.Clone()).ToList();
	}

	public void RegenerateJobs(AppConfiguration config, Random random)
	{
		config.Jobs = (from j in FixedSimulationCatalog.CreateJobs()
			select j.Clone()).ToList();
	}
}
