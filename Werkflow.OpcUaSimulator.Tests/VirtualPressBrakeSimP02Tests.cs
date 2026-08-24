using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class VirtualPressBrakeSimP02Tests
{
	[Fact]
	public void SIM_P02_Baseline_DisablesOperatorWaits()
	{
		var pressBrake = new PressBrakeKinematicsState
		{
			UnattendedBaselineEnabled = true
		};
		var part = new PressBrakePartDefinition
		{
			PartId = "PRT-TEST",
			OperatorWaitChance = 0.2,
			BendSteps = []
		};
		Assert.False(InvokeShouldOperatorWait(part, 194, 7, pressBrake));
	}

	[Fact]
	public void SIM_P02_PhaseRemaining_IsMonotonicDuringSetup()
	{
		var session = CreateSession();
		session.Simulation.IsProductionMotionActive = true;
		double previous = PressBrakeKinematicsEngine.GetPhaseRemainingSeconds(session.Simulation.PressBrake, 194);
		for (int i = 0; i < 25; i++)
		{
			Tick(session);
			double remaining = PressBrakeKinematicsEngine.GetPhaseRemainingSeconds(session.Simulation.PressBrake, 194);
			Assert.True(remaining <= previous + 0.001);
			previous = remaining;
		}
	}

	[Fact]
	public void SIM_P02_AutomaticPartContinuation_WithoutManualStart()
	{
		var session = CreateSession();
		session.Simulation.IsProductionMotionActive = true;
		int startParts = session.Simulation.PressBrake.ProducedParts;
		for (int i = 0; i < 4000; i++)
		{
			Tick(session);
			if (session.Simulation.PressBrake.ProducedParts >= startParts + 2)
			{
				break;
			}
		}

		Assert.True(session.Simulation.PressBrake.ProducedParts >= startParts + 2);
	}

	[Fact]
	public void SIM_P02_ProgramTransitionPause_MatchesKinematicDesign()
	{
		var nextJob = VirtualPressBrakeRunProfile.ResolveJobDefinition(VirtualPressBrakeContract.MachineId, 1);
		VirtualPressBrakeRunProfile.ResolveJobChangePauseRange(
			VirtualPressBrakeContract.MachineId,
			1,
			out int minPause,
			out int maxPause);
		double expected = PressBrakePhaseObservability.EstimateJobChangePauseSeconds(
			VirtualPressBrakeContract.MachineId,
			nextJob,
			VirtualPressBrakeRunProfile.RandomSeed);
		Assert.Equal((int)Math.Ceiling(expected), minPause);
		Assert.Equal(minPause, maxPause);
		Assert.True(minPause < FixedSimulationCatalog.MinJobChangePauseSeconds);
	}

	[Fact]
	public void SIM_P02_ContinuationIndicator_ShowsAutomaticWaitDuringSetup()
	{
		var session = CreateSession();
		session.Simulation.IsProductionMotionActive = true;
		var snapshot = PressBrakePhaseObservability.BuildSnapshot(session.Simulation.PressBrake, 194);
		Assert.Equal("Rüsten", snapshot.PhaseDisplayName);
		Assert.Equal(PressBrakeContinuationKind.AutoWait, snapshot.ContinuationKind);
		Assert.Contains("Automatischer Fortlauf", snapshot.ContinuationIndicator, StringComparison.Ordinal);
	}

	[Fact]
	public void SIM_P02_FrozenOpcContract_Remains14Signals()
	{
		var profile = VigilPressBrakeReducedProfileFactory.Create();
		Assert.Equal(14, profile.Signals.Count(s => s.IsEnabled));
		Assert.Equal(VigilPressBrakeReducedProfileFactory.ContractSignalIds.OrderBy(s => s),
			profile.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).OrderBy(s => s));
	}

	[Fact]
	public void SIM_P02_CompleteBaselineDuration_EstimateIsPositive()
	{
		var (minimum, nominal, maximum) = VirtualPressBrakeRunProfile.EstimateCompleteBaselineWallClockSeconds();
		Assert.True(minimum > 0, $"minimum={minimum}");
		Assert.True(nominal >= minimum, $"minimum={minimum}, nominal={nominal}");
		Assert.True(maximum >= nominal, $"nominal={nominal}, maximum={maximum}");
		Assert.True(nominal >= 8 * 60, $"nominal wall estimate too low: {nominal:0.#}s");
	}

	[Fact]
	public void SIM_P02_ProgramTransition_ContinuesAutomatically()
	{
		var session = CreateSession();
		session.Simulation.IsProductionMotionActive = true;
		session.Simulation.PressBrake.ProducedParts = session.Simulation.PressBrake.TargetParts;
		var nextJob = VirtualPressBrakeRunProfile.ResolveJobDefinition(VirtualPressBrakeContract.MachineId, 1);
		PressBrakeKinematicsEngine.AbortProductionForJobChange(session.Simulation, nextJob);
		session.Simulation.IsJobChangePauseActive = true;
		session.Simulation.IsProductionMotionActive = false;

		for (int i = 0; i < 8000; i++)
		{
			Tick(session);
			if (!session.Simulation.IsJobChangePauseActive)
			{
				break;
			}
		}

		Assert.False(session.Simulation.IsJobChangePauseActive);
		Assert.True(session.Simulation.IsProductionMotionActive);
	}

	[Fact]
	public void SIM_P02_LaserContract_Unchanged()
	{
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		Assert.Equal(VirtualMachineContract.MachineId, machine.Id);
		Assert.Equal("laser-processing-machine-300", machine.PhysicalProfileId);
	}

	private static PhysicalMachineSession CreateSession()
	{
		PhysicalMachineProfile profile = VigilPressBrakeReducedProfileFactory.Create();
		PhysicalSimulationEngine engine = new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
		PhysicalMachineSession session = new()
		{
			MachineId = VirtualPressBrakeContract.MachineId,
			MachineName = VirtualPressBrakeContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null),
			PressBrakeGroundTruth = new PressBrakeGroundTruthRecorder()
		};
		((PressBrakeGroundTruthRecorder)session.PressBrakeGroundTruth!).BeginSession(
			VirtualPressBrakeContract.MachineId,
			VirtualPressBrakeRunProfile.RandomSeed,
			Path.Combine(Path.GetTempPath(), $"pb-gt-p02-{Guid.NewGuid():N}.jsonl"));
		session.Simulation.TimeFactor = 20.0;
		session.Simulation.Job.TargetQuantity = 10;
		engine.Initialize(session, VirtualPressBrakeRunProfile.RandomSeed);
		PhysicalJobCoordinator.ApplyDefinition(
			session.Simulation,
			VirtualPressBrakeRunProfile.ResolveJobDefinition(session.MachineId, 0),
			session.Runtime);
		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, VirtualPressBrakeRunProfile.RandomSeed);
		return session;
	}

	private static void Tick(PhysicalMachineSession session)
	{
		PhysicalSimulationEngine engine = new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
		engine.Tick(session, TimeSpan.FromMilliseconds(20));
	}

	private static bool InvokeShouldOperatorWait(
		PressBrakePartDefinition part,
		int seed,
		int producedParts,
		PressBrakeKinematicsState pressBrake)
	{
		var method = typeof(PressBrakeKinematicsEngine).GetMethod(
			"ShouldOperatorWait",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		Assert.NotNull(method);
		return (bool)method!.Invoke(null, [part, seed, producedParts, pressBrake])!;
	}
}
