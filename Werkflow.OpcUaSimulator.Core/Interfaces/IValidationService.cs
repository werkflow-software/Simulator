using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IValidationService
{
	ValidationResult ValidateForSimulationStart(AppConfiguration config);

	ValidationResult ValidateMachine(MachineConfiguration machine, IReadOnlyList<MachineConfiguration> allMachines);

	ValidationResult ValidatePorts(IReadOnlyList<MachineConfiguration> machines);
}
