using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Services;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class ValidationServiceTests
{
	[Fact]
	public void ValidatePorts_DetectsConflict()
	{
		ValidationService validationService = new ValidationService();
		List<MachineConfiguration> machines = new List<MachineConfiguration>
		{
			new MachineConfiguration
			{
				Name = "A",
				Port = 4840
			},
			new MachineConfiguration
			{
				Name = "B",
				Port = 4840
			}
		};
		ValidationResult validationResult = validationService.ValidatePorts(machines);
		Assert.False(validationResult.IsValid);
	}
}
