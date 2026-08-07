using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class TechnicalLearningMachine300R1ProfileTests
{
	private static readonly TechnicalSignalValueGenerator Generator = new TechnicalSignalValueGenerator();

	private static PhysicalMachineProfile Profile => TechnicalLearningMachine300ProfileFactory.Create();

	[Fact]
	public void Profile300_IsValid_AndInSignalRange()
	{
		PhysicalProfileValidationResult physicalProfileValidationResult = new PhysicalMachineProfileValidator().Validate(Profile);
		Assert.True(physicalProfileValidationResult.IsValid, string.Join("; ", physicalProfileValidationResult.Errors.Select((PhysicalProfileIssue e) => e.Message)));
		Assert.InRange(Profile.Signals.Count, 285, 320);
	}

	[Fact]
	public void Profile300_ContainsAllRequiredDataTypes()
	{
		HashSet<PhysicalSignalDataType> set = Profile.Signals.Select((SignalDefinition s) => s.DataType).Distinct().ToHashSet();
		Assert.Contains(PhysicalSignalDataType.Double, set);
		Assert.Contains(PhysicalSignalDataType.Float, set);
		Assert.Contains(PhysicalSignalDataType.Int32, set);
		Assert.Contains(PhysicalSignalDataType.Int64, set);
		Assert.Contains(PhysicalSignalDataType.Boolean, set);
		Assert.Contains(PhysicalSignalDataType.String, set);
		Assert.Contains(PhysicalSignalDataType.DateTime, set);
	}

	[Fact]
	public void Profile300_ContainsMinimumSemanticSignals()
	{
		Assert.True(Profile.Signals.Count((SignalDefinition s) => s.DataType == PhysicalSignalDataType.Boolean) >= 5);
		Assert.True(Profile.Signals.Count((SignalDefinition s) => s.DataType == PhysicalSignalDataType.String) >= 3);
		Assert.True(Profile.Signals.Count((SignalDefinition s) => s.DataType == PhysicalSignalDataType.DateTime) >= 3);
	}

	[Fact]
	public void Profile300_CounterSignals_UseIntegerTypes()
	{
		foreach (SignalDefinition item in Profile.Signals.Where((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.Counter))
		{
			PhysicalSignalDataType dataType = item.DataType;
			bool condition = (uint)(dataType - 2) <= 1u;
			Assert.True(condition, item.SignalId);
		}
	}

	[Fact]
	public void Profile300_BooleanState_UsesBooleanOnly()
	{
		foreach (SignalDefinition item in Profile.Signals.Where((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.BooleanState))
		{
			Assert.Equal(PhysicalSignalDataType.Boolean, item.DataType);
		}
	}

	[Fact]
	public void Profile300_TextState_UsesStringOnly()
	{
		foreach (SignalDefinition item in Profile.Signals.Where((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.TextState))
		{
			Assert.Equal(PhysicalSignalDataType.String, item.DataType);
		}
	}

	[Fact]
	public void Profile300_Timestamp_UsesDateTimeOnly()
	{
		foreach (SignalDefinition item in Profile.Signals.Where((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.Timestamp))
		{
			Assert.Equal(PhysicalSignalDataType.DateTime, item.DataType);
		}
	}

	[Fact]
	public void Profile300_NonNumericTypes_DoNotRequireNumericRanges()
	{
		PhysicalProfileValidationResult physicalProfileValidationResult = new PhysicalMachineProfileValidator().Validate(Profile);
		Assert.DoesNotContain((IEnumerable<PhysicalProfileIssue>)physicalProfileValidationResult.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "SIGNAL_NORMAL_RANGE_INVALID" && Profile.Signals.Any(delegate(SignalDefinition s)
		{
			PhysicalSignalDataType dataType = s.DataType;
			return (uint)(dataType - 4) <= 2u;
		})));
	}

	[Fact]
	public void Generator_Counter_IsMonotonicInteger()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.SignalId == "Production.CycleCounter");
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId,
			CurrentValue = signalDefinition.InitialValue
		};
		SignalRuntimeValueHelper.Initialize(signalDefinition, state, DateTimeOffset.UtcNow);
		long? num = null;
		for (long num2 = 1L; num2 <= 20; num2++)
		{
			long num3 = Convert.ToInt64(Generator.GenerateNextValue(signalDefinition, state, 42, num2));
			Assert.True(num3 % 1 == 0);
			if (num.HasValue)
			{
				Assert.True(num3 >= num.Value, $"Counter decreased at seq {num2}");
			}
			SignalRuntimeValueHelper.SetCurrentValue(signalDefinition, state, num3);
			num = num3;
		}
	}

	[Fact]
	public void Generator_Counter_DoesNotExceedHardMaximum()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.SignalId == "Production.CycleCounter");
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId,
			CurrentValue = signalDefinition.HardMaximum - 5.0
		};
		SignalRuntimeValueHelper.Initialize(signalDefinition, state, DateTimeOffset.UtcNow);
		for (long num = 1L; num <= 50; num++)
		{
			long num2 = Convert.ToInt64(Generator.GenerateNextValue(signalDefinition, state, 42, num));
			Assert.True((double)num2 <= signalDefinition.HardMaximum);
			SignalRuntimeValueHelper.SetCurrentValue(signalDefinition, state, num2);
		}
	}

	[Fact]
	public void Generator_Boolean_RemainsBoolean()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.BooleanState);
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId
		};
		SignalRuntimeValueHelper.Initialize(signalDefinition, state, DateTimeOffset.UtcNow);
		for (long num = 1L; num <= 30; num++)
		{
			object @object = Generator.GenerateNextValue(signalDefinition, state, 42, num);
			Assert.IsType<bool>(@object);
		}
	}

	[Fact]
	public void Generator_String_RemainsString()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.TextState);
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId
		};
		SignalRuntimeValueHelper.Initialize(signalDefinition, state, DateTimeOffset.UtcNow);
		for (long num = 1L; num <= 30; num++)
		{
			object @object = Generator.GenerateNextValue(signalDefinition, state, 42, num);
			Assert.IsType<string>(@object);
		}
	}

	[Fact]
	public void Generator_DateTime_IsUtc()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.Timestamp);
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId
		};
		SignalRuntimeValueHelper.Initialize(signalDefinition, state, DateTimeOffset.UtcNow);
		Assert.Equal(DateTimeKind.Utc, ((DateTime)Generator.GenerateNextValue(signalDefinition, state, 42, 30L)).Kind);
	}

	[Fact]
	public void Generator_Continuous_StaysInNormalRange()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.Continuous && s.DataType == PhysicalSignalDataType.Double);
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId,
			CurrentValue = signalDefinition.NominalValue
		};
		SignalRuntimeValueHelper.Initialize(signalDefinition, state, DateTimeOffset.UtcNow);
		for (long num = 1L; num <= 50; num++)
		{
			double actual = Convert.ToDouble(Generator.GenerateNextValue(signalDefinition, state, 42, num));
			Assert.InRange(actual, signalDefinition.NormalMinimum, signalDefinition.NormalMaximum);
		}
	}

	[Fact]
	public void Generator_SameSeed_ProducesSameSequence()
	{
		SignalDefinition signalDefinition = Profile.Signals.First((SignalDefinition s) => s.TechnicalBehavior == TechnicalSignalBehavior.Continuous);
		SignalRuntimeState state = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId,
			CurrentValue = signalDefinition.NominalValue
		};
		SignalRuntimeState state2 = new SignalRuntimeState
		{
			SignalId = signalDefinition.SignalId,
			CurrentValue = signalDefinition.NominalValue
		};
		object expected = Generator.GenerateNextValue(signalDefinition, state, 77, 5L);
		object actual = Generator.GenerateNextValue(signalDefinition, state2, 77, 5L);
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task Profile300_ExportsJson_AndReloads()
	{
		string path = Path.Combine(Path.GetTempPath(), "TechnicalLearningMachine300-test.json");
		await PhysicalMachineProfileJsonExporter.ExportToFileAsync(Profile, path);
		PhysicalMachineProfile loaded = await new JsonPhysicalMachineProfileLoader(new PhysicalMachineProfileValidator()).LoadFromFileAsync(path);
		Assert.InRange(loaded.Signals.Count, 285, 320);
		ProfileDistribution dist = PhysicalProfileStatistics.Analyze(loaded);
		Assert.True(dist.ByDataType.Count >= 7);
	}
}
