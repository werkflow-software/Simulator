using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalSignalVerificationTests
{
	private static readonly string ResultsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-02-physical-signals", "verification-results"));

	[Fact]
	public async Task Verification_Smoke_OneMachine_30Seconds()
	{
		VerificationReport report = await PhysicalSignalVerificationHarness.RunAsync(new VerificationOptions
		{
			MachineCount = 1,
			PublishDuration = TimeSpan.FromSeconds(30.0),
			Seed = 42,
			TestPauseResume = true,
			TestDataChange = true
		});
		int num = report.SignalCountPerMachine[0];
		Assert.True(num >= 285 && num <= 320);
		Assert.True(report.RegisteredNodesPerMachine[0] > 0);
		Assert.True(report.FailedUpdatesPerMachine.All((int f) => f == 0));
		Assert.True(report.ActivePublishers >= 0);
		Directory.CreateDirectory(ResultsDir);
		await File.WriteAllTextAsync(Path.Combine(ResultsDir, "smoke-test-a.json"), report.ToJson());
	}

	[Fact]
	public async Task Verification_TestC_PauseResume_TwoMachines_WhenEnabled()
	{
		if (string.Equals(Environment.GetEnvironmentVariable("PHYSICAL_VERIFY_C"), "1", StringComparison.Ordinal))
		{
			Directory.CreateDirectory(ResultsDir);
			VerificationReport testC = await PhysicalSignalVerificationHarness.RunAsync(new VerificationOptions
			{
				MachineCount = 2,
				PublishDuration = TimeSpan.FromMinutes(2.0),
				Seed = 42,
				TestPauseResume = true,
				TestDataChange = false,
				TestMachineIsolation = false
			});
			await File.WriteAllTextAsync(Path.Combine(ResultsDir, "test-c-pause-resume.json"), testC.ToJson());
			Assert.True(testC.ValuesStableDuringPause);
			Assert.True(testC.ServerReachableDuringPause);
			Assert.True(testC.NoDuplicatePublishersAfterResume);
		}
	}

	[Fact]
	public async Task Verification_Full_WhenEnabled()
	{
		if (string.Equals(Environment.GetEnvironmentVariable("PHYSICAL_VERIFY_FULL"), "1", StringComparison.Ordinal))
		{
			Directory.CreateDirectory(ResultsDir);
			VerificationReport testA = await PhysicalSignalVerificationHarness.RunAsync(new VerificationOptions
			{
				MachineCount = 1,
				PublishDuration = TimeSpan.FromMinutes(10.0),
				Seed = 42,
				TestPauseResume = true,
				TestDataChange = true
			});
			await File.WriteAllTextAsync(Path.Combine(ResultsDir, "test-a-one-machine-10min.json"), testA.ToJson());
			VerificationReport testB = await PhysicalSignalVerificationHarness.RunAsync(new VerificationOptions
			{
				MachineCount = 2,
				PublishDuration = TimeSpan.FromMinutes(10.0),
				Seed = 42,
				TestPauseResume = false,
				TestDataChange = false,
				TestMachineIsolation = true
			});
			await File.WriteAllTextAsync(Path.Combine(ResultsDir, "test-b-two-machines-10min.json"), testB.ToJson());
			VerificationReport testC = await PhysicalSignalVerificationHarness.RunAsync(new VerificationOptions
			{
				MachineCount = 2,
				PublishDuration = TimeSpan.FromMinutes(2.0),
				Seed = 42,
				TestPauseResume = true,
				TestDataChange = false
			});
			await File.WriteAllTextAsync(Path.Combine(ResultsDir, "test-c-pause-resume.json"), testC.ToJson());
			VerificationReport testD = await PhysicalSignalVerificationHarness.RunAsync(new VerificationOptions
			{
				MachineCount = 1,
				PublishDuration = TimeSpan.FromSeconds(30.0),
				Seed = 42,
				TestPauseResume = false,
				TestDataChange = false,
				TestStopRestartCycles = 3
			});
			await File.WriteAllTextAsync(Path.Combine(ResultsDir, "test-d-restart-cycle.json"), testD.ToJson());
			await File.WriteAllTextAsync(Path.Combine(ResultsDir, "test-e-datachange.json"), JsonSerializer.Serialize(testA.DataChangeResults, new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			Assert.True(testA.FailedUpdatesPerMachine.All((int x) => x == 0));
			Assert.True(testB.Machine1StoppedSuccessfully);
			Assert.True(testB.Machine1RestartSameNodeCount);
			Assert.True(testC.ValuesStableDuringPause);
			Assert.True(testD.StopRestartCycles.Count == 3);
			Assert.True(testD.StopRestartCycles.All((StopRestartCycleResult c) => c.RegistryCleared && c.SameNodeCount && c.SinglePublisher));
			Assert.True(testA.DataChangeResults.Count >= 7);
			Assert.True(testA.DataChangeResults.All((DataChangeSample s) => s.TypeMatches));
		}
	}
}
