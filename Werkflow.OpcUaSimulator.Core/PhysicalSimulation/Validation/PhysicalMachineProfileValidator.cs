using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

public sealed class PhysicalMachineProfileValidator : IPhysicalMachineProfileValidator
{
	private const int MaxDecimalPlaces = 12;

	public PhysicalProfileValidationResult Validate(PhysicalMachineProfile profile)
	{
		ArgumentNullException.ThrowIfNull(profile, "profile");
		PhysicalProfileValidationResult result = new PhysicalProfileValidationResult();
		ValidateProfileHeader(profile, result);
		ValidateSignals(profile, result);
		ValidateHiddenStates(profile, result);
		ValidateDependencies(profile, result);
		ValidateHiddenStateDependencies(profile, result);
		return result;
	}

	private static void ValidateProfileHeader(PhysicalMachineProfile profile, PhysicalProfileValidationResult result)
	{
		if (string.IsNullOrWhiteSpace(profile.ProfileId))
		{
			result.AddError("PROFILE_ID_MISSING", "ProfileId fehlt.", "profileId");
		}
		Version result2;
		if (string.IsNullOrWhiteSpace(profile.ProfileVersion))
		{
			result.AddError("PROFILE_VERSION_MISSING", "ProfileVersion fehlt.", "profileVersion");
		}
		else if (!Version.TryParse(NormalizeVersion(profile.ProfileVersion), out result2))
		{
			result.AddWarning("PROFILE_VERSION_FORMAT", "ProfileVersion ist kein kanonisches Versionsformat (z. B. 1.0.0).", "profileVersion");
		}
		if (string.IsNullOrWhiteSpace(profile.DisplayName))
		{
			result.AddError("PROFILE_DISPLAYNAME_MISSING", "DisplayName fehlt.", "displayName");
		}
		if (profile.Signals.Count == 0)
		{
			result.AddError("PROFILE_SIGNALS_EMPTY", "Mindestens ein Signal ist erforderlich.", "signals");
		}
		if (profile.DefaultUpdateInterval <= TimeSpan.Zero)
		{
			result.AddError("PROFILE_UPDATE_INTERVAL_INVALID", "DefaultUpdateInterval muss größer als null sein.", "defaultUpdateInterval");
		}
	}

