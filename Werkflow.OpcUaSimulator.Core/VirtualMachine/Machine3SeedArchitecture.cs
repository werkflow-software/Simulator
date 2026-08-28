namespace Werkflow.OpcUaSimulator.Core.VirtualMachine;

/// <summary>
/// Deterministic seed hierarchy for Machine 3. Master seed is a stable FNV-1a hash of "M3BASE2026"
/// (not platform-dependent string.GetHashCode).
/// </summary>
public static class Machine3SeedArchitecture
{
	/// <summary>Stable integer representing the conceptual label M3BASE2026.</summary>
	public static readonly int MasterScenarioSeed = StableFnv1a32("M3BASE2026");

	public const uint PhysicalProcessXor = 0xA11CE001u;
	public const uint RobotVariabilityXor = 0xA0B07A01u;
	public const uint LogisticsAmrXor = 0xA4F1EE01u;
	public const uint VisionXor = 0xB1510001u;
	public const uint ThermalXor = 0x7E8EA001u;
	public const uint SignalNoiseXor = 0xA015E001u;
	public const uint IrrelevantSignalsXor = 0x1E8E1001u;
	public const uint SparseSignalsXor = 0x5FA25E01u;
	public const uint DropoutAvailabilityXor = 0xD0A0A701u;

	public static int PhysicalProcessSeed => Combine(MasterScenarioSeed, PhysicalProcessXor);
	public static int RobotVariabilitySeed => Combine(MasterScenarioSeed, RobotVariabilityXor);
	public static int LogisticsAmrSeed => Combine(MasterScenarioSeed, LogisticsAmrXor);
	public static int VisionSeed => Combine(MasterScenarioSeed, VisionXor);
	public static int ThermalSeed => Combine(MasterScenarioSeed, ThermalXor);
	public static int SignalNoiseSeed => Combine(MasterScenarioSeed, SignalNoiseXor);
	public static int IrrelevantSignalsSeed => Combine(MasterScenarioSeed, IrrelevantSignalsXor);
	public static int SparseSignalsSeed => Combine(MasterScenarioSeed, SparseSignalsXor);
	public static int DropoutAvailabilitySeed => Combine(MasterScenarioSeed, DropoutAvailabilityXor);

	public static int ProfileTierSeed(string profileName) => StableFnv1a32(profileName) ^ MasterScenarioSeed;

	public static int IrrelevantSlotSeed(int slotIndex) =>
		Combine(IrrelevantSignalsSeed, (uint)(slotIndex + 1) * 0x9E3779B9u);

	public static int StableFnv1a32(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		unchecked
		{
			uint hash = 2166136261u;
			foreach (char ch in text)
			{
				hash ^= ch;
				hash *= 16777619u;
			}

			return (int)hash;
		}
	}

	private static int Combine(int master, uint xor) => master ^ unchecked((int)xor);
}
