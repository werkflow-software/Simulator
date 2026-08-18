using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public sealed class VigilLabVirtualMachineP09Tests
{
	[Fact]
	public void P09_RootCause_StaleJobPool_IsReplacedByCatalogOnAssign()
	{
		var harness = new VigilLabP09TestHarness();
		SimulationJob staleJob = harness.ConfigurationService.Configuration.Jobs.First(j => j.CatalogIndex == 0);
		staleJob.JobName = "Job-001";
		staleJob.TargetQuantity = 125;

		harness.Engine.AssignJobIfMissingAsync(harness.VigilLab.Id).GetAwaiter().GetResult();

		MachineRuntimeState runtime = harness.Runtime;
		Assert.Equal("JOB-001", runtime.JobName);
		Assert.Equal(VigilLabRunProfile.ActiveJob1Quantity, runtime.TargetCounter);
		Assert.Equal(VigilLabRunProfile.ActiveJob1Quantity, harness.Publisher.GetLatest(harness.VigilLab.Id, NodeSemanticType.TargetCounter));
	}

	[Fact]
	public async Task P09_JobTransition_PublishesNewJobNameAndTargetCounter()
	{
		var harness = new VigilLabP09TestHarness();
		await harness.StartVigilLabAsync();

		Assert.Equal("JOB-001", harness.Runtime.JobName);
		Assert.Equal(VigilLabRunProfile.ActiveJob1Quantity, harness.Runtime.TargetCounter);

		await harness.Engine.CompleteJobAsync(harness.VigilLab.Id);
		Assert.True(harness.Runtime.IsJobChangeActive);
		Assert.Equal("JOB-002", harness.Runtime.NextJobNamePreview);

		await harness.AdvanceDueJobChangeAsync();

		Assert.False(harness.Runtime.IsJobChangeActive);
		Assert.Equal("JOB-002", harness.Runtime.JobName);
		Assert.Equal(VigilLabRunProfile.ActiveJob2Quantity, harness.Runtime.TargetCounter);
		Assert.Equal(0, harness.Runtime.ActualCounter);
		Assert.Contains("JOB-002", harness.Publisher.GetHistory(harness.VigilLab.Id, NodeSemanticType.JobName).Cast<string>());
		Assert.Contains(VigilLabRunProfile.ActiveJob2Quantity, harness.Publisher.GetHistory(harness.VigilLab.Id, NodeSemanticType.TargetCounter).Cast<int>());
	}

	[Fact]
	public async Task P09_HmiAndOpcState_AreConsistentAfterJobTransition()
	{
		var harness = new VigilLabP09TestHarness();
		await harness.StartVigilLabAsync();

		var viewModel = VirtualMachineHmiContextHarness.CreateViewModel(
			harness.Stack,
			[harness.ExistingLaser, harness.VigilLab],
			harness.Engine,
			harness.Server);
		viewModel.EnsureActivated();
		viewModel.SelectedMachineId = harness.VigilLab.Id;
		viewModel.Refresh();

		Assert.Equal(harness.Runtime.JobName, viewModel.JobName);
		Assert.Equal($"{harness.Runtime.ActualCounter} / {harness.Runtime.TargetCounter}", viewModel.CounterText);

		await harness.Engine.CompleteJobAsync(harness.VigilLab.Id);
		await harness.AdvanceDueJobChangeAsync();
		viewModel.Refresh();

		Assert.Equal(harness.Runtime.JobName, viewModel.JobName);
		Assert.Equal(harness.Publisher.GetLatest(harness.VigilLab.Id, NodeSemanticType.JobName), viewModel.JobName);
		Assert.Equal($"{harness.Runtime.ActualCounter} / {harness.Runtime.TargetCounter}", viewModel.CounterText);
	}

	[Fact]
	public async Task P09_DataChangeRegression_JobNameAndTargetCounterChangeOnTransition()
	{
		var harness = new VigilLabP09TestHarness();
		await harness.StartVigilLabAsync();

		int initialJobChanges = harness.Publisher.GetChangeCount(harness.VigilLab.Id, NodeSemanticType.JobName);
		Assert.True(initialJobChanges >= 1);

		await harness.Engine.CompleteJobAsync(harness.VigilLab.Id);
		await harness.AdvanceDueJobChangeAsync();

		Assert.True(harness.Publisher.GetChangeCount(harness.VigilLab.Id, NodeSemanticType.JobName) > initialJobChanges);
		Assert.Equal(VigilLabRunProfile.ActiveJob2Quantity, harness.Runtime.TargetCounter);
	}

	[Fact]
	public async Task P09_VigilLabIsolation_ExistingVirtualMachineKeepsCatalogTargets()
	{
		var harness = new VigilLabP09TestHarness();
		await harness.Engine.StartMachineServerAsync(harness.ExistingLaser.Id);
		await harness.Engine.AssignJobIfMissingAsync(harness.ExistingLaser.Id);

		MachineRuntimeState existingRuntime = harness.Engine.GetRuntimeState(harness.ExistingLaser.Id)!;
		Assert.Equal(FixedSimulationCatalog.GetDefinition(0).TargetQuantity, existingRuntime.TargetCounter);
		Assert.Equal(50, existingRuntime.TargetCounter);
	}

	[Fact]
	public void P09_ActiveShortProfile_UsesCatalogGeometriesWithRun004Quantities()
	{
		var job1 = VigilLabRunProfile.ResolveJobDefinition(VigilLabMachineContract.MachineId, 0);
		var job2 = VigilLabRunProfile.ResolveJobDefinition(VigilLabMachineContract.MachineId, 1);
		var catalog1 = FixedSimulationCatalog.GetDefinition(0);
		var catalog2 = FixedSimulationCatalog.GetDefinition(1);

		Assert.Equal(catalog1.PartName, job1.PartName);
		Assert.Equal(catalog2.PartName, job2.PartName);
		Assert.Equal(VigilLabRunProfile.ActiveJob1Quantity, job1.TargetQuantity);
		Assert.Equal(VigilLabRunProfile.ActiveJob2Quantity, job2.TargetQuantity);
		Assert.Equal(6, job1.TargetQuantity);
		Assert.Equal(6, job2.TargetQuantity);
	}
}
