using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class PhysicalProfileDependencyBuilder
{
	public static IReadOnlyList<HiddenProcessStateDefinition> CreateLaserHiddenStates()
	{
		return new _003C_003Ez__ReadOnlyArray<HiddenProcessStateDefinition>(new HiddenProcessStateDefinition[12]
		{
			H("MechanicalLoad", 0.2, 0.8, 0.45, 0.4, 0.05, 0.02),
			H("ThermalLoad", 0.15, 0.75, 0.4, 2.0, 0.01, 0.015, 2.0),
			H("CoolingEfficiency", 0.7, 1.0, 0.92, 1.5, 0.005, 0.01),
			H("Friction", 0.1, 0.6, 0.28, 0.8, 0.02, 0.02),
			H("ProcessDemand", 0.2, 0.9, 0.55, 0.5, 0.03, 0.02),
			H("ElectricalStability", 0.75, 1.0, 0.95, 1.0, 0.01, 0.008),
			H("ToolCondition", 0.6, 1.0, 0.88, 2.0, 0.005, 0.01),
			H("OpticalCondition", 0.7, 1.0, 0.93, 1.5, 0.01, 0.012),
			H("AmbientInfluence", 0.1, 0.4, 0.22, 3.0, 0.005, 0.008),
			H("AxisAlignment", 0.7, 1.0, 0.9, 1.2, 0.01, 0.01),
			H("LubricationQuality", 0.65, 1.0, 0.85, 2.5, 0.008, 0.01),
			H("MaterialResistance", 0.2, 0.85, 0.5, 1.0, 0.02, 0.015)
		});
	}

	public static IReadOnlyList<HiddenProcessStateDefinition> CreateBendingHiddenStates()
	{
		return new _003C_003Ez__ReadOnlyArray<HiddenProcessStateDefinition>(new HiddenProcessStateDefinition[12]
		{
			H("HydraulicEfficiency", 0.65, 1.0, 0.88, 1.5, 0.01, 0.01, 1.0),
			H("OilCondition", 0.6, 1.0, 0.85, 2.0, 0.008, 0.012),
			H("ToolDeflection", 0.05, 0.5, 0.18, 0.6, 0.02, 0.015),
			H("PressLoad", 0.2, 0.9, 0.5, 0.5, 0.03, 0.035),
			H("StructuralThermalLoad", 0.15, 0.7, 0.35, 3.0, 0.01, 0.01),
			H("ValveResponse", 0.7, 1.0, 0.9, 0.4, 0.02, 0.01),
			H("MaterialSpringback", 0.1, 0.6, 0.3, 1.0, 0.015, 0.02),
			H("PumpEfficiency", 0.7, 1.0, 0.9, 1.2, 0.01, 0.028),
			H("AxisFriction", 0.1, 0.55, 0.25, 0.8, 0.02, 0.02),
			H("AmbientInfluence", 0.1, 0.4, 0.22, 3.0, 0.005, 0.008),
			H("ElectricalStability", 0.75, 1.0, 0.95, 1.0, 0.01, 0.008),
			H("MechanicalWear", 0.05, 0.5, 0.2, 5.0, 0.003, 0.01)
		});
	}

	public static List<HiddenStateDependencyDefinition> CreateLaserHiddenDependencies()
	{
		List<HiddenStateDependencyDefinition> list = new List<HiddenStateDependencyDefinition>();
		int num = 0;
		list.Add(Hd($"hs-{++num}", "ProcessDemand", "MechanicalLoad", DependencyType.Linear, 0.35, 0.1, null, null));
		list.Add(Hd($"hs-{++num}", "MechanicalLoad", "ThermalLoad", DependencyType.DelayedLinear, 0.4, 0.05, TimeSpan.FromSeconds(15.0), null));
		list.Add(Hd($"hs-{++num}", "CoolingEfficiency", "ThermalLoad", DependencyType.InverseLinear, 0.25, 0.1, null, null));
		list.Add(Hd($"hs-{++num}", "Friction", "MechanicalLoad", DependencyType.Linear, 0.2, 0.05, null, null));
		list.Add(Hd($"hs-{++num}", "AmbientInfluence", "CoolingEfficiency", DependencyType.InverseLinear, 0.15, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "MaterialResistance", "ProcessDemand", DependencyType.Linear, 0.2, 0.05, null, null));
		list.Add(Hd($"hs-{++num}", "ToolCondition", "Friction", DependencyType.Polynomial, 0.15, 0.0, null, null));
		string id = $"hs-{++num}";
		double? max = 0.3;
		list.Add(Hd(id, "OpticalCondition", "ProcessDemand", DependencyType.Saturating, 0.1, 0.0, null, max));
		list.Add(Hd($"hs-{++num}", "LubricationQuality", "Friction", DependencyType.InverseLinear, 0.2, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "AxisAlignment", "MechanicalLoad", DependencyType.PiecewiseLinear, 0.12, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "ElectricalStability", "ProcessDemand", DependencyType.Sigmoid, 0.1, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "ThermalLoad", "CoolingEfficiency", DependencyType.InverseLinear, 0.08, 0.0, null, null));
		string id2 = $"hs-{++num}";
		max = 0.1;
		list.Add(Hd(id2, "MechanicalLoad", "ProcessDemand", DependencyType.RateLimited, 0.05, 0.0, null, max));
		list.Add(Hd($"hs-{++num}", "AmbientInfluence", "ThermalLoad", DependencyType.DelayedLinear, 0.12, 0.0, TimeSpan.FromSeconds(30.0), null));
		list.Add(Hd($"hs-{++num}", "ProcessDemand", "ThermalLoad", DependencyType.Threshold, 0.3, 0.0, null, null, 0.5));
		return list;
	}

	public static List<HiddenStateDependencyDefinition> CreateBendingHiddenDependencies()
	{
		List<HiddenStateDependencyDefinition> list = new List<HiddenStateDependencyDefinition>();
		int num = 0;
		list.Add(Hd($"hs-{++num}", "PressLoad", "HydraulicEfficiency", DependencyType.InverseLinear, 0.04, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "PressLoad", "StructuralThermalLoad", DependencyType.DelayedLinear, 0.35, 0.0, TimeSpan.FromSeconds(20.0), null));
		list.Add(Hd($"hs-{++num}", "OilCondition", "PumpEfficiency", DependencyType.Linear, 0.25, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "PumpEfficiency", "HydraulicEfficiency", DependencyType.Linear, 0.3, 0.1, null, null));
		string id = $"hs-{++num}";
		double? max = 0.4;
		list.Add(Hd(id, "ValveResponse", "PressLoad", DependencyType.Saturating, 0.2, 0.0, null, max));
		list.Add(Hd($"hs-{++num}", "ToolDeflection", "MaterialSpringback", DependencyType.Linear, 0.3, 0.05, null, null));
		list.Add(Hd($"hs-{++num}", "AxisFriction", "PressLoad", DependencyType.Linear, 0.15, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "MechanicalWear", "AxisFriction", DependencyType.PiecewiseLinear, 0.2, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "AmbientInfluence", "StructuralThermalLoad", DependencyType.Linear, 0.1, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "ElectricalStability", "PumpEfficiency", DependencyType.Sigmoid, 0.1, 0.0, null, null));
		string id2 = $"hs-{++num}";
		max = 0.08;
		list.Add(Hd(id2, "PressLoad", "OilCondition", DependencyType.RateLimited, 0.05, 0.0, null, max));
		list.Add(Hd($"hs-{++num}", "MaterialSpringback", "PressLoad", DependencyType.InverseLinear, 0.1, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "HydraulicEfficiency", "ValveResponse", DependencyType.Hysteresis, 0.15, 0.0, null, null, 0.55));
		list.Add(Hd($"hs-{++num}", "StructuralThermalLoad", "OilCondition", DependencyType.InverseLinear, 0.12, 0.0, null, null));
		list.Add(Hd($"hs-{++num}", "PressLoad", "ToolDeflection", DependencyType.Threshold, 0.25, 0.0, null, null, 0.55));
		return list;
	}

	public static List<SignalDependencyDefinition> CreateLaserSignalDependencies()
	{
		List<SignalDependencyDefinition> list = new List<SignalDependencyDefinition>();
		int num = 0;
		for (int i = 1; i <= 6; i++)
		{
			string text = $"Axis{i:D2}";
			double weight = ((i == 1) ? 4.0 : 9.0);
			int num2 = ((i == 1) ? 12 : 28);
			int num3 = ((i == 1) ? 15 : 42);
			list.Add(Sd($"sd-{++num}", "MechanicalLoad", text + ".MotorCurrent", DependencyType.Linear, weight, 2.5, null, null, null));
			list.Add(Sd($"sd-{++num}", "MechanicalLoad", text + ".Load", DependencyType.PiecewiseLinear, num2, 16.0, null, null, null));
			list.Add(Sd($"sd-{++num}", "LubricationQuality", text + ".Load", DependencyType.InverseLinear, 2.5, 0.0, null, null, null));
			list.Add(Sd($"sd-{++num}", "Friction", text + ".Speed", DependencyType.InverseLinear, num3, 950.0, null, null, null));
			list.Add(Sd($"sd-{++num}", "Friction", text + ".MotorCurrent", DependencyType.Linear, (i == 1) ? 4.2 : 6.0, 1.5, null, null, null));
			list.Add(Sd($"sd-{++num}", "ThermalLoad", text + ".MotorTemperature", DependencyType.DelayedLinear, 32.0, 20.0, TimeSpan.FromSeconds(20.0), null, null));
			list.Add(Sd($"sd-{++num}", "MechanicalLoad", text + ".VibrationRms", DependencyType.PiecewiseLinear, 0.8, 0.5, null, null, null));
			list.Add(Sd($"sd-{++num}", "MechanicalLoad", text + ".Torque", DependencyType.Linear, 12.0, 3.0, null, null, null));
		}
		list.Add(Sd($"sd-{++num}", "ProcessDemand", "Process.SpindleSpeed", DependencyType.Linear, 500.0, 350.0, null, null, null));
		string id = $"sd-{++num}";
		double? minEffect = 1500.0;
		double? maxEffect = 2500.0;
		list.Add(Sd(id, "ProcessDemand", "Process.FeedRate", DependencyType.Saturating, 700.0, 1850.0, null, minEffect, maxEffect));
		string id2 = $"sd-{++num}";
		maxEffect = 1.3;
		list.Add(Sd(id2, "ProcessDemand", "Process.PowerDemand", DependencyType.RateLimited, 3.2, 6.5, null, null, maxEffect));
		string id3 = $"sd-{++num}";
		maxEffect = 0.95;
		list.Add(Sd(id3, "MechanicalLoad", "Process.PowerDemand", DependencyType.RateLimited, 1.8, 4.0, null, null, maxEffect));
		list.Add(Sd($"sd-{++num}", "AmbientInfluence", "Process.PowerDemand", DependencyType.Linear, 1.2, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "AmbientInfluence", "Process.PowerDemand", DependencyType.Linear, 1.8, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ThermalLoad", "Thermal.SpindleMotorTemp", DependencyType.DelayedLinear, 28.0, 32.0, TimeSpan.FromSeconds(25.0), null, null));
		list.Add(Sd($"sd-{++num}", "ThermalLoad", "Thermal.CabinetTemperature", DependencyType.DelayedLinear, 6.0, 30.0, TimeSpan.FromSeconds(45.0), null, null));
		list.Add(Sd($"sd-{++num}", "CoolingEfficiency", "Cooling.PrimaryCircuit.Temperature", DependencyType.InverseLinear, 7.0, 30.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "AmbientInfluence", "Cooling.PrimaryCircuit.Temperature", DependencyType.Linear, 4.0, 22.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "CoolingEfficiency", "Cooling.PrimaryCircuit.Pressure", DependencyType.Threshold, 1.5, 4.0, null, null, null, 0.6));
		list.Add(Sd($"sd-{++num}", "CoolingEfficiency", "Cooling.PrimaryCircuit.Flow", DependencyType.Hysteresis, 8.0, 15.0, null, null, null, 0.7));
		list.Add(Sd($"sd-{++num}", "OpticalCondition", "Process.FocusPosition", DependencyType.Linear, 0.15, 0.0, null, null, null));
		string id4 = $"sd-{++num}";
		maxEffect = 8.0;
		list.Add(Sd(id4, "ProcessDemand", "Process.LaserPowerActual", DependencyType.Saturating, 5.0, 2.0, null, null, maxEffect));
		list.Add(Sd($"sd-{++num}", "MechanicalLoad", "Mechanical.VibrationRms", DependencyType.PiecewiseLinear, 0.6, 0.6, null, null, null));
		list.Add(Sd($"sd-{++num}", "AmbientInfluence", "Thermal.AmbientTemperature", DependencyType.Linear, 8.0, 18.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ElectricalStability", "Electrical.MainsVoltage", DependencyType.Linear, 15.0, 385.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ProcessDemand", "Process.QualityIndex", DependencyType.Sigmoid, 2.2, 96.2, null, null, null));
		list.Add(Sd($"sd-{++num}", "MaterialResistance", "Process.QualityIndex", DependencyType.InverseLinear, 3.5, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "OpticalCondition", "Process.QualityIndex", DependencyType.Linear, 2.5, 94.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "MaterialResistance", "Process.CycleTime", DependencyType.Linear, 5.0, 14.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "MaterialResistance", "Process.FeedRate", DependencyType.InverseLinear, 600.0, 2200.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ToolCondition", "Process.ToolWearIndex", DependencyType.InverseLinear, 30.0, 35.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ElectricalStability", "Electrical.PowerFactor", DependencyType.Linear, 0.05, 0.92, null, null, null));
		return list;
	}

	public static List<SignalDependencyDefinition> CreateBendingSignalDependencies()
	{
		List<SignalDependencyDefinition> list = new List<SignalDependencyDefinition>();
		int num = 0;
		list.Add(Sd($"sd-{++num}", "PressLoad", "Hydraulic.SupplyPressure", DependencyType.Linear, 16.0, 120.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "HydraulicEfficiency", "Hydraulic.SupplyPressure", DependencyType.Linear, 95.0, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "PumpEfficiency", "Hydraulic.SupplyPressure", DependencyType.Linear, 18.0, 172.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "OilCondition", "Hydraulic.SupplyPressure", DependencyType.Linear, 6.0, 4.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ValveResponse", "Hydraulic.SupplyPressure", DependencyType.Linear, 4.0, 1.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "HydraulicEfficiency", "Hydraulic.PumpCurrent", DependencyType.InverseLinear, 22.0, 2.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "PumpEfficiency", "Hydraulic.PumpCurrent", DependencyType.InverseLinear, 10.0, 1.5, null, null, null));
		string id = $"sd-{++num}";
		double? maxEffect = 165.0;
		list.Add(Sd(id, "PressLoad", "Bending.PressForce", DependencyType.Saturating, 50.0, 98.0, null, null, maxEffect));
		list.Add(Sd($"sd-{++num}", "ToolDeflection", "Bending.PressForce", DependencyType.Linear, 4.0, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "MaterialSpringback", "Bending.PressForce", DependencyType.InverseLinear, 5.0, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "PumpEfficiency", "Hydraulic.PumpSpeed", DependencyType.Linear, 580.0, 780.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "StructuralThermalLoad", "Hydraulic.OilTemperature", DependencyType.DelayedLinear, 20.0, 35.0, TimeSpan.FromSeconds(30.0), null, null));
		list.Add(Sd($"sd-{++num}", "StructuralThermalLoad", "Thermal.CabinetTemperature", DependencyType.DelayedLinear, 8.0, 28.0, TimeSpan.FromSeconds(40.0), null, null));
		list.Add(Sd($"sd-{++num}", "ToolDeflection", "Bending.ToolPosition", DependencyType.Linear, 5.0, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "MaterialSpringback", "Bending.AngleMeasured", DependencyType.InverseLinear, 2.0, 90.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ValveResponse", "Bending.CycleTime", DependencyType.InverseLinear, 3.0, 22.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "PressLoad", "Bending.RamPosition", DependencyType.Linear, 50.0, 10.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "AxisFriction", "Axis01.Speed", DependencyType.InverseLinear, 22.0, 400.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "AmbientInfluence", "Axis01.Speed", DependencyType.Linear, 4.0, 350.0, null, null, null));
		for (int i = 1; i <= 4; i++)
		{
			string text = $"Axis{i:D2}";
			string id2 = $"sd-{++num}";
			string target = text + ".MotorCurrent";
			maxEffect = 14.0;
			list.Add(Sd(id2, "PressLoad", target, DependencyType.Saturating, 7.0, 3.5, null, null, maxEffect));
			list.Add(Sd($"sd-{++num}", "AxisFriction", text + ".Speed", DependencyType.InverseLinear, 42.0, 350.0, null, null, null));
			list.Add(Sd($"sd-{++num}", "AmbientInfluence", text + ".Speed", DependencyType.Linear, 6.0, 350.0, null, null, null));
			list.Add(Sd($"sd-{++num}", "AxisFriction", text + ".MotorCurrent", DependencyType.Linear, 5.0, 1.2, null, null, null));
			list.Add(Sd($"sd-{++num}", "StructuralThermalLoad", text + ".MotorTemperature", DependencyType.DelayedLinear, 24.0, 34.0, TimeSpan.FromSeconds(25.0), null, null));
			list.Add(Sd($"sd-{++num}", "PressLoad", text + ".Load", DependencyType.Linear, 50.0, 15.0, null, null, null));
			list.Add(Sd($"sd-{++num}", "PressLoad", text + ".VibrationRms", DependencyType.PiecewiseLinear, 0.5, 0.5, null, null, null));
		}
		list.Add(Sd($"sd-{++num}", "PressLoad", "Process.PowerDemand", DependencyType.Linear, 8.0, 4.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "HydraulicEfficiency", "Cooling.PrimaryCircuit.Flow", DependencyType.Threshold, 6.0, 14.0, null, null, null, 0.55));
		list.Add(Sd($"sd-{++num}", "OilCondition", "Hydraulic.FilterLoad", DependencyType.InverseLinear, 40.0, 50.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "MechanicalWear", "Bending.DieWearIndex", DependencyType.Linear, 40.0, 10.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "AmbientInfluence", "Thermal.AmbientTemperature", DependencyType.Linear, 8.0, 18.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ElectricalStability", "Electrical.MainsVoltage", DependencyType.Linear, 15.0, 385.0, null, null, null));
		string id3 = $"sd-{++num}";
		maxEffect = 0.2;
		list.Add(Sd(id3, "PressLoad", "Bending.SpringbackCompensation", DependencyType.RateLimited, 0.5, 0.0, null, null, maxEffect));
		list.Add(Sd($"sd-{++num}", "MaterialSpringback", "Quality.ProcessQualityIndex", DependencyType.Sigmoid, 1.8, 96.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ToolDeflection", "Quality.ProcessQualityIndex", DependencyType.InverseLinear, 2.5, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "PressLoad", "Quality.ProcessQualityIndex", DependencyType.InverseLinear, 1.2, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "ValveResponse", "Pneumatic.SupplyPressure", DependencyType.Hysteresis, 2.0, 5.0, null, null, null, 0.65));
		list.Add(Sd($"sd-{++num}", "PumpEfficiency", "Electrical.TotalCurrent", DependencyType.Polynomial, 15.0, 8.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "PressLoad", "Bending.FrameDeflection", DependencyType.Linear, 0.3, 0.05, null, null, null));
		list.Add(Sd($"sd-{++num}", "ToolDeflection", "Bending.BendAngleError", DependencyType.Linear, 0.5, 0.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "HydraulicEfficiency", "Hydraulic.ReturnPressure", DependencyType.InverseLinear, 5.0, 10.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "OilCondition", "Hydraulic.OilLevel", DependencyType.InverseLinear, 20.0, 90.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "StructuralThermalLoad", "Drive01.Temperature", DependencyType.DelayedLinear, 15.0, 35.0, TimeSpan.FromSeconds(35.0), null, null));
		list.Add(Sd($"sd-{++num}", "PressLoad", "Production.LastCycleDuration", DependencyType.Linear, 4.0, 12.0, null, null, null));
		list.Add(Sd($"sd-{++num}", "MechanicalWear", "Production.ToolLifeRemaining", DependencyType.InverseLinear, 30.0, 80.0, null, null, null));
		return list;
	}

	public static void ApplyHiddenInputs(IList<SignalDefinition> signals, IReadOnlyList<SignalDependencyDefinition> dependencies)
	{
		Dictionary<string, string[]> dictionary = dependencies.GroupBy<SignalDependencyDefinition, string>((SignalDependencyDefinition d) => d.TargetSignalId, StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, SignalDependencyDefinition>, string, string[]>((IGrouping<string, SignalDependencyDefinition> g) => g.Key, (IGrouping<string, SignalDependencyDefinition> g) => g.Select((SignalDependencyDefinition d) => d.SourceStateId).Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < signals.Count; i++)
		{
			SignalDefinition signalDefinition = signals[i];
			if (dictionary.TryGetValue(signalDefinition.SignalId, out var value))
			{
				signals[i] = new SignalDefinition
				{
					SignalId = signalDefinition.SignalId,
					NodeId = signalDefinition.NodeId,
					BrowseName = signalDefinition.BrowseName,
					DisplayName = signalDefinition.DisplayName,
					Description = signalDefinition.Description,
					Category = signalDefinition.Category,
					DataType = signalDefinition.DataType,
					EngineeringUnit = signalDefinition.EngineeringUnit,
					NormalMinimum = signalDefinition.NormalMinimum,
					NormalMaximum = signalDefinition.NormalMaximum,
					NominalValue = signalDefinition.NominalValue,
					HardMinimum = signalDefinition.HardMinimum,
					HardMaximum = signalDefinition.HardMaximum,
					NoiseModel = signalDefinition.NoiseModel,
					NoiseAmplitude = signalDefinition.NoiseAmplitude,
					UpdateInterval = signalDefinition.UpdateInterval,
					DecimalPlaces = signalDefinition.DecimalPlaces,
					ResponseInertia = signalDefinition.ResponseInertia,
					InitialValue = signalDefinition.InitialValue,
					IsEnabled = signalDefinition.IsEnabled,
					IsWritable = signalDefinition.IsWritable,
					TechnicalBehavior = signalDefinition.TechnicalBehavior,
					CounterStepSize = signalDefinition.CounterStepSize,
					InitialStringValue = signalDefinition.InitialStringValue,
					InitialDateTimeUtc = signalDefinition.InitialDateTimeUtc,
					AllowedValues = signalDefinition.AllowedValues,
					HiddenProcessInputs = value
				};
			}
		}
	}

	private static HiddenProcessStateDefinition H(string id, double nMin, double nMax, double nominal, double inertia, double drift, double noise, double hardMaximum = 1.2)
	{
		return new HiddenProcessStateDefinition
		{
			StateId = id,
			DisplayName = id,
			NormalMinimum = nMin,
			NormalMaximum = nMax,
			NominalValue = nominal,
			HardMinimum = 0.0,
			HardMaximum = hardMaximum,
			InitialValue = nominal,
			ResponseInertia = inertia,
			NaturalDrift = drift,
			NoiseAmplitude = noise,
			RecoveryRate = 0.03,
			UpdateInterval = TimeSpan.FromSeconds(1.0)
		};
	}

	private static HiddenStateDependencyDefinition Hd(string id, string source, string target, DependencyType type, double weight, double offset, TimeSpan? delay = null, double? max = null, double threshold = 0.0)
	{
		return new HiddenStateDependencyDefinition
		{
			DependencyId = id,
			SourceStateId = source,
			TargetStateId = target,
			DependencyType = type,
			Weight = weight,
			Offset = offset,
			ResponseDelay = (delay ?? TimeSpan.Zero),
			MaximumEffect = max,
			ThresholdValue = threshold
		};
	}

	private static SignalDependencyDefinition Sd(string id, string source, string target, DependencyType type, double weight, double offset, TimeSpan? delay = null, double? minEffect = null, double? maxEffect = null, double threshold = 0.0)
	{
		return new SignalDependencyDefinition
		{
			DependencyId = id,
			SourceStateId = source,
			TargetSignalId = target,
			DependencyType = type,
			Weight = weight,
			Offset = offset,
			ResponseDelay = (delay ?? TimeSpan.Zero),
			ResponseInertia = (delay.HasValue ? (delay.Value.TotalSeconds * 0.1) : 0.2),
			MinimumEffect = minEffect,
			MaximumEffect = maxEffect,
			ThresholdValue = threshold
		};
	}
}
