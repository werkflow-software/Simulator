namespace Werkflow.OpcUaSimulator.Core.Models;

public static class LogCategoryExtensions
{
	public static string ToGermanLabel(this LogCategory category)
	{
		if (1 == 0)
		{
		}
		string result = category switch
		{
			LogCategory.Server => "Server", 
			LogCategory.Production => "Produktion", 
			LogCategory.Job => "Auftrag", 
			LogCategory.Error => "Fehler", 
			LogCategory.Warning => "Warnung", 
			LogCategory.Connection => "Verbindung", 
			LogCategory.Configuration => "Konfiguration", 
			_ => category.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
