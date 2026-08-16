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

[Collection("PhysicalVerification")]
public sealed class VigilLabVirtualMachineP03Tests
{
	[Fact]
	public void P03_VirtualMachineMode_ContainsExactlyTwoTargetMachines()
	{
		var configurationService = CreateConfigurationService();
		configurationService.InitializeAsync(ApplicationOperatingMode.VirtualMachine).GetAwaiter().GetResult();

		var machines = configurationService.Configuration.Machines;
		Assert.Equal(2, machines.Count);
		Assert.Contains(machines, m => m.Id == VirtualMachineContract.MachineId);
		Assert.Contains(machines, m => m.Id == VigilLabMachineContract.MachineId);
		Assert.DoesNotContain(machines, m => m.Port is 4841 or 4842 or 4843);
	}

	[Fact]
	public void P03_VigilLabMachineContract_IsStable()
	{
		Assert.Equal(new Guid("b2222222-2222-4222-8222-222222222222"), VigilLabMachineContract.MachineId);
		Assert.Equal("opc.tcp://localhost:4844", VigilLabMachineContract.Endpoint);
		Assert.Equal("urn:werkflow:simulator:vigil-lab", VigilLabMachineContract.NamespaceUri);
		Assert.Equal("vigil-lab-laser-reduced", VigilLabMachineContract.PhysicalProfileId);
	}

	[Fact]
	public void P03_ExistingVirtualMachineContract_Unchanged()
	{
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		Assert.Equal(VirtualMachineContract.MachineId, machine.Id);
		Assert.Equal(VirtualMachineContract.DisplayName, machine.Name);
		Assert.Equal(VirtualMachineContract.Endpoint, machine.Endpoint);
		Assert.Equal(VirtualMachineContract.PhysicalProfileId, machine.PhysicalProfileId);
		Assert.Equal(309, LaserProcessingMachine300ProfileFactory.Create().Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void P03_LaserKinematicsEngine_EnablesForBothVirtualLasersOnly()
	{
		Assert.True(LaserKinematicsEngine.ShouldEnable(VirtualMachineContract.MachineId));
		Assert.True(LaserKinematicsEngine.ShouldEnable(VigilLabMachineContract.MachineId));
		Assert.False(LaserKinematicsEngine.ShouldEnable(DefaultMachines.Create()[1].Id));
	}

	[Fact]
	public void P03_ReducedProfile_EnablesExactlySevenPhysicalSignals()
	{
		var profile = VigilLabLaserReducedProfileFactory.Create();
		var enabled = profile.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).OrderBy(s => s).ToArray();

		Assert.Equal(7, enabled.Length);
		Assert.Equal(
			VigilLabLaserReducedProfileFactory.EnabledPhysicalSignalIds.OrderBy(s => s).ToArray(),
			enabled);
	}

	[Fact]
	public void P03_ReducedProfile_DoesNotMutateFullLaserProfile()
	{
		int before = LaserProcessingMachine300ProfileFactory.Create().Signals.Count(s => s.IsEnabled);
		_ = VigilLabLaserReducedProfileFactory.Create();
		int after = LaserProcessingMachine300ProfileFactory.Create().Signals.Count(s => s.IsEnabled);
		Assert.Equal(309, before);
		Assert.Equal(309, after);
	}

	[Fact]
	public void P03_VigilLabSessions_AreIsolated()
	{
		var factory = CreateSessionFactory();
		var existing = factory.TryCreateSession(
			VirtualMachineContract.MachineId,
			VirtualMachineContract.DisplayName,
			VirtualMachineContract.PhysicalProfileId)!;
		var vigilLab = factory.TryCreateSession(
			VigilLabMachineContract.MachineId,
			VigilLabMachineContract.DisplayName,
			VigilLabMachineContract.PhysicalProfileId)!;

		var engine = PhysicalTestServiceFactory.CreateEngine();
		engine.Initialize(existing, 42);
		engine.Initialize(vigilLab, 42);

		existing.Simulation.Kinematics.X = 111.0;
		vigilLab.Simulation.Kinematics.X = 222.0;

		Assert.Equal(111.0, existing.Simulation.Kinematics.X);
		Assert.Equal(222.0, vigilLab.Simulation.Kinematics.X);
		Assert.NotSame(existing.Runtime, vigilLab.Runtime);
		Assert.NotSame(existing.Simulation, vigilLab.Simulation);
	}

	[Fact]
	public void P03_VigilLabDeterminism_SameSeedProducesSameInitialKinematics()
	{
		var factory = CreateSessionFactory();
		var first = factory.TryCreateSession(
			VigilLabMachineContract.MachineId,
			VigilLabMachineContract.DisplayName,
			VigilLabMachineContract.PhysicalProfileId)!;
		var second = factory.TryCreateSession(
			VigilLabMachineContract.MachineId,
			VigilLabMachineContract.DisplayName,
			VigilLabMachineContract.PhysicalProfileId)!;

		int seed = VigilLabRunProfile.ResolveSimulationSeed(VigilLabMachineContract.MachineId, 99)
			^ VigilLabMachineContract.MachineId.GetHashCode();
		var engine = PhysicalTestServiceFactory.CreateEngine();
		engine.Initialize(first, seed);
		engine.Initialize(second, seed);

		Assert.Equal(first.Simulation.Kinematics.X, second.Simulation.Kinematics.X);
		Assert.Equal(first.Simulation.Kinematics.Y, second.Simulation.Kinematics.Y);
		Assert.Equal(first.Simulation.Kinematics.MotionPhase, second.Simulation.Kinematics.MotionPhase);
	}

	[Fact]
	public void P03_VigilLabRunProfile_UsesFixedJobSequenceFromCatalog()
	{
		Assert.Equal(["JOB-001", "JOB-002", "JOB-003", "JOB-004"], VigilLabRunProfile.FixedJobIds);
		Assert.Equal(42, VigilLabRunProfile.RandomSeed);

		var settings = FixedSimulationCatalog.CreateDefaultSettings();
		VigilLabRunProfile.ApplyDeterministicSettings(settings);
		Assert.False(settings.GenerateNewSeedOnStart);
		Assert.False(settings.RandomModeEnabled);
		Assert.Equal(42, settings.RandomSeed);
	}

	[Fact]
	public void P03_SessionFactory_RegistersReducedProfile()
	{
		var factory = CreateSessionFactory();
		var profile = factory.ResolveProfile(VigilLabMachineContract.PhysicalProfileId);
		Assert.NotNull(profile);
		Assert.Equal(7, profile!.Signals.Count(s => s.IsEnabled));
	}

	private static ConfigurationService CreateConfigurationService()
	{
		return new ConfigurationService(new LogService(), new JobGenerator());
	}

	private static PhysicalMachineSessionFactory CreateSessionFactory() =>
		new(
			new JsonPhysicalMachineProfileLoader(new PhysicalMachineProfileValidator()),
			new PhysicalMachineProfileValidator(),
			new PhysicalMachineRuntimeFactory());
}
