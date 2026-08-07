using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalPhysicsR1VerificationHarness
{
	private sealed class MonitoredSignalPlan
	{
		public required string[] StatisticsSignals { get; init; }

		public required CorrelationPlan[] CorrelationGroups { get; init; }

		public required UncorrelatedPlan[] UncorrelatedPairs { get; init; }
	}

	private sealed record CorrelationPlan(string PairId, string HiddenStateId, string TargetSignalId, string Direction, string DependencyType, int ExpectedLagSeconds);

	private sealed record UncorrelatedPlan(string PairId, string A, string B);

	private static readonly MonitoredSignalPlan LaserPlan = new MonitoredSignalPlan
	{
		StatisticsSignals = new string[31]
		{
			"Axis01.MotorCurrent", "Axis01.Load", "Axis01.Speed", "Axis01.MotorTemperature", "Axis01.VibrationRms", "Axis02.MotorCurrent", "Axis02.MotorTemperature", "Process.SpindleSpeed", "Process.FeedRate", "Process.PowerDemand",
			"Process.LaserPowerActual", "Process.FocusPosition", "Process.QualityIndex", "Thermal.SpindleMotorTemp", "Thermal.CabinetTemperature", "Thermal.AmbientTemperature", "Cooling.PrimaryCircuit.Temperature", "Cooling.PrimaryCircuit.Flow", "Cooling.PrimaryCircuit.Pressure", "Mechanical.VibrationRms",
			"Electrical.MainsVoltage", "Electrical.PowerFactor", "Production.OeeAvailability", "Production.Throughput", "Quality.SurfaceInspectionScore", "Vibration.Point01.Rms", "Process.CycleTime", "Process.ToolWearIndex", "Axis03.MotorCurrent", "Axis04.MotorTemperature",
			"Drive01.Temperature"
		},
		CorrelationGroups = new CorrelationPlan[10]
		{
			new CorrelationPlan("laser-01", "MechanicalLoad", "Axis01.MotorCurrent", "positive", "linear", 0),
			new CorrelationPlan("laser-02", "MechanicalLoad", "Axis01.Load", "positive", "linear", 0),
			new CorrelationPlan("laser-03", "MechanicalLoad", "Mechanical.VibrationRms", "positive", "piecewiseLinear", 0),
			new CorrelationPlan("laser-04", "Friction", "Axis01.Speed", "negative", "inverseLinear", 0),
			new CorrelationPlan("laser-05", "Friction", "Axis01.MotorCurrent", "positive", "polynomial", 0),
			new CorrelationPlan("laser-06", "ThermalLoad", "Axis01.MotorTemperature", "positive", "delayedLinear", 20),
			new CorrelationPlan("laser-07", "CoolingEfficiency", "Cooling.PrimaryCircuit.Temperature", "negative", "inverseLinear", 0),
			new CorrelationPlan("laser-08", "ProcessDemand", "Process.PowerDemand", "positive", "linear", 0),
			new CorrelationPlan("laser-09", "OpticalCondition", "Process.FocusPosition", "positive", "linear", 0),
			new CorrelationPlan("laser-10", "MaterialResistance", "Process.FeedRate", "negative", "linear", 0)
		},
		UncorrelatedPairs = new UncorrelatedPlan[5]
		{
			new UncorrelatedPlan("laser-u01", "Electrical.CabinetHumidity", "Production.ActiveProgram"),
			new UncorrelatedPlan("laser-u02", "Diagnostic.WatchdogCounter", "Process.FocusPosition"),
			new UncorrelatedPlan("laser-u03", "Production.ToolLifeRemaining", "Axis01.VibrationRms"),
			new UncorrelatedPlan("laser-u04", "Production.NextMaintenanceHours", "Cooling.PrimaryCircuit.Pressure"),
			new UncorrelatedPlan("laser-u05", "Production.ActiveRecipeNumber", "Axis01.MotorTemperature")
		}
	};

	private static readonly MonitoredSignalPlan BendingPlan = new MonitoredSignalPlan
	{
		StatisticsSignals = new string[32]
		{
			"Hydraulic.SupplyPressure", "Hydraulic.OilTemperature", "Hydraulic.FilterLoad", "Hydraulic.PumpSpeed", "Bending.PressForce", "Bending.RamPosition", "Bending.CycleTime", "Bending.AngleMeasured", "Axis01.MotorCurrent", "Axis01.Speed",
			"Axis01.Load", "Axis01.MotorTemperature", "Thermal.FrameTemperature", "Thermal.CabinetTemperature", "Thermal.AmbientTemperature", "Electrical.TotalCurrent", "Electrical.MainsVoltage", "Process.PowerDemand", "Quality.ProcessQualityIndex", "Quality.AngleDeviation",
			"Cooling.PrimaryCircuit.Flow", "Pneumatic.SupplyPressure", "Production.LastCycleDuration", "Production.Throughput", "Vibration.Point01.Rms", "Drive01.Temperature", "Hydraulic.ReturnPressure", "Bending.BendAngleError", "Environment.Humidity", "Production.EnergyPerPart",
			"Mechanical.FrameStress", "Quality.EdgeQualityIndex"
		},
		CorrelationGroups = new CorrelationPlan[10]
		{
			new CorrelationPlan("bend-01", "PressLoad", "Hydraulic.SupplyPressure", "positive", "linear", 0),
			new CorrelationPlan("bend-02", "PressLoad", "Bending.PressForce", "positive", "saturating", 0),
			new CorrelationPlan("bend-03", "HydraulicEfficiency", "Hydraulic.SupplyPressure", "positive", "linear", 0),
			new CorrelationPlan("bend-04", "OilCondition", "Hydraulic.FilterLoad", "negative", "inverseLinear", 0),
			new CorrelationPlan("bend-05", "ToolDeflection", "Bending.BendAngleError", "positive", "linear", 0),
			new CorrelationPlan("bend-06", "MaterialSpringback", "Quality.ProcessQualityIndex", "negative", "sigmoid", 0),
			new CorrelationPlan("bend-07", "AxisFriction", "Axis01.Speed", "negative", "inverseLinear", 0),
			new CorrelationPlan("bend-08", "PumpEfficiency", "Hydraulic.PumpSpeed", "positive", "linear", 0),
			new CorrelationPlan("bend-09", "StructuralThermalLoad", "Thermal.FrameTemperature", "positive", "delayedLinear", 30),
			new CorrelationPlan("bend-10", "ValveResponse", "Bending.CycleTime", "negative", "inverseLinear", 0)
		},
		UncorrelatedPairs = new UncorrelatedPlan[5]
		{
			new UncorrelatedPlan("bend-u01", "Environment.Humidity", "Production.ProgramRevision"),
			new UncorrelatedPlan("bend-u02", "Diagnostic.Fieldbus.Node01.Latency", "Bending.ToolPosition"),
			new UncorrelatedPlan("bend-u03", "Environment.LightIntensity", "Axis01.VibrationRms"),
			new UncorrelatedPlan("bend-u04", "Production.SetupProgress", "Cooling.PrimaryCircuit.Pressure"),
			new UncorrelatedPlan("bend-u05", "Production.OrderQueueDepth", "Axis01.MotorTemperature")
		}
	};

	public static bool IsFullMode => string.Equals(Environment.GetEnvironmentVariable("PHYSICS_VERIFY_FULL"), "1", StringComparison.Ordinal);

	public static TimeSpan RunDuration => IsFullMode ? TimeSpan.FromMinutes(30.0) : TimeSpan.FromSeconds(90.0);

	public static string EvidenceDirectory => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "handoff", "ap-03-r1-physical-verification"));

	public static async Task<R1LongRunReport> RunSingleMachineAsync(int seed = 42, CancellationToken cancellationToken = default(CancellationToken))
	{
		return await RunAsync(1, seed, seed + 1, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task<R1LongRunReport> RunDualMachineAsync(int seed1 = 42, int seed2 = 99, CancellationToken cancellationToken = default(CancellationToken))
	{
		return await RunAsync(2, seed1, seed2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task<R1LongRunReport> RunAsync(int machineCount, int seed1, int seed2, CancellationToken cancellationToken = default(CancellationToken))
	{
		string previousShort = Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT");
		if (IsFullMode)
		{
			Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", null);
		}
		R1LongRunReport report = new R1LongRunReport
		{
			StartedAtUtc = DateTime.UtcNow,
			Duration = RunDuration,
			FullMode = IsFullMode,
			SeedMachine1 = seed1,
			SeedMachine2 = seed2,
			MachineCount = machineCount
		};
		TestLogService log = new TestLogService();
		PhysicalSignalPublishingCoordinator coordinator = PhysicalTestServiceFactory.CreateCoordinator(log);
		MachineServerService serverService = new MachineServerService(log, coordinator);
		PhysicalStatisticsRecorder stats = new PhysicalStatisticsRecorder();
		PhysicalCorrelationRecorder correlation = new PhysicalCorrelationRecorder();
		List<MemorySample> memorySamples = new List<MemorySample>();
		TimeSpan sampleInterval = (IsFullMode ? TimeSpan.FromMinutes(5.0) : TimeSpan.FromSeconds(15.0));
		DateTime nextSample = DateTime.UtcNow;
		List<MachineConfiguration> machines = new List<MachineConfiguration>
		{
			CreatePhysicsMachine(1, 14870, "laser-processing-machine-300", seed1),
			CreatePhysicsMachine(2, 14871, "bending-hydraulic-machine-300", seed2)
		}.Take(machineCount).ToList();
		try
		{
			foreach (MachineConfiguration machine in machines)
			{
				coordinator.PrepareMachine(machine, machine.Id.GetHashCode());
				await serverService.StartServerAsync(machine, new MachineRuntimeState
				{
					MachineId = machine.Id
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			memorySamples.Add(CaptureMemory(DateTime.UtcNow, coordinator));
			DateTime endAt = DateTime.UtcNow + RunDuration;
			while (DateTime.UtcNow < endAt)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await Task.Delay(1000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				foreach (PhysicalMachineSession session in coordinator.GetSessions())
				{
					RecordSamples(session, stats, correlation);
				}
				if (DateTime.UtcNow >= nextSample)
				{
					memorySamples.Add(CaptureMemory(DateTime.UtcNow, coordinator));
					nextSample = DateTime.UtcNow + sampleInterval;
				}
			}
			foreach (PhysicalMachineSession session2 in coordinator.GetSessions())
			{
				R1MachineReport machineReport = BuildMachineReport(session2);
				report.Machines.Add(machineReport);
				report.HardLimitViolations += session2.Simulation.Metrics.HardLimitPrevented;
				report.PlausibilityViolations += session2.Simulation.Metrics.PlausibilityViolations;
			}
			report.PhaseChanges = coordinator.GetSessions().Sum((PhysicalMachineSession s) => s.Simulation.Metrics.PhaseChanges);
			report.Statistics = stats.BuildSnapshots().ToList();
			report.Correlations = BuildCorrelationResults(correlation, machines.Count);
			report.UncorrelatedPairs = BuildUncorrelatedResults(correlation, machines.Count);
			report.MemorySamples = memorySamples;
			report.Exceptions = (from e in log.Entries
				where e.Category == LogCategory.Error
				select e.Message).ToList();
			report.Passed = report.HardLimitViolations == 0 && report.Exceptions.Count == 0;
		}
		finally
		{
			Environment.SetEnvironmentVariable("PHYSICS_VERIFY_SHORT", previousShort);
			await coordinator.StopAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await serverService.StopAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			report.EndedAtUtc = DateTime.UtcNow;
		}
		return report;
	}

	public static async Task ExportEvidenceAsync(R1LongRunReport single, R1LongRunReport? dual = null)
	{
		Directory.CreateDirectory(EvidenceDirectory);
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-03-R1-single-machine-longrun.json"), JsonSerializer.Serialize(single, options)).ConfigureAwait(continueOnCapturedContext: false);
		if (dual != null)
		{
			await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-03-R1-dual-machine-longrun.json"), JsonSerializer.Serialize(dual, options)).ConfigureAwait(continueOnCapturedContext: false);
		}
		await File.WriteAllTextAsync(contents: JsonSerializer.Serialize(new
		{
			generatedAtUtc = DateTime.UtcNow,
			machines = from m in single.Machines.Concat(dual?.Machines ?? new List<R1MachineReport>())
				select new
				{
					ProfileId = m.ProfileId,
					SignalCount = m.SignalCount,
					statistics = single.Statistics.Where((SignalStatisticsSnapshot s) => m.MonitoredSignals.Contains<string>(s.SignalId, StringComparer.OrdinalIgnoreCase)).Take(35)
				}
		}, options), path: Path.Combine(EvidenceDirectory, "AP-03-R1-normal-range-statistics.json")).ConfigureAwait(continueOnCapturedContext: false);
		await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-03-R1-correlation-verification.json"), JsonSerializer.Serialize(new
		{
			singleMachine = new
			{
				correlations = single.Correlations,
				uncorrelatedPairs = single.UncorrelatedPairs
			},
			dualMachine = ((dual == null) ? null : new
			{
				correlations = dual.Correlations,
				uncorrelatedPairs = dual.UncorrelatedPairs
			})
		}, options)).ConfigureAwait(continueOnCapturedContext: false);
		await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "AP-03-R1-performance-and-memory.json"), JsonSerializer.Serialize(new
		{
			singleMachineMemory = single.MemorySamples,
			dualMachineMemory = dual?.MemorySamples
		}, options)).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static void RecordSamples(PhysicalMachineSession session, PhysicalStatisticsRecorder stats, PhysicalCorrelationRecorder correlation)
	{
		DateTimeOffset utcNow = DateTimeOffset.UtcNow;
		MonitoredSignalPlan monitoredSignals = GetMonitoredSignals(session.Profile.ProfileId);
		string[] statisticsSignals = monitoredSignals.StatisticsSignals;
		foreach (string signalId in statisticsSignals)
		{
			SignalDefinition signalDefinition = session.Profile.Signals.FirstOrDefault((SignalDefinition s) => s.SignalId == signalId);
			SignalRuntimeState signalRuntimeState = session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId == signalId);
			bool flag = signalDefinition == null || signalRuntimeState == null;
			bool flag2 = flag;
			if (!flag2)
			{
				PhysicalSignalDataType dataType = signalDefinition.DataType;
				bool flag3 = (uint)dataType <= 1u;
				flag2 = !flag3;
			}
			if (!flag2)
			{
				stats.Record(signalId, signalRuntimeState.CurrentValue, signalDefinition, utcNow, null);
			}
		}
		CorrelationPlan[] correlationGroups = monitoredSignals.CorrelationGroups;
		foreach (CorrelationPlan group in correlationGroups)
		{
			HiddenProcessRuntimeState hiddenProcessRuntimeState = session.Runtime.HiddenProcessStates.FirstOrDefault((HiddenProcessRuntimeState s) => s.StateId == group.HiddenStateId);
			SignalRuntimeState signalRuntimeState2 = session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId == group.TargetSignalId);
			if (hiddenProcessRuntimeState != null && signalRuntimeState2 != null)
			{
				correlation.RecordPair(group.PairId, hiddenProcessRuntimeState.CurrentValue, signalRuntimeState2.CurrentValue);
			}
		}
		UncorrelatedPlan[] uncorrelatedPairs = monitoredSignals.UncorrelatedPairs;
		foreach (UncorrelatedPlan pair in uncorrelatedPairs)
		{
			SignalRuntimeState signalRuntimeState3 = session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId == pair.A);
			SignalRuntimeState signalRuntimeState4 = session.Runtime.Signals.FirstOrDefault((SignalRuntimeState s) => s.SignalId == pair.B);
			if (signalRuntimeState3 != null && signalRuntimeState4 != null)
			{
				correlation.RecordPair(pair.PairId, signalRuntimeState3.CurrentValue, signalRuntimeState4.CurrentValue);
			}
		}
	}

	private static R1MachineReport BuildMachineReport(PhysicalMachineSession session)
	{
		MonitoredSignalPlan monitoredSignals = GetMonitoredSignals(session.Profile.ProfileId);
		return new R1MachineReport
		{
			MachineId = session.MachineId,
			MachineName = session.MachineName,
			Endpoint = session.MachineName,
			ProfileId = session.Profile.ProfileId,
			ProfileVersion = session.Profile.ProfileVersion,
			SignalCount = session.Profile.Signals.Count,
			HiddenStateCount = session.Profile.HiddenProcessStates.Count,
			SignalDependencyCount = session.Profile.Dependencies.Count,
			HiddenStateDependencyCount = session.Profile.HiddenStateDependencies.Count,
			EngineTicks = session.Simulation.Metrics.TotalEngineTicks,
			OpcUaUpdates = ((session.Metrics.TotalPublishedUpdates > 0) ? session.Metrics.TotalPublishedUpdates : ((long)(session.Metrics.UpdatesPerSecond * RunDuration.TotalSeconds))),
			AverageCalculationDurationMs = session.Simulation.Metrics.AverageCalculationDurationMs,
			MaxCalculationDurationMs = session.Simulation.Metrics.MaxCalculationDurationMs,
			AveragePublishDurationMs = session.Metrics.AveragePublishDurationMs,
			MaxPublishDurationMs = session.Metrics.MaxPublishDurationMs,
			PlausibilityViolations = session.Simulation.Metrics.PlausibilityViolations,
			HardLimitViolations = session.Simulation.Metrics.HardLimitPrevented,
			CurrentPhase = session.Simulation.CurrentPhase.ToString(),
			MonitoredSignals = monitoredSignals.StatisticsSignals.ToList()
		};
	}

	private static List<CorrelationGroupResult> BuildCorrelationResults(PhysicalCorrelationRecorder correlation, int machineCount)
	{
		List<CorrelationGroupResult> list = new List<CorrelationGroupResult>();
		if (machineCount >= 1)
		{
			list.AddRange(GetMonitoredSignals("laser-processing-machine-300").CorrelationGroups.Select((CorrelationPlan g) => correlation.Analyze(g.PairId, "laser-processing-machine-300", g.HiddenStateId, g.TargetSignalId, g.Direction, g.DependencyType, g.ExpectedLagSeconds)));
		}
		if (machineCount >= 2)
		{
			list.AddRange(GetMonitoredSignals("bending-hydraulic-machine-300").CorrelationGroups.Select((CorrelationPlan g) => correlation.Analyze(g.PairId, "bending-hydraulic-machine-300", g.HiddenStateId, g.TargetSignalId, g.Direction, g.DependencyType, g.ExpectedLagSeconds)));
		}
		return list;
	}

	private static List<UncorrelatedPairResult> BuildUncorrelatedResults(PhysicalCorrelationRecorder correlation, int machineCount)
	{
		List<UncorrelatedPairResult> list = new List<UncorrelatedPairResult>();
		string[] array = ((machineCount < 2) ? new string[1] { "laser-processing-machine-300" } : new string[2] { "laser-processing-machine-300", "bending-hydraulic-machine-300" });
		foreach (string profileId in array)
		{
			UncorrelatedPlan[] uncorrelatedPairs = GetMonitoredSignals(profileId).UncorrelatedPairs;
			foreach (UncorrelatedPlan uncorrelatedPlan in uncorrelatedPairs)
			{
				CorrelationGroupResult correlationGroupResult = correlation.Analyze(uncorrelatedPlan.PairId, profileId, null, uncorrelatedPlan.B, "uncorrelated", "independent", 0);
				list.Add(new UncorrelatedPairResult
				{
					PairId = uncorrelatedPlan.PairId,
					SignalA = uncorrelatedPlan.A,
					SignalB = uncorrelatedPlan.B,
					Pearson = correlationGroupResult.Pearson,
					StrongestLag = correlationGroupResult.StrongestCrossCorrelationLag,
					StrongestCrossCorrelation = correlationGroupResult.StrongestCrossCorrelation,
					Assessment = ((Math.Abs(correlationGroupResult.Pearson) < 0.35) ? "pass" : "review")
				});
			}
		}
		return list;
	}

	private static MemorySample CaptureMemory(DateTime timestampUtc, PhysicalSignalPublishingCoordinator coordinator)
	{
		Process currentProcess = Process.GetCurrentProcess();
		return new MemorySample
		{
			TimestampUtc = timestampUtc,
			WorkingSetMb = (double)currentProcess.WorkingSet64 / 1048576.0,
			PrivateMemoryMb = (double)currentProcess.PrivateMemorySize64 / 1048576.0,
			GcHeapMb = (double)GC.GetTotalMemory(forceFullCollection: false) / 1048576.0,
			Gen0Collections = GC.CollectionCount(0),
			Gen1Collections = GC.CollectionCount(1),
			Gen2Collections = GC.CollectionCount(2),
			ActiveEngines = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Simulation.IsEngineActive),
			ActivePublishers = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Running),
			RegisteredNodes = coordinator.GetSessions().Sum((PhysicalMachineSession s) => s.OpcUaNodeCount)
		};
	}

	private static MachineConfiguration CreatePhysicsMachine(int index, int port, string profileId, int seed)
	{
		MachineConfiguration machineConfiguration = DefaultMachines.Create()[index - 1];
		machineConfiguration.PhysicalProfileId = profileId;
		machineConfiguration.Port = port;
		machineConfiguration.UpdateEndpointFromHostPort();
		return machineConfiguration;
	}

	private static MonitoredSignalPlan GetMonitoredSignals(string profileId)
	{
		return (profileId == "bending-hydraulic-machine-300") ? BendingPlan : LaserPlan;
	}
}
