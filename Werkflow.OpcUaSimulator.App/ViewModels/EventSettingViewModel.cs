using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class EventSettingViewModel
{
	public string Label { get; }

	public bool IsEnabled { get; }

	public double ProbabilityPercent { get; }

	public int MinDurationMs { get; }

	public int MaxDurationMs { get; }

	public EventSettingViewModel(EventTypeSettings settings)
	{
		Label = settings.EventType.ToGermanLabel();
		IsEnabled = settings.IsEnabled;
		ProbabilityPercent = settings.ProbabilityPercent;
		MinDurationMs = settings.MinDurationMs;
		MaxDurationMs = settings.MaxDurationMs;
	}
}
