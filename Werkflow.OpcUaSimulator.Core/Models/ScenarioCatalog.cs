using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.Models;

public static class ScenarioCatalog
{
	public static IReadOnlyList<ScenarioDefinition> CreateDefaults()
	{
		return new _003C_003Ez__ReadOnlyArray<ScenarioDefinition>(new ScenarioDefinition[7]
		{
			new ScenarioDefinition
			{
				Id = "normal-production",
				Name = "Normale Produktion",
				Description = "Alle Maschinen online, unterschiedliche Geschwindigkeiten, keine Fehler, regelmäßige Zähleränderungen."
			},
			new ScenarioDefinition
			{
				Id = "counter-freeze",
				Name = "Zählerstillstand",
				Description = "Ausgewählte Maschine bleibt online, Heartbeat läuft weiter, Istzähler ändert sich nicht – für Stillstandserkennung."
			},
			new ScenarioDefinition
			{
				Id = "machine-error",
				Name = "Maschinenfehler",
				Description = "ErrorActive=true, Fehlermeldung gesetzt, MachineState=Error, Zähler bleibt stehen."
			},
			new ScenarioDefinition
			{
				Id = "no-connection",
				Name = "Keine Verbindung",
				Description = "OPC-UA-Server wird vollständig gestoppt und nach einstellbarer Dauer wieder gestartet."
			},
			new ScenarioDefinition
			{
				Id = "job-near-complete",
				Name = "Auftrag fast fertig",
				Description = "Istzähler wird auf 90 % des Sollzählers gesetzt, Produktion läuft danach normal weiter."
			},
			new ScenarioDefinition
			{
				Id = "job-completed",
				Name = "Auftrag abgeschlossen",
				Description = "Istzähler erreicht Sollzähler, Maschine geht auf Bereit, optional neuer Auftrag."
			},
			new ScenarioDefinition
			{
				Id = "mixed-disturbances",
				Name = "Wechselnde Störungen",
				Description = "Fehler, Stillstand und Offline wechseln zufällig, mindestens eine andere Maschine produziert weiter."
			}
		});
	}
}
