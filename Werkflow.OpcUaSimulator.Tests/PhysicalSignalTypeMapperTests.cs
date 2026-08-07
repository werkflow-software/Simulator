using Opc.Ua;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Exceptions;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalSignalTypeMapperTests
{
	private readonly PhysicalSignalTypeMapper _mapper = new PhysicalSignalTypeMapper();

	[Fact]
	public void MapDataType_MapsAllSupportedTypes()
	{
		Assert.Equal(DataTypeIds.Double, _mapper.MapDataType(PhysicalSignalDataType.Double));
		Assert.Equal(DataTypeIds.Float, _mapper.MapDataType(PhysicalSignalDataType.Float));
		Assert.Equal(DataTypeIds.Int32, _mapper.MapDataType(PhysicalSignalDataType.Int32));
		Assert.Equal(DataTypeIds.Int64, _mapper.MapDataType(PhysicalSignalDataType.Int64));
		Assert.Equal(DataTypeIds.Boolean, _mapper.MapDataType(PhysicalSignalDataType.Boolean));
		Assert.Equal(DataTypeIds.String, _mapper.MapDataType(PhysicalSignalDataType.String));
		Assert.Equal(DataTypeIds.DateTime, _mapper.MapDataType(PhysicalSignalDataType.DateTime));
	}

	[Fact]
	public void MapDataType_UnsupportedType_Throws()
	{
		Assert.Throws<PhysicalProfileException>(() => _mapper.MapDataType((PhysicalSignalDataType)999));
	}
}
