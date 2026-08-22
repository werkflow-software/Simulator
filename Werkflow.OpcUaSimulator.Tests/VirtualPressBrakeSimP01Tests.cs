using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class VirtualPressBrakeContractTests
{
	[Fact]
	public void SIM_P01_Contract_Machine2_Port4841()
	{
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualPressBrakeContract.Port);
		Assert.Equal(VirtualPressBrakeContract.MachineId, machine.Id);
		Assert.Equal(VirtualPressBrakeContract.DisplayName, machine.Name);
		Assert.Equal(VirtualPressBrakeContract.Endpoint, machine.Endpoint);
		Assert.Equal(VirtualPressBrakeContract.PhysicalProfileId, machine.PhysicalProfileId);
	}

	[Fact]
	public void SIM_P01_Profile_HasExactly14EnabledSignals()
	{
		var profile = VigilPressBrakeReducedProfileFactory.Create();
		var enabled = profile.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).OrderBy(s => s).ToList();
		Assert.Equal(14, enabled.Count);
		Assert.Equal(VigilPressBrakeReducedProfileFactory.ContractSignalIds.OrderBy(s => s), enabled);
	}

	[Fact]
	public void SIM_P01_Profile_NoGroundTruthSignalIds()
	{
		var profile = VigilPressBrakeReducedProfileFactory.Create();
		foreach (var signal in profile.Signals.Where(s => s.IsEnabled))
		{
			Assert.DoesNotContain("GroundTruth", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("HiddenState", signal.SignalId, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void SIM_P01_LaserContract_Unchanged_Port4840()
	{
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		Assert.Equal(VirtualMachineContract.MachineId, machine.Id);
		Assert.Equal("laser-processing-machine-300", machine.PhysicalProfileId);
	}
}

public class PressBrakeKinematicsPlausibilityTests
{
	[Fact]
	public void SIM_P01_PressBrake_StateProgression_AndDeterministicSeed()
	{
		var (sessionA, producedA, ramMaxA) = RunTicks(194);
		var (sessionB, producedB, ramMaxB) = RunTicks(194);
		Assert.Equal(producedA, producedB);
		Assert.Equal(ramMaxA, ramMaxB);
		Assert.True(producedA > 0);
		Assert.True(ramMaxA > 120.0);
		Assert.True(sessionA.Simulation.PressBrake.BendAngleDeg >= 0.0);
	}

	[Fact]
	public void SIM_P01_PressBrake_RamVelocitySigns_ArePlausible()
	{
		var (session, _, _) = RunTicks(194, tickCount: 500);
		var velocitySignal = session.Runtime.Signals.First(s => s.SignalId == "Ram.Velocity");
		Assert.True(Math.Abs(velocitySignal.CurrentValue) <= 40.0);
	}

	[Fact]
	public void SIM_P01_PressBrake_ProgramAndPartIds_Change()
	{
		var (session, _, _) = RunTicks(194, tickCount: 2500);
		Assert.NotEqual("—", session.Simulation.PressBrake.ProgramId);
		Assert.NotEqual("—", session.Simulation.PressBrake.PartId);
		Assert.Contains("PRG-", session.Simulation.PressBrake.ProgramId, StringComparison.Ordinal);
	}

	private static (PhysicalMachineSession Session, int Produced, double RamMax) RunTicks(int seed, int tickCount = 1800)
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
			seed,
			Path.Combine(Path.GetTempPath(), $"pb-gt-tick-{Guid.NewGuid():N}.jsonl"));
		session.Simulation.TimeFactor = 20.0;
		session.Simulation.Job.TargetQuantity = 6;
		engine.Initialize(session, seed);
		PhysicalJobCoordinator.ApplyDefinition(session.Simulation, VirtualPressBrakeRunProfile.ResolveJobDefinition(session.MachineId, 0), session.Runtime);
		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, seed);
		session.Simulation.IsProductionMotionActive = true;

		double ramMax = 0.0;
		for (int i = 0; i < tickCount; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
			ramMax = Math.Max(ramMax, session.Simulation.PressBrake.RamPositionMm);
		}

		return (session, session.Simulation.PressBrake.ProducedParts, ramMax);
	}
}

public class PressBrakeGroundTruthTests
{
	[Fact]
	public void SIM_P01_GroundTruth_IsInternalOnly_AndDeterministic()
	{
		var recorder = new PressBrakeGroundTruthRecorder();
		string path = Path.Combine(Path.GetTempPath(), $"pb-gt-test-{Guid.NewGuid():N}.jsonl");
		recorder.BeginSession(VirtualPressBrakeContract.MachineId, 194, path);

		var evt = new PressBrakeGroundTruthEvent
		{
			TimestampUtc = DateTimeOffset.UtcNow,
			MachineId = VirtualPressBrakeContract.MachineId,
			EventType = "bend_step_start",
			ProgramReference = "PRG-7F2A",
			PartReference = "PRT-19C3",
			BendStepReference = 1,
			PhysicalPhase = "Forming",
			Source = "test"
		};
		recorder.Record(evt);
		recorder.Flush();

		Assert.True(File.Exists(path));
		string line = File.ReadAllLines(path).Single();
		Assert.Contains("bend_step_start", line);
		Assert.Contains("PRG-7F2A", line);
		Assert.DoesNotContain("GroundTruth", line, StringComparison.OrdinalIgnoreCase);

		var profile = VigilPressBrakeReducedProfileFactory.Create();
		Assert.DoesNotContain(profile.Signals, s => s.SignalId.Contains("GroundTruth", StringComparison.OrdinalIgnoreCase));

		File.Delete(path);
	}
}

public class PressBrakeStructuralIndependenceTests
{
	[Fact]
	public void SIM_P01_StructuralIndependence_FromLaser()
	{
		Assert.NotEqual(typeof(LaserMotionPhase), typeof(PressBrakeMotionPhase));
		Assert.False(VirtualLaserMachineRegistry.IsVirtualLaserMachine(VirtualPressBrakeContract.MachineId));
		Assert.True(VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(VirtualPressBrakeContract.MachineId));

		var laserReduced = VigilLabLaserReducedProfileFactory.Create();
		var pressProfile = VigilPressBrakeReducedProfileFactory.Create();
		var laserIds = laserReduced.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var pressIds = pressProfile.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).ToHashSet(StringComparer.OrdinalIgnoreCase);
		Assert.Empty(laserIds.Intersect(pressIds));

		Assert.NotEqual(VirtualMachineContract.Port, VirtualPressBrakeContract.Port);
		Assert.NotEqual(VirtualMachineContract.MachineId, VirtualPressBrakeContract.MachineId);
	}
}
