using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalPhysicsVerificationTests
{
	[Trait("Category", "Integration")]
	[Fact]
	public async Task Physics_NormalOperation_OneMachine()
	{
		R1LongRunReport report = await PhysicalPhysicsR1VerificationHarness.RunSingleMachineAsync();
		Assert.True(report.Passed);
		Assert.Equal(0, report.HardLimitViolations);
		Assert.InRange(report.Machines[0].SignalCount, 285, 320);
	}

	[Trait("Category", "Integration")]
	[Fact]
	public async Task Physics_NormalOperation_TwoMachines()
	{
		R1LongRunReport report = await PhysicalPhysicsR1VerificationHarness.RunDualMachineAsync();
		Assert.True(report.Passed);
		Assert.Equal(2, report.Machines.Count);
		Assert.Equal(2, report.Machines.Select((R1MachineReport m) => m.ProfileId).Distinct().Count());
	}

	[Trait("Category", "Integration")]
	[Fact]
	public async Task Physics_R1EvidenceExport_WhenRequested()
	{
		if (string.Equals(Environment.GetEnvironmentVariable("PHYSICS_VERIFY_EXPORT"), "1", StringComparison.Ordinal))
		{
			await PhysicalPhysicsR1VerificationHarness.ExportEvidenceAsync(await PhysicalPhysicsR1VerificationHarness.RunSingleMachineAsync(), await PhysicalPhysicsR1VerificationHarness.RunDualMachineAsync());
			Assert.True(Directory.Exists(PhysicalPhysicsR1VerificationHarness.EvidenceDirectory));
		}
	}
}
