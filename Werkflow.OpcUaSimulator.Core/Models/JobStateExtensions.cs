namespace Werkflow.OpcUaSimulator.Core.Models;

public static class JobStateExtensions
{
	public static string ToGermanLabel(this JobState state)
	{
		if (1 == 0)
		{
		}
		string result = state switch
		{
			JobState.Pending => "Wartend", 
			JobState.Assigned => "Zugewiesen", 
			JobState.Running => "Läuft", 
			JobState.Completed => "Abgeschlossen", 
			JobState.Cancelled => "Abgebrochen", 
			_ => state.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
