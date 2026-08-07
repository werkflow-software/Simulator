using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;

public interface IPhysicalSignalTypeMapper
{
	NodeId MapDataType(PhysicalSignalDataType dataType);

	object? ConvertToOpcValue(PhysicalSignalDataType dataType, object? value);

	bool AreValuesEqual(PhysicalSignalDataType dataType, object? left, object? right);
}
