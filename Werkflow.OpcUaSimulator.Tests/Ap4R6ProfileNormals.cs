using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;

namespace Werkflow.OpcUaSimulator.Tests;

/// <summary>
/// Profile-derived normal bands for AP-4-R6 recovery toward-normal evaluation.
/// </summary>
internal sealed record Ap4R6SignalNormalBand(
    double NormalMin,
    double NormalMax,
    double NominalValue)
{
    public double BandWidth => Math.Max(NormalMax - NormalMin, 1e-9);

    public static Ap4R6SignalNormalBand FromHidden(HiddenProcessStateDefinition definition) =>
        new(definition.NormalMinimum, definition.NormalMaximum, definition.NominalValue);

    public static Ap4R6SignalNormalBand FromSignal(SignalDefinition definition) =>
        new(definition.NormalMinimum, definition.NormalMaximum, definition.NominalValue);
}

internal static class Ap4R6ProfileNormals
{
    private static readonly Lazy<PhysicalMachineProfile> BendingProfile =
        new(BendingHydraulicMachine300ProfileFactory.Create);

    public static Ap4R6SignalNormalBand GetBendingBand(string signalOrHiddenId)
    {
        if (signalOrHiddenId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase))
        {
            var hidden = BendingProfile.Value.HiddenProcessStates.First(s =>
                s.StateId.Equals("HydraulicEfficiency", StringComparison.OrdinalIgnoreCase));
            return Ap4R6SignalNormalBand.FromHidden(hidden);
        }

        var signal = BendingProfile.Value.Signals.First(s =>
            s.SignalId.Equals(signalOrHiddenId, StringComparison.OrdinalIgnoreCase));
        return Ap4R6SignalNormalBand.FromSignal(signal);
    }

    public static Dictionary<string, Ap4R6SignalNormalBand> GetBendingHydraulicRecoveryBands() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["HydraulicEfficiency"] = GetBendingBand("HydraulicEfficiency"),
            ["Hydraulic.SupplyPressure"] = GetBendingBand("Hydraulic.SupplyPressure"),
            ["Hydraulic.PumpCurrent"] = GetBendingBand("Hydraulic.PumpCurrent")
        };
}
