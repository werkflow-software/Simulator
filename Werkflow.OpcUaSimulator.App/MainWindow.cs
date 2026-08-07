using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Werkflow.OpcUaSimulator.App.ViewModels;
using Werkflow.OpcUaSimulator.App.Views;

namespace Werkflow.OpcUaSimulator.App;

public class MainWindow : Window, IComponentConnector
{
	private readonly ManualControlViewModel _manualControlViewModel;

	private bool _contentLoaded;

	public MainWindow(MainViewModel viewModel, OverviewViewModel overviewViewModel, ManualControlViewModel manualControlViewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		_manualControlViewModel = manualControlViewModel;
		overviewViewModel.ManualControlRequested += OnManualControlRequested;
	}

	private void OnManualControlRequested(object? sender, Guid machineId)
	{
		_manualControlViewModel.LoadMachine(machineId);
		ManualControlWindow manualControlWindow = new ManualControlWindow(_manualControlViewModel)
		{
			Owner = this
		};
		manualControlWindow.ShowDialog();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Werkflow OPC UA Simulator;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
