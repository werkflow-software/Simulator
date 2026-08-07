using System;
using System.Globalization;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class TechnicalSignalValueGenerator
{
	public object? GenerateNextValue(SignalDefinition signal, SignalRuntimeState state, int seed, long sequence)
	{
		TechnicalSignalBehavior technicalBehavior = signal.TechnicalBehavior;
		if (1 == 0)
		{
		}
		object result = technicalBehavior switch
		{
			TechnicalSignalBehavior.Stable => SignalRuntimeValueHelper.GetCurrentValue(signal, state), 
			TechnicalSignalBehavior.Counter => GenerateCounter(signal, state), 
			TechnicalSignalBehavior.BooleanState => GenerateBoolean(signal, state, seed, sequence), 
			TechnicalSignalBehavior.TextState => GenerateText(signal, state, seed, sequence), 
			TechnicalSignalBehavior.Timestamp => GenerateTimestamp(signal, state, sequence), 
			TechnicalSignalBehavior.DiscreteState => GenerateDiscrete(signal, state, seed, sequence), 
			TechnicalSignalBehavior.SlowContinuous => GenerateContinuous(signal, state, seed, sequence, slow: true), 
			_ => GenerateContinuous(signal, state, seed, sequence, slow: false), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public void ResetCounters(PhysicalMachineProfile profile, PhysicalMachineRuntime runtime)
	{
		foreach (SignalDefinition signal in profile.Signals)
		{
			if (signal.TechnicalBehavior == TechnicalSignalBehavior.Counter)
			{
				SignalRuntimeState signalRuntimeState = runtime.Signals.First((SignalRuntimeState s) => s.SignalId == signal.SignalId);
				SignalRuntimeValueHelper.SetCurrentValue(signal, signalRuntimeState, signal.InitialValue);
				signalRuntimeState.UpdateSequence = 0L;
			}
		}
	}

	private static object GenerateCounter(SignalDefinition signal, SignalRuntimeState state)
	{
		int num = Math.Max(1, signal.CounterStepSize);
		long num2 = ((signal.DataType == PhysicalSignalDataType.Int64) ? ((long)Math.Round(state.CurrentValue)) : ((int)Math.Round(state.CurrentValue)));
		long num3 = num2 + num;
		if (signal.HardMaximum > 0.0)
		{
			num3 = Math.Min(num3, (long)signal.HardMaximum);
		}
		return (signal.DataType == PhysicalSignalDataType.Int64) ? num3 : ((int)num3);
	}

	private static object GenerateBoolean(SignalDefinition signal, SignalRuntimeState state, int seed, long sequence)
	{
		bool flag = state.CurrentBooleanValue ?? (state.CurrentValue >= 0.5);
		if (sequence % 500 != 0)
		{
			return flag;
		}
		int num = Hash(signal.SignalId, seed, sequence);
		return (num % 100 < 5) ? (!flag) : flag;
	}

	private static object GenerateText(SignalDefinition signal, SignalRuntimeState state, int seed, long sequence)
	{
		string[] array = signal.AllowedValues.Where((string v) => !string.IsNullOrWhiteSpace(v)).ToArray();
		if (array.Length == 0)
		{
			return string.IsNullOrWhiteSpace(state.CurrentStringValue) ? signal.InitialStringValue : state.CurrentStringValue;
		}
		if (sequence % 300 != 0)
		{
			return state.CurrentStringValue ?? array[0];
		}
		int num = Math.Abs(Hash(signal.SignalId, seed, sequence)) % array.Length;
		return array[num];
	}

	private static object GenerateTimestamp(SignalDefinition signal, SignalRuntimeState state, long sequence)
	{
		bool flag = signal.SignalId.Contains("Cycle", StringComparison.OrdinalIgnoreCase) || signal.SignalId.Contains("StateChange", StringComparison.OrdinalIgnoreCase);
		if (!flag && sequence % 120 != 0)
		{
			return state.CurrentDateTimeUtc ?? signal.InitialDateTimeUtc ?? DateTime.UtcNow;
		}
		if (flag && sequence % 30 != 0)
		{
			return state.CurrentDateTimeUtc ?? signal.InitialDateTimeUtc ?? DateTime.UtcNow;
		}
		return DateTime.UtcNow;
	}

	private static object GenerateDiscrete(SignalDefinition signal, SignalRuntimeState state, int seed, long sequence)
	{
		string[] array = signal.AllowedValues.Where((string v) => !string.IsNullOrWhiteSpace(v)).ToArray();
		object result;
		if (array.Length == 0)
		{
			PhysicalSignalDataType dataType = signal.DataType;
			if (1 == 0)
			{
			}
			result = dataType switch
			{
				PhysicalSignalDataType.Int32 => (int)Math.Round(state.CurrentValue), 
				PhysicalSignalDataType.Int64 => (long)Math.Round(state.CurrentValue), 
				PhysicalSignalDataType.Boolean => state.CurrentBooleanValue == true, 
				PhysicalSignalDataType.String => state.CurrentStringValue ?? signal.InitialStringValue, 
				_ => state.CurrentValue, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
		if (sequence % 200 != 0)
		{
			return SignalRuntimeValueHelper.GetCurrentValue(signal, state);
		}
		int num = Math.Abs(Hash(signal.SignalId, seed, sequence)) % array.Length;
		PhysicalSignalDataType dataType2 = signal.DataType;
		if (1 == 0)
		{
		}
		result = dataType2 switch
		{
			PhysicalSignalDataType.Int32 => int.Parse(array[num], CultureInfo.InvariantCulture), 
			PhysicalSignalDataType.Int64 => long.Parse(array[num], CultureInfo.InvariantCulture), 
			PhysicalSignalDataType.Boolean => Convert.ToBoolean(array[num], CultureInfo.InvariantCulture), 
			PhysicalSignalDataType.String => array[num], 
			_ => double.Parse(array[num], CultureInfo.InvariantCulture), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static object GenerateContinuous(SignalDefinition signal, SignalRuntimeState state, int seed, long sequence, bool slow)
	{
		int num = Hash(signal.SignalId, seed, sequence);
		double num2 = signal.NormalMaximum - signal.NormalMinimum;
		if (num2 <= 0.0)
		{
			return CastNumeric(signal, signal.NominalValue);
		}
		double num3 = (slow ? 0.003 : 0.01);
		double num4 = Math.Min(signal.NoiseAmplitude, num2 * num3);
		if (num4 <= 0.0)
		{
			num4 = num2 * num3;
		}
		double num5 = (double)(num % 2001 - 1000) / 1000.0 * num4;
		double value = signal.NominalValue + num5;
		value = Math.Clamp(value, signal.NormalMinimum, signal.NormalMaximum);
		value = Math.Clamp(value, signal.HardMinimum, signal.HardMaximum);
		return CastNumeric(signal, value);
	}

	private static object CastNumeric(SignalDefinition signal, double value)
	{
		PhysicalSignalDataType dataType = signal.DataType;
		if (1 == 0)
		{
		}
		double num = dataType switch
		{
			PhysicalSignalDataType.Int32 => (int)Math.Round(value, MidpointRounding.AwayFromZero), 
			PhysicalSignalDataType.Int64 => (long)Math.Round(value, MidpointRounding.AwayFromZero), 
			PhysicalSignalDataType.Float => (float)value, 
			_ => Math.Round(value, signal.DecimalPlaces, MidpointRounding.AwayFromZero), 
		};
		if (1 == 0)
		{
		}
		return num;
	}

	private static int Hash(string signalId, int seed, long sequence)
	{
		int num = seed;
		foreach (char c in signalId)
		{
			num = num * 31 + c;
		}
		num = num * 31 + (int)(sequence & 0x7FFFFFFF);
		return num ^ (int)(sequence >> 32);
	}

	public static bool TryConvertManualValue(string input, PhysicalSignalDataType dataType, out object? value, out string? error)
	{
		value = null;
		error = null;
		try
		{
			if (1 == 0)
			{
			}
			object obj = dataType switch
			{
				PhysicalSignalDataType.Boolean => Convert.ToBoolean(input, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Int32 => int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Int64 => long.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Float => float.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Double => double.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.DateTime => DateTime.Parse(input, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), 
				PhysicalSignalDataType.String => input, 
				_ => throw new NotSupportedException($"Datentyp {dataType} wird nicht unterstützt."), 
			};
			if (1 == 0)
			{
			}
			value = obj;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}
}
