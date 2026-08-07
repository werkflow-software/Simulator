using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public interface IPhysicalMachineProfileValidator
{
	PhysicalProfileValidationResult Validate(PhysicalMachineProfile profile);
}
