using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IPhysicalMachineSessionFactory
{
	PhysicalMachineSession? TryCreateSession(Guid machineId, string machineName, string? physicalProfileId);

	PhysicalMachineProfile? ResolveProfile(string profileId);

	IReadOnlyList<string> GetAvailableProfileIds();
}
