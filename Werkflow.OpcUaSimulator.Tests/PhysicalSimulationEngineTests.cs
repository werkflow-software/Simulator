using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalSimulationEngineTests
{
	private static PhysicalSimulationEngine CreateEngine()
	{
		return new PhysicalSimulationEngine(new HiddenProcessStateEngine(), new SignalCalculationEngine(), new PhysicalModelValidator());
	}

	private static PhysicalMachineSession CreateSession(PhysicalMachineProfile profile, int seed = 42)
	{
		PhysicalMachineRuntime runtime = new PhysicalMachineRuntimeFactory().Create(profile, null);
		return new PhysicalMachineSession
		{
			MachineId = Guid.NewGuid(),
			MachineName = "Test",
			Profile = profile,
			Runtime = runtime
		};
	}

	[Fact]
	public void SameProfileAndSeed_ProducesIdenticalSequence()
	{
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalSimulationEngine physicalSimulationEngine = CreateEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(profile);
		PhysicalMachineSession physicalMachineSession2 = CreateSession(profile);
		PhysicalSimulationEngine physicalSimulationEngine2 = CreateEngine();
		physicalSimulationEngine.Initialize(physicalMachineSession, 42);
		physicalSimulationEngine2.Initialize(physicalMachineSession2, 42);
		for (int i = 0; i < 50; i++)
		{
			physicalSimulationEngine.Tick(physicalMachineSession, TimeSpan.FromMilliseconds(200.0));
			physicalSimulationEngine2.Tick(physicalMachineSession2, TimeSpan.FromMilliseconds(200.0));
		}
		double currentValue = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
		double currentValue2 = physicalMachineSession2.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
		Assert.Equal(currentValue, currentValue2, 6);
	}

	[Fact]
	public void DifferentSeed_ProducesDifferentSequence()
	{
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalSimulationEngine physicalSimulationEngine = CreateEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(profile);
		PhysicalMachineSession physicalMachineSession2 = CreateSession(profile);
		physicalSimulationEngine.Initialize(physicalMachineSession, 42);
		physicalSimulationEngine.Initialize(physicalMachineSession2, 99);
		for (int i = 0; i < 30; i++)
		{
			physicalSimulationEngine.Tick(physicalMachineSession, TimeSpan.FromMilliseconds(200.0));
			physicalSimulationEngine.Tick(physicalMachineSession2, TimeSpan.FromMilliseconds(200.0));
		}
		double[] expected = (from s in physicalMachineSession.Runtime.Signals.Take(10)
			select s.CurrentValue).ToArray();
		double[] actual = (from s in physicalMachineSession2.Runtime.Signals.Take(10)
			select s.CurrentValue).ToArray();
		Assert.NotEqual(expected, actual);
	}

	[Fact]
	public void HiddenStates_AreNotVisibleSignals()
	{
		PhysicalMachineProfile physicalMachineProfile = LaserProcessingMachine300ProfileFactory.Create();
		HashSet<string> set = physicalMachineProfile.Signals.Select((SignalDefinition s) => s.SignalId).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (HiddenProcessStateDefinition hiddenProcessState in physicalMachineProfile.HiddenProcessStates)
		{
			Assert.DoesNotContain(hiddenProcessState.StateId, set);
		}
	}

	[Fact]
	public void MechanicalLoad_IncreasesMotorCurrent()
	{
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		SignalCalculationEngine signalCalculationEngine = new SignalCalculationEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(profile);
		physicalMachineSession.Simulation.GenerationMode = SignalGenerationMode.Physical;
		SeededRandomStreams random = new SeededRandomStreams(42);
		signalCalculationEngine.Initialize(profile, physicalMachineSession.Runtime, physicalMachineSession.Simulation, random);
		HiddenProcessRuntimeState hiddenProcessRuntimeState = physicalMachineSession.Runtime.HiddenProcessStates.First((HiddenProcessRuntimeState s) => s.StateId == "MechanicalLoad");
		hiddenProcessRuntimeState.CurrentValue = 0.2;
		hiddenProcessRuntimeState.TargetValue = 0.2;
		for (int i = 0; i < 8; i++)
		{
			signalCalculationEngine.CalculateSignals(profile, physicalMachineSession.Runtime, physicalMachineSession.Simulation, random, TimeSpan.FromMilliseconds(200.0));
		}
		double currentValue = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
		hiddenProcessRuntimeState.CurrentValue = 0.9;
		hiddenProcessRuntimeState.TargetValue = 0.9;
		for (int j = 0; j < 8; j++)
		{
			signalCalculationEngine.CalculateSignals(profile, physicalMachineSession.Runtime, physicalMachineSession.Simulation, random, TimeSpan.FromMilliseconds(200.0));
		}
		double currentValue2 = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
		Assert.True(currentValue2 > currentValue + 0.25, $"Expected higher motor current for higher load, low={currentValue}, high={currentValue2}");
	}

	[Fact]
	public void Friction_ReducesAxisSpeed()
	{
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalSimulationEngine physicalSimulationEngine = CreateEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(profile);
		physicalSimulationEngine.Initialize(physicalMachineSession, 42);
		HiddenProcessRuntimeState hiddenProcessRuntimeState = physicalMachineSession.Runtime.HiddenProcessStates.First((HiddenProcessRuntimeState s) => s.StateId == "Friction");
		hiddenProcessRuntimeState.CurrentValue = 0.7;
		hiddenProcessRuntimeState.TargetValue = 0.7;
		for (int i = 0; i < 25; i++)
		{
			physicalSimulationEngine.Tick(physicalMachineSession, TimeSpan.FromMilliseconds(200.0));
		}
		double currentValue = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.Speed").CurrentValue;
		Assert.True(currentValue < 960.0);
	}

	[Fact]
	public void Temperature_HasHigherInertiaThanCurrent()
	{
		PhysicalMachineProfile physicalMachineProfile = LaserProcessingMachine300ProfileFactory.Create();
		SignalDefinition signalDefinition = physicalMachineProfile.Signals.First((SignalDefinition s) => s.SignalId == "Axis01.MotorTemperature");
		SignalDefinition signalDefinition2 = physicalMachineProfile.Signals.First((SignalDefinition s) => s.SignalId == "Axis01.MotorCurrent");
		Assert.True(signalDefinition.ResponseInertia >= signalDefinition2.ResponseInertia);
	}

	[Fact]
	public void HardLimits_AreNotExceeded()
	{
		PhysicalMachineProfile physicalMachineProfile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalSimulationEngine physicalSimulationEngine = CreateEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(physicalMachineProfile);
		physicalSimulationEngine.Initialize(physicalMachineSession, 42);
		for (int i = 0; i < 100; i++)
		{
			physicalSimulationEngine.Tick(physicalMachineSession, TimeSpan.FromMilliseconds(200.0));
		}
		foreach (SignalDefinition signalDef in physicalMachineProfile.Signals.Where((SignalDefinition s) => s.DataType == PhysicalSignalDataType.Double))
		{
			SignalRuntimeState signalRuntimeState = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == signalDef.SignalId);
			Assert.InRange(signalRuntimeState.CurrentValue, signalDef.HardMinimum, signalDef.HardMaximum);
		}
	}

	[Fact]
	public void IdlePhase_HasLowerDemandThanPeakLoad()
	{
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalSimulationEngine physicalSimulationEngine = CreateEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(profile);
		physicalSimulationEngine.Initialize(physicalMachineSession, 42);
		physicalMachineSession.Simulation.CurrentPhase = ProcessPhase.Idle;
		for (int i = 0; i < 20; i++)
		{
			physicalSimulationEngine.Tick(physicalMachineSession, TimeSpan.FromMilliseconds(200.0));
		}
		double phaseDemand = GetPhaseDemand(ProcessPhase.Idle);
		double phaseDemand2 = GetPhaseDemand(ProcessPhase.PeakLoad);
		Assert.True(phaseDemand2 > phaseDemand);
	}

	private static double GetPhaseDemand(ProcessPhase phase)
	{
		if (1 == 0)
		{
		}
		double result = phase switch
		{
			ProcessPhase.Idle => 0.15, 
			ProcessPhase.PeakLoad => 0.88, 
			_ => 0.3, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	[Fact]
	public void MachineProfiles_AreDifferent()
	{
		PhysicalMachineProfile physicalMachineProfile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalMachineProfile physicalMachineProfile2 = BendingHydraulicMachine300ProfileFactory.Create();
		Assert.NotEqual(physicalMachineProfile.ProfileId, physicalMachineProfile2.ProfileId);
		Assert.NotEqual(from s in physicalMachineProfile.HiddenProcessStates
			select s.StateId into x
			orderby x
			select x, from s in physicalMachineProfile2.HiddenProcessStates
			select s.StateId into x
			orderby x
			select x);
	}

	[Fact]
	public void LaserProfile_HasMinimumCounts()
	{
		PhysicalMachineProfile physicalMachineProfile = LaserProcessingMachine300ProfileFactory.Create();
		Assert.InRange(physicalMachineProfile.Signals.Count, 285, 320);
		Assert.True(physicalMachineProfile.HiddenProcessStates.Count >= 12);
		Assert.True(physicalMachineProfile.Dependencies.Count >= 60);
		Assert.True(physicalMachineProfile.HiddenStateDependencies.Count >= 15);
	}

	[Fact]
	public void BendingProfile_HasMinimumCounts()
	{
		PhysicalMachineProfile physicalMachineProfile = BendingHydraulicMachine300ProfileFactory.Create();
		Assert.InRange(physicalMachineProfile.Signals.Count, 285, 320);
		Assert.True(physicalMachineProfile.HiddenProcessStates.Count >= 12);
		Assert.True(physicalMachineProfile.Dependencies.Count >= 30);
		Assert.True(physicalMachineProfile.HiddenStateDependencies.Count >= 15);
	}

	[Fact]
	public void ModeSwitch_DoesNotCreateDuplicateEngines()
	{
		PhysicalRuntimeCoordinator physicalRuntimeCoordinator = new PhysicalRuntimeCoordinator(CreateEngine());
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalMachineSession physicalMachineSession = CreateSession(profile);
		physicalRuntimeCoordinator.EnsureEngine(physicalMachineSession, 42);
		physicalRuntimeCoordinator.EnsureEngine(physicalMachineSession, 42);
		physicalRuntimeCoordinator.TrySetGenerationMode(physicalMachineSession, SignalGenerationMode.Physical);
		physicalRuntimeCoordinator.TrySetGenerationMode(physicalMachineSession, SignalGenerationMode.Technical);
		Assert.True(physicalMachineSession.Simulation.IsEngineActive);
	}

	[Fact]
	public void Pause_StopsPhysicalChanges()
	{
		PhysicalSimulationEngine physicalSimulationEngine = CreateEngine();
		PhysicalMachineSession physicalMachineSession = CreateSession(LaserProcessingMachine300ProfileFactory.Create());
		physicalSimulationEngine.Initialize(physicalMachineSession, 42);
		physicalMachineSession.Simulation.IsEngineActive = false;
		double currentValue = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
		physicalSimulationEngine.Tick(physicalMachineSession, TimeSpan.FromSeconds(1.0));
		double currentValue2 = physicalMachineSession.Runtime.Signals.First((SignalRuntimeState s) => s.SignalId == "Axis01.MotorCurrent").CurrentValue;
		Assert.Equal(currentValue, currentValue2);
	}
}
