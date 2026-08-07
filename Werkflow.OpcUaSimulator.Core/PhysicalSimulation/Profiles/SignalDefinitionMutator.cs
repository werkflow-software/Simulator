using System;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

internal static class SignalDefinitionMutator
{
	public static SignalDefinition Copy(SignalDefinition source, Action<MutableSignalDefinition> configure)
	{
		MutableSignalDefinition mutableSignalDefinition = MutableSignalDefinition.From(source);
		configure(mutableSignalDefinition);
		return mutableSignalDefinition.ToDefinition();
	}
}
