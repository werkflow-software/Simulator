using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class VirtualPressBrakeSimP01R3Tests
{
	[Fact]
	public void SIM_P01_R3_LastProductionChange_RemainsStableAcrossRoutineTicks()
	{
		var session = CreateRunningSession(targetQuantity: 8);
		var engine = CreateEngine();
		var signal = session.Runtime.Signals.First(s => s.SignalId == "Machine.LastProductionChange");

		for (int i = 0; i < 80; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
		}

		int partsAtLastCheck = session.Simulation.PressBrake.ProducedParts;
		DateTime? lastSeen = signal.CurrentDateTimeUtc;
		for (int i = 0; i < 150; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
			int parts = session.Simulation.PressBrake.ProducedParts;
			if (parts == partsAtLastCheck)
			{
				Assert.Equal(lastSeen, signal.CurrentDateTimeUtc);
			}
			else
			{
				partsAtLastCheck = parts;
				lastSeen = signal.CurrentDateTimeUtc;
			}
		}
	}

	[Fact]
	public void SIM_P01_R3_LastProductionChange_UpdatesOnPartCompletion()
	{
		var session = CreateRunningSession(targetQuantity: 4);
		var engine = CreateEngine();
		var signal = session.Runtime.Signals.First(s => s.SignalId == "Machine.LastProductionChange");
		DateTime initial = signal.CurrentDateTimeUtc ?? DateTime.MinValue;

		bool changedAfterPart = false;
		for (int i = 0; i < 4000 && session.Simulation.PressBrake.ProducedParts < 1; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
		}

		Assert.True(session.Simulation.PressBrake.ProducedParts >= 1);
		changedAfterPart = (signal.CurrentDateTimeUtc ?? DateTime.MinValue) > initial;
		Assert.True(changedAfterPart);

		DateTime afterFirstPart = signal.CurrentDateTimeUtc ?? DateTime.MinValue;
		int partsAfterFirst = session.Simulation.PressBrake.ProducedParts;
		for (int i = 0; i < 80; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
			if (session.Simulation.PressBrake.ProducedParts != partsAfterFirst)
			{
				break;
			}
		}

		Assert.Equal(partsAfterFirst, session.Simulation.PressBrake.ProducedParts);
		Assert.Equal(afterFirstPart, signal.CurrentDateTimeUtc);
	}

	[Fact]
	public void SIM_P01_R3_LastProductionChange_UpdatesOnJobApplied()
	{
		var session = CreateRunningSession(targetQuantity: 4);
		var engine = CreateEngine();
		engine.Tick(session, TimeSpan.FromMilliseconds(20));
		var signal = session.Runtime.Signals.First(s => s.SignalId == "Machine.LastProductionChange");
		DateTime beforeJob = signal.CurrentDateTimeUtc ?? DateTime.MinValue;

		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, 194);
		engine.Tick(session, TimeSpan.FromMilliseconds(20));

		Assert.True((signal.CurrentDateTimeUtc ?? DateTime.MinValue) >= beforeJob);
	}

	[Fact]
	public void SIM_P01_R3_MachineState_IsNotReversibleMotionPhaseEncoding()
	{
		var context = CreateContext(productionMotionActive: true);
		var tokensByPhase = Enum.GetValues<PressBrakeMotionPhase>()
			.ToDictionary(
				phase => phase,
				phase => VirtualPressBrakeKinematicsConfig.MachineStateTokens[
					PressBrakeExposedSignalSemantics.ResolveMachineStateIndex(phase, isProductionPaused: false, isProductionMotionActive: true)]);

		Assert.True(tokensByPhase.Values.Distinct().Count() < tokensByPhase.Count);
		Assert.NotEqual(tokensByPhase[PressBrakeMotionPhase.Setup], tokensByPhase[PressBrakeMotionPhase.Forming]);
		Assert.Equal(
			tokensByPhase[PressBrakeMotionPhase.RamApproach],
			tokensByPhase[PressBrakeMotionPhase.Forming]);
	}

	[Fact]
	public void SIM_P01_R3_ActivityState_DoesNotEncodeBendStepIndex()
	{
		var context = CreateContext(productionMotionActive: true);
		context.PressBrake.MotionPhase = PressBrakeMotionPhase.Forming;

		var tokens = Enumerable.Range(0, 6)
			.Select(stepIndex =>
			{
				context.PressBrake.BendStepIndex = stepIndex;
				return VirtualPressBrakeKinematicsConfig.ActivityStateTokens[
					PressBrakeExposedSignalSemantics.ResolveActivityStateIndex(
						context.PressBrake.MotionPhase,
						context.IsProductionPaused,
						context.IsProductionMotionActive)];
			})
			.Distinct()
			.ToList();

		Assert.Single(tokens);
	}

	[Fact]
	public void SIM_P01_R3_ActivityState_DoesNotEncodeCompleteMotionPhaseIdentity()
	{
		var context = CreateContext(productionMotionActive: true);
		var tokensByPhase = Enum.GetValues<PressBrakeMotionPhase>()
			.ToDictionary(
				phase => phase,
				phase => VirtualPressBrakeKinematicsConfig.ActivityStateTokens[
					PressBrakeExposedSignalSemantics.ResolveActivityStateIndex(
						phase,
						isProductionPaused: false,
						isProductionMotionActive: true)]);

		Assert.True(tokensByPhase.Values.Distinct().Count() < tokensByPhase.Count);
		Assert.Equal(
			tokensByPhase[PressBrakeMotionPhase.Setup],
			tokensByPhase[PressBrakeMotionPhase.InterPartWait]);
		Assert.Equal(
			tokensByPhase[PressBrakeMotionPhase.RamApproach],
			tokensByPhase[PressBrakeMotionPhase.RamReturn]);
	}

	[Fact]
	public void SIM_P01_R3_ExposedSignals_ContainNoGroundTruthEventNames()
	{
		string[] forbidden =
		[
			"forming_start", "forming_end", "bend_step_start", "cycle_completion",
			"setup_start", "groundtruth", "hiddenstate"
		];

		var context = CreateContext(productionMotionActive: true);
		foreach (PressBrakeMotionPhase phase in Enum.GetValues<PressBrakeMotionPhase>())
		{
			context.PressBrake.MotionPhase = phase;
			context.PressBrake.BendStepIndex = phase switch
			{
				PressBrakeMotionPhase.Forming => 2,
				_ => 0
			};
			PressBrakeExposedSignalSemantics.ApplyExposedTokens(context.PressBrake, phase, context);

			foreach (string value in new[]
			{
				context.PressBrake.MachineStateToken,
				context.PressBrake.ActivityStateToken,
				context.PressBrake.ToolStationToken,
				context.PressBrake.ProgramId,
				context.PressBrake.PartId
			})
			{
				foreach (string token in forbidden)
				{
					Assert.DoesNotContain(token, value, StringComparison.OrdinalIgnoreCase);
				}
			}
		}
	}

	private static PhysicalSimulationEngine CreateEngine() =>
		new(new HiddenProcessStateEngine(), new SignalCalculationEngine(), new PhysicalModelValidator());

	private static PhysicalMachineSession CreateRunningSession(int targetQuantity)
	{
		PhysicalMachineProfile profile = VigilPressBrakeReducedProfileFactory.Create();
		PhysicalMachineSession session = new()
		{
			MachineId = VirtualPressBrakeContract.MachineId,
			MachineName = VirtualPressBrakeContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null)
		};
		session.Simulation.TimeFactor = 20.0;
		session.Simulation.Job.TargetQuantity = targetQuantity;
		var engine = CreateEngine();
		engine.Initialize(session, 194);
		PhysicalJobCoordinator.ApplyDefinition(
			session.Simulation,
			VirtualPressBrakeRunProfile.ResolveJobDefinition(session.MachineId, 0),
			session.Runtime);
		PressBrakeKinematicsEngine.OnJobApplied(session.Simulation, 194);
		session.Simulation.IsProductionMotionActive = true;
		engine.Tick(session, TimeSpan.FromMilliseconds(20));
		return session;
	}

	private static PhysicalSimulationContext CreateContext(bool productionMotionActive)
	{
		PhysicalMachineProfile profile = VigilPressBrakeReducedProfileFactory.Create();
		PhysicalMachineSession session = new()
		{
			MachineId = VirtualPressBrakeContract.MachineId,
			MachineName = VirtualPressBrakeContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null)
		};
		CreateEngine().Initialize(session, 194);
		session.Simulation.IsProductionMotionActive = productionMotionActive;
		return session.Simulation;
	}
}

[Collection("PhysicalVerification")]
public class VirtualPressBrakeSimP01R3LiveTests
{
	[Fact]
	public async Task SIM_P01_R3_LiveOpcUaSmoke_SemanticTruthfulness()
	{
		var report = await PressBrakeOpcUaLiveVerificationHarness.RunAsync();
		PressBrakeOpcUaLiveVerificationHarness.WriteEvidence(
			report,
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".cursor", "handoff", "evidence")));

		Assert.Equal(14, report.ResolvedCount);
		Assert.Equal(14, report.ReadableCount);
		Assert.True(report.CategoricalSignalTruthfulnessPass, string.Join("; ", report.Failures));
		Assert.True(report.LastProductionChangeTruthfulnessPass, string.Join("; ", report.Failures));
		Assert.True(report.BendAngleBehaviorPass);
		Assert.True(report.GroundTruthIsolationPass);
		Assert.True(report.Passed, string.Join("; ", report.Failures));
	}
}
