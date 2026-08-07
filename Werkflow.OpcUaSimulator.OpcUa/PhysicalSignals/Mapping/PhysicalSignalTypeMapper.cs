using System;
using System.Globalization;
using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Exceptions;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;

public sealed class PhysicalSignalTypeMapper : IPhysicalSignalTypeMapper
{
	public NodeId MapDataType(PhysicalSignalDataType dataType)
	{
		if (1 == 0)
		{
		}
		NodeId result = dataType switch
		{
			PhysicalSignalDataType.Double => DataTypeIds.Double, 
			PhysicalSignalDataType.Float => DataTypeIds.Float, 
			PhysicalSignalDataType.Int32 => DataTypeIds.Int32, 
			PhysicalSignalDataType.Int64 => DataTypeIds.Int64, 
			PhysicalSignalDataType.Boolean => DataTypeIds.Boolean, 
			PhysicalSignalDataType.String => DataTypeIds.String, 
			PhysicalSignalDataType.DateTime => DataTypeIds.DateTime, 
			_ => throw new PhysicalProfileException($"Nicht unterstützter physikalischer Datentyp: {dataType}"), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public object? ConvertToOpcValue(PhysicalSignalDataType dataType, object? value)
	{
		if (value == null)
		{
			return null;
		}
		try
		{
			if (1 == 0)
			{
			}
			object result = dataType switch
			{
				PhysicalSignalDataType.Double => Convert.ToDouble(value, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Float => Convert.ToSingle(value, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Int32 => Convert.ToInt32(value, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Int64 => Convert.ToInt64(value, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.Boolean => Convert.ToBoolean(value, CultureInfo.InvariantCulture), 
				PhysicalSignalDataType.String => value.ToString(), 
				PhysicalSignalDataType.DateTime => (value is DateTime dateTime) ? dateTime.ToUniversalTime() : DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), 
				_ => throw new PhysicalProfileException($"Nicht unterstützter physikalischer Datentyp: {dataType}"), 
			};
			if (1 == 0)
			{
			}
			return result;
		}
		catch (Exception ex)
		{
			throw new PhysicalProfileException($"Ungültiger Signalwert für {dataType}: {ex.Message}", (string?)null, dataType.ToString());
		}
	}

	public bool AreValuesEqual(PhysicalSignalDataType dataType, object? left, object? right)
	{
		if (left == null && right == null)
		{
			return true;
		}
		if (left == null || right == null)
		{
			return false;
		}
		object obj = ConvertToOpcValue(dataType, left);
		object obj2 = ConvertToOpcValue(dataType, right);
		if (1 == 0)
		{
		}
		bool result;
		switch (dataType)
		{
		case PhysicalSignalDataType.Double:
		case PhysicalSignalDataType.Float:
			result = Math.Abs(Convert.ToDouble(obj, CultureInfo.InvariantCulture) - Convert.ToDouble(obj2, CultureInfo.InvariantCulture)) < 1E-09;
			break;
		case PhysicalSignalDataType.String:
			result = string.Equals(obj?.ToString(), obj2?.ToString(), StringComparison.Ordinal);
			break;
		case PhysicalSignalDataType.Boolean:
			result = object.Equals(obj, obj2);
			break;
		case PhysicalSignalDataType.DateTime:
			result = ((DateTime)obj).Equals((DateTime)obj2);
			break;
		default:
			result = object.Equals(obj, obj2);
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}
}
