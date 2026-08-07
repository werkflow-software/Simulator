using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Services;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class JobGeneratorTests
{
	[Fact]
	public void GenerateJobs_CreatesFixedCatalog()
	{
		JobGenerator jobGenerator = new JobGenerator();
		List<SimulationJob> list = jobGenerator.GenerateJobs(new SimulationSettings(), new Random(99));
		Assert.Equal(20, list.Count);
		Assert.Equal("Part-001", list[0].PartName);
		Assert.Equal("Job-020", list[19].JobName);
		Assert.Equal(FixedSimulationCatalog.BatchSizes[0], list[0].TargetQuantity);
		Assert.Equal(FixedSimulationCatalog.BatchSizes[19], list[19].TargetQuantity);
	}
}
