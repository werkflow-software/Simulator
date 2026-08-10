using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;

internal static class HmiSemanticLabels
{
	private static readonly Dictionary<HmiSemantic, string> GermanLabels = new()
	{
		[HmiSemantic.XPosition] = "X Position",
		[HmiSemantic.YPosition] = "Y Position",
		[HmiSemantic.ZPosition] = "Z Position",
		[HmiSemantic.FeedRate] = "Vorschub",
		[HmiSemantic.MotorCurrent] = "Motorstrom",
		[HmiSemantic.MotorTemperature] = "Motortemperatur",
		[HmiSemantic.CoolingTemperature] = "Kühlmitteltemperatur",
		[HmiSemantic.PowerDemand] = "Leistungsbedarf",
		[HmiSemantic.VibrationRms] = "Vibration RMS",
		[HmiSemantic.JobName] = "Auftrag",
		[HmiSemantic.PartName] = "Teil",
		[HmiSemantic.ActualCounter] = "Istzähler",
		[HmiSemantic.TargetCounter] = "Sollzähler",
		[HmiSemantic.RemainingCounter] = "Rest",
		[HmiSemantic.MachineState] = "Maschinenstatus",
		[HmiSemantic.ErrorActive] = "Fehler aktiv",
		[HmiSemantic.ErrorMessage] = "Fehlermeldung",
		[HmiSemantic.ProcessPhase] = "Prozessphase",
		[HmiSemantic.ProcessDemand] = "Prozessleistung",
		[HmiSemantic.ProcessSpeed] = "Prozessgeschwindigkeit",
		[HmiSemantic.FocusPosition] = "Fokusposition",
		[HmiSemantic.MaterialThickness] = "Materialdicke",
		[HmiSemantic.CycleTime] = "Zykluszeit",
		[HmiSemantic.QualityIndex] = "Qualitätsindex",
		[HmiSemantic.CoolingFlow] = "Kühlmittelfluss",
		[HmiSemantic.CoolingPressure] = "Kühlmitteldruck",
		[HmiSemantic.CoolingPumpCurrent] = "Pumpenstrom",
		[HmiSemantic.CoolingPumpSpeed] = "Pumpendrehzahl",
		[HmiSemantic.CoolingFanSpeed] = "Lüfterdrehzahl",
		[HmiSemantic.CoolingStatus] = "Kühlstatus",
		[HmiSemantic.Voltage] = "Spannung",
		[HmiSemantic.Current] = "Strom",
		[HmiSemantic.VibrationPeak] = "Vibration Peak",
		[HmiSemantic.ProductionRunning] = "Produktion läuft",
		[HmiSemantic.LastProductionChange] = "Letzte Änderung"
	};

	public static string German(HmiSemantic semantic) =>
		GermanLabels.TryGetValue(semantic, out string? label) ? label : semantic.ToString();

	public static HmiSemantic? TryParse(string label) =>
		GermanLabels.FirstOrDefault(kv => kv.Value.Equals(label, StringComparison.OrdinalIgnoreCase)).Key is HmiSemantic key
			? key
			: null;
}
