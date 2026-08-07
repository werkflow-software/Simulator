using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Werkflow.OpcUaSimulator.App.ViewModels;

namespace Werkflow.OpcUaSimulator.App.Views;

public class ManualControlWindow : Window, IComponentConnector
{
	private bool _contentLoaded;

	public ManualControlWindow(ManualControlViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Werkflow OPC UA Simulator;component/views/manualcontrolwindow.xaml", UriKind.Relative);
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
