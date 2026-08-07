using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class TechnicalLearningMachine300SemanticCorrector
{
	private static readonly HashSet<string> BooleanSignalIds = new HashSet<string> { "Production.ChangeoverActive", "Production.OperatorPresence", "Production.AutomaticMode", "Electrical.SafetyCircuitOk", "Electrical.EmergencyStopReleased", "Electrical.DoorInterlockClosed", "Electrical.LightCurtainClear", "Quality.DimensionCheckPassed", "Quality.LastInspectionResult" };

	private static readonly HashSet<string> BooleanSuffixes = new HashSet<string> { "MotionActive", "ReferenceValid", "BrakeEngaged" };

	private static readonly HashSet<string> Int64CounterIds = new HashSet<string> { "Production.CycleCounter", "Production.PartCounterShift", "Electrical.EnergyCounter", "Quality.CommunicationErrorCount", "Diagnostic.WatchdogCounter", "Diagnostic.UptimeHours" };

	private static readonly HashSet<string> Int64CounterSuffixes = new HashSet<string> { "OperatingHours" };

	private static readonly HashSet<string> StringSignalIds = new HashSet<string> { "Production.ActiveProgram", "Process.MaterialName", "Process.RecipeName", "Process.ToolDesignation", "Process.ProcessVariant", "Quality.QualityClass", "Production.MaterialDesignation", "Production.RecipeName" };

	private static readonly HashSet<string> DateTimeSignalIds = new HashSet<string> { "Production.LastCycleStartUtc", "Production.LastCycleEndUtc", "Production.LastReferenceUtc", "Production.LastMaintenanceUtc", "Production.LastProgramChangeUtc", "Process.LastStateChangeUtc", "Diagnostic.LastControllerRestartUtc", "Quality.LastCalibrationUtc", "Process.LastToolChangeUtc", "Production.LastSetupCompletedUtc" };

	private static readonly HashSet<string> Int32DiscreteIds = new HashSet<string> { "Production.QueueLength", "Diagnostic.AlarmCountActive", "Diagnostic.WarningCountActive", "Quality.CalibrationAgeDays", "Production.ActiveRecipeNumber", "Production.ActiveToolNumber" };

	private static readonly HashSet<string> Int32DiscreteSuffixes = new HashSet<string> { "OperatingState" };

	private static readonly HashSet<string> FloatSignalIds = new HashSet<string>
	{
		"Production.OeeAvailability", "Production.OeePerformance", "Production.OeeQuality", "Production.ToolLifeRemaining", "Production.BatchProgress", "Production.ScrapRate", "Production.ReworkRate", "Electrical.CabinetHumidity", "Electrical.UpsBatteryLevel", "Electrical.PowerFactor",
		"Electrical.HarmonicDistortion", "Cooling.ReservoirLevel", "Cooling.HeatExchangerEfficiency", "Cooling.CondensateLevel", "Hydraulic.OilLevel", "Hydraulic.FilterLoad", "Process.FilterLoad", "Process.GasPurity", "Quality.VisionSystemConfidence", "Quality.SensorHealthIndex",
		"Quality.SurfaceInspectionScore", "Diagnostic.CpuLoad", "Diagnostic.MemoryUsage", "Diagnostic.DiskFreeSpace", "Pneumatic.DryerDewPoint", "Process.CoolantConcentration", "Process.ToolWearIndex", "Axis01.Load", "Axis02.Load", "Axis03.Load",
		"Axis04.Load", "Axis05.Load", "Axis06.Load"
	};

	private static readonly HashSet<string> SlowContinuousIds = new HashSet<string> { "Thermal.AmbientTemperature", "Thermal.AirInletTemp", "Thermal.AirOutletTemp", "Thermal.CabinetTemperature", "Hydraulic.OilLevel", "Cooling.ReservoirLevel" };

	private static readonly HashSet<string> StableSignalIds = new HashSet<string> { "Process.MaterialThickness", "Process.MaterialName", "Process.RecipeName", "Process.ToolDesignation", "Production.MaterialDesignation", "Production.RecipeName", "Quality.QualityClass", "Process.ProcessVariant" };

	public static List<SignalDefinition> Apply(IReadOnlyList<SignalDefinition> signals)
	{
		List<SignalDefinition> list = new List<SignalDefinition>(signals.Count);
		foreach (SignalDefinition signal in signals)
		{
			list.Add(ApplyRules(signal));
		}
		EnsureRequiredSignals(list);
		PruneRedundantSignals(list);
		return list;
	}

	private static void PruneRedundantSignals(List<SignalDefinition> signals)
	{
		HashSet<string> removeIds = new HashSet<string>(StringComparer.Ordinal)
		{
			"Pneumatic.Valve06.Position", "Pneumatic.Valve07.Position", "Pneumatic.Valve08.Position", "Pneumatic.Valve09.Position", "Pneumatic.Valve10.Position", "Hydraulic.Valve06.Position", "Hydraulic.Valve07.Position", "Hydraulic.Valve08.Position", "Hydraulic.Valve09.Position", "Hydraulic.Valve10.Position",
			"Thermal.PanelSurfaceTemp", "Thermal.GearboxTemp", "Thermal.HeatSinkTemp", "Cooling.CondensateLevel", "Cooling.CircuitConductivity", "Hydraulic.FilterLoad", "Process.ChamberPressure", "Process.ExhaustFlow"
		};
		signals.RemoveAll((SignalDefinition s) => removeIds.Contains(s.SignalId));
	}

	private static SignalDefinition ApplyRules(SignalDefinition signal)
	{
		string signalId = signal.SignalId;
		string text = signalId.Split('.')[^1];
		if (BooleanSignalIds.Contains(signalId) || BooleanSuffixes.Contains(text))
		{
			return ToBoolean(signal, !text.Contains("Brake") && !text.Contains("Changeover"));
		}
		if (Int64CounterIds.Contains(signalId) || Int64CounterSuffixes.Contains(text))
		{
			return ToInt64Counter(signal);
		}
		if (StringSignalIds.Contains(signalId))
		{
			return ToStringState(signal);
		}
		if (DateTimeSignalIds.Contains(signalId))
		{
			return ToTimestamp(signal);
		}
		if (Int32DiscreteIds.Contains(signalId) || Int32DiscreteSuffixes.Contains(text))
		{
			return ToInt32Discrete(signal);
		}
		if (FloatSignalIds.Contains(signalId) || signal.EngineeringUnit == "%")
		{
			return ToFloat(signal, SlowContinuousIds.Contains(signalId) ? TechnicalSignalBehavior.SlowContinuous : TechnicalSignalBehavior.Continuous);
		}
		if (StableSignalIds.Contains(signalId))
		{
			return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
			{
				m.TechnicalBehavior = TechnicalSignalBehavior.Stable;
				if (signal.DataType != PhysicalSignalDataType.String)
				{
					m.NoiseAmplitude = 0.0;
				}
			});
		}
		string engineeringUnit = signal.EngineeringUnit;
		bool flag = ((engineeringUnit == "°C" || engineeringUnit == "h") ? true : false);
		if (flag && signal.NormalMaximum - signal.NormalMinimum < 30.0)
		{
			return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
			{
				m.TechnicalBehavior = TechnicalSignalBehavior.SlowContinuous;
			});
		}
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.TechnicalBehavior = TechnicalSignalBehavior.Continuous;
		});
	}

	private static void EnsureRequiredSignals(List<SignalDefinition> signals)
	{
		RenameSignal(signals, "Production.ActiveProgram", (SignalDefinition s) => SignalDefinitionMutator.Copy(ToStringState(s), delegate(MutableSignalDefinition m)
		{
			m.DisplayName = "Active Program";
			m.InitialStringValue = "PRG-12045";
			int num = 4;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			span[0] = "PRG-12045";
			span[1] = "PRG-12046";
			span[2] = "PRG-22010";
			span[3] = "PRG-33008";
			m.AllowedValues = list;
		}));
		AddOrReplaceTimestamp(signals, "Production.LastCycleStartUtc", "Last Cycle Start", SignalCategory.Production, 5.0);
		AddOrReplaceTimestamp(signals, "Production.LastCycleEndUtc", "Last Cycle End", SignalCategory.Production, 5.0);
		AddOrReplaceTimestamp(signals, "Production.LastReferenceUtc", "Last Reference Run", SignalCategory.Production, 300.0);
		AddOrReplaceTimestamp(signals, "Production.LastMaintenanceUtc", "Last Maintenance", SignalCategory.Production, 3600.0);
		AddOrReplaceTimestamp(signals, "Production.LastProgramChangeUtc", "Last Program Change", SignalCategory.Production, 600.0);
		AddOrReplaceTimestamp(signals, "Process.LastStateChangeUtc", "Last Process State Change", SignalCategory.Process, 30.0);
		AddOrReplaceTimestamp(signals, "Diagnostic.LastControllerRestartUtc", "Last Controller Restart", SignalCategory.Diagnostic, 3600.0);
		AddOrReplaceTimestamp(signals, "Quality.LastCalibrationUtc", "Last Calibration", SignalCategory.Quality, 3600.0);
		AddOrReplaceTimestamp(signals, "Process.LastToolChangeUtc", "Last Tool Change", SignalCategory.Process, 1800.0);
		AddOrReplaceTimestamp(signals, "Production.LastSetupCompletedUtc", "Last Setup Completed", SignalCategory.Production, 1800.0);
		AddOrReplaceString(signals, "Process.MaterialName", "Material Name", SignalCategory.Process, "AlMg3-EN-AW5754", new string[4] { "AlMg3-EN-AW5754", "S235JR", "1.4301", "CuZn37" });
		AddOrReplaceString(signals, "Process.RecipeName", "Recipe Name", SignalCategory.Process, "LaserCut-Standard-A", new string[3] { "LaserCut-Standard-A", "LaserCut-Fine-B", "Mill-Contour-C" });
		AddOrReplaceString(signals, "Process.ToolDesignation", "Tool Designation", SignalCategory.Process, "Nozzle-1.2-Cu", new string[3] { "Nozzle-1.2-Cu", "Nozzle-1.5-Cu", "Mill-6HSS" });
		AddOrReplaceString(signals, "Process.ProcessVariant", "Process Variant", SignalCategory.Process, "Variant-A", new string[3] { "Variant-A", "Variant-B", "Variant-C" });
		AddOrReplaceString(signals, "Quality.QualityClass", "Quality Class", SignalCategory.Quality, "Class-A", new string[3] { "Class-A", "Class-B", "Class-C" });
		AddOrReplaceString(signals, "Production.MaterialDesignation", "Material Designation", SignalCategory.Production, "Sheet-2mm", new string[3] { "Sheet-2mm", "Sheet-3mm", "Plate-6mm" });
		AddOrReplaceString(signals, "Production.RecipeName", "Production Recipe", SignalCategory.Production, "Batch-7741", new string[3] { "Batch-7741", "Batch-7742", "Batch-8801" });
		AddOrReplaceInt32(signals, "Production.ActiveRecipeNumber", "Active Recipe Number", SignalCategory.Production, 12045, new int[4] { 12045, 12046, 22010, 33008 }, 1, 99999);
		AddOrReplaceInt32(signals, "Production.ActiveToolNumber", "Active Tool Number", SignalCategory.Production, 12, new int[4] { 12, 15, 22, 31 }, 1, 200);
	}

	private static SignalDefinition ToBoolean(SignalDefinition signal, bool defaultValue)
	{
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.DataType = PhysicalSignalDataType.Boolean;
			m.TechnicalBehavior = TechnicalSignalBehavior.BooleanState;
			m.InitialValue = (defaultValue ? 1 : 0);
			m.NoiseAmplitude = 0.0;
			m.NormalMinimum = 0.0;
			m.NormalMaximum = 1.0;
			m.HardMinimum = 0.0;
			m.HardMaximum = 1.0;
		});
	}

	private static SignalDefinition ToInt64Counter(SignalDefinition signal)
	{
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.DataType = PhysicalSignalDataType.Int64;
			m.TechnicalBehavior = TechnicalSignalBehavior.Counter;
			m.CounterStepSize = 1;
			m.InitialValue = Math.Max(0.0, signal.InitialValue);
			m.NoiseAmplitude = 0.0;
			m.DecimalPlaces = 0;
		});
	}

	private static SignalDefinition ToInt32Discrete(SignalDefinition signal)
	{
		string[] values = BuildDiscreteIntValues(signal);
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.DataType = PhysicalSignalDataType.Int32;
			m.TechnicalBehavior = TechnicalSignalBehavior.DiscreteState;
			m.AllowedValues = values.ToList();
			m.InitialValue = ((values.Length != 0) ? int.Parse(values[0]) : ((int)signal.InitialValue));
			m.NoiseAmplitude = 0.0;
			m.DecimalPlaces = 0;
		});
	}

	private static SignalDefinition ToStringState(SignalDefinition signal)
	{
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.DataType = PhysicalSignalDataType.String;
			m.TechnicalBehavior = TechnicalSignalBehavior.TextState;
			m.InitialStringValue = (string.IsNullOrWhiteSpace(signal.InitialStringValue) ? signal.DisplayName : signal.InitialStringValue);
			List<string> list;
			if (signal.AllowedValues.Count <= 0)
			{
				int num = 1;
				list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = signal.DisplayName;
			}
			else
			{
				list = signal.AllowedValues.ToList();
			}
			m.AllowedValues = list;
			m.NoiseAmplitude = 0.0;
		});
	}

	private static SignalDefinition ToTimestamp(SignalDefinition signal)
	{
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.DataType = PhysicalSignalDataType.DateTime;
			m.TechnicalBehavior = TechnicalSignalBehavior.Timestamp;
			m.InitialDateTimeUtc = DateTime.UtcNow.AddHours(-2.0);
			m.NoiseAmplitude = 0.0;
		});
	}

	private static SignalDefinition ToFloat(SignalDefinition signal, TechnicalSignalBehavior behavior)
	{
		return SignalDefinitionMutator.Copy(signal, delegate(MutableSignalDefinition m)
		{
			m.DataType = PhysicalSignalDataType.Float;
			m.TechnicalBehavior = behavior;
			m.DecimalPlaces = Math.Min(signal.DecimalPlaces, 3);
		});
	}

	private static string[] BuildDiscreteIntValues(SignalDefinition signal)
	{
		if (signal.AllowedValues.Count > 0)
		{
			return signal.AllowedValues.ToArray();
		}
		int num = (int)Math.Max(signal.HardMinimum, signal.NormalMinimum);
		int num2 = (int)Math.Min(signal.HardMaximum, signal.NormalMaximum);
		if (num2 - num <= 10)
		{
			return (from v in Enumerable.Range(num, num2 - num + 1)
				select v.ToString()).ToArray();
		}
		return new string[3]
		{
			num.ToString(),
			((num + num2) / 2).ToString(),
			num2.ToString()
		};
	}

	private static void RenameSignal(List<SignalDefinition> signals, string signalId, Func<SignalDefinition, SignalDefinition> transform)
	{
		int num = signals.FindIndex((SignalDefinition s) => s.SignalId == signalId);
		if (num >= 0)
		{
			signals[num] = transform(signals[num]);
		}
	}

	private static void AddOrReplaceTimestamp(List<SignalDefinition> signals, string signalId, string displayName, SignalCategory category, double intervalSeconds)
	{
		int num = signals.FindIndex((SignalDefinition s) => s.SignalId == signalId);
		SignalDefinition signalDefinition = new SignalDefinition
		{
			SignalId = signalId,
			NodeId = signalId,
			BrowseName = signalId.Split('.')[^1],
			DisplayName = displayName,
			Description = displayName,
			Category = category,
			DataType = PhysicalSignalDataType.DateTime,
			TechnicalBehavior = TechnicalSignalBehavior.Timestamp,
			UpdateInterval = TimeSpan.FromSeconds(intervalSeconds),
			InitialDateTimeUtc = DateTime.UtcNow.AddHours(-1.0),
			IsEnabled = true
		};
		if (num >= 0)
		{
			signals[num] = signalDefinition;
		}
		else
		{
			signals.Add(signalDefinition);
		}
	}

	private static void AddOrReplaceString(List<SignalDefinition> signals, string signalId, string displayName, SignalCategory category, string initial, string[] allowed)
	{
		int num = signals.FindIndex((SignalDefinition s) => s.SignalId == signalId);
		SignalDefinition signalDefinition = new SignalDefinition
		{
			SignalId = signalId,
			NodeId = signalId,
			BrowseName = signalId.Split('.')[^1],
			DisplayName = displayName,
			Description = displayName,
			Category = category,
			DataType = PhysicalSignalDataType.String,
			TechnicalBehavior = TechnicalSignalBehavior.TextState,
			InitialStringValue = initial,
			AllowedValues = allowed,
			UpdateInterval = TimeSpan.FromSeconds(60.0),
			IsEnabled = true
		};
		if (num >= 0)
		{
			signals[num] = signalDefinition;
		}
		else
		{
			signals.Add(signalDefinition);
		}
	}

	private static void AddOrReplaceInt32(List<SignalDefinition> signals, string signalId, string displayName, SignalCategory category, int initial, int[] allowed, int hardMin, int hardMax)
	{
		signals.Add(new SignalDefinition
		{
			SignalId = signalId,
			NodeId = signalId,
			BrowseName = signalId.Split('.')[^1],
			DisplayName = displayName,
			Description = displayName,
			Category = category,
			DataType = PhysicalSignalDataType.Int32,
			TechnicalBehavior = TechnicalSignalBehavior.DiscreteState,
			AllowedValues = allowed.Select((int a) => a.ToString()).ToArray(),
			InitialValue = initial,
			NormalMinimum = allowed.Min(),
			NormalMaximum = allowed.Max(),
			NominalValue = initial,
			HardMinimum = hardMin,
			HardMaximum = hardMax,
			UpdateInterval = TimeSpan.FromSeconds(10.0),
			IsEnabled = true
		});
	}
}
