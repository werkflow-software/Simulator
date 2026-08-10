using System.Windows;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Services;

public sealed class VirtualMachineWindowService
{
	private readonly VirtualMachineHmiViewModel _viewModel;
	private readonly IHmiTrayNotifier _trayNotifier;
	private VirtualMachineHmiWindow? _window;

	public VirtualMachineWindowService(VirtualMachineHmiViewModel viewModel, IHmiTrayNotifier trayNotifier)
	{
		_viewModel = viewModel;
		_trayNotifier = trayNotifier;
	}

	public void ShowOrFocus(Window? owner)
	{
		_viewModel.EnsureActivated();

		if (_window == null)
		{
			_window = new VirtualMachineHmiWindow(_viewModel, _trayNotifier);
		}

		if (!_window.IsVisible)
		{
			_window.Show();
		}

		_window.ShowInTaskbar = true;
		_window.WindowState = WindowState.Normal;
		_window.Activate();
		_window.Focus();
	}

	public void HideToTray()
	{
		if (_window != null)
		{
			_window.Hide();
			_window.ShowInTaskbar = false;
		}
	}

	public bool IsOpen => _window != null && _window.IsVisible;

	public VirtualMachineHmiWindow? Window => _window;
}
