using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Models;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

public sealed class VirtualMachineHmiWindow : Window
{
	private readonly VirtualMachineHmiViewModel _viewModel;
	private readonly IHmiTrayNotifier _trayNotifier;
	private readonly TabControl _tabControl;

	public VirtualMachineHmiWindow(VirtualMachineHmiViewModel viewModel, IHmiTrayNotifier trayNotifier)
	{
		_viewModel = viewModel;
		_trayNotifier = trayNotifier;
		Title = "Virtuelle Maschine";
		MinWidth = 1280;
		MinHeight = 720;
		Width = 1600;
		Height = 900;
		Background = new SolidColorBrush(Color.FromRgb(28, 32, 38));
		DataContext = viewModel;

		var root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(88) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		root.Children.Add(BuildHeader());
		_tabControl = BuildTabs();
		root.Children.Add(_tabControl);

		Content = root;
	}

	protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
	{
		e.Cancel = true;
		Hide();
		ShowInTaskbar = false;
		_trayNotifier.NotifyHmiHidden();
	}

	private UIElement BuildHeader()
	{
		var panel = new Border
		{
			Background = new SolidColorBrush(Color.FromRgb(20, 24, 30)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 78)),
			BorderThickness = new Thickness(0, 0, 0, 1),
			Padding = new Thickness(16, 10, 16, 10)
		};

		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var left = new StackPanel();
		left.Children.Add(MakeHeaderText(_viewModel.MachineTitle, 22, FontWeights.Bold));
		left.Children.Add(MakeBoundText("MachineStateText", 16));
		left.Children.Add(MakeBoundText("CounterText", 14));
		grid.Children.Add(left);

		var center = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
		center.Children.Add(MakeBoundText("JobName", 14, "Job: {0}"));
		center.Children.Add(MakeBoundText("PartName", 14, "Teil: {0}"));
		center.Children.Add(MakeBoundText("OpcUaStatus", 14, "OPC UA: {0}"));
		Grid.SetColumn(center, 1);
		grid.Children.Add(center);

		var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
		right.Children.Add(MakeBoundText("ClockText", 14));
		right.Children.Add(MakeButton("Vollbild", (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
		right.Children.Add(MakeButton("Maschine beenden", async (_, _) => await _viewModel.ShutdownMachineCommand.ExecuteAsync(null)));
		Grid.SetColumn(right, 2);
		grid.Children.Add(right);

		panel.Child = grid;
		Grid.SetRow(panel, 0);
		return panel;
	}

	private TabControl BuildTabs()
	{
		var tabs = new TabControl
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0),
			Margin = new Thickness(8)
		};

		tabs.Items.Add(CreateOverviewTab());
		foreach (var tab in _viewModel.Tabs)
		{
			tabs.Items.Add(CreateSignalTab(tab));
		}
		tabs.Items.Add(CreateDiagnosticsTab());

		Grid.SetRow(tabs, 1);
		return tabs;
	}

	private TabItem CreateOverviewTab()
	{
		var item = new TabItem { Header = "Übersicht" };
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var stack = new StackPanel { Margin = new Thickness(8) };

		stack.Children.Add(MakeSectionTitle("Maschinenbedienung"));
		var controls = new WrapPanel();
		controls.Children.Add(MakeCommandButton("Maschine starten", _viewModel.StartMachineCommand));
		controls.Children.Add(MakeCommandButton("Start", _viewModel.StartProductionCommand));
		controls.Children.Add(MakeCommandButton("Stop", _viewModel.StopProductionCommand));
		controls.Children.Add(MakeCommandButton("Pause", _viewModel.PauseProductionCommand));
		controls.Children.Add(MakeCommandButton("Resume", _viewModel.ResumeProductionCommand));
		controls.Children.Add(MakeCommandButton("Reset", _viewModel.ResetMachineCommand));
		controls.Children.Add(MakeCommandButton("Normalbetrieb", _viewModel.NormalOperationCommand));
		stack.Children.Add(controls);

		if (_viewModel.HasActiveTestScenario)
		{
			stack.Children.Add(MakeSectionTitle("TEST SCENARIO ACTIVE"));
		}
		stack.Children.Add(MakeBoundText("ActiveTestScenario", 14));

		stack.Children.Add(MakeSectionTitle("Hauptwerte"));
		stack.Children.Add(CreateSignalGrid(_viewModel.OverviewSignals));

		scroll.Content = stack;
		item.Content = scroll;
		return item;
	}

