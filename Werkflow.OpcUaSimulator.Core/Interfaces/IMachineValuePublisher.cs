using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IMachineValuePublisher
{
	void PublishAll(Guid machineId, MachineRuntimeState state, IReadOnlyList<NodeMapping> nodes);

	void PublishValue(Guid machineId, NodeSemanticType semanticType, object? value, IReadOnlyList<NodeMapping> nodes);

	object? GetLiveValue(Guid machineId, NodeSemanticType semanticType);
}
