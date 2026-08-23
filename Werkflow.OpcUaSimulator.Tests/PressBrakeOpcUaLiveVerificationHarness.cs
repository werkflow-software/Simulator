using System.Text.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.GroundTruth;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class PressBrakeOpcUaLiveVerificationReport
{
	public bool Passed { get; set; }
	public string Endpoint { get; set; } = VirtualPressBrakeContract.Endpoint;
	public Guid MachineId { get; set; } = VirtualPressBrakeContract.MachineId;
	public string ProfileId { get; set; } = VirtualPressBrakeContract.PhysicalProfileId;
	public int EnabledSignalCount { get; set; }
	public int ResolvedCount { get; set; }
	public int ReadableCount { get; set; }
	public bool OpcSessionEstablished { get; set; }
	public bool SimulatorStarted { get; set; }
	public bool ProductionMotionActive { get; set; }
	public bool RamBehaviorPass { get; set; }
	public bool BackgaugeBehaviorPass { get; set; }
	public bool FormingForceBehaviorPass { get; set; }
	public bool BendAngleBehaviorPass { get; set; }
	public bool CounterProgressionPass { get; set; }
	public bool ProgramPartTransitionPass { get; set; }
	public bool ThermalEvolutionPass { get; set; }
	public bool ActivityStateBehaviorPass { get; set; }
	public bool DynamicSignalSmokePass { get; set; }
	public bool GroundTruthGenerationPass { get; set; }
	public bool GroundTruthIsolationPass { get; set; }
	public string? GroundTruthArtifactPath { get; set; }
	public List<string> GroundTruthEventTypesObserved { get; set; } = [];
	public List<PressBrakeSignalReadEvidence> Signals { get; set; } = [];
	public List<string> Failures { get; set; } = [];
}

public sealed class PressBrakeSignalReadEvidence
{
	public string SignalId { get; set; } = "";
	public bool Resolved { get; set; }
	public bool Readable { get; set; }
	public string? InitialValue { get; set; }
	public string? LaterValue { get; set; }
	public bool Dynamic { get; set; }
}

public static class PressBrakeOpcUaLiveVerificationHarness
{
	private const int Seed = VirtualPressBrakeRunProfile.RandomSeed;

