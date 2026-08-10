using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

[Collection("PhysicalVerification")]
public class PhysicalAp6VirtualMachineTests
{
	[Fact]
	public void AP6_VirtualMachineContract_Machine1_Port4840()
	{
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		Assert.Equal(VirtualMachineContract.MachineId, machine.Id);
		Assert.Equal(VirtualMachineContract.DisplayName, machine.Name);
		Assert.Equal(VirtualMachineContract.Endpoint, machine.Endpoint);
		Assert.Equal(VirtualMachineContract.PhysicalProfileId, machine.PhysicalProfileId);
	}

	[Fact]
	public void AP6_DefaultMachines_FourMachines_Ports4840To4843()
	{
		var machines = DefaultMachines.Create();
		Assert.Equal(4, machines.Count);
		Assert.Equal([4840, 4841, 4842, 4843], machines.Select(m => m.Port).OrderBy(p => p).ToArray());
	}

	[Fact]
	public void AP6_MachineId_StableAcrossDefaultFactoryCalls()
	{
		Guid first = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port).Id;
		Guid second = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port).Id;
		Assert.Equal(VirtualMachineContract.MachineId, first);
		Assert.Equal(first, second);
	}

	[Fact]
	public void AP6_HmiSignalCoverage_AllLaserSignalsMapped()
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		var report = HmiSignalCoverageAnalyzer.Analyze(profile);
		Assert.Equal(report.PhysicalSignalsInProfile, report.HmiSignalsMapped);
		Assert.Empty(report.UnmappedSignals);
	}

	[Fact]
	public void AP6_HmiSignalCatalog_GroupsAxisAndThermal()
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		int axis = profile.Signals.Count(s => s.IsEnabled && s.Category == SignalCategory.Axis);
		int thermal = profile.Signals.Count(s => s.IsEnabled && s.Category == SignalCategory.Thermal);
		Assert.True(axis > 0);
		Assert.True(thermal > 0);
	}

	[Fact]
	public void AP6_HmiTabs_MapAxisProductionAndOtherCategories()
	{
		var profile = LaserProcessingMachine300ProfileFactory.Create();
		var enabled = profile.Signals.Where(s => s.IsEnabled).ToList();

		var axisTab = HmiSignalCatalog.TabDefinitions.First(t => t.TabKey == "axes");
		var productionTab = HmiSignalCatalog.TabDefinitions.First(t => t.TabKey == "production");
		var otherTab = HmiSignalCatalog.TabDefinitions.First(t => t.TabKey == "other");

		Assert.True(enabled.Any(s => axisTab.Categories.Contains(s.Category)));
		Assert.True(enabled.Any(s => productionTab.Categories.Contains(s.Category)));

		var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var tab in HmiSignalCatalog.TabDefinitions)
		{
			foreach (var signal in enabled.Where(s => tab.Categories.Contains(s.Category)))
			{
				mapped.Add(signal.SignalId);
			}
		}

		var unmapped = enabled.Where(s => !mapped.Contains(s.SignalId)).ToList();
		Assert.Empty(unmapped);
	}

	[Fact]
	public void AP6_HmiAxisKey_ExtractsAxisGroups()
	{
		Assert.Equal("Axis01", HmiSignalCatalog.ExtractAxisKey("Axis01.Position"));
		Assert.Equal("Axis02", HmiSignalCatalog.ExtractAxisKey("Axis02.MotorTemperature"));
		Assert.Null(HmiSignalCatalog.ExtractAxisKey("Process.FeedRate"));
	}

	[Fact]
	public void AP6_HmiDisplayName_NoNodeIdPath()
	{
		var definition = new SignalDefinition
		{
			SignalId = "Axis01.MotorTemperature",
			DisplayName = "Motor Temperature",
			EngineeringUnit = "°C",
			DecimalPlaces = 1
		};
		Assert.Equal("Motor Temperature", HmiSignalCatalog.FormatDisplayName(definition));
		Assert.Equal("52.5 °C", HmiSignalCatalog.FormatValue(definition, 52.4938));
	}

	[Fact]
	public async Task AP6_LaserFaultCatalog_FiltersBendingOnlyScenarios()
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		await stack.FaultScenarioService.InitializeAsync();
		var laserScenarios = stack.FaultScenarioService.GetCatalog()
			.Where(s => s.IsEnabled && s.MachineProfileIds.Any(id =>
				id.Equals(VirtualMachineContract.PhysicalProfileId, StringComparison.OrdinalIgnoreCase)))
			.Select(s => s.ScenarioId)
			.ToList();

		Assert.Contains("laser-overheating-axis-drive", laserScenarios);
		Assert.DoesNotContain(laserScenarios, id => id.StartsWith("bending-", StringComparison.OrdinalIgnoreCase));
		Assert.True(laserScenarios.Count >= 10);
	}

	[Fact]
	public async Task AP6_LeakageRegression_StillPasses()
	{
		var report = await PhysicalAp5R1VerificationHarness.RunLeakageVerificationAsync();
		Assert.True(report.Passed);
		Assert.Empty(report.Matches);
	}

	[Fact]
	public async Task AP6_VerificationHarness_Passes()
	{
		var report = await PhysicalAp6VerificationHarness.RunVerificationAsync();
		Assert.True(report.Passed, string.Join(",", report.FailedCriteria));
	}
}
