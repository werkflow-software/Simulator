using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public sealed class NodeMappingPresetInfo(string id, string label, Func<List<NodeMapping>> factory)
{
	public string Id { get; } = id;

	public string Label { get; } = label;

	public Func<List<NodeMapping>> Factory { get; } = factory;
}
