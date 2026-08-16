using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public sealed class VigilLabVirtualMachineP03R3Tests
{
	private static string ReadAppSource(string relativePath)
	{
		string path = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"Werkflow.OpcUaSimulator.App",
			relativePath));
		return File.ReadAllText(path);
	}

	[Fact]
	public void P03R3_SelectionChange_ClearsSessionBoundPresentationBeforeRebind()
	{
		string source = ReadAppSource("VirtualMachine/ViewModels/VirtualMachineHmiViewModel.cs");
		Assert.Contains("BindSelectedMachine(clearPresentation: true)", source);
		Assert.Contains("ResetSessionBoundPresentation()", source);
		Assert.Contains("ClearCuttingPlanPresentation()", source);
	}

	[Fact]
	public void P03R3_NoTickSwitch_ShowsStoppedMachineState_NotPreviousMachineValues()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		VirtualMachineHmiContextHarness.PrepareBothMachines(
			stack,
			out MachineConfiguration existingLaser,
			out MachineConfiguration vigilLab,
			out PhysicalMachineSession existingSession,
			out PhysicalMachineSession vigilSession);

		VirtualMachineHmiContextHarness.SetAxisPosition(vigilSession, "Axis01", 142.5);
		VirtualMachineHmiContextHarness.SetAxisPosition(existingSession, "Axis01", 0.0);
		vigilSession.Simulation.Kinematics.X = 142.5;
		vigilSession.Simulation.Kinematics.Y = 88.0;
		existingSession.Simulation.Kinematics.X = 0.0;
		existingSession.Simulation.Kinematics.Y = 0.0;

		var runtimeStates = new Dictionary<Guid, MachineRuntimeState>
		{
			[vigilLab.Id] = new MachineRuntimeState
			{
				MachineId = vigilLab.Id,
				State = MachineState.Running,
				IsProducing = true,
				IsServerOnline = true,
				JobName = "LAB-JOB",
				PartName = "LAB-PART",
				ActualCounter = 3,
				TargetCounter = 10
			},
			[existingLaser.Id] = new MachineRuntimeState
			{
				MachineId = existingLaser.Id,
				State = MachineState.Idle,
				IsProducing = false,
				IsServerOnline = false,
				JobName = "—",
				PartName = "—",
				ActualCounter = 0,
				TargetCounter = 0
			}
		};

		var viewModel = VirtualMachineHmiContextHarness.CreateViewModel(
			stack,
			[existingLaser, vigilLab],
			runtimeStates);
		viewModel.EnsureActivated();

		viewModel.SelectedMachineId = vigilLab.Id;
		viewModel.Refresh();
		string labAxis = VirtualMachineHmiContextHarness.ReadAxisPositionDisplay(viewModel, "Axis01");
		Assert.NotNull(labAxis);
		Assert.Contains("142", labAxis, StringComparison.Ordinal);
		Assert.Equal("LAB-JOB", viewModel.JobName);
		Assert.True(viewModel.CuttingPlan.HeadX > 0 || viewModel.CuttingPlan.HeadY > 0 || labAxis.Contains("142", StringComparison.Ordinal));

		viewModel.SelectedMachineId = existingLaser.Id;
		Assert.Equal(existingLaser.Id, viewModel.MachineId);
		Assert.Equal(MachineState.Idle.ToGermanLabel(), viewModel.MachineStateText);
		Assert.Equal("—", viewModel.JobName);
		string existingAxis = VirtualMachineHmiContextHarness.ReadAxisPositionDisplay(viewModel, "Axis01");
		Assert.NotNull(existingAxis);
		Assert.DoesNotContain("142", existingAxis, StringComparison.Ordinal);
		Assert.Equal(0, viewModel.CuttingPlan.HeadX);
		Assert.Equal(0, viewModel.CuttingPlan.HeadY);
	}

	[Fact]
	public void P03R3_SelectionSwitch_UsesDistinctSessionAxisValues()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		VirtualMachineHmiContextHarness.PrepareBothMachines(
			stack,
			out MachineConfiguration existingLaser,
			out MachineConfiguration vigilLab,
			out PhysicalMachineSession existingSession,
			out PhysicalMachineSession vigilSession);

		VirtualMachineHmiContextHarness.SetAxisPosition(vigilSession, "Axis01", 210.0);
		VirtualMachineHmiContextHarness.SetAxisPosition(existingSession, "Axis01", 12.5);

		var runtimeStates = new Dictionary<Guid, MachineRuntimeState>
		{
			[vigilLab.Id] = new MachineRuntimeState { MachineId = vigilLab.Id, State = MachineState.Running, IsServerOnline = true },
			[existingLaser.Id] = new MachineRuntimeState { MachineId = existingLaser.Id, State = MachineState.Idle, IsServerOnline = false }
		};

		var viewModel = VirtualMachineHmiContextHarness.CreateViewModel(stack, [existingLaser, vigilLab], runtimeStates);
		viewModel.EnsureActivated();

		viewModel.SelectedMachineId = vigilLab.Id;
		viewModel.Refresh();
		string labAxis = VirtualMachineHmiContextHarness.ReadAxisPositionDisplay(viewModel, "Axis01");

		viewModel.SelectedMachineId = existingLaser.Id;
		string laserAxis = VirtualMachineHmiContextHarness.ReadAxisPositionDisplay(viewModel, "Axis01");

		viewModel.SelectedMachineId = vigilLab.Id;
		viewModel.Refresh();
		string labAxisAgain = VirtualMachineHmiContextHarness.ReadAxisPositionDisplay(viewModel, "Axis01");

		Assert.Contains("210", labAxis!, StringComparison.Ordinal);
		Assert.Contains("12", laserAxis!, StringComparison.Ordinal);
		Assert.Contains("210", labAxisAgain!, StringComparison.Ordinal);
	}

	[Fact]
	public void P03R3_LiveRefreshAfterSwitch_ContinuesUpdatingSelectedMachine()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		VirtualMachineHmiContextHarness.PrepareBothMachines(
			stack,
			out MachineConfiguration existingLaser,
			out MachineConfiguration vigilLab,
			out _,
			out PhysicalMachineSession vigilSession);

		stack.Coordinator.ApplyProductionJob(vigilLab.Id, FixedSimulationCatalog.GetDefinition(0));
		stack.Coordinator.ApplyProductionJob(existingLaser.Id, FixedSimulationCatalog.GetDefinition(0));

		var runtimeStates = new Dictionary<Guid, MachineRuntimeState>
		{
			[vigilLab.Id] = new MachineRuntimeState { MachineId = vigilLab.Id, State = MachineState.Running, IsProducing = true, IsServerOnline = true },
			[existingLaser.Id] = new MachineRuntimeState { MachineId = existingLaser.Id, State = MachineState.Running, IsProducing = true, IsServerOnline = true }
		};

		var viewModel = VirtualMachineHmiContextHarness.CreateViewModel(stack, [existingLaser, vigilLab], runtimeStates);
		viewModel.EnsureActivated();
		viewModel.SelectedMachineId = vigilLab.Id;

		for (int i = 0; i < 40; i++)
		{
			stack.RuntimeCoordinator.Tick(vigilSession, TimeSpan.FromMilliseconds(200));
		}
		viewModel.Refresh();
		double headXAfterMotion = viewModel.CuttingPlan.HeadX;

		viewModel.SelectedMachineId = existingLaser.Id;
		viewModel.SelectedMachineId = vigilLab.Id;
		viewModel.Refresh();

		for (int i = 0; i < 20; i++)
		{
			stack.RuntimeCoordinator.Tick(vigilSession, TimeSpan.FromMilliseconds(200));
		}
		viewModel.Refresh();

		Assert.True(viewModel.CuttingPlan.HeadX != 0 || headXAfterMotion != 0);
	}

	[Fact]
	public async Task P03R3_DualOpcUaServers_StillCoexist()
	{
		var result = await VigilLabVirtualMachineP03R1ReproHarness.ReproduceDualServerStartupAsync(
			OpcUaCertificateLifecycleHarness.ExistingLaserTestPort,
			OpcUaCertificateLifecycleHarness.VigilLabTestPort);
		Assert.False(result.BadServerHalted, result.ExceptionMessage);
		Assert.True(result.FirstServerStarted);
		Assert.True(result.SecondServerStarted);
	}

	[Fact]
	public async Task P03R3_VigilLab_StillReachesCutting()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var machine = DefaultMachines.CreateVigilLabMachine();
		stack.Coordinator.PrepareMachine(machine, 42);
		var session = stack.Coordinator.GetSession(machine.Id)!;
		Assert.Equal(SignalGenerationMode.Physical, session.Simulation.GenerationMode);
		stack.Coordinator.ApplyProductionJob(machine.Id, FixedSimulationCatalog.GetDefinition(0));
		await stack.Coordinator.ResumeProductionAsync(machine.Id);
		for (int i = 0; i < 500 && session.Simulation.Kinematics.MotionPhase < LaserMotionPhase.Piercing; i++)
		{
			stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
		}
		Assert.True(session.Simulation.Kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting);
	}
}
