namespace Werkflow.OpcUaSimulator.Core.Models;

public static class SimulationStateExtensions
{
	public static string ToGermanLabel(this SimulationState state)
	{
		if (1 == 0)
		{
		}
		string result = state switch
		{
			SimulationState.Stopped => "Gestoppt", 
			SimulationState.Running => "Läuft", 
			SimulationState.Paused => "Pausiert", 
			_ => state.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
