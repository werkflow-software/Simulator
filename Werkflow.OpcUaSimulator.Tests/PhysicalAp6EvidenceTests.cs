using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp6EvidenceTests
{
	[Fact]
	public async Task AP6_Evidence_ExportVerificationJson()
	{
		var report = await PhysicalAp6VerificationHarness.RunVerificationAsync();
		await PhysicalAp6VerificationHarness.ExportEvidenceAsync(report);
		Assert.True(report.Passed, string.Join(",", report.FailedCriteria));
		Assert.False(string.IsNullOrWhiteSpace(report.VerificationRunId));
		Assert.Equal("opc.tcp://localhost:4840", report.VirtualMachine.Endpoint);
	}
}
