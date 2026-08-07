namespace Werkflow.OpcUaSimulator.Core.Models;

public static class SimulationEventTypeExtensions
{
	public static string ToGermanLabel(this SimulationEventType type)
	{
		if (1 == 0)
		{
		}
		string result = type switch
		{
			SimulationEventType.Error => "Fehler", 
			SimulationEventType.Warning => "Warnung", 
			SimulationEventType.ProductionStop => "Produktionsstillstand", 
			SimulationEventType.OpcUaDisconnect => "OPC-UA-Verbindungsabbruch", 
			SimulationEventType.SlowProduction => "Langsame Produktion", 
			SimulationEventType.FastProductionJump => "Schneller Produktionssprung", 
			SimulationEventType.CounterFreeze => "Zähler bleibt stehen", 
			SimulationEventType.CounterJump => "Zähler springt", 
			SimulationEventType.JobChange => "Auftrag wechseln", 
			SimulationEventType.TargetQuantityChange => "Sollmenge ändern", 
			SimulationEventType.SetupState => "Rüstzustand", 
			_ => type.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
