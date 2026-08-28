using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Kinematics;

public enum AutonomousCellAutomaticWaitKind
{
	None,
	RawMaterialReplenishment,
	ContainerExchange
}

public sealed record AutonomousCellHmiSnapshot(
	string ScenarioJobDisplayName,
	string ProductVariant,
	int CompletedParts,
	int TargetParts,
	int CurrentPartOrdinal,
	string PhaseDisplayName,
	string ActiveStation,
	double PartElapsedSeconds,
	double PartRemainingSeconds,
	double JobRemainingSeconds,
	int PalletRemaining,
	int PalletCapacity,
	int ContainerFillParts,
	int ContainerCapacity,
	AutonomousCellAutomaticWaitKind AutomaticWaitKind,
	string AutomaticWaitMessage,
	bool IsCompleted,
	bool OperatorActionRequired);

public static class AutonomousCellPhaseObservability
{
	private static readonly AutonomousCellMotionPhase[] StandardPartCycle =
	[
		AutonomousCellMotionPhase.LoadPick,
		AutonomousCellMotionPhase.LoadTransfer,
		AutonomousCellMotionPhase.FixtureClamp,
		AutonomousCellMotionPhase.ProcessApproach,
		AutonomousCellMotionPhase.ProcessPressFit,
		AutonomousCellMotionPhase.ProcessRetract,
		AutonomousCellMotionPhase.FixtureRelease,
		AutonomousCellMotionPhase.TransferPickup,
		AutonomousCellMotionPhase.TransferToVision,
		AutonomousCellMotionPhase.VisionInspect,
		AutonomousCellMotionPhase.SortOutput,
		AutonomousCellMotionPhase.ContainerFill
	];

	public static AutonomousCellHmiSnapshot BuildSnapshot(AutonomousCellKinematicsState cell, int seed)
	{
		int targetParts = cell.TargetParts > 0 ? cell.TargetParts : VirtualAutonomousCellRunProfile.TotalParts;
		int completed = cell.CompletedParts;
		int currentPartOrdinal = Math.Min(targetParts, completed + (cell.MotionPhase == AutonomousCellMotionPhase.Complete ? 0 : 1));
		double partElapsed = cell.PhaseElapsedSeconds;
		double partRemaining = AutonomousCellProductionTimeEstimator.EstimateCurrentPartRemainingSeconds(cell, seed);
		double jobRemaining = AutonomousCellProductionTimeEstimator.EstimateJobRemainingSeconds(cell, seed);
		AutonomousCellAutomaticWaitKind waitKind = ResolveAutomaticWaitKind(cell.MotionPhase);
		bool completedRun = cell.MotionPhase == AutonomousCellMotionPhase.Complete || completed >= targetParts;

		return new AutonomousCellHmiSnapshot(
			ScenarioJobDisplayName: VirtualAutonomousCellRunProfile.BaselineScenarioDisplayName,
			ProductVariant: cell.CurrentVariant.ToString(),
			CompletedParts: completed,
			TargetParts: targetParts,
			CurrentPartOrdinal: currentPartOrdinal,
			PhaseDisplayName: ToGermanPhaseName(cell.MotionPhase),
			ActiveStation: ResolveActiveStation(cell.MotionPhase),
			PartElapsedSeconds: partElapsed,
			PartRemainingSeconds: completedRun ? 0.0 : partRemaining,
			JobRemainingSeconds: completedRun ? 0.0 : jobRemaining,
			PalletRemaining: cell.PalletQuantityRemaining,
			PalletCapacity: VirtualAutonomousCellRunProfile.PalletCapacity,
			ContainerFillParts: cell.ContainerParts,
			ContainerCapacity: VirtualAutonomousCellRunProfile.ContainerCapacity,
			AutomaticWaitKind: waitKind,
			AutomaticWaitMessage: ResolveAutomaticWaitMessage(waitKind),
			IsCompleted: completedRun,
			OperatorActionRequired: false);
	}

