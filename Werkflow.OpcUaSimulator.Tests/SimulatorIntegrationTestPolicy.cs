using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

/// <summary>
/// Gates legacy Machine-1/2 OPC integration and long-running E2E harness tests.
/// Not required for RUN-021 Machine-3 EXPANDED48 execution.
/// </summary>
internal static class SimulatorIntegrationTestPolicy
{
	internal const string EnableEnvironmentVariable = "SIMULATOR_INTEGRATION_E2E";

	internal static bool IsEnabled =>
		string.Equals(Environment.GetEnvironmentVariable(EnableEnvironmentVariable), "1", StringComparison.Ordinal);

	internal static string SkipReason =>
		$"Requires {EnableEnvironmentVariable}=1 for legacy Machine-1/2 OPC integration verification (not used by RUN-021).";
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class Machine12IntegrationFactAttribute : FactAttribute
{
	public override string? Skip => SimulatorIntegrationTestPolicy.IsEnabled ? null : SimulatorIntegrationTestPolicy.SkipReason;
}
