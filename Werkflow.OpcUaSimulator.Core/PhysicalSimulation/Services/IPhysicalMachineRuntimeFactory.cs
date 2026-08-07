using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IPhysicalMachineRuntimeFactory
{
	PhysicalMachineRuntime Create(PhysicalMachineProfile profile, DateTimeOffset? createdAt = null);
}
