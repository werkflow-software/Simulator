using System;
using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalMachineProfile
{
	public string ProfileId { get; init; } = string.Empty;

	public string ProfileVersion { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public string MachineType { get; init; } = string.Empty;

	public string Manufacturer { get; init; } = string.Empty;

	public TimeSpan DefaultUpdateInterval { get; init; } = TimeSpan.FromSeconds(1.0);

	public IReadOnlyList<SignalDefinition> Signals { get; init; } = Array.Empty<SignalDefinition>();

	public IReadOnlyList<HiddenProcessStateDefinition> HiddenProcessStates { get; init; } = Array.Empty<HiddenProcessStateDefinition>();

	public IReadOnlyList<SignalDependencyDefinition> Dependencies { get; init; } = Array.Empty<SignalDependencyDefinition>();

	public IReadOnlyList<HiddenStateDependencyDefinition> HiddenStateDependencies { get; init; } = Array.Empty<HiddenStateDependencyDefinition>();

	public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
