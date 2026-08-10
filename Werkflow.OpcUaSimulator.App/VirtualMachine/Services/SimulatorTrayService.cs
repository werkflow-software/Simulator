using System.Windows;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Services;

/// <summary>
/// Lightweight tray substitute without WinForms: restores HMI from hidden state.
/// </summary>
public sealed class SimulatorTrayService : IHmiTrayNotifier, IDisposable
{
	private readonly VirtualMachineWindowService _virtualMachineWindowService;
	private bool _disposed;

	public SimulatorTrayService(VirtualMachineWindowService virtualMachineWindowService)
	{
		_virtualMachineWindowService = virtualMachineWindowService;
	}

	public void NotifyHmiHidden()
	{
		// HMI hidden; machine continues running. Tray menu is available via main window.
	}

	public void OpenVirtualMachine()
	{
		_virtualMachineWindowService.ShowOrFocus(System.Windows.Application.Current?.MainWindow);
	}

	public void Dispose()
	{
		_disposed = true;
	}
}