	private static void ValidateSignals(PhysicalMachineProfile profile, PhysicalProfileValidationResult result)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet3 = (from s in profile.HiddenProcessStates
			select s.StateId into id
			where !string.IsNullOrWhiteSpace(id)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < profile.Signals.Count; i++)
		{
			SignalDefinition signalDefinition = profile.Signals[i];
			string text = $"signals[{i}]";
			if (string.IsNullOrWhiteSpace(signalDefinition.SignalId))
			{
				result.AddError("SIGNAL_ID_MISSING", "SignalId fehlt.", text + ".signalId");
			}
			else if (!hashSet.Add(signalDefinition.SignalId))
			{
				result.AddError("SIGNAL_ID_DUPLICATE", "Doppelte SignalId '" + signalDefinition.SignalId + "'.", text + ".signalId");
			}
			if (string.IsNullOrWhiteSpace(signalDefinition.NodeId))
			{
				result.AddError("SIGNAL_NODEID_MISSING", "NodeId fehlt.", text + ".nodeId");
			}
			else if (!hashSet2.Add(signalDefinition.NodeId))
			{
				result.AddError("SIGNAL_NODEID_DUPLICATE", "Doppelte NodeId '" + signalDefinition.NodeId + "'.", text + ".nodeId");
			}
			if (string.IsNullOrWhiteSpace(signalDefinition.BrowseName))
			{
				result.AddError("SIGNAL_BROWSENAME_MISSING", "BrowseName fehlt.", text + ".browseName");
			}
			else
			{
				string logicalParent = GetLogicalParent(signalDefinition.NodeId);
				if (!dictionary.TryGetValue(logicalParent, out var value))
				{
					value = (dictionary[logicalParent] = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				}
				if (!value.Add(signalDefinition.BrowseName))
				{
					result.AddError("SIGNAL_BROWSENAME_DUPLICATE", $"Doppelter BrowseName '{signalDefinition.BrowseName}' im logischen Pfad '{logicalParent}'.", text + ".browseName");
				}
			}
			if (!Enum.IsDefined(signalDefinition.DataType))
			{
				result.AddError("SIGNAL_DATATYPE_INVALID", $"Ungültiger Datentyp '{signalDefinition.DataType}'.", text + ".dataType");
			}
			if (!Enum.IsDefined(signalDefinition.Category))
			{
				result.AddError("SIGNAL_CATEGORY_INVALID", $"Ungültige Kategorie '{signalDefinition.Category}'.", text + ".category");
			}
			if (!Enum.IsDefined(signalDefinition.NoiseModel))
			{
				result.AddError("SIGNAL_NOISEMODEL_INVALID", $"Ungültiges NoiseModel '{signalDefinition.NoiseModel}'.", text + ".noiseModel");
			}
			if (!Enum.IsDefined(signalDefinition.TechnicalBehavior))
			{
				result.AddError("SIGNAL_BEHAVIOR_INVALID", $"Ungültiges TechnicalBehavior '{signalDefinition.TechnicalBehavior}'.", text + ".technicalBehavior");
			}
			ValidateTechnicalBehavior(signalDefinition, text, result);
			if (RequiresNumericRanges(signalDefinition.DataType))
			{
				ValidateNumericRanges(signalDefinition, text, result);
			}
			if (signalDefinition.UpdateInterval <= TimeSpan.Zero)
			{
				result.AddError("SIGNAL_UPDATE_INTERVAL_INVALID", "UpdateInterval muss größer als null sein.", text + ".updateInterval");
			}
			if (signalDefinition.DecimalPlaces < 0 || signalDefinition.DecimalPlaces > 12)
			{
				result.AddError("SIGNAL_DECIMAL_PLACES_INVALID", $"DecimalPlaces ({signalDefinition.DecimalPlaces}) muss zwischen 0 und {12} liegen.", text + ".decimalPlaces");
			}
			if (signalDefinition.NoiseAmplitude < 0.0)
			{
				result.AddError("SIGNAL_NOISE_AMPLITUDE_NEGATIVE", "NoiseAmplitude darf nicht negativ sein.", text + ".noiseAmplitude");
			}
			if (signalDefinition.ResponseInertia < 0.0)
			{
				result.AddError("SIGNAL_RESPONSE_INERTIA_NEGATIVE", "ResponseInertia darf nicht negativ sein.", text + ".responseInertia");
			}
			for (int j = 0; j < signalDefinition.HiddenProcessInputs.Count; j++)
			{
				string text2 = signalDefinition.HiddenProcessInputs[j];
				if (string.IsNullOrWhiteSpace(text2))
				{
					result.AddError("SIGNAL_HIDDEN_INPUT_EMPTY", "HiddenProcessInput ist leer.", $"{text}.hiddenProcessInputs[{j}]");
				}
				else if (!hashSet3.Contains(text2))
				{
					result.AddError("SIGNAL_HIDDEN_INPUT_MISSING", "HiddenProcessInput '" + text2 + "' existiert nicht in HiddenProcessStates.", $"{text}.hiddenProcessInputs[{j}]");
				}
			}
		}
	}

