using System;
using System.IO;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios;

public static class FaultScenarioPaths
{
	public const string DefaultRelativeDirectory = "FaultScenarios";

	public static string ResolveDirectory(string? baseDirectory = null)
	{
		string path = baseDirectory ?? AppContext.BaseDirectory;
		return Path.GetFullPath(Path.Combine(path, "FaultScenarios"));
	}
}
