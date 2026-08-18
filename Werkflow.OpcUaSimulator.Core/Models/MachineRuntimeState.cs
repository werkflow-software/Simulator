using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class MachineRuntimeState
{
	public Guid MachineId { get; set; }

	public bool IsServerOnline { get; set; }

	public int ConnectedClients { get; set; }

	public ushort NamespaceIndex { get; set; }

	public MachineState State { get; set; } = MachineState.Offline;

	public string PartName { get; set; } = "—";

	public string JobName { get; set; } = "—";

	public int ActualCounter { get; set; }

	public int TargetCounter { get; set; }

	public bool ErrorActive { get; set; }

	public string ErrorMessage { get; set; } = string.Empty;

	public DateTime? DisruptedStateStartedAt { get; set; }

	public ulong Heartbeat { get; set; }

	public DateTime LastProductionChange { get; set; } = DateTime.UtcNow;

	public bool IsProducing { get; set; }

	public bool IsCounterFrozen { get; set; }

	public bool IsDisconnected { get; set; }

	public Guid? AssignedJobId { get; set; }

	public int CurrentJobCatalogIndex { get; set; } = -1;

	public bool IsJobChangeActive { get; set; }

	public DateTime? JobChangeEndsAtUtc { get; set; }

	public int JobChangePauseSeconds { get; set; }

	public string NextJobNamePreview { get; set; } = "—";

	public string NextPartNamePreview { get; set; } = "—";

	public int NextTargetQuantityPreview { get; set; }

	public int PendingNextJobCatalogIndex { get; set; } = -1;

	public DateTime? SimulationStartedAt { get; set; }

	public Dictionary<NodeSemanticType, object?> LiveNodeValues { get; set; } = new Dictionary<NodeSemanticType, object>();

	public double ProgressPercent => (TargetCounter <= 0) ? 0.0 : Math.Min(100.0, (double)ActualCounter * 100.0 / (double)TargetCounter);

	public MachineRuntimeState CloneValues()
	{
		return new MachineRuntimeState
		{
			MachineId = MachineId,
			IsServerOnline = IsServerOnline,
			ConnectedClients = ConnectedClients,
			NamespaceIndex = NamespaceIndex,
			State = State,
			PartName = PartName,
			JobName = JobName,
			ActualCounter = ActualCounter,
			TargetCounter = TargetCounter,
			ErrorActive = ErrorActive,
			ErrorMessage = ErrorMessage,
			DisruptedStateStartedAt = DisruptedStateStartedAt,
			Heartbeat = Heartbeat,
			LastProductionChange = LastProductionChange,
			IsProducing = IsProducing,
			IsCounterFrozen = IsCounterFrozen,
			IsDisconnected = IsDisconnected,
			AssignedJobId = AssignedJobId,
			CurrentJobCatalogIndex = CurrentJobCatalogIndex,
			IsJobChangeActive = IsJobChangeActive,
			JobChangeEndsAtUtc = JobChangeEndsAtUtc,
			JobChangePauseSeconds = JobChangePauseSeconds,
			NextJobNamePreview = NextJobNamePreview,
			NextPartNamePreview = NextPartNamePreview,
			NextTargetQuantityPreview = NextTargetQuantityPreview,
			PendingNextJobCatalogIndex = PendingNextJobCatalogIndex,
			SimulationStartedAt = SimulationStartedAt,
			LiveNodeValues = new Dictionary<NodeSemanticType, object>(LiveNodeValues)
		};
	}
}
