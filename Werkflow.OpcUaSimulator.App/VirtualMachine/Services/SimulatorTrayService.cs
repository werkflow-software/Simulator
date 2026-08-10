using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.Interfaces;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Services;

/// <summary>
/// System tray integration: keeps simulator alive when HMI is hidden and exposes explicit shutdown paths.
/// </summary>
public sealed class SimulatorTrayService : IHmiTrayNotifier, IDisposable
{
	private readonly Func<VirtualMachineWindowService> _virtualMachineWindowServiceFactory;
	private readonly VirtualMachineHmiViewModel _hmiViewModel;
	private readonly IDialogService _dialogService;
	private NotifyIcon? _notifyIcon;
	private bool _disposed;

	public SimulatorTrayService(
		Func<VirtualMachineWindowService> virtualMachineWindowServiceFactory,
		VirtualMachineHmiViewModel hmiViewModel,
		IDialogService dialogService)
	{
		_virtualMachineWindowServiceFactory = virtualMachineWindowServiceFactory;
		_hmiViewModel = hmiViewModel;
		_dialogService = dialogService;
	}

	public void EnsureInitialized()
	{
		if (_notifyIcon != null)
		{
			return;
		}

		_notifyIcon = new NotifyIcon
		{
			Text = "Werkflow OPC UA Simulator",
			Icon = LoadTrayIcon(),
			Visible = true
		};

		var menu = new ContextMenuStrip();
		menu.Items.Add("Virtuelle Maschine öffnen", null, (_, _) => OpenVirtualMachine());
		menu.Items.Add("Simulator öffnen", null, (_, _) => OpenMainWindow());
		menu.Items.Add("Maschine beenden", null, async (_, _) => await ShutdownMachineAsync());
		menu.Items.Add("Beenden", null, (_, _) => ExitApplication());
		_notifyIcon.ContextMenuStrip = menu;
		_notifyIcon.DoubleClick += (_, _) => OpenVirtualMachine();
	}

	public void NotifyHmiHidden()
	{
		EnsureInitialized();
		_notifyIcon!.Visible = true;
	}

	public void NotifyMainWindowHidden()
	{
		EnsureInitialized();
		_notifyIcon!.Visible = true;
	}

	public void OpenVirtualMachine()
	{
		EnsureInitialized();
		UiDispatcher.Run(() => _virtualMachineWindowServiceFactory().ShowOrFocus(null));
	}

	public void OpenMainWindow()
	{
		EnsureInitialized();
		UiDispatcher.Run(() =>
		{
			if (System.Windows.Application.Current?.MainWindow is Window main)
			{
				main.ShowInTaskbar = true;
				main.Visibility = Visibility.Visible;
				main.WindowState = WindowState.Normal;
				main.Show();
				main.Activate();
				main.Focus();
			}
		});
	}

	private async Task ShutdownMachineAsync()
	{
		await _hmiViewModel.ShutdownMachineCommand.ExecuteAsync(null);
	}

	private void ExitApplication()
	{
		bool confirmed = _dialogService.ShowConfirmation(
			"Beenden",
			"Werkflow OPC UA Simulator wirklich beenden?\nLaufende Maschinen und OPC-UA-Server werden gestoppt.");
		if (!confirmed)
		{
			return;
		}

		UiDispatcher.Run(() => System.Windows.Application.Current?.Shutdown());
	}

	private static Icon LoadTrayIcon()
	{
		try
		{
			string exePath = Environment.ProcessPath ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(exePath))
			{
				return Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application;
			}
		}
		catch
		{
			// fallback below
		}

		return SystemIcons.Application;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		if (_notifyIcon != null)
		{
			_notifyIcon.Visible = false;
			_notifyIcon.Dispose();
			_notifyIcon = null;
		}
	}
}
