using System;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class LaserKinematicsPlausibilityTests
{
	[Fact]
	public void VirtualMachine_Kinematics_UseWorkspaceAndPhaseFeed()
	{
		PhysicalMachineProfile profile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalSimulationEngine engine = new PhysicalSimulationEngine(
			new HiddenProcessStateEngine(),
			new SignalCalculationEngine(),
			new PhysicalModelValidator());
		PhysicalMachineSession session = new PhysicalMachineSession
		{
			MachineId = VirtualMachineContract.MachineId,
			MachineName = VirtualMachineContract.DisplayName,
			Profile = profile,
			Runtime = new PhysicalMachineRuntimeFactory().Create(profile, null)
		};
		session.Simulation.TimeFactor = 20.0;
		session.Simulation.Job.TargetQuantity = 3;
		PhysicalJobCoordinator.ApplyDefinition(session.Simulation, FixedSimulationCatalog.GetDefinition(2), session.Runtime);

		engine.Initialize(session, 42);
		Assert.True(session.Simulation.Kinematics.IsEnabled);
		Assert.InRange(session.Simulation.Kinematics.X, 20.0, 40.0);
		Assert.InRange(session.Simulation.Kinematics.Y, 40.0, 60.0);

		double maxFeed = 0.0;
		for (int i = 0; i < 800; i++)
		{
			engine.Tick(session, TimeSpan.FromMilliseconds(100));
			double feed = session.Runtime.Signals.First(s => s.SignalId == "Process.FeedRate").CurrentValue;
			maxFeed = Math.Max(maxFeed, feed);
		}

		var xPos = session.Runtime.Signals.First(s => s.SignalId == "Axis01.Position").CurrentValue;
		var yPos = session.Runtime.Signals.First(s => s.SignalId == "Axis02.Position").CurrentValue;
		Assert.True(xPos > 200.0);
		Assert.True(yPos > 150.0);
		Assert.True(session.Simulation.Kinematics.MaxX - session.Simulation.Kinematics.MinX > 300.0);
		Assert.True(session.Simulation.Kinematics.MaxY - session.Simulation.Kinematics.MinY > 200.0);
		Assert.True(maxFeed > 100.0);
		Assert.Contains(session.Simulation.Kinematics.MotionPhase, new[] { LaserMotionPhase.Cutting, LaserMotionPhase.RapidPositioning, LaserMotionPhase.Piercing });
	}
}
