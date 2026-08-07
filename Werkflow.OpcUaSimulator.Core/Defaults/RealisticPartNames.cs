using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public static class RealisticPartNames
{
	public static readonly string[] Names = (from j in FixedSimulationCatalog.CreateJobs()
		select j.PartName).ToArray();
}
