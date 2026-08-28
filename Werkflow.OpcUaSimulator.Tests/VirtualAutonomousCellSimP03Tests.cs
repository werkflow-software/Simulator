using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class VirtualAutonomousCellContractTests
{
	[Fact]
	public void SIM_P03_Contract_Machine3_Port4842()
	{
		MachineConfiguration machine = DefaultMachines.Create().First(m => m.Port == VirtualAutonomousProductionCellContract.Port);
		Assert.Equal(VirtualAutonomousProductionCellContract.MachineId, machine.Id);
		Assert.Equal(VirtualAutonomousProductionCellContract.DisplayName, machine.Name);
		Assert.Equal(VirtualAutonomousProductionCellContract.Endpoint, machine.Endpoint);
		Assert.Equal(VirtualAutonomousProductionCellContract.NamespaceUri, machine.NamespaceUri);
	}

	[Fact]
	public void SIM_P03_CoreProfile_Exactly24Signals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateCore24();
		List<string> enabled = profile.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).OrderBy(s => s).ToList();
		Assert.Equal(24, enabled.Count);
		Assert.Equal(AutonomousCellKinematicsState.CoreSignalIds.OrderBy(s => s), enabled);
	}

	[Fact]
	public void SIM_P03_FixtureClampForce_NotClampPressure()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateCore24();
		Assert.Contains(profile.Signals, s => s.SignalId == "Fixture.ClampForce");
		Assert.DoesNotContain(profile.Signals, s => s.SignalId == "Fixture.ClampPressure");
	}

	[Fact]
	public void SIM_P03_ExpandedProfile_Exactly48Signals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		Assert.Equal(48, profile.Signals.Count(s => s.IsEnabled));
		foreach (string core in AutonomousCellKinematicsState.CoreSignalIds)
		{
			Assert.Contains(profile.Signals.Where(s => s.IsEnabled), s => s.SignalId == core);
		}
	}

	[Fact]
	public void SIM_P03_NoGroundTruthOrAmrOpcSignals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		foreach (SignalDefinition signal in profile.Signals.Where(s => s.IsEnabled))
		{
			Assert.DoesNotContain("GroundTruth", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("AMR", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Hidden", signal.SignalId, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void SIM_P03_MasterSeed_IsStableFnv1a()
	{
		int a = Machine3SeedArchitecture.MasterScenarioSeed;
		int b = Machine3SeedArchitecture.StableFnv1a32("M3BASE2026");
		Assert.Equal(a, b);
		Assert.NotEqual(0, a);
	}

	[Fact]
	public void SIM_P03_LaserAndPressBrakeRegression_Unchanged()
	{
		MachineConfiguration laser = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		MachineConfiguration press = DefaultMachines.Create().First(m => m.Port == VirtualPressBrakeContract.Port);
		Assert.Equal(VirtualMachineContract.MachineId, laser.Id);
		Assert.Equal(VirtualPressBrakeContract.MachineId, press.Id);
		Assert.Equal(14, VigilPressBrakeReducedProfileFactory.Create().Signals.Count(s => s.IsEnabled));
	}
}

public class VirtualAutonomousCellBaselineTests
{
	[Fact]
	public void SIM_P03_Baseline_Completes28Parts_Unattended()
	{
		(BaselineRunResult resultA, _) = RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24());
		Assert.Equal(28, resultA.CompletedParts);
		Assert.Equal(2, resultA.ReplenishmentEvents);
		Assert.Equal(2, resultA.ContainerExchangeEvents);
		Assert.Equal(new string(VirtualAutonomousCellRunProfile.ProductSequence), resultA.ProductSequenceObserved);
	}

	[Fact]
	public void SIM_P03_CoreValues_Invariant_AcrossProfileSelection()
	{
		(BaselineRunResult core, Dictionary<string, double> coreSnapshots) = RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24(), captureCoreSnapshots: true);
		(BaselineRunResult expanded, Dictionary<string, double> expandedSnapshots) = RunBaseline(VigilAutonomousCellProfileFactory.CreateExpanded48(), captureCoreSnapshots: true);
		Assert.Equal(core.CompletedParts, expanded.CompletedParts);
		Assert.Equal(core.ReplenishmentEvents, expanded.ReplenishmentEvents);
		Assert.Equal(core.ContainerExchangeEvents, expanded.ContainerExchangeEvents);
		foreach (string key in coreSnapshots.Keys)
		{
			Assert.Equal(coreSnapshots[key], expandedSnapshots[key]);
		}
	}

	[Fact]
	public void SIM_P03_GroundTruth_IsInternalOnly()
	{
		var recorder = new AutonomousCellGroundTruthRecorder();
		string path = Path.Combine(Path.GetTempPath(), $"m3-gt-{Guid.NewGuid():N}.jsonl");
		recorder.BeginSession(VirtualAutonomousProductionCellContract.MachineId, Machine3SeedArchitecture.MasterScenarioSeed, path);
		recorder.Record(new AutonomousCellGroundTruthEvent
		{
			TimestampUtc = DateTimeOffset.UtcNow,
			MachineId = VirtualAutonomousProductionCellContract.MachineId,
			EventType = "test",
			AmrTaskState = "inbound_delivery",
			Source = "test"
		});
		Assert.True(File.Exists(path));
		string line = File.ReadAllLines(path).Single();
		Assert.Contains("inbound_delivery", line);
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		Assert.DoesNotContain(profile.Signals, s => s.SignalId.Contains("GroundTruth", StringComparison.OrdinalIgnoreCase));
		File.Delete(path);
	}

	private static (BaselineRunResult Result, Dictionary<string, double> Snapshots) RunBaseline(
		PhysicalMachineProfile profile,
		bool captureCoreSnapshots = false)
	{
		PhysicalSimulationEngine engine = new(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
		PhysicalMachineSession session = new()
		{
			MachineId = VirtualAutonomousProductionCellContract.MachineId,
			MachineName = VirtualAutonomousProductionCellContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null),
			AutonomousCellGroundTruth = new AutonomousCellGroundTruthRecorder()
		};
		((AutonomousCellGroundTruthRecorder)session.AutonomousCellGroundTruth!).BeginSession(
			VirtualAutonomousProductionCellContract.MachineId,
			Machine3SeedArchitecture.MasterScenarioSeed,
			Path.Combine(Path.GetTempPath(), $"m3-gt-run-{Guid.NewGuid():N}.jsonl"));
		session.Simulation.TimeFactor = 50.0;
		session.Simulation.Job.TargetQuantity = VirtualAutonomousCellRunProfile.TotalParts;
		int seed = Machine3SeedArchitecture.MasterScenarioSeed;
		engine.Initialize(session, seed);
		PhysicalJobCoordinator.ApplyDefinition(session.Simulation, VirtualAutonomousCellRunProfile.ResolveJobDefinition(session.MachineId, 0), session.Runtime);
		AutonomousCellKinematicsEngine.OnJobApplied(session.Simulation, seed);
		session.Simulation.IsProductionMotionActive = true;

		List<char> variants = [];
		Dictionary<string, double> snapshots = new(StringComparer.OrdinalIgnoreCase);
		int ticks = 0;
		while (session.Simulation.AutonomousCell.MotionPhase != AutonomousCellMotionPhase.Complete && ticks < 200_000)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(20));
			if (session.Simulation.AutonomousCell.MotionPhase == AutonomousCellMotionPhase.VisionInspect
			    && session.Simulation.AutonomousCell.PhaseElapsedSeconds > 0.1)
			{
				char variant = session.Simulation.AutonomousCell.CurrentVariant;
				if (variants.Count == 0 || variants[^1] != variant || session.Simulation.AutonomousCell.CompletedParts != variants.Count - 1)
				{
					if (variants.Count < session.Simulation.AutonomousCell.CompletedParts + 1)
					{
						variants.Add(variant);
					}
				}
			}

			ticks++;
		}

		if (captureCoreSnapshots)
		{
			foreach (string signalId in AutonomousCellKinematicsState.CoreSignalIds)
			{
				SignalRuntimeState? signal = session.Runtime.Signals.FirstOrDefault(s => s.SignalId == signalId);
				if (signal != null)
				{
					snapshots[signalId] = signal.CurrentValue;
				}
			}
		}

		while (variants.Count < session.Simulation.AutonomousCell.CompletedParts)
		{
			variants.Add('?');
		}

		return (new BaselineRunResult
		{
			CompletedParts = session.Simulation.AutonomousCell.CompletedParts,
			ReplenishmentEvents = session.Simulation.AutonomousCell.ReplenishmentEvents,
			ContainerExchangeEvents = session.Simulation.AutonomousCell.ContainerExchangeEvents,
			ProductSequenceObserved = new string(variants.Take(VirtualAutonomousCellRunProfile.TotalParts).ToArray())
		}, snapshots);
	}

	private sealed class BaselineRunResult
	{
		public int CompletedParts { get; init; }
		public int ReplenishmentEvents { get; init; }
		public int ContainerExchangeEvents { get; init; }
		public string ProductSequenceObserved { get; init; } = string.Empty;
	}
}
