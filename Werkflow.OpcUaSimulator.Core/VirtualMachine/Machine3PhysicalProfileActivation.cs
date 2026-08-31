using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic Machine-3 physical profile activation for VIGIL LAB scale runs.
/// </summary>
public static class Machine3PhysicalProfileActivation
{
	public const string EnvironmentVariableName = "WERKFLOW_MACHINE3_PHYSICAL_PROFILE_ID";

	public static void Apply(IList<MachineConfiguration> machines)
	{
		string? requested = Environment.GetEnvironmentVariable(EnvironmentVariableName);
		if (string.IsNullOrWhiteSpace(requested))
		{
			return;
		}

		MachineConfiguration? machine = machines.FirstOrDefault(m =>
			m.Id == VirtualAutonomousProductionCellContract.MachineId
			|| m.Port == VirtualAutonomousProductionCellContract.Port);
		if (machine is null)
		{
			return;
		}

		string normalized = requested.Trim();
		if (!IsSupportedProfileId(normalized))
		{
			throw new InvalidOperationException(
				$"Unsupported {EnvironmentVariableName}='{normalized}'. " +
				$"Supported values: {VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24}, " +
				$"{VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48}, " +
				$"{VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96}.");
		}

		machine.PhysicalProfileId = normalized;
	}

	public static bool IsSupportedProfileId(string profileId) =>
		profileId.Equals(VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24, StringComparison.OrdinalIgnoreCase)
		|| profileId.Equals(VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48, StringComparison.OrdinalIgnoreCase)
		|| profileId.Equals(VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96, StringComparison.OrdinalIgnoreCase);

	public static int ResolveEnabledSignalCount(string? physicalProfileId)
	{
		if (physicalProfileId?.Equals(
			    VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96,
			    StringComparison.OrdinalIgnoreCase) == true)
		{
			return VigilAutonomousCellProfileFactory.CreateScale96().Signals.Count(s => s.IsEnabled);
		}

		if (physicalProfileId?.Equals(
			    VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48,
			    StringComparison.OrdinalIgnoreCase) == true)
		{
			return VigilAutonomousCellProfileFactory.CreateExpanded48().Signals.Count(s => s.IsEnabled);
		}

		return VigilAutonomousCellProfileFactory.CreateCore24().Signals.Count(s => s.IsEnabled);
	}

	public static string ResolveOperatorProfileLabel(string? physicalProfileId)
	{
		if (physicalProfileId?.Equals(
			    VirtualAutonomousProductionCellContract.PhysicalProfileIdScale96,
			    StringComparison.OrdinalIgnoreCase) == true)
		{
			return "SCALE96";
		}

		if (physicalProfileId?.Equals(
			    VirtualAutonomousProductionCellContract.PhysicalProfileIdExpanded48,
			    StringComparison.OrdinalIgnoreCase) == true)
		{
			return "EXPANDED48";
		}

		if (physicalProfileId?.Equals(
			    VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24,
			    StringComparison.OrdinalIgnoreCase) == true)
		{
			return "CORE24";
		}

		return physicalProfileId ?? "UNKNOWN";
	}
}
