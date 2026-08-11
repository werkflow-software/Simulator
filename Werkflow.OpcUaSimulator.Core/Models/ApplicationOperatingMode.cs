namespace Werkflow.OpcUaSimulator.Core.Models;

/// <summary>
/// Host-level operating mode for a single application session.
/// Determines which runtime and main window are active — not physics or signal generation.
/// </summary>
public enum ApplicationOperatingMode
{
	ClassicSimulator,
	VirtualMachine
}
