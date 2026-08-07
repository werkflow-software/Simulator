using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IPhysicalMachineProfileLoader
{
	Task<PhysicalMachineProfile> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

	Task<IReadOnlyList<PhysicalMachineProfile>> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default(CancellationToken));

	PhysicalMachineProfile Deserialize(string json, string? sourcePath = null);
}