	public static string ToGermanPhaseName(AutonomousCellMotionPhase phase) =>
		phase switch
		{
			AutonomousCellMotionPhase.Idle => "Bereit",
			AutonomousCellMotionPhase.WaitRawMaterial => "Warte auf Rohmaterial",
			AutonomousCellMotionPhase.HiddenInboundDelivery => "Materialnachschub",
			AutonomousCellMotionPhase.LoadPick => "Belader greift zu",
			AutonomousCellMotionPhase.LoadTransfer => "Belader übergibt",
			AutonomousCellMotionPhase.FixtureClamp => "Spannvorrichtung schließt",
			AutonomousCellMotionPhase.ProcessApproach => "Prozess Anfahren",
			AutonomousCellMotionPhase.ProcessPressFit => "Press-Fit",
			AutonomousCellMotionPhase.ProcessRetract => "Prozess Rückzug",
			AutonomousCellMotionPhase.FixtureRelease => "Spannvorrichtung öffnet",
			AutonomousCellMotionPhase.TransferPickup => "Transfer greift zu",
			AutonomousCellMotionPhase.TransferToVision => "Transfer zu Vision",
			AutonomousCellMotionPhase.VisionInspect => "Vision prüft",
			AutonomousCellMotionPhase.SortOutput => "Sortierung",
			AutonomousCellMotionPhase.ContainerFill => "Behälter füllen",
			AutonomousCellMotionPhase.WaitReplenishment => "Automatischer Materialnachschub",
			AutonomousCellMotionPhase.WaitContainerExchange => "Automatischer Behälterwechsel",
			AutonomousCellMotionPhase.HiddenOutboundExchange => "Behälterwechsel",
			AutonomousCellMotionPhase.Complete => "Abgeschlossen",
			_ => phase.ToString()
		};

	public static string ResolveActiveStation(AutonomousCellMotionPhase phase) =>
		phase switch
		{
			AutonomousCellMotionPhase.WaitRawMaterial or AutonomousCellMotionPhase.HiddenInboundDelivery
				or AutonomousCellMotionPhase.WaitReplenishment => "Inbound",
			AutonomousCellMotionPhase.LoadPick or AutonomousCellMotionPhase.LoadTransfer => "Load Robot",
			AutonomousCellMotionPhase.FixtureClamp or AutonomousCellMotionPhase.FixtureRelease => "Fixture",
			AutonomousCellMotionPhase.ProcessApproach or AutonomousCellMotionPhase.ProcessPressFit
				or AutonomousCellMotionPhase.ProcessRetract => "Press-Fit",
			AutonomousCellMotionPhase.TransferPickup or AutonomousCellMotionPhase.TransferToVision => "Transfer Robot",
			AutonomousCellMotionPhase.VisionInspect => "Vision",
			AutonomousCellMotionPhase.SortOutput => "Sorting",
			AutonomousCellMotionPhase.ContainerFill or AutonomousCellMotionPhase.WaitContainerExchange
				or AutonomousCellMotionPhase.HiddenOutboundExchange => "Container",
			AutonomousCellMotionPhase.Complete => "Cell",
			_ => "Cell"
		};

	public static AutonomousCellAutomaticWaitKind ResolveAutomaticWaitKind(AutonomousCellMotionPhase phase) =>
		phase switch
		{
			AutonomousCellMotionPhase.WaitReplenishment or AutonomousCellMotionPhase.HiddenInboundDelivery
				or AutonomousCellMotionPhase.WaitRawMaterial => AutonomousCellAutomaticWaitKind.RawMaterialReplenishment,
			AutonomousCellMotionPhase.WaitContainerExchange or AutonomousCellMotionPhase.HiddenOutboundExchange
				=> AutonomousCellAutomaticWaitKind.ContainerExchange,
			_ => AutonomousCellAutomaticWaitKind.None
		};

	public static string ResolveAutomaticWaitMessage(AutonomousCellAutomaticWaitKind waitKind) =>
		waitKind switch
		{
			AutonomousCellAutomaticWaitKind.RawMaterialReplenishment =>
				"Materialnachschub läuft automatisch – keine Bedienaktion erforderlich",
			AutonomousCellAutomaticWaitKind.ContainerExchange =>
				"Behälterwechsel läuft automatisch – keine Bedienaktion erforderlich",
			_ => string.Empty
		};

	public static IReadOnlyList<AutonomousCellMotionPhase> GetStandardPartCycle() => StandardPartCycle;

	public static string FormatPartElapsed(double seconds) =>
		seconds > 0.0 ? FormatDuration(seconds) : "00:00";

	public static string FormatEstimatedRemaining(double seconds) =>
		seconds <= 0.0 ? "00:00" : $"~{FormatDuration(seconds)}";

	private static string FormatDuration(double seconds)
	{
		int total = (int)Math.Ceiling(seconds);
		int hours = total / 3600;
		int minutes = (total % 3600) / 60;
		int secs = total % 60;
		return hours > 0 ? $"{hours:D2}:{minutes:D2}:{secs:D2}" : $"{minutes:D2}:{secs:D2}";
	}
}
