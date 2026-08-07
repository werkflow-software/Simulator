namespace Werkflow.OpcUaSimulator.Core.Models;

public static class MachineStateExtensions
{
	public static string ToGermanLabel(this MachineState state)
	{
		if (1 == 0)
		{
		}
		string result = state switch
		{
			MachineState.Offline => "Offline", 
			MachineState.Idle => "Bereit", 
			MachineState.Running => "Produziert", 
			MachineState.Warning => "Warnung", 
			MachineState.Error => "Fehler", 
			MachineState.Paused => "Pausiert", 
			MachineState.Setup => "Rüsten", 
			_ => state.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
