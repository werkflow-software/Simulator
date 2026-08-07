using System;
using System.IO;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

public static class PhysicalMachineProfilePaths
{
	public const string DefaultRelativeDirectory = "MachineProfiles";

	public const string ReferenceProfileFileName = "ReferenceMachine.json";

	public static string ResolveProfilesDirectory(string? baseDirectory = null)
	{
		string path = baseDirectory ?? AppContext.BaseDirectory;
		return Path.GetFullPath(Path.Combine(path, "MachineProfiles"));
	}

	public static string ResolveReferenceProfilePath(string? baseDirectory = null)
	{
		return Path.Combine(ResolveProfilesDirectory(baseDirectory), "ReferenceMachine.json");
	}
}
