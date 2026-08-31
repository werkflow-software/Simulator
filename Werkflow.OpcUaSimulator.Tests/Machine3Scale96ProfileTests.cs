using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class Machine3Scale96ProfileTests
{
	private static readonly string[] FrozenScale96AdditionalSignals =
	[
		"Process.HydraulicPressureSupply",
		"Process.HydraulicPressureReturn",
		"Fixture.AlignmentPinPosition",
		"LoadRobot.MotorCurrentPhaseA",
		"LoadRobot.MotorCurrentPhaseB",
		"TransferRobot.MotorCurrentPhaseA",
		"Sorting.LaneOccupancyIndex",
		"Output.ConveyorSpeedActual",
		"Inbound.ScaleGrossWeight",
		"TransferRobot.VacuumPumpCurrent",
		"Process.ClampApproachVelocity",
		"Vision.PartEdgeGradient",
		"Process.ForcePeakFiltered",
		"Fixture.ClampPressureSecondary",
		"TransferRobot.AxisPositionEncoder",
		"Vision.DimensionOffsetDuplicate",
		"Cell.LineFrequencyHz",
		"Output.ContainerFillLevelSmoothed",
		"TransferRobot.BeltTensionActual",
		"Cell.CompressedAirPressure",
		"Cell.CoolantFlowRate",
		"Cell.LineVoltageRms",
		"LoadRobot.BrakeReleasePressure",
		"Process.FilterDifferentialPressure",
		"Process.DieTemperatureZoneA",
		"Fixture.GuideWearIndicator",
		"Vision.LensTemperature",
		"Process.OilTemperatureSump",
		"Vision.FocusDrivePosition",
		"Sorting.DiverterActuationCount",
		"Cell.GroundLeakageMilliamp",
		"Output.LabelApplicatorReady",
		"Cell.PowerFactorInstantaneous",
		"Process.ServoBusUtilization",
		"LoadRobot.FollowingError",
		"Vision.SurfaceReflectanceIndex",
		"Auxiliary.PlantChilledWaterSupplyTemp",
		"Auxiliary.NeighborPressVibration",
		"Auxiliary.BuildingHvacDamperPosition",
		"Auxiliary.UnrelatedStackLightState",
		"Vision.BarcodeReadConfidence",
		"Output.RejectChutePosition",
		"Inbound.BarcodeScannerTrigger",
		"Vision.BacklightIntensity",
		"Cell.DoorInterlockState",
		"Cell.ShiftMaintenanceModeActive",
		"Process.CycleTimeSlidingAverage",
		"Process.PeakForceLastStroke"
	];

	[Fact]
	public void SIM_P36_Scale96_ProfileResolvesExactly96EnabledSignals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateScale96();
		Assert.Equal(VigilAutonomousCellProfileFactory.ProfileIdScale96, profile.ProfileId);
		Assert.Equal(96, profile.Signals.Count(s => s.IsEnabled));
		Assert.Equal(96, profile.Signals.Count);
	}

	[Fact]
	public void SIM_P36_Core24_RemainsExactly24EnabledSignals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateCore24();
		Assert.Equal(24, profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void SIM_P36_Expanded48_RemainsExactly48EnabledSignals()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateExpanded48();
		Assert.Equal(48, profile.Signals.Count(s => s.IsEnabled));
	}

	[Fact]
	public void SIM_P36_Expanded48_IsExactPrefixOfScale96()
	{
		List<string> expanded = VigilAutonomousCellProfileFactory.CreateExpanded48()
			.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).ToList();
		List<string> scale96 = VigilAutonomousCellProfileFactory.CreateScale96()
			.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).ToList();

		Assert.Equal(expanded, scale96.Take(48));
		Assert.Equal(AutonomousCellScale96SignalIds.All, scale96);
	}

	[Fact]
	public void SIM_P36_Core24_IsExactPrefixOfScale96()
	{
		List<string> core = VigilAutonomousCellProfileFactory.CreateCore24()
			.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).ToList();
		List<string> scale96 = VigilAutonomousCellProfileFactory.CreateScale96()
			.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).ToList();

		Assert.Equal(core, scale96.Take(24));
	}

	[Fact]
	public void SIM_P36_Signals49to96_MatchFrozenContract()
	{
		List<string> additional = VigilAutonomousCellProfileFactory.CreateScale96()
			.Signals.Where(s => s.IsEnabled).Select(s => s.SignalId).Skip(48).ToList();

		Assert.Equal(FrozenScale96AdditionalSignals, additional);
		Assert.Equal(AutonomousCellScale96SignalIds.Additional, additional);
	}

	[Fact]
	public void SIM_P36_SignalKeysAndNodeIds_AreUnique()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateScale96();
		List<string> keys = profile.Signals.Select(s => s.SignalId).ToList();
		List<string> nodeIds = profile.Signals.Select(s => s.NodeId).ToList();
		Assert.Equal(96, keys.Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(96, nodeIds.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void SIM_P36_NoHiddenAmrOrGtOrBankLeakage()
	{
		PhysicalMachineProfile profile = VigilAutonomousCellProfileFactory.CreateScale96();
		foreach (SignalDefinition signal in profile.Signals.Where(s => s.IsEnabled))
		{
			Assert.DoesNotContain("GroundTruth", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("AMR", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Hidden", signal.SignalId, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Bank.", signal.SignalId, StringComparison.Ordinal);
			Assert.DoesNotContain("evaluator", signal.DisplayName, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("irrelevant", signal.DisplayName, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void SIM_P36_EnvVar_SelectsScale96Profile()
	{
		string? previous = Environment.GetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName);
		try
		{
			Environment.SetEnvironmentVariable(
				Machine3PhysicalProfileActivation.EnvironmentVariableName,
				VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96);

			List<MachineConfiguration> machines = DefaultMachines.Create();
			Machine3PhysicalProfileActivation.Apply(machines);
			MachineConfiguration machine = machines.First(m => m.Port == VirtualAutonomousProductionCellContract.Port);

			Assert.Equal(VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96, machine.PhysicalProfileId);
			Assert.Equal("SCALE96", Machine3PhysicalProfileActivation.ResolveOperatorProfileLabel(machine.PhysicalProfileId));
			Assert.Equal(96, Machine3PhysicalProfileActivation.ResolveEnabledSignalCount(machine.PhysicalProfileId));
		}
		finally
		{
			Environment.SetEnvironmentVariable(Machine3PhysicalProfileActivation.EnvironmentVariableName, previous);
		}
	}

	[Fact]
	public void SIM_P36_Hmi_ExposesScale96ProfileAndCountBindings()
	{
		string viewModel = ReadAppSource("VirtualMachine/ViewModels/VirtualMachineHmiViewModel.cs");
		string window = ReadAppSource("VirtualMachine/Views/VirtualMachineHmiWindow.cs");
		string activation = File.ReadAllText(Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"Werkflow.OpcUaSimulator.Core",
			"VirtualMachine",
			"Machine3PhysicalProfileActivation.cs")));

		Assert.Contains("ActiveSignalProfileText", viewModel);
		Assert.Contains("ActiveSignalCountText", viewModel);
		Assert.Contains("Machine3PhysicalProfileActivation.ResolveOperatorProfileLabel", viewModel);
		Assert.Contains("Signale:", window, StringComparison.Ordinal);
		Assert.Contains("SCALE96", activation, StringComparison.Ordinal);
		Assert.Equal("SCALE96", Machine3PhysicalProfileActivation.ResolveOperatorProfileLabel(
			VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96));
		Assert.Equal("96", Machine3PhysicalProfileActivation.ResolveEnabledSignalCount(
			VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96).ToString());
	}

	[Fact]
	public void SIM_P36_Baseline_28Parts_UnchangedAcrossProfiles()
	{
		VirtualAutonomousCellBaselineTests.BaselineRunResult core =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24()).Result;
		VirtualAutonomousCellBaselineTests.BaselineRunResult expanded =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateExpanded48()).Result;
		VirtualAutonomousCellBaselineTests.BaselineRunResult scale96 =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateScale96()).Result;

		Assert.Equal(28, scale96.CompletedParts);
		Assert.Equal(core.ReplenishmentEvents, scale96.ReplenishmentEvents);
		Assert.Equal(core.ContainerExchangeEvents, scale96.ContainerExchangeEvents);
		Assert.Equal(core.CompletedParts, expanded.CompletedParts);
		Assert.Equal(new string(VirtualAutonomousCellRunProfile.ProductSequence), scale96.ProductSequenceObserved);
	}

	[Fact]
	public void SIM_P36_CoreValues_Invariant_WithScale96Profile()
	{
		(VirtualAutonomousCellBaselineTests.BaselineRunResult core, Dictionary<string, double> coreSnapshots) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateCore24(), captureSnapshots: true);
		(VirtualAutonomousCellBaselineTests.BaselineRunResult scale96, Dictionary<string, double> scale96Snapshots) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateScale96(), captureSnapshots: true);

		Assert.Equal(core.CompletedParts, scale96.CompletedParts);
		foreach (string key in AutonomousCellKinematicsState.CoreSignalIds)
		{
			Assert.Equal(coreSnapshots[key], scale96Snapshots[key]);
		}
	}

	[Fact]
	public void SIM_P36_DeterministicNoisyChannels_RepeatableForSameSeed()
	{
		Dictionary<string, double> first = CaptureScale96Snapshots();
		Dictionary<string, double> second = CaptureScale96Snapshots();

		foreach (string noisy in new[]
		         {
			         "Cell.PowerFactorInstantaneous",
			         "Process.ServoBusUtilization",
			         "LoadRobot.FollowingError",
			         "Vision.SurfaceReflectanceIndex"
		         })
		{
			Assert.Equal(first[noisy], second[noisy]);
		}
	}

	[Fact]
	public void SIM_P36_SparseSignals_AreNotConstantMarkers()
	{
		Dictionary<string, double> snapshots = CaptureScale96Snapshots();
		Assert.True(snapshots["Process.DieTemperatureZoneA"] > 20);
		Assert.True(snapshots["Auxiliary.PlantChilledWaterSupplyTemp"] > 8);
		Assert.NotEqual(0, snapshots["Fixture.GuideWearIndicator"]);
	}

	private static Dictionary<string, double> CaptureScale96Snapshots()
	{
		(_, Dictionary<string, double> snapshots) =
			VirtualAutonomousCellBaselineTests.RunBaseline(VigilAutonomousCellProfileFactory.CreateScale96(), captureSnapshots: true);
		return snapshots;
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