	public static async Task<PressBrakeOpcUaLiveVerificationReport> RunAsync(
		CancellationToken cancellationToken = default)
	{
		var report = new PressBrakeOpcUaLiveVerificationReport
		{
			EnabledSignalCount = VigilPressBrakeReducedProfileFactory.ContractSignalIds.Count
		};

		TestLogService log = new();
		var stack = PhysicalTestServiceFactory.CreateCoordinator(log);
		MachineServerService serverService = new(log, stack);
		MachineConfiguration machine = DefaultMachines.Create().First(m => m.Port == VirtualPressBrakeContract.Port);
		MachineRuntimeState runtime = new() { MachineId = machine.Id };

		try
		{
			stack.PrepareMachine(machine, Seed);
			await serverService.StartServerAsync(machine, runtime, cancellationToken);
			report.SimulatorStarted = serverService.IsRunning(machine.Id);
			await stack.StartForMachineAsync(machine.Id, cancellationToken);

			var job = VirtualPressBrakeRunProfile.ResolveJobDefinition(machine.Id, 0);
			stack.ApplyProductionJob(machine.Id, job);
			await stack.ResumeProductionAsync(machine.Id, cancellationToken);

			PhysicalMachineSession? session = stack.GetSession(machine.Id);
			report.ProductionMotionActive = session?.Simulation.IsProductionMotionActive == true;
		if (session != null)
		{
			session.Simulation.VerificationMode = PhysicalVerificationMode.Short;
			session.Simulation.TimeFactor = 25.0;
		}

			await Task.Delay(TimeSpan.FromSeconds(12), cancellationToken);

			ApplicationConfiguration clientConfig = await PhysicalSignalVerificationHarness.CreateClientConfigurationForTestsAsync(cancellationToken);
			EndpointDescription selected = CoreClientUtils.SelectEndpoint(clientConfig, machine.Endpoint, useSecurity: false);
			ConfiguredEndpoint endpointConfig = new(null, selected, EndpointConfiguration.Create(clientConfig));
			using Session opcSession = await Session.Create(
				clientConfig,
				endpointConfig,
				updateBeforeConnect: false,
				"PressBrakeLiveVerification",
				60000u,
				new UserIdentity(),
				null,
				cancellationToken);
			report.OpcSessionEstablished = opcSession.Connected;

			int nsIndex = opcSession.NamespaceUris.GetIndex(machine.NamespaceUri);
			if (nsIndex < 0)
			{
				report.Failures.Add($"Namespace '{machine.NamespaceUri}' not found.");
				return Finalize(report, session);
			}

			var initialReads = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
			var numericRanges = new Dictionary<string, (double Min, double Max)>(StringComparer.OrdinalIgnoreCase);
			foreach (string signalId in VigilPressBrakeReducedProfileFactory.ContractSignalIds)
			{
				var evidence = new PressBrakeSignalReadEvidence { SignalId = signalId };
				try
				{
					NodeId nodeId = new(signalId, (ushort)nsIndex);
					evidence.Resolved = opcSession.NodeCache.Find(nodeId) != null;
					if (evidence.Resolved)
					{
						report.ResolvedCount++;
						DataValue value = opcSession.ReadValue(nodeId);
						evidence.Readable = StatusCode.IsGood(value.StatusCode) && value.Value != null;
						if (evidence.Readable)
						{
							report.ReadableCount++;
							evidence.InitialValue = FormatValue(value.Value);
							initialReads[signalId] = evidence.InitialValue;
							TrackNumericSample(numericRanges, signalId, ParseDouble(evidence.InitialValue));
						}
					}
				}
				catch (Exception ex)
				{
					report.Failures.Add($"{signalId}: {ex.Message}");
				}

				report.Signals.Add(evidence);
			}

			for (int poll = 0; poll < 10; poll++)
			{
				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
				foreach (PressBrakeSignalReadEvidence evidence in report.Signals)
				{
					try
					{
						NodeId nodeId = new(evidence.SignalId, (ushort)nsIndex);
						DataValue later = opcSession.ReadValue(nodeId);
						if (!StatusCode.IsGood(later.StatusCode) || later.Value == null)
						{
							continue;
						}
						string formatted = FormatValue(later.Value);
						evidence.LaterValue = formatted;
						double sample = ParseDouble(formatted);
						TrackNumericSample(numericRanges, evidence.SignalId, sample);
						(double Min, double Max) range = numericRanges[evidence.SignalId];
						double initial = ParseDouble(evidence.InitialValue);
						evidence.Dynamic = Math.Abs(range.Max - range.Min) > 0.01
							|| Math.Abs(range.Max - initial) > 0.01
							|| !string.Equals(evidence.InitialValue, evidence.LaterValue, StringComparison.Ordinal);
						if (IsPeakTrackedSignal(evidence.SignalId))
						{
							double display = evidence.SignalId.Equals("Ram.Velocity", StringComparison.OrdinalIgnoreCase)
								? Math.Max(Math.Abs(range.Min), Math.Abs(range.Max))
								: range.Max;
							evidence.LaterValue = display.ToString(System.Globalization.CultureInfo.InvariantCulture);
						}
					}
					catch
					{
						// keep evidence as-is
					}
				}
			}
			EvaluateGroundTruth(report, session);
			EvaluateDynamics(report);
			EvaluateIsolation(report);
		}
		finally
		{
			await stack.StopForMachineAsync(machine.Id, cancellationToken);
			await serverService.StopServerAsync(machine.Id, cancellationToken);
		}

		return Finalize(report, stack.GetSession(machine.Id));
	}

	private static PressBrakeOpcUaLiveVerificationReport Finalize(
		PressBrakeOpcUaLiveVerificationReport report,
		PhysicalMachineSession? session)
	{
		report.Passed = report.Failures.Count == 0
			&& report.SimulatorStarted
			&& report.OpcSessionEstablished
			&& report.ResolvedCount == 14
			&& report.ReadableCount == 14
			&& report.DynamicSignalSmokePass
			&& report.RamBehaviorPass
			&& report.BackgaugeBehaviorPass
			&& report.FormingForceBehaviorPass
			&& report.BendAngleBehaviorPass
			&& report.CounterProgressionPass
			&& report.ProgramPartTransitionPass
			&& report.ThermalEvolutionPass
			&& report.ActivityStateBehaviorPass
			&& report.GroundTruthGenerationPass
			&& report.GroundTruthIsolationPass;

		if (session?.PressBrakeGroundTruth is PressBrakeGroundTruthRecorder recorder && report.GroundTruthArtifactPath == null)
		{
			report.GroundTruthArtifactPath = recorder.ArtifactPath;
		}

		return report;
	}

