using System.Collections.ObjectModel;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Models;

public sealed class HmiTabContent
{
	public required string TabKey { get; init; }

	public required string Title { get; init; }

	public ObservableCollection<HmiSignalDisplayItem> Signals { get; } = [];

	public ObservableCollection<HmiAxisPanel> AxisPanels { get; } = [];
}

public sealed class HmiAxisPanel
{
	public required string AxisName { get; init; }

	public ObservableCollection<HmiSignalDisplayItem> Signals { get; } = [];
}
