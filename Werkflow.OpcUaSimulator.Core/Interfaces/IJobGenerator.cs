using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IJobGenerator
{
	List<SimulationJob> GenerateJobs(SimulationSettings settings, Random random);

	void RegenerateJobs(AppConfiguration config, Random random);
}
