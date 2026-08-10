using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Werkflow.OpcUaSimulator.App.ViewModels;
using Werkflow.OpcUaSimulator.App.Views;

namespace Werkflow.OpcUaSimulator.App;

public class MainWindow : Window, IComponentConnector
{
	private readonly MainViewModel _viewModel;
	private readonly ManualControlViewModel _manualControlViewModel;

	private bool _contentLoaded;

	public MainWindow(MainViewModel viewModel, OverviewViewModel overviewViewModel, ManualControlViewModel manualControlViewModel, ExperimentsViewModel experimentsViewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		base.DataContext = viewModel;
		_manualControlViewModel = manualControlViewModel;
		Resources.Add(typeof(ExperimentsViewModel), new ExperimentsView { DataContext = experimentsViewModel });
		overviewViewModel.ManualControlRequested += OnManualControlRequested;
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (Content is not Grid root || root.Children.Count == 0)
		{
			return;
		}

		var host = root.Children[0] as Panel ?? root;
		var button = new Button
		{
			Content = "Virtuelle Maschine",
			Margin = new Thickness(8),
			Padding = new Thickness(16, 8, 16, 8),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Command = _viewModel.OpenVirtualMachineCommand
		};
		host.Children.Add(button);
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
