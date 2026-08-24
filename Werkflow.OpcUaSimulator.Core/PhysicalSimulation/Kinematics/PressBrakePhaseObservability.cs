using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public sealed record PressBrakePhaseSnapshot(
	string PhaseDisplayName,
	double ElapsedSeconds,
	double TotalDurationSeconds,
	double RemainingSeconds,
	PressBrakeContinuationKind ContinuationKind,
	string ContinuationIndicator,
	string NextStepPreview,
	bool OperatorInteractionRequired);

public static class PressBrakePhaseObservability
{
	public static string ToGermanDisplayName(PressBrakeMotionPhase phase) =>
		phase switch
		{
			PressBrakeMotionPhase.Idle => "Bereit",
			PressBrakeMotionPhase.Setup => "Rüsten",
			PressBrakeMotionPhase.OperatorWait => "Automatische Bedienerpause",
			PressBrakeMotionPhase.ToolChange => "Werkzeugwechsel",
			PressBrakeMotionPhase.ProgramTransition => "Programmwechsel",
			PressBrakeMotionPhase.BackgaugeMove => "Rückanschlag",
			PressBrakeMotionPhase.RamApproach => "Anfahren",
			PressBrakeMotionPhase.Forming => "Umformen",
			PressBrakeMotionPhase.Hold => "Halten",
			PressBrakeMotionPhase.RamReturn => "Rückhub",
			PressBrakeMotionPhase.InterStepWait => "Zwischenschritt-Wartezeit",
			PressBrakeMotionPhase.InterPartWait => "Inter-Part-Wartezeit",
			PressBrakeMotionPhase.InterruptRecovery => "Unterbrechungswiederherstellung",
			_ => phase.ToString()
		};

	public static PressBrakePhaseSnapshot BuildSnapshot(
		PressBrakeKinematicsState pressBrake,
		int seed,
		string? nextProgramId = null,
		string? nextPartId = null)
	{
		PressBrakePartDefinition? part = GetCurrentPart(pressBrake);
		PressBrakeBendStepDefinition? step = GetCurrentStep(pressBrake, part);
		double total = pressBrake.PhaseTotalDurationSeconds > 0.0
			? pressBrake.PhaseTotalDurationSeconds
			: PressBrakeKinematicsEngine.ResolvePhaseDurationSeconds(pressBrake, part, step, seed);
		double elapsed = Math.Max(0.0, pressBrake.PhaseElapsedSeconds);
		double remaining = Math.Max(0.0, total - elapsed);
		PressBrakeContinuationKind continuation = ResolveContinuationKind(pressBrake);
		string nextStep = BuildNextStepPreview(pressBrake, nextProgramId, nextPartId);
		string indicator = BuildContinuationIndicator(continuation, pressBrake.MotionPhase);
		return new PressBrakePhaseSnapshot(
			ToGermanDisplayName(pressBrake.MotionPhase),
			elapsed,
			total,
			remaining,
			continuation,
			indicator,
			nextStep,
			continuation == PressBrakeContinuationKind.OperatorActionRequired);
	}

	public static double EstimateJobChangePauseSeconds(
		Guid machineId,
		FixedProductionJobDefinition nextJob,
		int seed)
	{
		if (!VirtualPressBrakeMachineRegistry.IsVirtualPressBrakeMachine(machineId))
		{
			return FixedSimulationCatalog.MinJobChangePauseSeconds;
		}

		int programIndex = Math.Max(0, nextJob.CatalogIndex) % PressBrakeProgramCatalog.ProgramCount;
		PressBrakeProgramDefinition program = PressBrakeProgramCatalog.GetProgram(programIndex);
		bool toolChangeRequired = nextJob.CatalogIndex % 3 == 0;
		double transition = toolChangeRequired
			? program.ToolChangeDurationSeconds + program.SetupDurationSeconds
			: program.ProgramTransitionSeconds + program.SetupDurationSeconds;
		return Math.Max(transition, 1.0);
	}

	public static string FormatProgress(double elapsedSeconds, double totalSeconds)
	{
		return $"{FormatDuration(elapsedSeconds)} / {FormatDuration(totalSeconds)}";
	}

	public static string FormatRemaining(double remainingSeconds) =>
		remainingSeconds > 0.0 ? $"Noch {FormatDuration(remainingSeconds)}" : "—";

	private static PressBrakeContinuationKind ResolveContinuationKind(PressBrakeKinematicsState pressBrake) =>
		pressBrake.MotionPhase switch
		{
			PressBrakeMotionPhase.OperatorWait when pressBrake.OperatorInteractionRequired =>
				PressBrakeContinuationKind.OperatorActionRequired,
			PressBrakeMotionPhase.Setup
				or PressBrakeMotionPhase.ToolChange
				or PressBrakeMotionPhase.ProgramTransition
				or PressBrakeMotionPhase.InterStepWait
				or PressBrakeMotionPhase.InterPartWait
				or PressBrakeMotionPhase.OperatorWait
				or PressBrakeMotionPhase.InterruptRecovery =>
				PressBrakeContinuationKind.AutoWait,
			_ => PressBrakeContinuationKind.ActiveProduction
		};

	private static string BuildContinuationIndicator(PressBrakeContinuationKind kind, PressBrakeMotionPhase phase) =>
		kind switch
		{
			PressBrakeContinuationKind.OperatorActionRequired => "Bedienereingriff erforderlich",
			PressBrakeContinuationKind.AutoWait => phase is PressBrakeMotionPhase.ProgramTransition
				? "Automatischer Programmwechsel – kein Eingriff erforderlich"
				: "Automatischer Fortlauf – kein Eingriff erforderlich",
			_ => "Produktion aktiv"
		};

	private static string BuildNextStepPreview(
		PressBrakeKinematicsState pressBrake,
		string? nextProgramId,
		string? nextPartId)
	{
		if (!string.IsNullOrWhiteSpace(nextProgramId))
		{
			string part = string.IsNullOrWhiteSpace(nextPartId) ? "—" : nextPartId;
			return $"Nächstes Programm: {nextProgramId} / {part}";
		}

		if (pressBrake.MotionPhase is PressBrakeMotionPhase.ProgramTransition or PressBrakeMotionPhase.ToolChange)
		{
			string program = string.IsNullOrWhiteSpace(pressBrake.NextProgramIdPreview)
				? pressBrake.ProgramId
				: pressBrake.NextProgramIdPreview;
			string part = string.IsNullOrWhiteSpace(pressBrake.NextPartIdPreview)
				? pressBrake.PartId
				: pressBrake.NextPartIdPreview;
			return $"Nächstes Programm: {program} / {part}";
		}

		return $"Aktuell: {pressBrake.ProgramId} / {pressBrake.PartId}";
	}

	private static string FormatDuration(double seconds)
	{
		int total = (int)Math.Floor(Math.Max(0.0, seconds));
		int minutes = total / 60;
		int secs = total % 60;
		return $"{minutes:D2}:{secs:D2}";
	}

	private static PressBrakePartDefinition? GetCurrentPart(PressBrakeKinematicsState pressBrake) =>
		pressBrake.ActiveProgram != null && pressBrake.ActiveProgram.Parts.Count > 0
			? pressBrake.ActiveProgram.Parts[pressBrake.PartIndex % pressBrake.ActiveProgram.Parts.Count]
			: null;

	private static PressBrakeBendStepDefinition? GetCurrentStep(
		PressBrakeKinematicsState pressBrake,
		PressBrakePartDefinition? part) =>
		part != null && pressBrake.BendStepIndex < part.BendSteps.Count
			? part.BendSteps[pressBrake.BendStepIndex]
			: null;
}
