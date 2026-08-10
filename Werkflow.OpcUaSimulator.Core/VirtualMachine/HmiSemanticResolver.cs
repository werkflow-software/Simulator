using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

public sealed class HmiSemanticBinding
{
	public HmiSemantic Semantic { get; init; }

	public string? SignalId { get; init; }

	public double? NumericValue { get; init; }

	public string? TextValue { get; init; }

	public string FormattedValue { get; init; } = "—";

	public bool IsBound { get; init; }
}

public static class HmiSemanticResolver
{
	public static HmiSemanticBinding ResolveNumeric(
		HmiSemantic semantic,
		PhysicalMachineProfile profile,
		IReadOnlyDictionary<string, SignalRuntimeState> runtimeById)
	{
		foreach (string signalId in HmiSemanticRegistry.GetCandidateSignalIds(semantic))
		{
			SignalDefinition? definition = profile.Signals.FirstOrDefault(s =>
				s.IsEnabled && s.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase));
			if (definition == null)
			{
				continue;
			}

			double value = runtimeById.TryGetValue(definition.SignalId, out SignalRuntimeState? runtime)
				? runtime.CurrentValue
				: definition.InitialValue;

			return new HmiSemanticBinding
			{
				Semantic = semantic,
				SignalId = definition.SignalId,
				NumericValue = value,
				FormattedValue = HmiSignalCatalog.FormatValue(definition, value),
				IsBound = true
			};
		}

		return new HmiSemanticBinding { Semantic = semantic, IsBound = false };
	}

	public static HmiSemanticBinding ResolveFromRuntime(
		HmiSemantic semantic,
		MachineRuntimeState? runtime)
	{
		if (runtime == null)
		{
			return new HmiSemanticBinding { Semantic = semantic, IsBound = false };
		}

		switch (semantic)
		{
		case HmiSemantic.JobName:
			return BindText(semantic, runtime.JobName);
		case HmiSemantic.PartName:
			return BindText(semantic, runtime.PartName);
		case HmiSemantic.ActualCounter:
			return BindNumber(semantic, runtime.ActualCounter);
		case HmiSemantic.TargetCounter:
			return BindNumber(semantic, runtime.TargetCounter);
		case HmiSemantic.RemainingCounter:
			return BindNumber(semantic, Math.Max(0, runtime.TargetCounter - runtime.ActualCounter));
		case HmiSemantic.MachineState:
			return BindText(semantic, runtime.State.ToGermanLabel());
		case HmiSemantic.ErrorActive:
			return BindText(semantic, runtime.ErrorActive ? "Ja" : "Nein");
		case HmiSemantic.ErrorMessage:
			return BindText(semantic, runtime.ErrorMessage);
		case HmiSemantic.ProductionRunning:
			return BindText(semantic, runtime.IsProducing ? "Ja" : "Nein");
		default:
			return new HmiSemanticBinding { Semantic = semantic, IsBound = false };
		}
	}

	public static HmiSemanticBinding Resolve(
		HmiSemantic semantic,
		PhysicalMachineProfile? profile,
		IReadOnlyDictionary<string, SignalRuntimeState> runtimeById,
		MachineRuntimeState? machineRuntime)
	{
		if (semantic is HmiSemantic.JobName or HmiSemantic.PartName
			or HmiSemantic.ActualCounter or HmiSemantic.TargetCounter
			or HmiSemantic.RemainingCounter or HmiSemantic.MachineState
			or HmiSemantic.ErrorActive or HmiSemantic.ErrorMessage
			or HmiSemantic.ProductionRunning)
		{
			var runtimeBinding = ResolveFromRuntime(semantic, machineRuntime);
			if (runtimeBinding.IsBound)
			{
				return runtimeBinding;
			}
		}

		if (profile == null)
		{
			return new HmiSemanticBinding { Semantic = semantic, IsBound = false };
		}

		return ResolveNumeric(semantic, profile, runtimeById);
	}

	private static HmiSemanticBinding BindText(HmiSemantic semantic, string? text)
	{
		string value = string.IsNullOrWhiteSpace(text) ? "—" : text.Trim();
		return new HmiSemanticBinding
		{
			Semantic = semantic,
			TextValue = value,
			FormattedValue = value,
			IsBound = !string.IsNullOrWhiteSpace(text)
		};
	}

	private static HmiSemanticBinding BindNumber(HmiSemantic semantic, int value) =>
		new()
		{
			Semantic = semantic,
			NumericValue = value,
			FormattedValue = value.ToString(),
			IsBound = true
		};
}