	private TabItem CreateSignalTab(HmiTabContent tab)
	{
		var item = new TabItem { Header = tab.Title };
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var panel = new StackPanel { Margin = new Thickness(8) };

		if (tab.AxisPanels.Count > 0)
		{
			foreach (var axis in tab.AxisPanels)
			{
				panel.Children.Add(MakeSectionTitle(axis.AxisName));
				panel.Children.Add(CreateSignalGrid(axis.Signals));
			}
		}
		else
		{
			panel.Children.Add(CreateSignalGrid(tab.Signals));
		}

		scroll.Content = panel;
		item.Content = scroll;
		return item;
	}

	private TabItem CreateDiagnosticsTab()
	{
		var item = new TabItem { Header = "Fehler / Diagnose" };
		var scroll = new ScrollViewer();
		var stack = new StackPanel { Margin = new Thickness(8) };

		stack.Children.Add(MakeSectionTitle("Maschinenmeldungen"));
		stack.Children.Add(MakeBoundText("ErrorMessage", 14, "Fehler: {0}"));
		stack.Children.Add(MakeBoundText("MachineStateText", 14, "Status: {0}"));
		stack.Children.Add(MakeBoundText("OpcUaStatus", 14, "OPC UA: {0}"));

		stack.Children.Add(MakeSectionTitle("SIMULATION / TEST"));
		var list = new ListBox
		{
			ItemsSource = _viewModel.LaserFaultScenarios,
			DisplayMemberPath = "DisplayName",
			MinHeight = 120,
			Margin = new Thickness(0, 4, 0, 8)
		};
		list.SelectionChanged += (_, _) => _viewModel.SetSelectedFaultScenario(list.SelectedItem as Werkflow.OpcUaSimulator.App.ViewModels.FaultScenarioListItem);
		stack.Children.Add(list);

		var faultControls = new WrapPanel();
		faultControls.Children.Add(MakeCommandButton("Szenario starten", _viewModel.StartFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Pause", _viewModel.PauseFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Resume", _viewModel.ResumeFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Stop", _viewModel.StopFaultScenarioCommand));
		stack.Children.Add(faultControls);
		stack.Children.Add(MakeBoundText("FaultRuntimeStatus", 14, "Szenario-Status: {0}"));

		scroll.Content = stack;
		item.Content = scroll;
		return item;
	}

	private static DataGrid CreateSignalGrid(System.Collections.ObjectModel.ObservableCollection<HmiSignalDisplayItem> signals)
	{
		var grid = new DataGrid
		{
			ItemsSource = signals,
			AutoGenerateColumns = false,
			IsReadOnly = true,
			HeadersVisibility = DataGridHeadersVisibility.Column,
			GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
			Background = new SolidColorBrush(Color.FromRgb(35, 40, 48)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 78)),
			Margin = new Thickness(0, 4, 0, 12),
			MinHeight = 120
		};
		grid.Columns.Add(new DataGridTextColumn { Header = "Signal", Binding = new Binding("DisplayName"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
		grid.Columns.Add(new DataGridTextColumn { Header = "Wert", Binding = new Binding("FormattedValue"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
		return grid;
	}

	private static TextBlock MakeHeaderText(string text, double size, FontWeight weight)
	{
		return new TextBlock
		{
			Text = text,
			FontSize = size,
			FontWeight = weight,
			Foreground = Brushes.White,
			Margin = new Thickness(0, 0, 0, 2)
		};
	}

	private static TextBlock MakeBoundText(string property, double size, string? format = null)
	{
		var block = new TextBlock
		{
			FontSize = size,
			Foreground = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
			Margin = new Thickness(0, 2, 0, 2)
		};
		if (format != null)
		{
			block.SetBinding(TextBlock.TextProperty, new Binding(property) { StringFormat = format });
		}
		else
		{
			block.SetBinding(TextBlock.TextProperty, new Binding(property));
		}
		return block;
	}

	private static TextBlock MakeSectionTitle(string title) =>
		new()
		{
			Text = title,
			FontSize = 16,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromRgb(120, 180, 255)),
			Margin = new Thickness(0, 12, 0, 6)
		};

	private static Button MakeButton(string text, RoutedEventHandler onClick)
	{
		var button = new Button
		{
			Content = text,
			Margin = new Thickness(4),
			Padding = new Thickness(12, 6, 12, 6),
			Background = new SolidColorBrush(Color.FromRgb(45, 55, 68)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(70, 80, 95))
		};
		button.Click += onClick;
		return button;
	}

	private static Button MakeCommandButton(string text, IAsyncRelayCommand command)
	{
		var button = MakeButton(text, async (_, _) => await command.ExecuteAsync(null));
		button.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(command.CanExecute)) { Source = command });
		return button;
	}
}
