using System;
using System.Globalization;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class SignalRuntimeValueHelper
{
	public static object? GetCurrentValue(SignalDefinition signal, SignalRuntimeState state)
	{
		PhysicalSignalDataType dataType = signal.DataType;
		if (1 == 0)
		{
		}
		object result = dataType switch
		{
			PhysicalSignalDataType.Boolean => state.CurrentBooleanValue ?? (state.CurrentValue >= 0.5), 
			PhysicalSignalDataType.String => state.CurrentStringValue ?? signal.InitialStringValue, 
			PhysicalSignalDataType.DateTime => state.CurrentDateTimeUtc ?? signal.InitialDateTimeUtc ?? DateTime.UtcNow, 
			PhysicalSignalDataType.Int32 => (int)Math.Round(state.CurrentValue, MidpointRounding.AwayFromZero), 
			PhysicalSignalDataType.Int64 => (long)Math.Round(state.CurrentValue, MidpointRounding.AwayFromZero), 
			PhysicalSignalDataType.Float => (float)state.CurrentValue, 
			_ => state.CurrentValue, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static void SetCurrentValue(SignalDefinition signal, SignalRuntimeState state, object? value)
	{
		state.PreviousValue = state.CurrentValue;
		switch (signal.DataType)
		{
		case PhysicalSignalDataType.Boolean:
		{
			bool flag = Convert.ToBoolean(value);
			state.CurrentBooleanValue = flag;
			state.CurrentValue = (flag ? 1 : 0);
			break;
		}
		case PhysicalSignalDataType.String:
			state.CurrentStringValue = value?.ToString() ?? string.Empty;
			break;
		case PhysicalSignalDataType.DateTime:
		{
			DateTime value2 = ((value is DateTime dateTime) ? dateTime.ToUniversalTime() : DateTime.Parse(value.ToString(), null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));
			state.CurrentDateTimeUtc = value2;
			state.CurrentValue = value2.Ticks;
			break;
		}
		case PhysicalSignalDataType.Int32:
			state.CurrentValue = Convert.ToInt32(value);
			break;
		case PhysicalSignalDataType.Int64:
			state.CurrentValue = Convert.ToInt64(value);
			break;
		case PhysicalSignalDataType.Float:
			state.CurrentValue = Convert.ToSingle(value);
			break;
		default:
			state.CurrentValue = Convert.ToDouble(value);
			break;
		}
		UpdateRangeFlags(signal, state);
	}

	public static void Initialize(SignalDefinition signal, SignalRuntimeState state, DateTimeOffset timestamp)
	{
		switch (signal.DataType)
		{
		case PhysicalSignalDataType.Boolean:
			state.CurrentBooleanValue = signal.InitialValue >= 0.5;
			state.CurrentValue = signal.InitialValue;
			break;
		case PhysicalSignalDataType.String:
			state.CurrentStringValue = (string.IsNullOrWhiteSpace(signal.InitialStringValue) ? signal.DisplayName : signal.InitialStringValue);
			break;
		case PhysicalSignalDataType.DateTime:
			state.CurrentDateTimeUtc = signal.InitialDateTimeUtc ?? timestamp.UtcDateTime;
			state.CurrentValue = state.CurrentDateTimeUtc.Value.Ticks;
			break;
		default:
			state.CurrentValue = signal.InitialValue;
			state.TargetValue = signal.InitialValue;
			state.PreviousValue = signal.InitialValue;
			break;
		}
		state.LastUpdatedAt = timestamp;
		state.LastChangedAt = timestamp;
		UpdateRangeFlags(signal, state);
	}

	private static void UpdateRangeFlags(SignalDefinition signal, SignalRuntimeState state)
	{
		PhysicalSignalDataType dataType = signal.DataType;
		if ((uint)(dataType - 4) <= 2u)
		{
			state.IsWithinNormalRange = true;
			state.IsWithinHardLimits = true;
		}
		else
		{
			state.IsWithinNormalRange = state.CurrentValue >= signal.NormalMinimum && state.CurrentValue <= signal.NormalMaximum;
			state.IsWithinHardLimits = state.CurrentValue >= signal.HardMinimum && state.CurrentValue <= signal.HardMaximum;
		}
	}
}
