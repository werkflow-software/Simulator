namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Models;

public sealed class HmiSignalDisplayItem
{
	public required string SignalId { get; init; }

	public required string DisplayName { get; init; }

	public string FormattedValue { get; set; } = "—";

	public string Unit { get; init; } = "";

	public string GroupKey { get; init; } = "";

	public int DisplayOrder { get; init; }
}
