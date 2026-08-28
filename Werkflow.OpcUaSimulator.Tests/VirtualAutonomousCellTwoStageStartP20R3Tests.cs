using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class VirtualAutonomousCellTwoStageStartP20R3Tests
{
	private static readonly int Seed = Machine3SeedArchitecture.MasterScenarioSeed;

	[Fact]
	public void P20R3_Session_InitializesMachine3Profile()
	{
		PhysicalMachineSession session = CreateSession();
		Assert.True(session.Simulation.AutonomousCell.IsEnabled);
		Assert.Equal(24, session.Profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void P20R3_JobApply_StartsMachineWithoutProductionMotion()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);

		Assert.False(session.Simulation.IsProductionMotionActive);
		Assert.Equal(AutonomousCellMotionPhase.Idle, session.Simulation.AutonomousCell.MotionPhase);
	}

	[Fact]
	public void P20R3_AfterJobApply_CompletedPartCountRemainsZero()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 500);

		Assert.Equal(0, session.Simulation.AutonomousCell.CompletedParts);
		Assert.Equal(0, session.Runtime.Signals.First(s => s.SignalId == "Cell.CompletedPartCount").CurrentValue);
	}

	[Fact]
	public void P20R3_AfterJobApply_ProductSequenceDoesNotAdvance()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 500);

		Assert.Equal('A', session.Simulation.AutonomousCell.CurrentVariant);
		Assert.Equal(0, session.Simulation.AutonomousCell.PartIndex);
	}

	[Fact]
	public void P20R3_AfterJobApply_LoadRobotRemainsInactive()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 500);

		Assert.Equal(0.0, session.Simulation.AutonomousCell.LoadVelocityMmPerS);
		Assert.Equal(0.0, session.Simulation.AutonomousCell.LoadAxisPositionMm);
	}

	[Fact]
	public void P20R3_AfterJobApply_FixtureAndProcessRemainInactive()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 500);

		Assert.Equal(0.0, session.Simulation.AutonomousCell.ProcessForceKn);
		Assert.Equal(0.0, session.Simulation.AutonomousCell.FixtureClampForceN);
	}

	[Fact]
	public void P20R3_AfterJobApply_ContainerFillRemainsZero()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 500);

		Assert.Equal(0, session.Simulation.AutonomousCell.ContainerParts);
		Assert.Equal(0.0, session.Simulation.AutonomousCell.ContainerFillLevel);
	}

	[Fact]
	public void P20R3_AfterJobApply_PalletRemainsAtInitialValue()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 500);

		Assert.Equal(0, session.Simulation.AutonomousCell.PalletQuantityRemaining);
	}

	[Fact]
	public void P20R3_ProductionStart_TransitionsToActiveMotion()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		BeginProduction(session);

		Assert.True(session.Simulation.IsProductionMotionActive);
		Assert.NotEqual(AutonomousCellMotionPhase.Idle, session.Simulation.AutonomousCell.MotionPhase);
	}

	[Fact]
	public void P20R3_FirstPartCycle_BeginsOnlyAfterProductionStart()
	{
		PhysicalMachineSession session = CreateSession();
		ApplyJobOnly(session);
		TickMany(session, 200);
		Assert.Equal(0, session.Simulation.AutonomousCell.CompletedParts);

		BeginProduction(session);
		for (int i = 0; i < 12_000 && session.Simulation.AutonomousCell.CompletedParts < 1; i++)
		{
			Tick(session);
		}

		Assert.True(session.Simulation.AutonomousCell.CompletedParts >= 1);
	}

	[Fact]
	public void P20R3_SingleStart_SufficientForUnattendedBaseline()
	{
		(VirtualAutonomousCellBaselineTests.BaselineRunResult result, _) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24());
		Assert.Equal(28, result.CompletedParts);
	}

	[Fact]
	public void P20R3_PauseResume_PreservesMidRunState()
	{
		PhysicalMachineSession session = CreateSession();
		BeginProduction(session);
		for (int i = 0; i < 2_000; i++)
		{
			Tick(session);
			if (session.Simulation.AutonomousCell.CompletedParts >= 1)
			{
				break;
			}
		}

		int completed = session.Simulation.AutonomousCell.CompletedParts;
		var phase = session.Simulation.AutonomousCell.MotionPhase;
		session.Simulation.IsProductionMotionActive = false;
		session.Simulation.IsProductionPaused = true;
		TickMany(session, 100);
		Assert.Equal(completed, session.Simulation.AutonomousCell.CompletedParts);
		Assert.Equal(phase, session.Simulation.AutonomousCell.MotionPhase);

		session.Simulation.IsProductionPaused = false;
		session.Simulation.IsProductionMotionActive = true;
		TickMany(session, 200);
		Assert.True(session.Simulation.AutonomousCell.CompletedParts >= completed);
	}

	[Fact]
	public void P20R3_Stop_ResetsToIdleReadyState()
	{
		PhysicalMachineSession session = CreateSession();
		BeginProduction(session);
		TickMany(session, 500);
		AutonomousCellKinematicsEngine.StopAndResetProduction(session.Simulation, Seed);

		Assert.False(session.Simulation.IsProductionMotionActive);
		Assert.Equal(0, session.Simulation.AutonomousCell.CompletedParts);
		Assert.Equal(AutonomousCellMotionPhase.Idle, session.Simulation.AutonomousCell.MotionPhase);
	}

	[Fact]
	public void P20R3_CoordinatorApplyProductionJob_DoesNotAutoStartMotion()
	{
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(new TestLogService());
		PhysicalSignalPublishingCoordinator coordinator = stack.Coordinator;
		MachineConfiguration machine = DefaultMachines.Create()
			.First(m => m.Id == VirtualAutonomousProductionCellContract.MachineId);
		coordinator.PrepareMachine(machine, Seed);
		coordinator.ApplyProductionJob(
			machine.Id,
			VirtualAutonomousCellRunProfile.ResolveJobDefinition(machine.Id, 0));

		PhysicalMachineSession? session = coordinator.GetSession(machine.Id);
		Assert.NotNull(session);
		Assert.False(session.Simulation.IsProductionMotionActive);
	}

	[Fact]
	public void P20R3_LaserJobApply_DoesNotAutoStartMotion()
	{
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(new TestLogService());
		PhysicalSignalPublishingCoordinator coordinator = stack.Coordinator;
		MachineConfiguration machine = DefaultMachines.Create().First(m => m.Id == VirtualMachineContract.MachineId);
		coordinator.PrepareMachine(machine, 194);
		coordinator.ApplyProductionJob(machine.Id, VigilLabRunProfile.ResolveJobDefinition(machine.Id, 0));

		PhysicalMachineSession? session = coordinator.GetSession(machine.Id);
		Assert.NotNull(session);
		Assert.False(session.Simulation.IsProductionMotionActive);
	}

	[Fact]
	public void P20R3_PressBrakeJobApply_DoesNotAutoStartMotion()
	{
		PhysicalMachineSession session = CreatePressBrakeSession();
		PhysicalJobCoordinator.ApplyDefinition(
			session.Simulation,
			VirtualPressBrakeRunProfile.ResolveJobDefinition(VirtualPressBrakeContract.MachineId, 0),
			session.Runtime);
		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, 194);

		Assert.False(session.Simulation.IsProductionMotionActive);
	}

	[Fact]
	public void P20R3_Core24Contract_Unchanged()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateCore24();
		Assert.Equal(24, profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void P20R3_OpcContract_NoHiddenAmrExposure()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		int hidden = profile.Signals.Count(s => s.IsEnabled && (
			s.SignalId.Contains("AMR", StringComparison.OrdinalIgnoreCase)
			|| s.SignalId.Contains("GroundTruth", StringComparison.OrdinalIgnoreCase)
			|| s.SignalId.Contains("Hidden", StringComparison.OrdinalIgnoreCase)));
		Assert.Equal(0, hidden);
	}

	private static PhysicalMachineSession CreateSession()
	{
		PhysicalSimulationEngine engine = CreateEngine();
		PhysicalMachineSession session = new()
		{
			MachineId = VirtualAutonomousProductionCellContract.MachineId,
			MachineName = VirtualAutonomousProductionCellContract.DisplayName,
			Profile = VigilAutonomousCellProfileFactory.CreateCore24(),
			Runtime = new PhysicalMachineRuntimeFactory().Create(
				VigilAutonomousCellProfileFactory.CreateCore24(),
				null)
		};
		engine.Initialize(session, Seed);
		return session;
	}

	private static PhysicalMachineSession CreatePressBrakeSession()
	{
		PhysicalSimulationEngine engine = CreateEngine();
		PhysicalMachineProfile profile = VigilPressBrakeReducedProfileFactory.Create();
		PhysicalMachineSession session = new()
		{
			MachineId = VirtualPressBrakeContract.MachineId,
			MachineName = VirtualPressBrakeContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null)
		};
		engine.Initialize(session, 194);
		return session;
	}

	private static void ApplyJobOnly(PhysicalMachineSession session)
	{
		PhysicalJobCoordinator.ApplyDefinition(
			session.Simulation,
			VirtualAutonomousCellRunProfile.ResolveJobDefinition(session.MachineId, 0),
			session.Runtime);
		AutonomousCellKinematicsEngine.OnJobApplied(session.Simulation, Seed);
	}

	private static void BeginProduction(PhysicalMachineSession session)
	{
		AutonomousCellKinematicsEngine.OnProductionResumed(session.Simulation, Seed);
		session.Simulation.IsProductionMotionActive = true;
		session.Simulation.ProductionRunStartedAtUtc = DateTimeOffset.UtcNow;
	}

	private static PhysicalSimulationEngine CreateEngine() =>
		new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());

	private static void Tick(PhysicalMachineSession session) =>
		CreateEngine().Tick(session, TimeSpan.FromMilliseconds(20));

	private static void TickMany(PhysicalMachineSession session, int count)
	{
		PhysicalSimulationEngine engine = CreateEngine();
		for (int i = 0; i < count; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
		}
	}
}