	private static void ValidateHiddenStates(PhysicalMachineProfile profile, PhysicalProfileValidationResult result)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < profile.HiddenProcessStates.Count; i++)
		{
			HiddenProcessStateDefinition hiddenProcessStateDefinition = profile.HiddenProcessStates[i];
			string text = $"hiddenProcessStates[{i}]";
			if (string.IsNullOrWhiteSpace(hiddenProcessStateDefinition.StateId))
			{
				result.AddError("STATE_ID_MISSING", "StateId fehlt.", text + ".stateId");
			}
			else if (!hashSet.Add(hiddenProcessStateDefinition.StateId))
			{
				result.AddError("STATE_ID_DUPLICATE", "Doppelte StateId '" + hiddenProcessStateDefinition.StateId + "'.", text + ".stateId");
			}
			if (hiddenProcessStateDefinition.NormalMinimum >= hiddenProcessStateDefinition.NormalMaximum)
			{
				result.AddError("STATE_NORMAL_RANGE_INVALID", $"NormalMinimum ({hiddenProcessStateDefinition.NormalMinimum}) muss kleiner als NormalMaximum ({hiddenProcessStateDefinition.NormalMaximum}) sein.", text + ".normalMinimum");
			}
			if (hiddenProcessStateDefinition.HardMinimum > hiddenProcessStateDefinition.NormalMinimum)
			{
				result.AddError("STATE_HARD_MIN_INVALID", $"HardMinimum ({hiddenProcessStateDefinition.HardMinimum}) muss kleiner oder gleich NormalMinimum ({hiddenProcessStateDefinition.NormalMinimum}) sein.", text + ".hardMinimum");
			}
			if (hiddenProcessStateDefinition.HardMaximum < hiddenProcessStateDefinition.NormalMaximum)
			{
				result.AddError("STATE_HARD_MAX_INVALID", $"HardMaximum ({hiddenProcessStateDefinition.HardMaximum}) muss größer oder gleich NormalMaximum ({hiddenProcessStateDefinition.NormalMaximum}) sein.", text + ".hardMaximum");
			}
			if (hiddenProcessStateDefinition.NominalValue < hiddenProcessStateDefinition.NormalMinimum || hiddenProcessStateDefinition.NominalValue > hiddenProcessStateDefinition.NormalMaximum)
			{
				result.AddError("STATE_NOMINAL_OUT_OF_RANGE", $"NominalValue ({hiddenProcessStateDefinition.NominalValue}) liegt außerhalb des Normalbereichs.", text + ".nominalValue");
			}
			if (hiddenProcessStateDefinition.InitialValue < hiddenProcessStateDefinition.HardMinimum || hiddenProcessStateDefinition.InitialValue > hiddenProcessStateDefinition.HardMaximum)
			{
				result.AddError("STATE_INITIAL_OUT_OF_HARD_LIMITS", $"InitialValue ({hiddenProcessStateDefinition.InitialValue}) liegt außerhalb der Hard Limits.", text + ".initialValue");
			}
			if (hiddenProcessStateDefinition.ResponseInertia < 0.0)
			{
				result.AddError("STATE_RESPONSE_INERTIA_NEGATIVE", "ResponseInertia darf nicht negativ sein.", text + ".responseInertia");
			}
			if (hiddenProcessStateDefinition.NoiseAmplitude < 0.0)
			{
				result.AddError("STATE_NOISE_AMPLITUDE_NEGATIVE", "NoiseAmplitude darf nicht negativ sein.", text + ".noiseAmplitude");
			}
		}
	}

	private static void ValidateDependencies(PhysicalMachineProfile profile, PhysicalProfileValidationResult result)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = (from s in profile.HiddenProcessStates
			select s.StateId into id
			where !string.IsNullOrWhiteSpace(id)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet3 = (from s in profile.Signals
			select s.SignalId into id
			where !string.IsNullOrWhiteSpace(id)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < profile.Dependencies.Count; i++)
		{
			SignalDependencyDefinition signalDependencyDefinition = profile.Dependencies[i];
			string text = $"dependencies[{i}]";
			if (string.IsNullOrWhiteSpace(signalDependencyDefinition.DependencyId))
			{
				result.AddError("DEPENDENCY_ID_MISSING", "DependencyId fehlt.", text + ".dependencyId");
			}
			else if (!hashSet.Add(signalDependencyDefinition.DependencyId))
			{
				result.AddError("DEPENDENCY_ID_DUPLICATE", "Doppelte DependencyId '" + signalDependencyDefinition.DependencyId + "'.", text + ".dependencyId");
			}
			if (string.IsNullOrWhiteSpace(signalDependencyDefinition.SourceStateId))
			{
				result.AddError("DEPENDENCY_SOURCE_MISSING", "SourceStateId fehlt.", text + ".sourceStateId");
			}
			else if (!hashSet2.Contains(signalDependencyDefinition.SourceStateId))
			{
				result.AddError("DEPENDENCY_SOURCE_NOT_FOUND", "SourceStateId '" + signalDependencyDefinition.SourceStateId + "' existiert nicht.", text + ".sourceStateId");
			}
			if (string.IsNullOrWhiteSpace(signalDependencyDefinition.TargetSignalId))
			{
				result.AddError("DEPENDENCY_TARGET_MISSING", "TargetSignalId fehlt.", text + ".targetSignalId");
			}
			else if (!hashSet3.Contains(signalDependencyDefinition.TargetSignalId))
			{
				result.AddError("DEPENDENCY_TARGET_NOT_FOUND", "TargetSignalId '" + signalDependencyDefinition.TargetSignalId + "' existiert nicht.", text + ".targetSignalId");
			}
			if (!Enum.IsDefined(signalDependencyDefinition.DependencyType))
			{
				result.AddError("DEPENDENCY_TYPE_INVALID", $"Ungültiger DependencyType '{signalDependencyDefinition.DependencyType}'.", text + ".dependencyType");
			}
			if (signalDependencyDefinition.ResponseDelay < TimeSpan.Zero)
			{
				result.AddError("DEPENDENCY_RESPONSE_DELAY_NEGATIVE", "ResponseDelay darf nicht negativ sein.", text + ".responseDelay");
			}
			if (signalDependencyDefinition.ResponseInertia < 0.0)
			{
				result.AddError("DEPENDENCY_RESPONSE_INERTIA_NEGATIVE", "ResponseInertia darf nicht negativ sein.", text + ".responseInertia");
			}
			if (HasInvalidEffectRange(signalDependencyDefinition.MinimumEffect, signalDependencyDefinition.MaximumEffect))
			{
				result.AddError("DEPENDENCY_EFFECT_RANGE_INVALID", $"MinimumEffect ({signalDependencyDefinition.MinimumEffect}) muss kleiner oder gleich MaximumEffect ({signalDependencyDefinition.MaximumEffect}) sein.", text + ".minimumEffect");
			}
			string item = $"{signalDependencyDefinition.SourceStateId}|{signalDependencyDefinition.TargetSignalId}|{signalDependencyDefinition.DependencyType}|{signalDependencyDefinition.Weight}|{signalDependencyDefinition.Offset}";
			if (!string.IsNullOrWhiteSpace(signalDependencyDefinition.SourceStateId) && !string.IsNullOrWhiteSpace(signalDependencyDefinition.TargetSignalId) && !hashSet4.Add(item))
			{
				result.AddError("DEPENDENCY_DUPLICATE", $"Exakt doppelte Abhängigkeit von '{signalDependencyDefinition.SourceStateId}' zu '{signalDependencyDefinition.TargetSignalId}'.", text);
			}
		}
	}

	private static void ValidateHiddenStateDependencies(PhysicalMachineProfile profile, PhysicalProfileValidationResult result)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = (from s in profile.HiddenProcessStates
			select s.StateId into id
			where !string.IsNullOrWhiteSpace(id)
			select id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < profile.HiddenStateDependencies.Count; i++)
		{
			HiddenStateDependencyDefinition hiddenStateDependencyDefinition = profile.HiddenStateDependencies[i];
			string text = $"hiddenStateDependencies[{i}]";
			if (string.IsNullOrWhiteSpace(hiddenStateDependencyDefinition.DependencyId))
			{
				result.AddError("HIDDEN_DEPENDENCY_ID_MISSING", "DependencyId fehlt.", text + ".dependencyId");
			}
			else if (!hashSet.Add(hiddenStateDependencyDefinition.DependencyId))
			{
				result.AddError("HIDDEN_DEPENDENCY_ID_DUPLICATE", "Doppelte DependencyId '" + hiddenStateDependencyDefinition.DependencyId + "'.", text + ".dependencyId");
			}
			if (string.IsNullOrWhiteSpace(hiddenStateDependencyDefinition.SourceStateId))
			{
				result.AddError("HIDDEN_DEPENDENCY_SOURCE_MISSING", "SourceStateId fehlt.", text + ".sourceStateId");
			}
			else if (!hashSet2.Contains(hiddenStateDependencyDefinition.SourceStateId))
			{
				result.AddError("HIDDEN_DEPENDENCY_SOURCE_NOT_FOUND", "SourceStateId '" + hiddenStateDependencyDefinition.SourceStateId + "' existiert nicht.", text + ".sourceStateId");
			}
			if (string.IsNullOrWhiteSpace(hiddenStateDependencyDefinition.TargetStateId))
			{
				result.AddError("HIDDEN_DEPENDENCY_TARGET_MISSING", "TargetStateId fehlt.", text + ".targetStateId");
			}
			else if (!hashSet2.Contains(hiddenStateDependencyDefinition.TargetStateId))
			{
				result.AddError("HIDDEN_DEPENDENCY_TARGET_NOT_FOUND", "TargetStateId '" + hiddenStateDependencyDefinition.TargetStateId + "' existiert nicht.", text + ".targetStateId");
			}
			if (!Enum.IsDefined(hiddenStateDependencyDefinition.DependencyType))
			{
				result.AddError("HIDDEN_DEPENDENCY_TYPE_INVALID", $"Ungültiger DependencyType '{hiddenStateDependencyDefinition.DependencyType}'.", text + ".dependencyType");
			}
			if (hiddenStateDependencyDefinition.ResponseDelay < TimeSpan.Zero)
			{
				result.AddError("HIDDEN_DEPENDENCY_RESPONSE_DELAY_NEGATIVE", "ResponseDelay darf nicht negativ sein.", text + ".responseDelay");
			}
			if (hiddenStateDependencyDefinition.ResponseInertia < 0.0)
			{
				result.AddError("HIDDEN_DEPENDENCY_RESPONSE_INERTIA_NEGATIVE", "ResponseInertia darf nicht negativ sein.", text + ".responseInertia");
			}
			if (HasInvalidEffectRange(hiddenStateDependencyDefinition.MinimumEffect, hiddenStateDependencyDefinition.MaximumEffect))
			{
				result.AddError("HIDDEN_DEPENDENCY_EFFECT_RANGE_INVALID", $"MinimumEffect ({hiddenStateDependencyDefinition.MinimumEffect}) muss kleiner oder gleich MaximumEffect ({hiddenStateDependencyDefinition.MaximumEffect}) sein.", text + ".minimumEffect");
			}
			string item = $"{hiddenStateDependencyDefinition.SourceStateId}|{hiddenStateDependencyDefinition.TargetStateId}|{hiddenStateDependencyDefinition.DependencyType}|{hiddenStateDependencyDefinition.Weight}|{hiddenStateDependencyDefinition.Offset}";
			if (!string.IsNullOrWhiteSpace(hiddenStateDependencyDefinition.SourceStateId) && !string.IsNullOrWhiteSpace(hiddenStateDependencyDefinition.TargetStateId) && !hashSet3.Add(item))
			{
				result.AddError("HIDDEN_DEPENDENCY_DUPLICATE", $"Exakt doppelte Hidden-State-Abhängigkeit von '{hiddenStateDependencyDefinition.SourceStateId}' zu '{hiddenStateDependencyDefinition.TargetStateId}'.", text);
			}
		}
		ValidateHiddenStateCycles(profile, result);
	}

	private static void ValidateHiddenStateCycles(PhysicalMachineProfile profile, PhysicalProfileValidationResult result)
	{
		HashSet<string> allowedFeedback = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"ThermalLoad|CoolingEfficiency", "CoolingEfficiency|ThermalLoad", "MechanicalLoad|ProcessDemand", "ProcessDemand|MechanicalLoad", "PressLoad|ToolDeflection", "ToolDeflection|PressLoad", "StructuralThermalLoad|OilCondition", "OilCondition|StructuralThermalLoad", "PressLoad|HydraulicEfficiency", "HydraulicEfficiency|ValveResponse",
			"ValveResponse|PressLoad"
		};
		Dictionary<string, List<string>> graph = profile.HiddenStateDependencies.Where((HiddenStateDependencyDefinition d) => d.IsEnabled).GroupBy<HiddenStateDependencyDefinition, string>((HiddenStateDependencyDefinition d) => d.SourceStateId, StringComparer.OrdinalIgnoreCase).ToDictionary<IGrouping<string, HiddenStateDependencyDefinition>, string, List<string>>((IGrouping<string, HiddenStateDependencyDefinition> g) => g.Key, (IGrouping<string, HiddenStateDependencyDefinition> g) => g.Select((HiddenStateDependencyDefinition d) => d.TargetStateId).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);
		List<string> list = (from s in profile.HiddenProcessStates
			select s.StateId into id
			where !string.IsNullOrWhiteSpace(id)
			select id).ToList();
		HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			if (DetectCycle(item, graph, visited, stack, allowedFeedback, out List<string> cycle))
			{
				result.AddError("HIDDEN_STATE_CYCLE_UNSTABLE", "Instabile Hidden-State-Rückkopplung erkannt: " + string.Join(" -> ", cycle), "hiddenStateDependencies");
				break;
			}
		}
	}

	private static bool DetectCycle(string node, IReadOnlyDictionary<string, List<string>> graph, HashSet<string> visited, HashSet<string> stack, HashSet<string> allowedFeedback, out List<string> cycle)
	{
		cycle = new List<string>();
		if (!visited.Add(node))
		{
			return false;
		}
		stack.Add(node);
		if (graph.TryGetValue(node, out List<string> value))
		{
			foreach (string item in value)
			{
				List<string> value2;
				if (!stack.Contains(item))
				{
					if (DetectCycle(item, graph, visited, stack, allowedFeedback, out cycle))
					{
						cycle.Insert(0, node);
						return true;
					}
				}
				else if (!allowedFeedback.Contains(node + "|" + item) && graph.TryGetValue(item, out value2) && value2.Any((string n) => n.Equals(node, StringComparison.OrdinalIgnoreCase)))
				{
					int num = 3;
					List<string> list = new List<string>(num);
					CollectionsMarshal.SetCount(list, num);
					Span<string> span = CollectionsMarshal.AsSpan(list);
					span[0] = node;
					span[1] = item;
					span[2] = node;
					cycle = list;
					return true;
				}
			}
		}
		stack.Remove(node);
		return false;
	}

	private static bool HasInvalidEffectRange(double? minimum, double? maximum)
	{
		return minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value;
	}

	private static bool RequiresNumericRanges(PhysicalSignalDataType dataType)
	{
		if ((uint)dataType <= 3u)
		{
			return true;
		}
		return false;
	}

	private static void ValidateNumericRanges(SignalDefinition signal, string path, PhysicalProfileValidationResult result)
	{
		if (signal.NormalMinimum >= signal.NormalMaximum)
		{
			result.AddError("SIGNAL_NORMAL_RANGE_INVALID", $"NormalMinimum ({signal.NormalMinimum}) muss kleiner als NormalMaximum ({signal.NormalMaximum}) sein.", path + ".normalMinimum");
		}
		if (signal.HardMinimum > signal.NormalMinimum)
		{
			result.AddError("SIGNAL_HARD_MIN_INVALID", $"HardMinimum ({signal.HardMinimum}) muss kleiner oder gleich NormalMinimum ({signal.NormalMinimum}) sein.", path + ".hardMinimum");
		}
		if (signal.HardMaximum < signal.NormalMaximum)
		{
			result.AddError("SIGNAL_HARD_MAX_INVALID", $"HardMaximum ({signal.HardMaximum}) muss größer oder gleich NormalMaximum ({signal.NormalMaximum}) sein.", path + ".hardMaximum");
		}
		if (signal.NominalValue < signal.NormalMinimum || signal.NominalValue > signal.NormalMaximum)
		{
			result.AddError("SIGNAL_NOMINAL_OUT_OF_RANGE", $"NominalValue ({signal.NominalValue}) liegt außerhalb des Normalbereichs.", path + ".nominalValue");
		}
		if (signal.InitialValue < signal.HardMinimum || signal.InitialValue > signal.HardMaximum)
		{
			result.AddError("SIGNAL_INITIAL_OUT_OF_HARD_LIMITS", $"InitialValue ({signal.InitialValue}) liegt außerhalb der Hard Limits.", path + ".initialValue");
		}
	}

	private static void ValidateTechnicalBehavior(SignalDefinition signal, string path, PhysicalProfileValidationResult result)
	{
		switch (signal.TechnicalBehavior)
		{
		case TechnicalSignalBehavior.Counter:
		{
			PhysicalSignalDataType dataType = signal.DataType;
			if ((uint)(dataType - 2) > 1u)
			{
				result.AddError("SIGNAL_COUNTER_DATATYPE", "Counter erfordert Int32 oder Int64.", path + ".technicalBehavior");
			}
			if (signal.CounterStepSize <= 0)
			{
				result.AddError("SIGNAL_COUNTER_STEP_INVALID", "CounterStepSize muss größer als null sein.", path + ".counterStepSize");
			}
			break;
		}
		case TechnicalSignalBehavior.BooleanState:
			if (signal.DataType != PhysicalSignalDataType.Boolean)
			{
				result.AddError("SIGNAL_BOOLEAN_DATATYPE", "BooleanState erfordert Boolean.", path + ".technicalBehavior");
			}
			break;
		case TechnicalSignalBehavior.TextState:
			if (signal.DataType != PhysicalSignalDataType.String)
			{
				result.AddError("SIGNAL_TEXT_DATATYPE", "TextState erfordert String.", path + ".technicalBehavior");
			}
			break;
		case TechnicalSignalBehavior.Timestamp:
			if (signal.DataType != PhysicalSignalDataType.DateTime)
			{
				result.AddError("SIGNAL_TIMESTAMP_DATATYPE", "Timestamp erfordert DateTime.", path + ".technicalBehavior");
			}
			break;
		case TechnicalSignalBehavior.DiscreteState:
		{
			PhysicalSignalDataType dataType = signal.DataType;
			if ((uint)(dataType - 2) > 3u)
			{
				result.AddError("SIGNAL_DISCRETE_DATATYPE", "DiscreteState erfordert Int32, Int64, Boolean oder String.", path + ".technicalBehavior");
			}
			break;
		}
		}
		if (signal.AllowedValues.Any(string.IsNullOrWhiteSpace))
		{
			result.AddError("SIGNAL_ALLOWED_VALUE_EMPTY", "AllowedValues dürfen keine leeren Einträge enthalten.", path + ".allowedValues");
		}
	}

	private static string GetLogicalParent(string nodeId)
	{
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			return string.Empty;
		}
		int num = nodeId.LastIndexOf('.');
		return (num <= 0) ? string.Empty : nodeId.Substring(0, num);
	}

	private static string NormalizeVersion(string version)
	{
		int num = version.Count((char c) => c == '.');
		if (1 == 0)
		{
		}
		string result = num switch
		{
			0 => version + ".0.0", 
			1 => version + ".0", 
			_ => version, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
