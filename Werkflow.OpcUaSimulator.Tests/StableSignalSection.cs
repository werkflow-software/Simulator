using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class StableSignalSection
{
	public string SignalId { get; set; } = "";

	public string NodeId { get; set; } = "";

	public string DataType { get; set; } = "";

	public string TechnicalBehavior { get; set; } = "";

	public string UpdateInterval { get; set; } = "";

	public string NormalRange { get; set; } = "";

	public string HardLimits { get; set; } = "";

	public string Result { get; set; } = "Failed";

	public List<NotificationRecord> Notifications { get; set; } = new List<NotificationRecord>();

	public int ObservationSeconds { get; set; }

	public int InitialNotifications { get; set; }

	public int AdditionalValueNotifications { get; set; }
}
