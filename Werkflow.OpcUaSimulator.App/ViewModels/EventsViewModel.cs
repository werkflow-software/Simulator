using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class EventsViewModel : ObservableObject
{
	public ObservableCollection<EventSettingViewModel> EventSettings { get; } = new ObservableCollection<EventSettingViewModel>();

	public ObservableCollection<string> ErrorMessages { get; } = new ObservableCollection<string>();

	public string InfoText => "Feste Fehler-, Warn- und StÃ¶rprofile. Nur die Zeitpunkte variieren zufÃ¤llig â€” Inhalte und Wahrscheinlichkeiten sind fÃ¼r reproduzierbare Tests vorgegeben.";

	public EventsViewModel(IConfigurationService configurationService)
	{
		foreach (EventTypeSettings @event in configurationService.Configuration.Events.Events)
		{
			EventSettings.Add(new EventSettingViewModel(@event));
		}
		foreach (string errorMessage in configurationService.Configuration.Events.ErrorMessages)
		{
			ErrorMessages.Add(errorMessage);
		}
	}
}
