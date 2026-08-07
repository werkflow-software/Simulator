using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public static class PhysicalVerificationSettings
{
	public static bool IsShortMode => string.Equals(Environment.GetEnvironmentVariable("PHYSICS_VERIFY_SHORT"), "1", StringComparison.Ordinal);

	public static bool IsFullMode => string.Equals(Environment.GetEnvironmentVariable("PHYSICS_VERIFY_FULL"), "1", StringComparison.Ordinal);

	public static bool IsExportMode => string.Equals(Environment.GetEnvironmentVariable("PHYSICS_VERIFY_EXPORT"), "1", StringComparison.Ordinal);

	public static PhysicalVerificationMode VerificationMode => IsShortMode ? PhysicalVerificationMode.Short : PhysicalVerificationMode.Normal;

	public static TimeSpan IntegrationRunDuration => IsShortMode ? TimeSpan.FromMinutes(5.0) : (IsFullMode ? TimeSpan.FromMinutes(30.0) : TimeSpan.FromSeconds(90.0));

	public static double ShortModeTimeFactor => 5.0;
}
