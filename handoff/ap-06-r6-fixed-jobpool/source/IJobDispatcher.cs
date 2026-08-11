using System;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IJobDispatcher
{
	void AssignJobs(AppConfiguration config, Random random);

	SimulationJob? GetNextJobForMachine(Guid machineId, AppConfiguration config, Random random);

	SimulationJob? GetJobByCatalogIndex(int catalogIndex, AppConfiguration config);

	void CompleteJob(SimulationJob job, MachineRuntimeState runtime);
}
