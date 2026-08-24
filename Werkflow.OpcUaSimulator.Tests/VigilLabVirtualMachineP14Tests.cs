using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public sealed class VigilLabVirtualMachineP14Tests
{
	[Fact]
	public void P14_Run004ShortProfile_Job001_TargetQuantity_IsSix()
	{
		var job = VigilLabRunProfile.ResolveJobDefinition(VigilLabMachineContract.MachineId, 0);

		Assert.Equal("JOB-001", job.JobName);
		Assert.Equal("Halter_01", job.PartName);
		Assert.Equal(6, job.TargetQuantity);
		Assert.Equal(VigilLabRunProfile.Run004Job1Quantity, job.TargetQuantity);
	}

	[Fact]
	public void P14_Run004ShortProfile_Job002_TargetQuantity_IsSix()
	{
		var job = VigilLabRunProfile.ResolveJobDefinition(VigilLabMachineContract.MachineId, 1);

		Assert.Equal("JOB-002", job.JobName);
		Assert.Equal("Flansch_02", job.PartName);
		Assert.Equal(6, job.TargetQuantity);
		Assert.Equal(VigilLabRunProfile.Run004Job2Quantity, job.TargetQuantity);
	}

	[Fact]
	public void P14_OtherMachines_KeepCatalogTargetQuantities()
	{
		var existingLaser = DefaultMachines.Create().First(m => m.Id == VirtualMachineContract.MachineId);
		var job1 = VigilLabRunProfile.ResolveJobDefinition(existingLaser.Id, 0);
		var job2 = VigilLabRunProfile.ResolveJobDefinition(existingLaser.Id, 1);

		Assert.Equal(50, job1.TargetQuantity);
		Assert.Equal(75, job2.TargetQuantity);
		Assert.Equal(FixedSimulationCatalog.GetDefinition(0).TargetQuantity, job1.TargetQuantity);
		Assert.Equal(FixedSimulationCatalog.GetDefinition(1).TargetQuantity, job2.TargetQuantity);
	}

	[Fact]
	public async Task P14_JobTransition_RemainsNaturalWithRun004Quantities()
	{
		var harness = new VigilLabP09TestHarness();
		await harness.StartVigilLabAsync();

		Assert.Equal("JOB-001", harness.Runtime.JobName);
		Assert.Equal(6, harness.Runtime.TargetCounter);

		await harness.Engine.CompleteJobAsync(harness.VigilLab.Id);
		Assert.True(harness.Runtime.IsJobChangeActive);
		Assert.Equal("JOB-002", harness.Runtime.NextJobNamePreview);
		Assert.Equal(6, harness.Runtime.NextTargetQuantityPreview);

		await harness.AdvanceDueJobChangeAsync();

		Assert.False(harness.Runtime.IsJobChangeActive);
		Assert.Equal("JOB-002", harness.Runtime.JobName);
		Assert.Equal(6, harness.Runtime.TargetCounter);
		Assert.Equal(0, harness.Runtime.ActualCounter);
	}

	[Fact]
	public void P14_JobChangeDelay_UsesRun004SimulationBounds()
	{
		VigilLabRunProfile.ResolveJobChangePauseRange(
			VigilLabMachineContract.MachineId,
			0,
			out int minPauseSeconds,
			out int maxPauseSeconds);

		Assert.Equal(60, minPauseSeconds);
		Assert.Equal(120, maxPauseSeconds);
	}

	[Fact]
	public void P14_JobChangeDelay_ResolvesToThirtyToSixtySecondsWallClockAtDefaultSpeedFactors()
	{
		var vigilLab = DefaultMachines.CreateVigilLabMachine();
		var settings = FixedSimulationCatalog.CreateDefaultSettings();
		VigilLabRunProfile.ApplyDeterministicSettings(settings);

		(double minWallSeconds, double maxWallSeconds) = VigilLabRunProfile.ResolveExpectedJobChangeWallClock(
			settings.SimulationSpeedFactor,
			vigilLab.ProductionSpeedFactor);

		Assert.Equal(30.0, minWallSeconds, precision: 1);
		Assert.Equal(60.0, maxWallSeconds, precision: 1);
	}

	[Fact]
	public async Task P14_ScheduledJobChange_WallClockDelayFallsWithinExpectedRange()
	{
		var harness = new VigilLabP09TestHarness();
		await harness.StartVigilLabAsync();

		DateTime before = DateTime.UtcNow;
		await harness.Engine.CompleteJobAsync(harness.VigilLab.Id);
		DateTime afterSchedule = DateTime.UtcNow;

		Assert.True(harness.Runtime.IsJobChangeActive);
		Assert.InRange(harness.Runtime.JobChangePauseSeconds, 60, 120);
		Assert.NotNull(harness.Runtime.JobChangeEndsAtUtc);

		double speedFactor = Math.Max(
			0.1,
			harness.ConfigurationService.Configuration.Settings.SimulationSpeedFactor
				* harness.VigilLab.ProductionSpeedFactor);
		double expectedMinWallMs = 60_000.0 / speedFactor;
		double expectedMaxWallMs = 120_000.0 / speedFactor;
		double actualWallMs = (harness.Runtime.JobChangeEndsAtUtc!.Value - before).TotalMilliseconds;

		Assert.InRange(actualWallMs, expectedMinWallMs - 250.0, expectedMaxWallMs + 250.0);
		Assert.True(afterSchedule >= before);
	}

	[Fact]
	public void P14_ReducedSignalContract_RemainsUnchanged()
	{
		var profile = VigilLabLaserReducedProfileFactory.Create();
		var enabled = profile.Signals.Where(signal => signal.IsEnabled).Select(signal => signal.SignalId).OrderBy(id => id).ToArray();

		Assert.Equal(7, enabled.Length);
		Assert.Equal(
			VigilLabLaserReducedProfileFactory.EnabledPhysicalSignalIds.OrderBy(id => id).ToArray(),
			enabled);
	}

	[Fact]
	public void P14_SinglePartPhysicsProfile_RemainsUnchanged()
	{
		var profile = VigilLabLaserReducedProfileFactory.Create();

		Assert.Equal("vigil-lab-laser-reduced", profile.ProfileId);
		Assert.Equal("vigil-lab-run-001", profile.Metadata["purpose"]);
		Assert.Equal(VigilLabMachineContract.PhysicalProfileId, profile.ProfileId);
	}

	[Fact]
	public void P14_Run002FrozenReference_RemainsAvailableForHistoricalRuns()
	{
		Assert.Equal(11, VigilLabRunProfile.Run002Job1Quantity);
		Assert.Equal(15, VigilLabRunProfile.Run002Job2Quantity);
	}

	[Fact]
	public void P14_NonVigilLabMachines_KeepCatalogJobChangePauseBounds()
	{
		var existingLaser = DefaultMachines.Create().First(m => m.Id == VirtualMachineContract.MachineId);
		VigilLabRunProfile.ResolveJobChangePauseRange(
			existingLaser.Id,
			0,
			out int minPauseSeconds,
			out int maxPauseSeconds);

		Assert.Equal(FixedSimulationCatalog.MinJobChangePauseSeconds, minPauseSeconds);
		Assert.Equal(FixedSimulationCatalog.MaxJobChangePauseSeconds, maxPauseSeconds);
	}
}
