using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Validation;

public sealed class FaultScenarioValidator : IFaultScenarioValidator
{
	public FaultScenarioValidationResult ValidateCatalog(IReadOnlyList<FaultScenarioDefinition> scenarios)
	{
		FaultScenarioValidationResult result = new FaultScenarioValidationResult();
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (FaultScenarioDefinition scenario in scenarios)
		{
			ValidateDefinition(scenario, null, result, ids);
		}
		return result;
	}

	public FaultScenarioValidationResult ValidateForProfile(FaultScenarioDefinition scenario, PhysicalMachineProfile profile)
	{
		FaultScenarioValidationResult result = new FaultScenarioValidationResult();
		ValidateDefinition(scenario, profile, result, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		return result;
	}

	private static void ValidateDefinition(FaultScenarioDefinition scenario, PhysicalMachineProfile? profile, FaultScenarioValidationResult result, HashSet<string> ids)
	{
		string scenarioId = scenario.ScenarioId;
		if (string.IsNullOrWhiteSpace(scenarioId))
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "scenarioId",
				Message = "ScenarioId fehlt."
			});
			return;
		}
		if (ids.Contains(scenarioId))
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "scenarioId",
				Message = "Doppelte ScenarioId."
			});
		}
		else
		{
			ids.Add(scenarioId);
		}
		if (string.IsNullOrWhiteSpace(scenario.ScenarioVersion))
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "scenarioVersion",
				Message = "Version fehlt."
			});
		}
		if (string.IsNullOrWhiteSpace(scenario.DisplayName))
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "displayName",
				Message = "DisplayName fehlt."
			});
		}
		if (scenario.MachineProfileIds.Count == 0)
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "machineProfileIds",
				Message = "Keine kompatiblen Profile."
			});
		}
		if (profile != null && !scenario.MachineProfileIds.Any((string p) => p.Equals(profile.ProfileId, StringComparison.OrdinalIgnoreCase)))
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "machineProfileIds",
				Message = "Profil " + profile.ProfileId + " nicht kompatibel."
			});
		}
		if (scenario.MinimumDuration > scenario.MaximumDuration)
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "minimumDuration",
				Message = "MinimumDuration größer als MaximumDuration."
			});
		}
		if (scenario.MinimumIntensity > scenario.MaximumIntensity)
		{
			result.Errors.Add(new FaultScenarioValidationError
			{
				ScenarioId = scenarioId,
				FieldPath = "minimumIntensity",
				Message = "MinimumIntensity größer als MaximumIntensity."
			});
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (FaultEffectDefinition effect in scenario.Effects)
		{
			if (string.IsNullOrWhiteSpace(effect.EffectId))
			{
				result.Errors.Add(new FaultScenarioValidationError
				{
					ScenarioId = scenarioId,
					FieldPath = "effects",
					Message = "EffectId fehlt."
				});
				continue;
			}
			if (hashSet.Contains(effect.EffectId))
			{
				result.Errors.Add(new FaultScenarioValidationError
				{
					ScenarioId = scenarioId,
					FieldPath = "effects." + effect.EffectId,
					Message = "Doppelte EffectId."
				});
			}
			else
			{
				hashSet.Add(effect.EffectId);
			}
			if (effect.MinimumEffect > effect.MaximumEffect)
			{
				result.Errors.Add(new FaultScenarioValidationError
				{
					ScenarioId = scenarioId,
					FieldPath = "effects." + effect.EffectId,
					Message = "Effect-Minimum größer als Maximum."
				});
			}
			if (profile == null || effect.TargetType != 0 || profile.HiddenProcessStates.Any((HiddenProcessStateDefinition s) => s.StateId.Equals(effect.TargetId, StringComparison.OrdinalIgnoreCase)))
			{
				bool flag = profile != null;
				bool flag2 = flag;
				if (flag2)
				{
					FaultEffectTargetType targetType = effect.TargetType;
					bool flag3 = (uint)targetType <= 4u;
					flag2 = !flag3;
				}
				if (flag2 && !effect.TargetId.StartsWith("Machine.", StringComparison.OrdinalIgnoreCase) && !profile.Signals.All((SignalDefinition s) => !s.SignalId.Equals(effect.TargetId, StringComparison.OrdinalIgnoreCase)))
				{
				}
			}
		}
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (FaultThresholdRule rule in scenario.ThresholdRules)
		{
			if (string.IsNullOrWhiteSpace(rule.RuleId))
			{
				result.Errors.Add(new FaultScenarioValidationError
				{
					ScenarioId = scenarioId,
					FieldPath = "thresholdRules",
					Message = "RuleId fehlt."
				});
				continue;
			}
			if (hashSet2.Contains(rule.RuleId))
			{
				result.Errors.Add(new FaultScenarioValidationError
				{
					ScenarioId = scenarioId,
					FieldPath = "thresholdRules." + rule.RuleId,
					Message = "Doppelte RuleId."
				});
			}
			else
			{
				hashSet2.Add(rule.RuleId);
			}
			if ((profile == null || rule.SourceType != 0 || profile.HiddenProcessStates.Any((HiddenProcessStateDefinition s) => s.StateId.Equals(rule.SourceId, StringComparison.OrdinalIgnoreCase))) && profile != null && rule.SourceType == FaultThresholdSourceType.Signal && profile.Signals.Any((SignalDefinition s) => s.SignalId.Equals(rule.SourceId, StringComparison.OrdinalIgnoreCase)))
			{
			}
		}
		foreach (string mutuallyExclusiveScenarioId in scenario.MutuallyExclusiveScenarioIds)
		{
			if (mutuallyExclusiveScenarioId.Equals(scenarioId, StringComparison.OrdinalIgnoreCase))
			{
				result.Errors.Add(new FaultScenarioValidationError
				{
					ScenarioId = scenarioId,
					FieldPath = "mutuallyExclusiveScenarioIds",
					Message = "Szenario schließt sich selbst aus."
				});
			}
		}
	}
}
