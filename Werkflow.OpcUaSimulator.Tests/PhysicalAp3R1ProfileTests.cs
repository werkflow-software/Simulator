using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalAp3R1ProfileTests
{
	private static readonly PhysicalMachineProfileValidator Validator = new PhysicalMachineProfileValidator();

	[Theory]
	[InlineData(new object[] { "laser" })]
	[InlineData(new object[] { "bending" })]
	public void Profile_SignalCount_IsWithinTargetRange(string kind)
	{
		PhysicalMachineProfile physicalMachineProfile = ((kind == "laser") ? LaserProcessingMachine300ProfileFactory.Create() : BendingHydraulicMachine300ProfileFactory.Create());
		Assert.InRange(physicalMachineProfile.Signals.Count, 285, 320);
	}

	[Theory]
	[InlineData(new object[] { "laser" })]
	[InlineData(new object[] { "bending" })]
	public void Profile_AllDependencyTargetsExist(string kind)
	{
		PhysicalMachineProfile physicalMachineProfile = ((kind == "laser") ? LaserProcessingMachine300ProfileFactory.Create() : BendingHydraulicMachine300ProfileFactory.Create());
		HashSet<string> signalIds = physicalMachineProfile.Signals.Select((SignalDefinition s) => s.SignalId).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> stateIds = physicalMachineProfile.HiddenProcessStates.Select((HiddenProcessStateDefinition s) => s.StateId).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		Assert.All(physicalMachineProfile.Dependencies, delegate(SignalDependencyDefinition d)
		{
			Assert.Contains(d.SourceStateId, stateIds);
		});
		Assert.All(physicalMachineProfile.Dependencies, delegate(SignalDependencyDefinition d)
		{
			Assert.Contains(d.TargetSignalId, signalIds);
		});
		Assert.All(physicalMachineProfile.HiddenStateDependencies, delegate(HiddenStateDependencyDefinition d)
		{
			Assert.Contains(d.SourceStateId, stateIds);
		});
		Assert.All(physicalMachineProfile.HiddenStateDependencies, delegate(HiddenStateDependencyDefinition d)
		{
			Assert.Contains(d.TargetStateId, stateIds);
		});
	}

	[Theory]
	[InlineData(new object[] { "laser" })]
	[InlineData(new object[] { "bending" })]
	public void Profile_HasMinimumHiddenStateDependencies(string kind)
	{
		PhysicalMachineProfile physicalMachineProfile = ((kind == "laser") ? LaserProcessingMachine300ProfileFactory.Create() : BendingHydraulicMachine300ProfileFactory.Create());
		Assert.True(physicalMachineProfile.HiddenStateDependencies.Count >= 15);
	}

	[Theory]
	[InlineData(new object[] { "laser" })]
	[InlineData(new object[] { "bending" })]
	public void Profile_UsesAtLeastFiveDependencyTypes(string kind)
	{
		PhysicalMachineProfile physicalMachineProfile = ((kind == "laser") ? LaserProcessingMachine300ProfileFactory.Create() : BendingHydraulicMachine300ProfileFactory.Create());
		int num = physicalMachineProfile.Dependencies.Select((SignalDependencyDefinition d) => d.DependencyType).Concat(physicalMachineProfile.HiddenStateDependencies.Select((HiddenStateDependencyDefinition d) => d.DependencyType)).Distinct()
			.Count();
		Assert.True(num >= 5, $"Only {num} types used.");
	}

	[Fact]
	public void BendingProfile_KeepsHydraulicFilterLoadSignal()
	{
		PhysicalMachineProfile physicalMachineProfile = BendingHydraulicMachine300ProfileFactory.Create();
		Assert.Contains((IEnumerable<SignalDefinition>)physicalMachineProfile.Signals, (Predicate<SignalDefinition>)((SignalDefinition s) => s.SignalId == "Hydraulic.FilterLoad"));
		Assert.Contains((IEnumerable<SignalDependencyDefinition>)physicalMachineProfile.Dependencies, (Predicate<SignalDependencyDefinition>)((SignalDependencyDefinition d) => d.TargetSignalId == "Hydraulic.FilterLoad"));
	}

	[Fact]
	public void EffectLimits_NullMeansNoClamp()
	{
		double actual = DependencyEvaluator.Evaluate(DependencyType.Linear, 0.8, 10.0, 2.0, null, null, 0.0, null);
		Assert.Equal(10.0, actual, 3);
	}

	[Fact]
	public void EffectLimits_MinimumLimitsNegativeContribution()
	{
		double actual = DependencyEvaluator.ApplyEffectLimits(-5.0, -1.0, null);
		Assert.Equal(-1.0, actual);
	}

	[Fact]
	public void EffectLimits_MaximumLimitsPositiveContribution()
	{
		double actual = DependencyEvaluator.ApplyEffectLimits(12.0, null, 8.0);
		Assert.Equal(8.0, actual);
	}

	[Fact]
	public void EffectLimits_ZeroIsActualZeroNotUnlimited()
	{
		double actual = DependencyEvaluator.ApplyEffectLimits(5.0, 0.0, 0.0);
		Assert.Equal(0.0, actual);
	}

	[Fact]
	public void EffectLimits_InvalidRange_IsRejectedByValidator()
	{
		PhysicalMachineProfile physicalMachineProfile = LaserProcessingMachine300ProfileFactory.Create();
		PhysicalMachineProfile profile = new PhysicalMachineProfile
		{
			ProfileId = physicalMachineProfile.ProfileId,
			ProfileVersion = physicalMachineProfile.ProfileVersion,
			DisplayName = physicalMachineProfile.DisplayName,
			MachineType = physicalMachineProfile.MachineType,
			Manufacturer = physicalMachineProfile.Manufacturer,
			DefaultUpdateInterval = physicalMachineProfile.DefaultUpdateInterval,
			Signals = physicalMachineProfile.Signals,
			HiddenProcessStates = physicalMachineProfile.HiddenProcessStates,
			Dependencies = new[]
			{
				new SignalDependencyDefinition
				{
					DependencyId = "broken",
					SourceStateId = physicalMachineProfile.HiddenProcessStates[0].StateId,
					TargetSignalId = physicalMachineProfile.Signals[0].SignalId,
					MinimumEffect = 2.0,
					MaximumEffect = 1.0
				}
			},
			HiddenStateDependencies = physicalMachineProfile.HiddenStateDependencies
		};
		PhysicalProfileValidationResult physicalProfileValidationResult = Validator.Validate(profile);
		Assert.False(physicalProfileValidationResult.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)physicalProfileValidationResult.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "DEPENDENCY_EFFECT_RANGE_INVALID"));
	}

	[Fact]
	public void BendingHiddenState_UsesAbstractThermalName()
	{
		PhysicalMachineProfile physicalMachineProfile = BendingHydraulicMachine300ProfileFactory.Create();
		Assert.Contains((IEnumerable<HiddenProcessStateDefinition>)physicalMachineProfile.HiddenProcessStates, (Predicate<HiddenProcessStateDefinition>)((HiddenProcessStateDefinition s) => s.StateId == "StructuralThermalLoad"));
		Assert.DoesNotContain((IEnumerable<HiddenProcessStateDefinition>)physicalMachineProfile.HiddenProcessStates, (Predicate<HiddenProcessStateDefinition>)((HiddenProcessStateDefinition s) => s.StateId == "FrameTemperature"));
		Assert.Contains((IEnumerable<SignalDefinition>)physicalMachineProfile.Signals, (Predicate<SignalDefinition>)((SignalDefinition s) => s.SignalId == "Thermal.FrameTemperature"));
	}

	[Fact]
	public void Profiles_ValidateSuccessfully()
	{
		PhysicalMachineProfile[] array = new PhysicalMachineProfile[2]
		{
			LaserProcessingMachine300ProfileFactory.Create(),
			BendingHydraulicMachine300ProfileFactory.Create()
		};
		foreach (PhysicalMachineProfile profile in array)
		{
			PhysicalProfileValidationResult physicalProfileValidationResult = Validator.Validate(profile);
			Assert.True(physicalProfileValidationResult.IsValid, string.Join("; ", physicalProfileValidationResult.Errors.Select((PhysicalProfileIssue e) => e.Message)));
		}
	}
}
