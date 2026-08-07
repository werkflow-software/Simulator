namespace Werkflow.OpcUaSimulator.App.ViewModels;

public sealed class PhysicalSignalRowViewModel
{
	public required string SignalId { get; init; }

	public required string NodeId { get; init; }

	public required string DisplayName { get; init; }

	public required string DataType { get; init; }

	public required string Unit { get; init; }

	public required string CurrentValue { get; init; }

	public required string NormalRange { get; init; }

	public required string UpdateInterval { get; init; }

	public required string LastTimestamp { get; init; }

	public required bool IsEnabled { get; init; }

	public required string Category { get; init; }

	public required bool IsRegistered { get; init; }
}
