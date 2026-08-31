using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class Machine3PhysicalProfileActivationTests
{
	[Fact]
	public void SIM_P32R2_DefaultMachine3_UsesCore24Profile()
	{
		MachineConfiguration machine = DefaultMachines.Create()
			.First(m => m.Port == VirtualAutonomousProductionCellContract.Port);

		Assert.Equal(VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24, machine.PhysicalProfileId);
		Assert.Equal("CORE24", Machine3PhysicalProfileActivation.ResolveOperatorProfileLabel(machine.PhysicalProfileId));
		Assert.Equal(24, Machine3PhysicalProfileActivation.ResolveEnabledSignalCount(machine.PhysicalProfileId));
	}

	[Fact]
	public void SIM_P32R2_EnvVar_SelectsExpanded48Profile()
	{
		string? previous = Environment.GetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName);
		try
		{
			Environment.SetEnvironmentVariable(
				Machine3PhysicalProfileActivation.EnvironmentVariableName,
				VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48);

			List<MachineConfiguration> machines = DefaultMachines.Create();
			Machine3PhysicalProfileActivation.Apply(machines);
			MachineConfiguration machine = machines.First(m => m.Port == VirtualAutonomousProductionCellContract.Port);

			Assert.Equal(VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48, machine.PhysicalProfileId);
			Assert.Equal("EXPANDED48", Machine3PhysicalProfileActivation.ResolveOperatorProfileLabel(machine.PhysicalProfileId));
			Assert.Equal(48, Machine3PhysicalProfileActivation.ResolveEnabledSignalCount(machine.PhysicalProfileId));
		}
		finally
		{
			Environment.SetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName, previous);
		}
	}

	[Fact]
	public void SIM_P32R2_EnvVar_RejectsUnsupportedProfile()
	{
		string? previous = Environment.GetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName);
		try
		{
			Environment.SetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName, "vigil-autonomous-cell-unknown");
			List<MachineConfiguration> machines = DefaultMachines.Create();

			InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
				Machine3PhysicalProfileActivation.Apply(machines));

			Assert.Contains(Machine3PhysicalProfileActivation.EnvironmentVariableName, exception.Message, StringComparison.Ordinal);
		}
		finally
		{
			Environment.SetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName, previous);
		}
	}

	[Fact]
	public void SIM_P32R2_Core24_RegistersExactly24EnabledSignals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateCore24();
		Assert.Equal(24, profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void SIM_P32R2_Expanded48_RegistersExactly48EnabledSignals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		Assert.Equal(48, profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void SIM_P32R2_Core24_IsExactPrefixOfExpanded48()
	{
		List<string> core = VigilAutonomousCellProfileFactory.CreateCore24()
			.Signals.Where(s => s.IsEnabled)
			.Select(s => s.SignalId)
			.ToList();
		List<string> expanded = VigilAutonomousCellProfileFactory.CreateExpanded48()
			.Signals.Where(s => s.IsEnabled)
			.Select(s => s.SignalId)
			.ToList();

		Assert.Equal(AutonomousCellKinematicsState.CoreSignalIds.OrderBy(s => s), core.OrderBy(s => s));
		Assert.Equal(core, expanded.Take(core.Count));
		Assert.Equal(48, expanded.Count);
	}

	[Fact]
	public void SIM_P32R2_Expanded48_HasNo49PlusLeakageOrHiddenAmr()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		Assert.Equal(48, profile.Signals.Count);
		foreach (SignalDefinition signal in profile.Signals.Where(s => s.IsEnabled))
		{
			Assert.DoesNotContain("GroundTruth", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("AMR", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Hidden", signal.SignalId, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void SIM_P32R4_Hmi_ExposesActiveProfileAndSignalCountBindings()
	{
		string viewModel = ReadAppSource("VirtualMachine/ViewModels/VirtualMachineHmiViewModel.cs");
		string window = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");

		Assert.Contains("ActiveSignalProfileText", viewModel);
		Assert.Contains("ActiveSignalCountText", viewModel);
		Assert.Contains("UpdateActiveSignalProfilePresentation", viewModel);
		Assert.Contains("Machine3PhysicalProfileActivation.ResolveOperatorProfileLabel", viewModel);
		Assert.Contains(nameof(VirtualMachineHmiViewModel.ActiveSignalProfileText), window);
		Assert.Contains(nameof(VirtualMachineHmiViewModel.ActiveSignalCountText), window);
	}

	private static string ReadAppSource(string relativePath)
	{
		string path = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"Werkflow.OpcUaSimulator.App",
			relativePath));
		return File.ReadAllText(path);
	}
}
