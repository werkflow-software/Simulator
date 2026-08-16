using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public sealed class VigilLabVirtualMachineP03R1Tests
{
	[Fact]
	public async Task P03R1_DualOpcUaServers_CanStartAndReadBothEndpoints()
	{
		var result = await VigilLabVirtualMachineP03R1ReproHarness.ReproduceDualServerStartupAsync(
			VirtualMachineContract.Port,
			VigilLabMachineContract.Port);
		Assert.False(result.BadServerHalted, result.ExceptionMessage);
		Assert.True(result.FirstServerStarted, result.ExceptionMessage ?? "first server did not start");
		Assert.True(result.SecondServerStarted, result.ExceptionMessage ?? "second server did not start");
		Assert.True(result.FirstStillRunningAfterSecondStart);
		Assert.True(result.SecondStillRunningAfterFirstStop);
		Assert.True(result.FirstReadAfterSecondStart?.Success ?? false, result.FirstReadAfterSecondStart?.Value);
		Assert.True(result.SecondReadAfterSecondStart?.Success ?? false, result.SecondReadAfterSecondStart?.Value);
		Assert.True(result.SecondReadAfterFirstStop?.Success ?? false, result.SecondReadAfterFirstStop?.Value);
	}

	[Fact]
	public void P03R1_VigilLabProfile_ResolvesPhysicalGenerationMode()
	{
		var profile = VigilLabLaserReducedProfileFactory.Create();
		var session = CreateSessionFactory().TryCreateSession(
			VigilLabMachineContract.MachineId,
			VigilLabMachineContract.DisplayName,
			VigilLabMachineContract.PhysicalProfileId)!;
		var engine = PhysicalTestServiceFactory.CreateEngine();
		engine.Initialize(session, 42);
		Assert.Equal(SignalGenerationMode.Physical, session.Simulation.GenerationMode);
		Assert.True(session.Simulation.Kinematics.IsEnabled);
	}

	[Fact]
	public async Task P03R1_VigilLab_StartProduction_ReachesCuttingPhase()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var machine = DefaultMachines.CreateVigilLabMachine();
		machine.Host = "127.0.0.1";
		machine.Port = 48645;
		machine.UpdateEndpointFromHostPort();

		stack.Coordinator.PrepareMachine(machine, 42);
		var session = stack.Coordinator.GetSession(machine.Id)!;
		Assert.Equal(SignalGenerationMode.Physical, session.Simulation.GenerationMode);
		stack.Coordinator.ApplyProductionJob(machine.Id, FixedSimulationCatalog.GetDefinition(0));
		await stack.Coordinator.ResumeProductionAsync(machine.Id);

		for (int i = 0; i < 500 && session.Simulation.Kinematics.MotionPhase < LaserMotionPhase.Piercing; i++)
		{
			stack.RuntimeCoordinator.Tick(session, TimeSpan.FromMilliseconds(200));
		}

		Assert.Equal("JOB-001", session.Simulation.Job.JobName);
		Assert.NotNull(session.Simulation.Kinematics.ActiveCuttingPlan);
		Assert.True(session.Simulation.Kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting,
			$"Expected Pierce/Cutting, got {session.Simulation.Kinematics.MotionPhase}");
		Assert.True(session.Simulation.IsProductionMotionActive);
	}

	[Fact]
	public async Task P03R1_ParallelMachines_ProgressIndependently()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var existingLaser = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		var vigilLab = DefaultMachines.CreateVigilLabMachine();

		stack.Coordinator.PrepareMachine(existingLaser, 42);
		stack.Coordinator.PrepareMachine(vigilLab, 42);
		var existingSession = stack.Coordinator.GetSession(existingLaser.Id)!;
		var vigilSession = stack.Coordinator.GetSession(vigilLab.Id)!;

		Assert.Equal(309, existingSession.Profile.Signals.Count(s => s.IsEnabled));
		Assert.Equal(7, vigilSession.Profile.Signals.Count(s => s.IsEnabled));

		stack.Coordinator.ApplyProductionJob(existingLaser.Id, FixedSimulationCatalog.GetDefinition(0));
		stack.Coordinator.ApplyProductionJob(vigilLab.Id, FixedSimulationCatalog.GetDefinition(0));
		await stack.Coordinator.ResumeProductionAsync(existingLaser.Id);
		await stack.Coordinator.ResumeProductionAsync(vigilLab.Id);

		for (int i = 0; i < 300; i++)
		{
			stack.RuntimeCoordinator.Tick(existingSession, TimeSpan.FromMilliseconds(200));
			stack.RuntimeCoordinator.Tick(vigilSession, TimeSpan.FromMilliseconds(200));
		}

		Assert.NotEqual(existingSession.Simulation.Kinematics.X, vigilSession.Simulation.Kinematics.X);
		Assert.True(existingSession.Simulation.Kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting);
		Assert.True(vigilSession.Simulation.Kinematics.MotionPhase is LaserMotionPhase.Piercing or LaserMotionPhase.Cutting);
	}

	private static PhysicalMachineSessionFactory CreateSessionFactory() =>
		new(
			new JsonPhysicalMachineProfileLoader(new PhysicalMachineProfileValidator()),
			new PhysicalMachineProfileValidator(),
			new PhysicalMachineRuntimeFactory());
}
