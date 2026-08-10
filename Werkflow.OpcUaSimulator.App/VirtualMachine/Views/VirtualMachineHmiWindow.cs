using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Models;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

public sealed class VirtualMachineHmiWindow : Window
{
	private static readonly Brush BgDark = new SolidColorBrush(Color.FromRgb(28, 32, 38));
	private static readonly Brush PanelBg = new SolidColorBrush(Color.FromRgb(35, 40, 48));
	private static readonly Brush HeaderBg = new SolidColorBrush(Color.FromRgb(20, 24, 30));
	private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 78));
	private static readonly Brush TextPrimary = Brushes.White;
	private static readonly Brush TextSecondary = new SolidColorBrush(Color.FromRgb(200, 210, 220));
	private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(120, 180, 255));
	private static readonly Brush SimPanelBg = new SolidColorBrush(Color.FromRgb(48, 38, 32));

	private readonly VirtualMachineHmiViewModel _viewModel;
	private readonly IHmiTrayNotifier _trayNotifier;
	private readonly Grid _mainContent;
	private readonly StackPanel _navBar;

	public VirtualMachineHmiWindow(VirtualMachineHmiViewModel viewModel, IHmiTrayNotifier trayNotifier)
	{
		_viewModel = viewModel;
		_trayNotifier = trayNotifier;
		Title = "Virtuelle Maschine";
		MinWidth = 1280;
		MinHeight = 720;
		Width = 1600;
		Height = 900;
		Background = BgDark;
		DataContext = viewModel;

		var root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });

		root.Children.Add(BuildHeader());
		_mainContent = new Grid { Margin = new Thickness(6, 4, 6, 4) };
		Grid.SetRow(_mainContent, 1);
		root.Children.Add(_mainContent);

		_navBar = BuildNavBar();
		Grid.SetRow(_navBar, 2);
		root.Children.Add(_navBar);

		Content = root;
		viewModel.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(VirtualMachineHmiViewModel.SelectedTabIndex))
			{
				UpdateTabContent();
				UpdateNavHighlight();
			}
		};
		UpdateTabContent();
	}

	protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
	{
		e.Cancel = true;
		Hide();
		ShowInTaskbar = false;
		_trayNotifier.NotifyHmiHidden();
	}

	private void UpdateTabContent()
	{
		_mainContent.Children.Clear();
		UIElement content;
		switch (_viewModel.SelectedTabIndex)
		{
		case 0:
			content = BuildOverviewLayout();
			break;
		case 1:
			content = BuildAxisTab();
			break;
		case 2:
			content = BuildMotorTab();
			break;
		case 3:
			content = BuildTemperatureTab();
			break;
		case 4:
			content = BuildMetricTab(_viewModel.ProcessMetrics, "Prozess");
			break;
		case 5:
			content = BuildMetricTab(_viewModel.CoolingMetrics, "Kühlung");
			break;
		case 6:
			content = BuildMetricTab(_viewModel.PowerMetrics, "Elektrik / Leistung");
			break;
		case 7:
			content = BuildMetricTab(_viewModel.VibrationMetrics, "Vibration");
			break;
		case 8:
			content = BuildMetricTab(_viewModel.ProductionMetrics, "Produktion");
			break;
		case 9:
			content = BuildDiagnosticsTab();
			break;
		case 10:
			content = BuildOtherSignalsTab();
			break;
		default:
			content = BuildOverviewLayout();
			break;
		}
		_mainContent.Children.Add(content);
	}

	private UIElement BuildHeader()
	{
		var panel = new Border
		{
			Background = HeaderBg,
			BorderBrush = BorderBrush,
			BorderThickness = new Thickness(0, 0, 0, 1),
			Padding = new Thickness(12, 8, 12, 8)
		};

		var grid = new Grid();
		for (int i = 0; i < 6; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var title = MakeHeaderText(_viewModel.MachineTitle, 20, FontWeights.Bold);
		grid.Children.Add(title);

		var state = MakeBoundBlock("MachineStateText", 15, FontWeights.SemiBold);
		Grid.SetColumn(state, 1);
		grid.Children.Add(state);

		var opc = MakeBoundBlock("OpcUaStatus", 14, FontWeights.Normal, "OPC UA: {0}");
		Grid.SetColumn(opc, 2);
		grid.Children.Add(opc);

		var job = MakeBoundBlock("JobName", 14, FontWeights.Normal, "Job: {0}");
		Grid.SetColumn(job, 3);
		grid.Children.Add(job);

		var part = MakeBoundBlock("PartName", 14, FontWeights.Normal, "Teil: {0}");
		Grid.SetColumn(part, 4);
		grid.Children.Add(part);

		var counter = MakeBoundBlock("CounterText", 14, FontWeights.Normal, "Zähler: {0}");
		Grid.SetColumn(counter, 5);
		grid.Children.Add(counter);

		var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
		right.Children.Add(MakeBoundBlock("ClockText", 14, FontWeights.Normal));
		right.Children.Add(MakeButton("Vollbild", (_, _) =>
			WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
		right.Children.Add(MakeButton("Maschine beenden", async (_, _) => await _viewModel.ShutdownMachineCommand.ExecuteAsync(null)));
		Grid.SetColumn(right, 6);
		grid.Children.Add(right);

		panel.Child = grid;
		Grid.SetRow(panel, 0);
		return panel;
	}

	private void UpdateNavHighlight()
	{
		for (int i = 0; i < _navBar.Children.Count; i++)
		{
			if (_navBar.Children[i] is Button btn)
			{
				btn.Background = _viewModel.SelectedTabIndex == i ? Accent : PanelBg;
			}
		}
	}

	private StackPanel BuildNavBar()
	{
		var bar = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Background = HeaderBg,
			Margin = new Thickness(6, 0, 6, 6)
		};

		string[] labels = [
			"Übersicht", "Achsen", "Antriebe", "Temperaturen", "Prozess",
			"Kühlung", "Elektrik", "Vibration", "Produktion", "Diagnose", "Weitere Signale"
		];

		for (int i = 0; i < labels.Length; i++)
		{
			var btn = MakeNavButton(labels[i], i);
			bar.Children.Add(btn);
		}

		return bar;
	}

	private Button MakeNavButton(string label, int index)
	{
		var button = new Button
		{
			Content = label,
			Margin = new Thickness(2, 4, 2, 4),
			Padding = new Thickness(10, 4, 10, 4),
			Background = _viewModel.SelectedTabIndex == index ? Accent : PanelBg,
			Foreground = TextPrimary,
			BorderBrush = BorderBrush,
			FontSize = 12
		};
		button.Click += (_, _) => _viewModel.SelectedTabIndex = index;
		return button;
	}

	private UIElement BuildOverviewLayout()
	{
		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });

		grid.Children.Add(WrapPanel(BuildLeftControlColumn()));
		grid.Children.Add(WrapPanel(BuildCenterMetricsColumn()));
		Grid.SetColumn(grid.Children[^1], 1);
		grid.Children.Add(WrapPanel(BuildRightDiagnosticsColumn()));
		Grid.SetColumn(grid.Children[^1], 2);

		return grid;
	}

	private static Border WrapPanel(UIElement child) =>
		new()
		{
			Background = PanelBg,
			BorderBrush = BorderBrush,
			BorderThickness = new Thickness(1),
			Margin = new Thickness(3),
			Padding = new Thickness(8),
			Child = child
		};

	private UIElement BuildLeftControlColumn()
	{
		var stack = new StackPanel();

		stack.Children.Add(MakeSectionTitle("Maschinenbetrieb"));
		var machineControls = new WrapPanel();
		machineControls.Children.Add(MakeCommandButton("Maschine starten", _viewModel.StartMachineCommand));
		stack.Children.Add(machineControls);

		var prodControls = new WrapPanel();
		prodControls.Children.Add(MakeCommandButton("Start", _viewModel.StartProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Stop", _viewModel.StopProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Pause", _viewModel.PauseProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Resume", _viewModel.ResumeProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Reset", _viewModel.ResetMachineCommand));
		stack.Children.Add(prodControls);

		stack.Children.Add(MakeSectionTitle("Betriebszustand"));
		stack.Children.Add(MakeBoundBlock("StatusBadge", 18, FontWeights.Bold));

		stack.Children.Add(MakeSectionTitle("Aktiver Auftrag"));
		stack.Children.Add(MakeBoundBlock("JobName", 14, FontWeights.Normal, "Job: {0}"));
		stack.Children.Add(MakeBoundBlock("PartName", 14, FontWeights.Normal, "Teil: {0}"));
		stack.Children.Add(MakeBoundBlock("CounterText", 14, FontWeights.Normal, "Ist/Soll: {0}"));
		stack.Children.Add(MakeMetricRowFromOverview(11)); // cycle from overview if present - use counter

		return stack;
	}

	private UIElement BuildCenterMetricsColumn()
	{
		var grid = new Grid { Margin = new Thickness(4) };
		for (int r = 0; r < 3; r++)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		}
		for (int c = 0; c < 3; c++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}

		// Large overview metrics: X,Y,Z, Feed, Power, MotorCurrent, MotorTemp, Cooling, Vibration
		for (int i = 0; i < 9 && i < _viewModel.OverviewMetrics.Count; i++)
		{
			var tile = BuildLargeMetricTile(_viewModel.OverviewMetrics[i]);
			Grid.SetRow(tile, i / 3);
			Grid.SetColumn(tile, i % 3);
			grid.Children.Add(tile);
		}

		return grid;
	}

	private UIElement BuildRightDiagnosticsColumn()
	{
		var stack = new StackPanel();

		stack.Children.Add(MakeSectionTitle("Maschinenmeldung"));
		stack.Children.Add(MakeBoundBlock("ErrorMessage", 13, FontWeights.Normal));
		stack.Children.Add(MakeBoundBlock("MachineStateText", 13, FontWeights.Normal, "Status: {0}"));

		var simBorder = new Border
		{
			Background = SimPanelBg,
			BorderBrush = new SolidColorBrush(Color.FromRgb(120, 90, 60)),
			BorderThickness = new Thickness(1),
			Margin = new Thickness(0, 12, 0, 0),
			Padding = new Thickness(8)
		};
		var simStack = new StackPanel();
		simStack.Children.Add(MakeSectionTitle("SIMULATION / TEST"));
		simStack.Children.Add(MakeBoundText("ActiveTestScenario", 12, "Szenario: {0}"));

		var list = new ListBox
		{
			ItemsSource = _viewModel.LaserFaultScenarios,
			DisplayMemberPath = "DisplayName",
			MinHeight = 100,
			MaxHeight = 140,
			Margin = new Thickness(0, 4, 0, 4),
			Background = PanelBg,
			Foreground = TextPrimary,
			BorderBrush = BorderBrush
		};
		list.SelectionChanged += (_, _) => _viewModel.SetSelectedFaultScenario(
			list.SelectedItem as Werkflow.OpcUaSimulator.App.ViewModels.FaultScenarioListItem);
		simStack.Children.Add(list);

		var faultControls = new WrapPanel();
		faultControls.Children.Add(MakeCommandButton("Start", _viewModel.StartFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Stop", _viewModel.StopFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Pause", _viewModel.PauseFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Resume", _viewModel.ResumeFaultScenarioCommand));
		faultControls.Children.Add(MakeCommandButton("Normalbetrieb", _viewModel.NormalOperationCommand));
		simStack.Children.Add(faultControls);
		simStack.Children.Add(MakeBoundBlock("FaultRuntimeStatus", 12, FontWeights.Normal, "Status: {0}"));

		simBorder.Child = simStack;
		stack.Children.Add(simBorder);

		return stack;
	}

	private UIElement BuildAxisTab()
	{
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var wrap = new WrapPanel { Margin = new Thickness(4) };

		foreach (var axis in _viewModel.AxisPanels)
		{
			var panel = new Border
			{
				Width = 280,
				Margin = new Thickness(4),
				Padding = new Thickness(10),
				Background = PanelBg,
				BorderBrush = BorderBrush,
				BorderThickness = new Thickness(1)
			};
			var stack = new StackPanel();
			var title = new TextBlock
			{
				FontSize = 16,
				FontWeight = FontWeights.Bold,
				Foreground = TextPrimary,
				Margin = new Thickness(0, 0, 0, 4)
			};
			title.SetBinding(TextBlock.TextProperty, new Binding(nameof(HmiAxisPanelViewModel.AxisName))
			{
				Source = axis,
				StringFormat = "{0}-ACHSE"
			});
			stack.Children.Add(title);
			stack.Children.Add(MakeBoundAxisRow("Position", axis, nameof(HmiAxisPanelViewModel.Position)));
			stack.Children.Add(MakeBoundAxisRow("Sollposition", axis, nameof(HmiAxisPanelViewModel.TargetPosition)));
			stack.Children.Add(MakeBoundAxisRow("Geschwindigkeit", axis, nameof(HmiAxisPanelViewModel.Speed)));
			stack.Children.Add(MakeBoundAxisRow("Strom", axis, nameof(HmiAxisPanelViewModel.Current)));
			stack.Children.Add(MakeBoundAxisRow("Drehmoment", axis, nameof(HmiAxisPanelViewModel.Torque)));
			stack.Children.Add(MakeBoundAxisRow("Temperatur", axis, nameof(HmiAxisPanelViewModel.Temperature)));
			stack.Children.Add(MakeBoundAxisRow("Last", axis, nameof(HmiAxisPanelViewModel.Load)));
			stack.Children.Add(MakeBoundAxisRow("Positionsfehler", axis, nameof(HmiAxisPanelViewModel.PositionError)));
			stack.Children.Add(MakeBoundAxisRow("Servo", axis, nameof(HmiAxisPanelViewModel.ServoState)));
			panel.Child = stack;
			wrap.Children.Add(panel);
		}

		scroll.Content = wrap;
		return scroll;
	}

	private static Grid MakeBoundAxisRow(string label, HmiAxisPanelViewModel axis, string property)
	{
		var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.Children.Add(new TextBlock { Text = label, Foreground = TextSecondary, FontSize = 12 });
		var val = new TextBlock
		{
			Foreground = TextPrimary,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold
		};
		val.SetBinding(TextBlock.TextProperty, new Binding(property) { Source = axis });
		Grid.SetColumn(val, 1);
		grid.Children.Add(val);
		return grid;
	}

	private UIElement BuildMotorTab()
	{
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var stack = new StackPanel { Margin = new Thickness(4) };

		foreach (var group in _viewModel.MotorGroups)
		{
			stack.Children.Add(MakeSectionTitle(group.GroupName));
			stack.Children.Add(BuildMetricGrid(group.Metrics));
		}

		scroll.Content = stack;
		return scroll;
	}

	private UIElement BuildTemperatureTab()
	{
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var wrap = new WrapPanel { Margin = new Thickness(4) };

		foreach (var tile in _viewModel.TemperatureTiles)
		{
			var border = new Border
			{
				Width = 200,
				Margin = new Thickness(4),
				Padding = new Thickness(10),
				Background = tile.IsError
					? new SolidColorBrush(Color.FromRgb(80, 35, 35))
					: tile.IsWarning
						? new SolidColorBrush(Color.FromRgb(70, 55, 30))
						: PanelBg,
				BorderBrush = BorderBrush,
				BorderThickness = new Thickness(1)
			};
			var stack = new StackPanel();
			stack.Children.Add(MakeHeaderText(tile.Label, 13, FontWeights.SemiBold));
			stack.Children.Add(MakeHeaderText($"{tile.Value} {tile.Unit}", 20, FontWeights.Bold));
			if (!string.IsNullOrWhiteSpace(tile.NormalRange))
			{
				stack.Children.Add(MakeHeaderText(tile.NormalRange, 11, FontWeights.Normal));
			}
			border.Child = stack;
			wrap.Children.Add(border);
		}

		scroll.Content = wrap;
		return scroll;
	}

	private UIElement BuildMetricTab(ObservableCollection<HmiMetricItem> metrics, string title)
	{
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var stack = new StackPanel { Margin = new Thickness(8) };
		stack.Children.Add(MakeSectionTitle(title));
		stack.Children.Add(BuildMetricGrid(metrics));
		scroll.Content = stack;
		return scroll;
	}

	private UIElement BuildDiagnosticsTab()
	{
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var stack = new StackPanel { Margin = new Thickness(8) };

		stack.Children.Add(MakeSectionTitle("MASCHINENDIAGNOSE"));
		stack.Children.Add(MakeBoundBlock("ErrorMessage", 14, FontWeights.Normal, "Fehler: {0}"));
		stack.Children.Add(MakeBoundBlock("MachineStateText", 14, FontWeights.Normal, "Status: {0}"));
		stack.Children.Add(MakeBoundBlock("OpcUaStatus", 14, FontWeights.Normal, "OPC UA: {0}"));
		stack.Children.Add(MakeMetricRowFromOverview(14)); // ErrorActive

		var sim = BuildRightDiagnosticsColumn();
		stack.Children.Add(sim);

		scroll.Content = stack;
		return scroll;
	}

	private UIElement BuildOtherSignalsTab()
	{
		var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		var stack = new StackPanel { Margin = new Thickness(8) };
		stack.Children.Add(MakeSectionTitle("Weitere Signale"));
		stack.Children.Add(CreateSignalGrid(_viewModel.OtherSignals));
		scroll.Content = stack;
		return stack;
	}

	private static Grid BuildMetricGrid(ObservableCollection<HmiMetricItem> metrics)
	{
		var grid = new Grid { Margin = new Thickness(0, 4, 0, 8) };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		for (int i = 0; i < metrics.Count; i++)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			var label = new TextBlock
			{
				Text = metrics[i].Label,
				Foreground = TextSecondary,
				Margin = new Thickness(0, 3, 8, 3),
				FontSize = 13
			};
			var value = new TextBlock
			{
				Foreground = TextPrimary,
				FontWeight = FontWeights.SemiBold,
				FontSize = 13,
				Margin = new Thickness(0, 3, 0, 3)
			};
			value.SetBinding(TextBlock.TextProperty, new Binding($"[{i}].Value") { Source = metrics });
			Grid.SetRow(label, i);
			Grid.SetRow(value, i);
			Grid.SetColumn(value, 1);
			grid.Children.Add(label);
			grid.Children.Add(value);
		}

		return grid;
	}

	private Border BuildLargeMetricTile(HmiMetricItem metric)
	{
		var border = new Border
		{
			Margin = new Thickness(4),
			Padding = new Thickness(10, 8, 10, 8),
			Background = new SolidColorBrush(Color.FromRgb(42, 48, 58)),
			BorderBrush = BorderBrush,
			BorderThickness = new Thickness(1)
		};
		var stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = metric.Label,
			Foreground = TextSecondary,
			FontSize = 12
		});
		var valueBlock = new TextBlock
		{
			Foreground = Accent,
			FontSize = 28,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0, 4, 0, 0)
		};
		valueBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(HmiMetricItem.Value)) { Source = metric });
		stack.Children.Add(valueBlock);
		border.Child = stack;
		return border;
	}

	private UIElement MakeMetricRowFromOverview(int index)
	{
		if (index >= _viewModel.OverviewMetrics.Count)
		{
			return new TextBlock();
		}

		var metric = _viewModel.OverviewMetrics[index];
		var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		var label = new TextBlock { Text = metric.Label, Foreground = TextSecondary, FontSize = 13 };
		var value = new TextBlock { Foreground = TextPrimary, FontSize = 13, FontWeight = FontWeights.SemiBold };
		value.SetBinding(TextBlock.TextProperty, new Binding(nameof(HmiMetricItem.Value)) { Source = metric });
		Grid.SetColumn(value, 1);
		grid.Children.Add(label);
		grid.Children.Add(value);
		return grid;
	}

	private static DataGrid CreateSignalGrid(ObservableCollection<HmiSignalDisplayItem> signals)
	{
		var grid = new DataGrid
		{
			ItemsSource = signals,
			AutoGenerateColumns = false,
			IsReadOnly = true,
			HeadersVisibility = DataGridHeadersVisibility.Column,
			GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
			Background = PanelBg,
			Foreground = TextPrimary,
			BorderBrush = BorderBrush,
			Margin = new Thickness(0, 4, 0, 12),
			MinHeight = 200,
			RowHeight = 26
		};
		grid.Columns.Add(new DataGridTextColumn
		{
			Header = "Signal",
			Binding = new Binding("DisplayName"),
			Width = new DataGridLength(2, DataGridLengthUnitType.Star)
		});
		grid.Columns.Add(new DataGridTextColumn
		{
			Header = "Wert",
			Binding = new Binding("FormattedValue"),
			Width = new DataGridLength(1, DataGridLengthUnitType.Star)
		});
		return grid;
	}

	private static TextBlock MakeHeaderText(string text, double size, FontWeight weight) =>
		new()
		{
			Text = text,
			FontSize = size,
			FontWeight = weight,
			Foreground = TextPrimary,
			Margin = new Thickness(0, 0, 0, 2)
		};

	private static TextBlock MakeBoundBlock(string property, double size, FontWeight weight, string? format = null)
	{
		var block = new TextBlock
		{
			FontSize = size,
			FontWeight = weight,
			Foreground = TextSecondary,
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

	private static TextBlock MakeBoundText(string property, double size, string? format = null) =>
		MakeBoundBlock(property, size, FontWeights.Normal, format);

	private static TextBlock MakeSectionTitle(string title) =>
		new()
		{
			Text = title,
			FontSize = 14,
			FontWeight = FontWeights.SemiBold,
			Foreground = Accent,
			Margin = new Thickness(0, 8, 0, 4)
		};

	private static Button MakeButton(string text, RoutedEventHandler onClick)
	{
		var button = new Button
		{
			Content = text,
			Margin = new Thickness(4),
			Padding = new Thickness(10, 5, 10, 5),
			Background = new SolidColorBrush(Color.FromRgb(45, 55, 68)),
			Foreground = TextPrimary,
			BorderBrush = BorderBrush,
			FontSize = 12
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
