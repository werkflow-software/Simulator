using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class VirtualPressBrakeSimP01R1Tests
{
	[Fact]
	public async Task SIM_P01_R1_LiveOpcUaSmoke_All14SignalsReadable()
	{
		var report = await PressBrakeOpcUaLiveVerificationHarness.RunAsync();
		PressBrakeOpcUaLiveVerificationHarness.WriteEvidence(
			report,
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".cursor", "handoff", "evidence")));

		Assert.True(report.SimulatorStarted, string.Join("; ", report.Failures));
		Assert.True(report.OpcSessionEstablished);
		Assert.Equal(14, report.ResolvedCount);
		Assert.Equal(14, report.ReadableCount);
		Assert.True(report.DynamicSignalSmokePass);
		Assert.True(report.RamBehaviorPass);
		Assert.True(report.BackgaugeBehaviorPass);
		Assert.True(report.FormingForceBehaviorPass);
		Assert.True(report.BendAngleBehaviorPass);
		Assert.True(report.CounterProgressionPass);
		Assert.True(report.ProgramPartTransitionPass);
		Assert.True(report.ThermalEvolutionPass);
		Assert.True(report.ActivityStateBehaviorPass);
		Assert.True(report.GroundTruthGenerationPass);
		Assert.True(report.GroundTruthIsolationPass);
		Assert.True(report.Passed, string.Join("; ", report.Failures));
	}

	[Fact]
	public void SIM_P01_R1_GroundTruth_RuntimeTick_EmitsEvents()
	{
		PhysicalMachineProfile profile = VigilPressBrakeReducedProfileFactory.Create();
		PhysicalSimulationEngine engine = new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
		var recorder = new PressBrakeGroundTruthRecorder();
		string path = Path.Combine(Path.GetTempPath(), $"pb-gt-runtime-{Guid.NewGuid():N}.jsonl");
		recorder.BeginSession(VirtualPressBrakeContract.MachineId, 194, path);

		PhysicalMachineSession session = new()
		{
			MachineId = VirtualPressBrakeContract.MachineId,
			MachineName = VirtualPressBrakeContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null),
			PressBrakeGroundTruth = recorder
		};
		session.Simulation.TimeFactor = 20.0;
		session.Simulation.Job.TargetQuantity = 4;
		engine.Initialize(session, 194);
		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, 194);
		session.Simulation.IsProductionMotionActive = true;

		for (int i = 0; i < 2000; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
		}

		var eventTypes = recorder.GetEvents().Select(e => e.EventType).Distinct().ToList();
		Assert.Contains("bend_step_start", eventTypes);
		Assert.Contains("forming_start", eventTypes);
		Assert.True(File.Exists(path));
		Assert.True(recorder.GetEvents().Count > 5);
		File.Delete(path);
	}

	[Fact]
	public void SIM_P01_R2_BendAngle_TracksFormingPhase_InRuntimeAndOpcPath()
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
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null)
		};
		session.Simulation.TimeFactor = 20.0;
		session.Simulation.Job.TargetQuantity = 6;
		engine.Initialize(session, 194);
		PhysicalJobCoordinator.ApplyDefinition(
			session.Simulation,
			VirtualPressBrakeRunProfile.ResolveJobDefinition(session.MachineId, 0),
			session.Runtime);
		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, 194);
		session.Simulation.IsProductionMotionActive = true;

		double maxAngle = 0.0;
		double maxForce = 0.0;
		double maxPublishedAngle = 0.0;
		bool formingPhaseSeen = false;
		for (int i = 0; i < 2500; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
			maxAngle = Math.Max(maxAngle, session.Simulation.PressBrake.BendAngleDeg);
			maxForce = Math.Max(maxForce, session.Simulation.PressBrake.FormingForceKn);
			var angleSignal = session.Runtime.Signals.First(s => s.SignalId == "Process.BendAngle");
			maxPublishedAngle = Math.Max(maxPublishedAngle, angleSignal.CurrentValue);
			if (session.Simulation.PressBrake.MotionPhase is PressBrakeMotionPhase.Forming or PressBrakeMotionPhase.Hold
				&& session.Simulation.PressBrake.BendAngleDeg > 0.1)
			{
				formingPhaseSeen = true;
				var forceSignal = session.Runtime.Signals.First(s => s.SignalId == "Process.FormingForce");
				Assert.True(forceSignal.CurrentValue > 0.1);
				Assert.Equal(session.Simulation.PressBrake.BendAngleDeg, angleSignal.CurrentValue, precision: 3);
			}
		}

		Assert.True(formingPhaseSeen);
		Assert.True(maxAngle > 10.0);
		Assert.True(maxForce > 10.0);
		Assert.True(maxPublishedAngle > 10.0);
	}

	[Fact]
	public void SIM_P01_R1_StructuralIndependence_ProcessTopologyDiffersFromLaser()
	{
		Assert.Contains(PressBrakeMotionPhase.RamApproach, Enum.GetValues<PressBrakeMotionPhase>());
		Assert.Contains(LaserMotionPhase.Cutting, Enum.GetValues<LaserMotionPhase>());
		Assert.DoesNotContain(
			Enum.GetNames<PressBrakeMotionPhase>(),
			n => n.Equals(nameof(LaserMotionPhase.Cutting), StringComparison.Ordinal));
		Assert.DoesNotContain(
			Enum.GetNames<LaserMotionPhase>(),
			n => n.Contains("Ram", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(
			Enum.GetNames<PressBrakeMotionPhase>(),
			n => n.Contains("Ram", StringComparison.OrdinalIgnoreCase));
	}
}