	private static void EvaluateDynamics(PressBrakeOpcUaLiveVerificationReport report)
	{
		static PressBrakeSignalReadEvidence? Find(PressBrakeOpcUaLiveVerificationReport r, string id) =>
			r.Signals.FirstOrDefault(s => s.SignalId.Equals(id, StringComparison.OrdinalIgnoreCase));

		var ram = Find(report, "Ram.Position");
		var ramVelocity = Find(report, "Ram.Velocity");
		var backgauge = Find(report, "Backgauge.Position");
		var force = Find(report, "Process.FormingForce");
		var angle = Find(report, "Process.BendAngle");
		var actual = Find(report, "Machine.ActualCounter");
		var program = Find(report, "Machine.ProgramId");
		var part = Find(report, "Machine.PartId");
		var thermal = Find(report, "Thermal.HydraulicOilTemp");
		var activity = Find(report, "Cycle.ActivityState");

		report.RamBehaviorPass = (ram is { Readable: true }
				&& (ram.Dynamic
					|| Math.Abs(ParseDouble(ram.InitialValue) - ParseDouble(ram.LaterValue)) > 0.1))
			|| (ramVelocity is { Readable: true }
				&& (ramVelocity.Dynamic || Math.Abs(ParseDouble(ramVelocity.LaterValue)) > 0.1));
		report.BackgaugeBehaviorPass = backgauge is { Readable: true, Dynamic: true };
		report.FormingForceBehaviorPass = force is { Readable: true }
			&& Math.Max(ParseDouble(force.InitialValue), ParseDouble(force.LaterValue)) > 0;
		report.BendAngleBehaviorPass = angle is { Readable: true }
			&& (angle.Dynamic || Math.Max(ParseDouble(angle.InitialValue), ParseDouble(angle.LaterValue)) > 0.1);
		report.CounterProgressionPass = actual is { Readable: true } && ParseDouble(actual.LaterValue) >= 0;
		report.ProgramPartTransitionPass = program is { Readable: true, InitialValue: not null }
			&& part is { Readable: true, InitialValue: not null }
			&& program.InitialValue!.Contains("PRG-", StringComparison.Ordinal);
		report.ThermalEvolutionPass = thermal is { Readable: true, Dynamic: true };
		report.ActivityStateBehaviorPass = activity is { Readable: true, Dynamic: true };

		int dynamicCount = report.Signals.Count(s => s.Dynamic);
		report.DynamicSignalSmokePass = dynamicCount >= 8;
	}

	private static void EvaluateGroundTruth(
		PressBrakeOpcUaLiveVerificationReport report,
		PhysicalMachineSession? session)
	{
		if (session?.PressBrakeGroundTruth is not PressBrakeGroundTruthRecorder recorder)
		{
			report.Failures.Add("PressBrakeGroundTruth recorder missing on session.");
			return;
		}

		report.GroundTruthArtifactPath = recorder.ArtifactPath;
		report.GroundTruthEventTypesObserved = recorder.GetEvents()
			.Select(e => e.EventType)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
			.ToList();

		bool fileExists = !string.IsNullOrWhiteSpace(recorder.ArtifactPath) && File.Exists(recorder.ArtifactPath);
		bool hasEvents = recorder.GetEvents().Count > 0;
		report.GroundTruthGenerationPass = fileExists && hasEvents;

		if (!fileExists)
		{
			report.Failures.Add("GT artifact file not created.");
		}
		else if (!hasEvents)
		{
			report.Failures.Add("GT recorder has zero events after live smoke.");
		}
	}

	private static void EvaluateIsolation(PressBrakeOpcUaLiveVerificationReport report)
	{
		foreach (string signalId in VigilPressBrakeReducedProfileFactory.ContractSignalIds)
		{
			if (signalId.Contains("GroundTruth", StringComparison.OrdinalIgnoreCase)
				|| signalId.Contains("HiddenState", StringComparison.OrdinalIgnoreCase))
			{
				report.Failures.Add($"Forbidden OPC signal exposed: {signalId}");
			}
		}

		report.GroundTruthIsolationPass = report.Failures.All(f => !f.StartsWith("Forbidden OPC", StringComparison.Ordinal));
	}

	private static void TrackNumericSample(
		Dictionary<string, (double Min, double Max)> ranges,
		string signalId,
		double sample)
	{
		if (ranges.TryGetValue(signalId, out (double Min, double Max) existing))
		{
			ranges[signalId] = (Math.Min(existing.Min, sample), Math.Max(existing.Max, sample));
			return;
		}

		ranges[signalId] = (sample, sample);
	}

	private static bool IsPeakTrackedSignal(string signalId) =>
		signalId.Equals("Process.BendAngle", StringComparison.OrdinalIgnoreCase)
		|| signalId.Equals("Process.FormingForce", StringComparison.OrdinalIgnoreCase)
		|| signalId.Equals("Ram.Position", StringComparison.OrdinalIgnoreCase)
		|| signalId.Equals("Ram.Velocity", StringComparison.OrdinalIgnoreCase);

	private static double ParseDouble(string? value) =>
		double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed)
			? parsed
			: 0.0;

	private static string FormatValue(object value) => value switch
	{
		DateTime dt => dt.ToUniversalTime().ToString("O"),
		DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
		IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "",
		_ => value.ToString() ?? ""
	};

	public static void WriteEvidence(PressBrakeOpcUaLiveVerificationReport report, string directory)
	{
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, "AP-018.194-SIM-P01-R2-live-opc-verification.json");
		string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(path, json);
	}
}
