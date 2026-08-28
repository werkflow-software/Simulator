using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Werkflow.OpcUaSimulator.Core.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class VirtualAutonomousCellHmiObservabilityP20R2Tests
{
	private static readonly int Seed = Machine3SeedArchitecture.MasterScenarioSeed;

	[Fact]
	public void P20R2_TargetCount_ResolvesTo28()
	{
		FixedProductionJobDefinition job = VirtualAutonomousCellRunProfile.ResolveJobDefinition(
			VirtualAutonomousProductionCellContract.MachineId,
			0);
		Assert.Equal(28, job.TargetQuantity);
		Assert.Equal("Machine-3 Baseline", job.JobName);
	}

	[Fact]
	public void P20R2_JobResolution_DoesNotUseLaserCatalog50()
	{
		FixedProductionJobDefinition laserDefault = FixedSimulationCatalog.GetDefinition(0);
		Assert.Equal(50, laserDefault.TargetQuantity);
		FixedProductionJobDefinition m3 = VirtualAutonomousCellRunProfile.ResolveJobDefinition(
			VirtualAutonomousProductionCellContract.MachineId,
			0);
		Assert.NotEqual(laserDefault.TargetQuantity, m3.TargetQuantity);
	}

	[Fact]
	public void P20R2_CompletedCount_AdvancesFromKinematics()
	{
		PhysicalMachineSession session = CreateSession();
		StartProduction(session);
		int observed = 0;
		for (int i = 0; i < 8_000 && session.Simulation.AutonomousCell.CompletedParts < 2; i++)
		{
			Tick(session);
			int pending = AutonomousCellKinematicsEngine.ConsumePendingPartCompletions(session.Simulation);
			observed += pending;
		}

		Assert.True(session.Simulation.AutonomousCell.CompletedParts >= 2);
		Assert.True(observed >= 2);
	}

	[Fact]
	public void P20R2_FinalProgress_Is28Of28()
	{
		(VirtualAutonomousCellBaselineTests.BaselineRunResult result, _) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24());
		AutonomousCellHmiSnapshot snapshot = AutonomousCellPhaseObservability.BuildSnapshot(
			new AutonomousCellKinematicsState
			{
				IsEnabled = true,
				CompletedParts = result.CompletedParts,
				TargetParts = VirtualAutonomousCellRunProfile.TotalParts,
				MotionPhase = AutonomousCellMotionPhase.Complete
			},
			Seed);
		Assert.Equal(28, snapshot.CompletedParts);
		Assert.Equal(28, snapshot.TargetParts);
		Assert.True(snapshot.IsCompleted);
	}

	[Fact]
	public void P20R2_OverallRemaining_StartsHighAndEndsAtZero()
	{
		PhysicalMachineSession session = CreateSession();
		StartProduction(session);
		double startRemaining = AutonomousCellProductionTimeEstimator.EstimateJobRemainingSeconds(
			session.Simulation.AutonomousCell,
			Seed);
		Assert.True(startRemaining > 600.0);

		(VirtualAutonomousCellBaselineTests.BaselineRunResult result, _) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24());
		Assert.Equal(28, result.CompletedParts);

		var completed = new AutonomousCellKinematicsState
		{
			IsEnabled = true,
			CompletedParts = 28,
			TargetParts = 28,
			MotionPhase = AutonomousCellMotionPhase.Complete
		};
		Assert.Equal(0.0, AutonomousCellProductionTimeEstimator.EstimateJobRemainingSeconds(completed, Seed));
	}

	[Fact]
	public void P20R2_ProductVariant_FollowsSequence()
	{
		for (int i = 0; i < VirtualAutonomousCellRunProfile.TotalParts; i++)
		{
			char expected = VirtualAutonomousCellRunProfile.GetVariantForPartIndex(i);
			var cell = new AutonomousCellKinematicsState
			{
				IsEnabled = true,
				CompletedParts = i,
				TargetParts = VirtualAutonomousCellRunProfile.TotalParts,
				CurrentVariant = expected,
				MotionPhase = AutonomousCellMotionPhase.LoadPick
			};
			AutonomousCellHmiSnapshot snapshot = AutonomousCellPhaseObservability.BuildSnapshot(cell, Seed);
			Assert.Equal(expected.ToString(), snapshot.ProductVariant);
		}
	}

	[Theory]
	[InlineData(AutonomousCellMotionPhase.LoadPick, "Load Robot")]
	[InlineData(AutonomousCellMotionPhase.ProcessPressFit, "Press-Fit")]
	[InlineData(AutonomousCellMotionPhase.VisionInspect, "Vision")]
	[InlineData(AutonomousCellMotionPhase.WaitReplenishment, "Inbound")]
	[InlineData(AutonomousCellMotionPhase.ContainerFill, "Container")]
	public void P20R2_ActiveStation_FollowsPhase(AutonomousCellMotionPhase phase, string expectedStation)
	{
		Assert.Equal(expectedStation, AutonomousCellPhaseObservability.ResolveActiveStation(phase));
	}

	[Fact]
	public void P20R2_Pallet_StartsAtCapacityAfterInbound()
	{
		PhysicalMachineSession session = CreateSession();
		StartProduction(session);
		while (session.Simulation.AutonomousCell.MotionPhase != AutonomousCellMotionPhase.LoadPick)
		{
			Tick(session);
		}

		Assert.Equal(VirtualAutonomousCellRunProfile.PalletCapacity, session.Simulation.AutonomousCell.PalletQuantityRemaining);
		AutonomousCellHmiSnapshot snapshot = AutonomousCellPhaseObservability.BuildSnapshot(session.Simulation.AutonomousCell, Seed);
		Assert.Equal(12, snapshot.PalletRemaining);
		Assert.Equal(12, snapshot.PalletCapacity);
	}

	[Fact]
	public void P20R2_Container_StartsAtZeroFill()
	{
		PhysicalMachineSession session = CreateSession();
		StartProduction(session);
		AutonomousCellHmiSnapshot snapshot = AutonomousCellPhaseObservability.BuildSnapshot(session.Simulation.AutonomousCell, Seed);
		Assert.Equal(0, snapshot.ContainerFillParts);
		Assert.Equal(10, snapshot.ContainerCapacity);
	}

	[Fact]
	public void P20R2_ContainerFill_IncrementsAndResetsOnExchange()
	{
		(VirtualAutonomousCellBaselineTests.BaselineRunResult result, _) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24());
		Assert.Equal(2, result.ContainerExchangeEvents);
		Assert.Equal(28, result.CompletedParts);
	}

	[Fact]
	public void P20R2_MaterialWait_ClassifiedAutomatic()
	{
		AutonomousCellAutomaticWaitKind kind = AutonomousCellPhaseObservability.ResolveAutomaticWaitKind(
			AutonomousCellMotionPhase.WaitReplenishment);
		string message = AutonomousCellPhaseObservability.ResolveAutomaticWaitMessage(kind);
		Assert.Equal(AutonomousCellAutomaticWaitKind.RawMaterialReplenishment, kind);
		Assert.Contains("Materialnachschub", message, StringComparison.Ordinal);
		Assert.Contains("keine Bedienaktion", message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void P20R2_ContainerWait_ClassifiedAutomatic()
	{
		AutonomousCellAutomaticWaitKind kind = AutonomousCellPhaseObservability.ResolveAutomaticWaitKind(
			AutonomousCellMotionPhase.WaitContainerExchange);
		string message = AutonomousCellPhaseObservability.ResolveAutomaticWaitMessage(kind);
		Assert.Equal(AutonomousCellAutomaticWaitKind.ContainerExchange, kind);
		Assert.Contains("Behälterwechsel", message, StringComparison.Ordinal);
		Assert.Contains("keine Bedienaktion", message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void P20R2_AutomaticWaits_DoNotRequireOperatorAction()
	{
		var cell = new AutonomousCellKinematicsState
		{
			IsEnabled = true,
			MotionPhase = AutonomousCellMotionPhase.WaitReplenishment,
			TargetParts = VirtualAutonomousCellRunProfile.TotalParts
		};
		AutonomousCellHmiSnapshot snapshot = AutonomousCellPhaseObservability.BuildSnapshot(cell, Seed);
		Assert.False(snapshot.OperatorActionRequired);
	}

	[Fact]
	public void P20R2_PartElapsed_AdvancesDuringCycle()
	{
		PhysicalMachineSession session = CreateSession();
		StartProduction(session);
		double previous = 0.0;
		for (int i = 0; i < 200; i++)
		{
			Tick(session);
			double elapsed = session.Simulation.AutonomousCell.PhaseElapsedSeconds;
			Assert.True(elapsed >= previous);
			previous = elapsed;
			if (elapsed > 0.5)
			{
				break;
			}
		}

		Assert.True(previous > 0.0);
	}

	[Fact]
	public void P20R2_PartRemaining_IsNonNegative()
	{
		PhysicalMachineSession session = CreateSession();
		StartProduction(session);
		for (int i = 0; i < 500; i++)
		{
			Tick(session);
			double remaining = AutonomousCellProductionTimeEstimator.EstimateCurrentPartRemainingSeconds(
				session.Simulation.AutonomousCell,
				Seed);
			Assert.True(remaining >= 0.0);
		}
	}

	[Fact]
	public void P20R2_CompletionRemaining_IsZero()
	{
		var cell = new AutonomousCellKinematicsState
		{
			IsEnabled = true,
			CompletedParts = 28,
			TargetParts = 28,
			MotionPhase = AutonomousCellMotionPhase.Complete
		};
		AutonomousCellHmiSnapshot snapshot = AutonomousCellPhaseObservability.BuildSnapshot(cell, Seed);
		Assert.Equal(0.0, snapshot.PartRemainingSeconds);
		Assert.Equal(0.0, snapshot.JobRemainingSeconds);
		Assert.Equal("00:00", AutonomousCellPhaseObservability.FormatEstimatedRemaining(snapshot.JobRemainingSeconds));
	}

	[Fact]
	public void P20R2_ImplementedBaselineDuration_Is25To30Minutes()
	{
		double seconds = AutonomousCellProductionTimeEstimator.EstimateImplementedBaselineWallClockSeconds(Seed);
		double minutes = seconds / 60.0;
		Assert.InRange(minutes, 24.0, 31.0);
		Assert.Equal(25, VirtualAutonomousCellRunProfile.ApproximateBaselineWallClockMinutesMin);
		Assert.Equal(30, VirtualAutonomousCellRunProfile.ApproximateBaselineWallClockMinutesMax);
	}

	[Fact]
	public void P20R2_LaserHmiRegression_UnchangedCatalog()
	{
		FixedProductionJobDefinition laser = VigilLabRunProfile.ResolveJobDefinition(VirtualMachineContract.MachineId, 0);
		Assert.Equal("JOB-001", laser.JobName);
		Assert.Equal(50, laser.TargetQuantity);
	}

	[Fact]
	public void P20R2_PressBrakeHmiRegression_UnchangedContract()
	{
		var profile = VigilPressBrakeReducedProfileFactory.Create();
		Assert.Equal(14, profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void P20R2_Core24Contract_Unchanged()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateCore24();
		Assert.Equal(24, profile.Signals.Count(s => s.IsEnabled));
		Assert.Equal(AutonomousCellKinematicsState.CoreSignalIds.OrderBy(s => s),
			profile.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).OrderBy(s => s));
	}

	[Fact]
	public void P20R2_OpcContract_NoHiddenAmrExposure()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		int hiddenCount = profile.Signals.Count(s => s.IsEnabled && (
			s.SignalId.Contains("AMR", StringComparison.OrdinalIgnoreCase)
			|| s.SignalId.Contains("GroundTruth", StringComparison.OrdinalIgnoreCase)
			|| s.SignalId.Contains("Hidden", StringComparison.OrdinalIgnoreCase)));
		Assert.Equal(0, hiddenCount);
	}

	private static PhysicalMachineSession CreateSession()
	{
		PhysicalSimulationEngine engine = new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
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

	private static void StartProduction(PhysicalMachineSession session)
	{
		PhysicalJobCoordinator.ApplyDefinition(
			session.Simulation,
			VirtualAutonomousCellRunProfile.ResolveJobDefinition(session.MachineId, 0),
			session.Runtime);
		AutonomousCellKinematicsEngine.OnJobApplied(session.Simulation, Seed);
		AutonomousCellKinematicsEngine.OnProductionResumed(session.Simulation, Seed);
		session.Simulation.IsProductionMotionActive = true;
	}

	private static void Tick(PhysicalMachineSession session)
	{
		PhysicalSimulationEngine engine = new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
		engine.Tick(session, TimeSpan.FromMilliseconds(20));
	}
}
