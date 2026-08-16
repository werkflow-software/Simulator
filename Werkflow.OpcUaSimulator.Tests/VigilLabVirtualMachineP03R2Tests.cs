using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public sealed class VigilLabVirtualMachineP03R2Tests
{
	[Fact]
	public async Task P03R2_EmptyStore_CreatesValidCertificate()
	{
		var machine = OpcUaCertificateLifecycleHarness.CreateVigilLabTestMachine();
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(machine);

		var result = await OpcUaCertificateLifecycleHarness.ProbeCertificateEnsureAsync(machine);
		Assert.True(result.Success, result.ExceptionMessage);
		Assert.True(result.CertificateFound);
		Assert.True(result.HasPrivateKey);
		Assert.Equal(VigilLabMachineContract.NamespaceUri, result.ApplicationUri);
		Assert.Contains("VIGIL LAB Laser", result.CertificateSubjectResolved, StringComparison.OrdinalIgnoreCase);
		Assert.True(result.FilesAfter >= 2, "Expected cert + private key files in PKI store.");
	}

	[Fact]
	public async Task P03R2_ValidCertificateRestart_ReusesWithoutRecreatingFiles()
	{
		var machine = OpcUaCertificateLifecycleHarness.CreateVigilLabTestMachine();
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(machine);

		var first = await OpcUaCertificateLifecycleHarness.ProbeCertificateEnsureAsync(machine);
		Assert.True(first.Success, first.ExceptionMessage);
		int filesAfterFirst = first.FilesAfter;

		var second = await OpcUaCertificateLifecycleHarness.ProbeCertificateEnsureAsync(machine);
		Assert.True(second.Success, second.ExceptionMessage);
		Assert.Equal(filesAfterFirst, second.FilesAfter);
	}

	[Fact]
	public async Task P03R2_StaleWrongUriCertificate_RecoversThroughProductionEnsurePath()
	{
		var machine = OpcUaCertificateLifecycleHarness.CreateVigilLabTestMachine();
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(machine);
		await OpcUaCertificateLifecycleHarness.SeedStaleWrongUriCertificateAsync(machine);

		var staleProbe = await OpcUaCertificateLifecycleHarness.ProbeCertificateEnsureAsync(machine);
		Assert.True(staleProbe.Success, staleProbe.ExceptionMessage ?? staleProbe.StatusCode);
		Assert.True(staleProbe.HasPrivateKey);
		Assert.Contains("VIGIL LAB Laser", staleProbe.CertificateSubjectResolved, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(VigilLabMachineContract.NamespaceUri, staleProbe.ApplicationUri);
	}

	[Fact]
	public async Task P03R2_VigilLab_ProductionServerStartup_ReadsMachineState()
	{
		var machine = OpcUaCertificateLifecycleHarness.CreateVigilLabTestMachine();
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(machine);

		var result = await OpcUaCertificateLifecycleHarness.ProbeProductionServerStartupAsync(machine);
		Assert.True(result.Success, result.ExceptionMessage ?? result.ReadResult?.Value);
		Assert.True(result.ServerStarted);
		Assert.NotNull(result.ReadResult?.Value);
	}

	[Fact]
	public async Task P03R2_StaleCertificate_ProductionServerStartup_RecoversAndReadsMachineState()
	{
		var machine = OpcUaCertificateLifecycleHarness.CreateVigilLabTestMachine();
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(machine);
		await OpcUaCertificateLifecycleHarness.SeedStaleWrongUriCertificateAsync(machine);

		var result = await OpcUaCertificateLifecycleHarness.ProbeProductionServerStartupAsync(machine);
		Assert.True(result.Success, result.ExceptionMessage ?? result.ReadResult?.Value);
		Assert.True(result.ServerStarted);
	}

	[Fact]
	public async Task P03R2_DualOpcUaServers_CanStartAndReadBothEndpoints()
	{
		var result = await VigilLabVirtualMachineP03R1ReproHarness.ReproduceDualServerStartupAsync(
			OpcUaCertificateLifecycleHarness.ExistingLaserTestPort,
			OpcUaCertificateLifecycleHarness.VigilLabTestPort);
		Assert.False(result.BadServerHalted, result.ExceptionMessage);
		Assert.True(result.FirstServerStarted, result.ExceptionMessage ?? "first server did not start");
		Assert.True(result.SecondServerStarted);
		Assert.True(result.FirstStillRunningAfterSecondStart);
		Assert.True(result.SecondStillRunningAfterFirstStop);
		Assert.True(result.FirstReadAfterSecondStart?.Success ?? false, result.FirstReadAfterSecondStart?.Value);
		Assert.True(result.SecondReadAfterSecondStart?.Success ?? false, result.SecondReadAfterSecondStart?.Value);
		Assert.True(result.SecondReadAfterFirstStop?.Success ?? false, result.SecondReadAfterFirstStop?.Value);
	}

	[Fact]
	public async Task P03R2_BothMachineCertificates_AreIsolated()
	{
		var existingLaser = OpcUaCertificateLifecycleHarness.CreateExistingLaserTestMachine();
		var vigilLab = OpcUaCertificateLifecycleHarness.CreateVigilLabTestMachine();
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(existingLaser);
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(vigilLab);

		var existingResult = await OpcUaCertificateLifecycleHarness.ProbeCertificateEnsureAsync(existingLaser);
		var vigilResult = await OpcUaCertificateLifecycleHarness.ProbeCertificateEnsureAsync(vigilLab);

		Assert.True(existingResult.Success, existingResult.ExceptionMessage);
		Assert.True(vigilResult.Success, vigilResult.ExceptionMessage);
		Assert.NotEqual(existingResult.PkiRoot, vigilResult.PkiRoot);
		Assert.Equal("urn:werkflow:simulator:machine1", existingResult.ApplicationUri);
		Assert.Equal(VigilLabMachineContract.NamespaceUri, vigilResult.ApplicationUri);
	}
}
