using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Models;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Services;
using Werkflow.OpcUaSimulator.App.VirtualMachine.ViewModels;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

public sealed class VirtualMachineHmiWindow : Window
{
	private readonly VirtualMachineHmiViewModel _viewModel;
	private readonly IHmiTrayNotifier _trayNotifier;
	private readonly Grid _mainContent;
	private readonly StackPanel _navBar;
	private CuttingPlanCanvasControl? _cuttingPlanCanvas;

	public VirtualMachineHmiWindow(VirtualMachineHmiViewModel viewModel, IHmiTrayNotifier trayNotifier)
	{
		_viewModel = viewModel;
		_trayNotifier = trayNotifier;
		Title = "Virtuelle Maschine";
		MinWidth = 1280;
		MinHeight = 720;
		Width = 1600;
		Height = 900;
		Background = HmiVisualTheme.BgDark;
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
			if (e.PropertyName == nameof(VirtualMachineHmiViewModel.PlanVisualToken))
			{
				UpdateCuttingPlanCanvas();
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
			content = BuildMetricTab(_viewModel.ProcessMetrics, "Prozess");
			break;
		case 3:
			content = BuildMetricTab(_viewModel.ProductionMetrics, "Produktion");
			break;
		case 4:
			content = BuildTemperatureTab();
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
			content = BuildDiagnosticsTab();
			break;
		case 9:
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
		var outer = new StackPanel { Background = HmiVisualTheme.HeaderBg };

		var panel = new Border
		{
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(0, 0, 0, 1),
			Padding = new Thickness(12, 8, 12, 6)
		};

		var grid = new Grid();
		for (int i = 0; i < 8; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var selector = new ComboBox
		{
			MinWidth = 220,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 8, 0)
		};
		selector.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(VirtualMachineHmiViewModel.AvailableMachines)));
		selector.DisplayMemberPath = nameof(VirtualMachineSelectorItem.DisplayName);
		selector.SelectedValuePath = nameof(VirtualMachineSelectorItem.MachineId);
		selector.SetBinding(ComboBox.SelectedValueProperty, new Binding(nameof(VirtualMachineHmiViewModel.SelectedMachineId))
		{
			Mode = BindingMode.TwoWay
		});
		grid.Children.Add(selector);

		var endpoint = MakeBoundBlock(nameof(VirtualMachineHmiViewModel.Endpoint), 11, FontWeights.Normal, "Endpoint: {0}");
		Grid.SetColumn(endpoint, 1);
		grid.Children.Add(endpoint);

		var title = MakeHeaderText(_viewModel.MachineTitle, 18, FontWeights.Bold);
		Grid.SetColumn(title, 2);
		grid.Children.Add(title);

		var mode = MakeBoundBlock("ModeText", 12, FontWeights.Normal, "Betriebsart: {0}");
		Grid.SetColumn(mode, 3);
		grid.Children.Add(mode);

		var state = MakeBoundBlock("MachineStateText", 13, FontWeights.SemiBold, "Maschine: {0}");
		Grid.SetColumn(state, 4);
		grid.Children.Add(state);

		var phase = MakeBoundBlock("ProcessPhaseText", 13, FontWeights.Bold, "Phase: {0}");
		Grid.SetColumn(phase, 5);
		grid.Children.Add(phase);

		var job = MakeBoundBlock("JobName", 12, FontWeights.Normal, "Job: {0}");
		Grid.SetColumn(job, 6);
		grid.Children.Add(job);

		var counter = MakeBoundBlock("CounterText", 12, FontWeights.Normal, "Ist/Soll: {0}");
		Grid.SetColumn(counter, 7);
		grid.Children.Add(counter);

		var opc = MakeBoundBlock("OpcUaStatus", 12, FontWeights.SemiBold, "OPC UA: {0}");
		Grid.SetColumn(opc, 8);
		grid.Children.Add(opc);

		var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
		right.Children.Add(MakeBoundBlock("ClockText", 13, FontWeights.Normal));
		right.Children.Add(MakeButton("Vollbild", (_, _) =>
			WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
		right.Children.Add(MakeButton("Maschine beenden", async (_, _) => await _viewModel.ShutdownMachineCommand.ExecuteAsync(null)));
		Grid.SetColumn(right, 9);
		grid.Children.Add(right);

		panel.Child = grid;
		outer.Children.Add(panel);

		var statusBar = new Border
		{
			Height = 22,
			Background = HmiVisualTheme.StatusBarIdle,
			Padding = new Thickness(12, 2, 12, 2)
		};
		var statusText = MakeBoundBlock("ProcessPhaseText", 12, FontWeights.SemiBold, "Prozess: {0}");
		statusText.Foreground = HmiVisualTheme.TextOnDark;
		statusBar.Child = statusText;
		_viewModel.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(VirtualMachineHmiViewModel.StatusTone) or nameof(VirtualMachineHmiViewModel.ProcessPhaseText))
			{
				statusBar.Background = HmiVisualTheme.PhaseBannerBrush(_viewModel.StatusTone);
			}
		};
		outer.Children.Add(statusBar);

		return outer;
	}

	private void UpdateNavHighlight()
	{
		for (int i = 0; i < _navBar.Children.Count; i++)
		{
			if (_navBar.Children[i] is Button btn)
			{
				btn.Background = _viewModel.SelectedTabIndex == i ? HmiVisualTheme.NavSelectedBg : HmiVisualTheme.NavNormalBg;
			}
		}
	}

	private StackPanel BuildNavBar()
	{
		var bar = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Background = HmiVisualTheme.HeaderBg,
			Margin = new Thickness(6, 0, 6, 6)
		};

		string[] labels = [
			"Übersicht", "Achsen", "Prozess", "Produktion", "Temperaturen",
			"Kühlung", "Elektrik", "Vibration", "Fehler / Diagnose", "Weitere Signale"
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
			Foreground = HmiVisualTheme.TextPrimary,
			BorderBrush = HmiVisualTheme.PanelBorder,
			FontSize = 12
		};
		HmiVisualTheme.ApplyButtonStyle(button);
		button.Background = _viewModel.SelectedTabIndex == index ? HmiVisualTheme.NavSelectedBg : HmiVisualTheme.NavNormalBg;
		button.Click += (_, _) => _viewModel.SelectedTabIndex = index;
		return button;
	}

	private UIElement BuildOverviewLayout()
	{
		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

		var leftWrap = WrapPanel(BuildLeftAxisColumn());
		leftWrap.VerticalAlignment = VerticalAlignment.Stretch;
		grid.Children.Add(leftWrap);

		var centerWrap = WrapPanel(BuildCenterProcessColumn());
		centerWrap.VerticalAlignment = VerticalAlignment.Stretch;
		Grid.SetColumn(centerWrap, 1);
		grid.Children.Add(centerWrap);

		var rightWrap = WrapPanel(BuildRightControlColumn());
		rightWrap.VerticalAlignment = VerticalAlignment.Stretch;
		Grid.SetColumn(rightWrap, 2);
		grid.Children.Add(rightWrap);

		return grid;
	}

	private UIElement BuildLeftAxisColumn()
	{
		var stack = new StackPanel();
		stack.Children.Add(MakeSectionTitle("Achswerte"));
		stack.Children.Add(BuildAxisInsetRow("X", 0));
		stack.Children.Add(BuildAxisInsetRow("Y", 1));
		stack.Children.Add(BuildAxisInsetRow("Z", 2));
		stack.Children.Add(MakeBoundBlock("XSpeedText", 13, FontWeights.Normal, "Vx: {0}"));
		stack.Children.Add(MakeBoundBlock("YSpeedText", 13, FontWeights.Normal, "Vy: {0}"));
		stack.Children.Add(MakeBoundBlock("PathSpeedText", 13, FontWeights.Normal, "Bahngeschw.: {0}"));
		stack.Children.Add(MakeBoundBlock("FocusText", 13, FontWeights.Normal, "Fokus: {0}"));
		stack.Children.Add(BuildLargeValueTile("Vorschub", 3));
		return stack;
	}

	private UIElement BuildCenterProcessColumn()
	{
		var grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 280 });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var infoGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
		for (int i = 0; i < 4; i++)
		{
			infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}
		infoGrid.RowDefinitions.Add(new RowDefinition());
		infoGrid.RowDefinitions.Add(new RowDefinition());
		infoGrid.Children.Add(MakePlanInfoBlock("Plan", nameof(CuttingPlanViewModel.PlanId)));
		infoGrid.Children.Add(MakePlanInfoBlock("Job", nameof(CuttingPlanViewModel.JobId)));
		Grid.SetColumn(infoGrid.Children[^1], 1);
		infoGrid.Children.Add(MakePlanInfoBlock("Teil", nameof(CuttingPlanViewModel.PartName)));
		Grid.SetColumn(infoGrid.Children[^1], 2);
		infoGrid.Children.Add(MakePlanInfoBlock("Material", nameof(CuttingPlanViewModel.MaterialText)));
		Grid.SetColumn(infoGrid.Children[^1], 3);
		infoGrid.Children.Add(MakePlanInfoBlock("Dicke", nameof(CuttingPlanViewModel.ThicknessText)));
		Grid.SetColumn(infoGrid.Children[^1], 0);
		Grid.SetRow(infoGrid.Children[^1], 1);
		grid.Children.Add(infoGrid);

		var progressPanel = new Border
		{
			Background = HmiVisualTheme.MetricTileBg,
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(1),
			Padding = new Thickness(10, 6, 10, 6),
			Margin = new Thickness(0, 0, 0, 6)
		};
		var progressGrid = new Grid();
		for (int i = 0; i < 3; i++)
		{
			progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}
		progressGrid.Children.Add(MakeBoundBlock("CuttingPlan.ProcessedPartsText", 14, FontWeights.SemiBold));
		var contourBlock = MakeBoundBlock("CuttingPlan.ContourProgressText", 14, FontWeights.SemiBold);
		Grid.SetColumn(contourBlock, 1);
		progressGrid.Children.Add(contourBlock);
		var phaseBlock = MakeBoundBlock("CuttingPlan.CurrentPhaseText", 16, FontWeights.Bold, "Phase: {0}");
		Grid.SetColumn(phaseBlock, 2);
		progressGrid.Children.Add(phaseBlock);
		progressPanel.Child = progressGrid;
		Grid.SetRow(progressPanel, 1);
		grid.Children.Add(progressPanel);

		var timesPanel = BuildTimesPanel();
		Grid.SetRow(timesPanel, 2);
		grid.Children.Add(timesPanel);

		var banner = new Border
		{
			Background = HmiVisualTheme.PhaseBannerBrush(_viewModel.StatusTone),
			Padding = new Thickness(12, 8, 12, 8),
			Margin = new Thickness(0, 0, 0, 6),
			CornerRadius = new CornerRadius(3)
		};
		var phaseBlockLarge = MakeBoundBlock("ProcessPhaseText", 24, FontWeights.Bold);
		phaseBlockLarge.Foreground = HmiVisualTheme.TextOnDark;
		banner.Child = phaseBlockLarge;
		Grid.SetRow(banner, 3);
		grid.Children.Add(banner);

		var planBorder = new Border
		{
			Background = HmiVisualTheme.PlanSheetBg,
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(1),
			VerticalAlignment = VerticalAlignment.Stretch,
			MinHeight = 280
		};
		_cuttingPlanCanvas = new CuttingPlanCanvasControl
		{
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_cuttingPlanCanvas.Bind(_viewModel.CuttingPlan);
		planBorder.Child = _cuttingPlanCanvas;
		Grid.SetRow(planBorder, 4);
		grid.Children.Add(planBorder);

		var zoomRow = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
		zoomRow.Children.Add(MakeButton("Zoom +", (_, _) => _cuttingPlanCanvas?.ZoomIn()));
		zoomRow.Children.Add(MakeButton("Zoom −", (_, _) => _cuttingPlanCanvas?.ZoomOut()));
		zoomRow.Children.Add(MakeButton("Plan zentrieren", (_, _) => _cuttingPlanCanvas?.FitSheet()));
		Grid.SetRow(zoomRow, 5);
		grid.Children.Add(zoomRow);

		var nextJobBlock = MakeBoundBlock("CuttingPlan.NextJobPreview", 13, FontWeights.Normal, "Nächster Auftrag: {0}");
		Grid.SetRow(nextJobBlock, 6);
		grid.Children.Add(nextJobBlock);

		var indicatorRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
		for (int i = 0; i < 3; i++)
		{
			indicatorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		}
		indicatorRow.Children.Add(BuildIndicatorRow("Laser aktiv", "LaserActiveText"));
		var cut = BuildIndicatorRow("Schnitt aktiv", "CuttingActiveText");
		Grid.SetColumn(cut, 1);
		indicatorRow.Children.Add(cut);
		var pos = BuildIndicatorRow("Positionierung aktiv", "PositioningActiveText");
		Grid.SetColumn(pos, 2);
		indicatorRow.Children.Add(pos);
		Grid.SetRow(indicatorRow, 7);
		grid.Children.Add(indicatorRow);

		grid.VerticalAlignment = VerticalAlignment.Stretch;
		return grid;
	}

	private void UpdateCuttingPlanCanvas() => _cuttingPlanCanvas?.RefreshGeometry();

	private UIElement MakePlanInfoBlock(string label, string bindingPath)
	{
		var stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = HmiVisualTheme.TextSecondary,
			FontSize = 11
		});
		var val = new TextBlock { Foreground = HmiVisualTheme.TextPrimary, FontWeight = FontWeights.SemiBold, FontSize = 13 };
		val.SetBinding(TextBlock.TextProperty, new Binding($"CuttingPlan.{bindingPath}"));
		stack.Children.Add(val);
		return new Border { Child = stack, Padding = new Thickness(2) };
	}

	private UIElement BuildTimesPanel()
	{
		var border = new Border
		{
			Background = HmiVisualTheme.MetricTileBg,
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(1),
			Padding = new Thickness(10, 6, 10, 6),
			Margin = new Thickness(0, 0, 0, 6)
		};
		var stack = new StackPanel();
		stack.Children.Add(MakeSectionTitle("ZEITEN"));
		stack.Children.Add(BuildTimeRow("Teil", nameof(VirtualMachineHmiViewModel.PartRemainingText)));
		stack.Children.Add(BuildTimeRow("Auftrag", nameof(VirtualMachineHmiViewModel.JobRemainingText)));
		stack.Children.Add(BuildTimeRow("Einrichten", nameof(VirtualMachineHmiViewModel.SetupRemainingText)));
		stack.Children.Add(BuildTimeRow("Düsenwechsel", nameof(VirtualMachineHmiViewModel.NozzleRemainingText)));
		stack.Children.Add(BuildTimeRow("Laufzeit", nameof(VirtualMachineHmiViewModel.JobElapsedText)));
		border.Child = stack;
		return border;
	}

	private static Grid BuildTimeRow(string label, string bindingProperty)
	{
		var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
		grid.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = HmiVisualTheme.TextSecondary,
			FontSize = 12
		});
		var val = new TextBlock
		{
			Foreground = HmiVisualTheme.ValueAccent,
			FontWeight = FontWeights.SemiBold,
			FontSize = 14,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		val.SetBinding(TextBlock.TextProperty, new Binding(bindingProperty));
		Grid.SetColumn(val, 1);
		grid.Children.Add(val);
		return grid;
	}

	private UIElement BuildRightControlColumn()
	{
		var stack = new StackPanel();
		stack.Children.Add(MakeSectionTitle("Bedienung"));
		var prodControls = new WrapPanel();
		prodControls.Children.Add(MakeCommandButton("Start", _viewModel.StartProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Stop", _viewModel.StopProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Pause", _viewModel.PauseProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Resume", _viewModel.ResumeProductionCommand));
		prodControls.Children.Add(MakeCommandButton("Reset", _viewModel.ResetMachineCommand));
		stack.Children.Add(prodControls);

		stack.Children.Add(MakeCommandButton("Nächsten Job laden", _viewModel.ChangeJobCommand));
		stack.Children.Add(MakeCommandButton("Auftrag wählen", _viewModel.SelectJobCommand));
		stack.Children.Add(MakeCommandButton("Maschine starten", _viewModel.StartMachineCommand));
		stack.Children.Add(MakeCommandButton("Normalbetrieb", _viewModel.NormalOperationCommand));

		stack.Children.Add(MakeSectionTitle("Simulation"));
		stack.Children.Add(MakeBoundBlock("SimulationSpeedText", 13, FontWeights.Normal, "Zeitfaktor: {0}"));
		var speedRow = new WrapPanel();
		speedRow.Children.Add(MakeCommandButton("1x", _viewModel.SetSimulationSpeed1xCommand));
		speedRow.Children.Add(MakeCommandButton("2x", _viewModel.SetSimulationSpeed2xCommand));
		speedRow.Children.Add(MakeCommandButton("5x", _viewModel.SetSimulationSpeed5xCommand));
		speedRow.Children.Add(MakeCommandButton("10x", _viewModel.SetSimulationSpeed10xCommand));
		stack.Children.Add(speedRow);

		stack.Children.Add(BuildRightDiagnosticsColumn());
		return stack;
	}

	private static Grid BuildIndicatorRow(string label, string bindingProperty)
	{
		var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
		grid.Children.Add(new TextBlock { Text = label, Foreground = HmiVisualTheme.TextSecondary, FontSize = 13 });
		var val = new TextBlock { Foreground = HmiVisualTheme.TextPrimary, FontWeight = FontWeights.Bold, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Right };
		val.SetBinding(TextBlock.TextProperty, new Binding(bindingProperty));
		Grid.SetColumn(val, 1);
		grid.Children.Add(val);
		return grid;
	}

	private Border BuildAxisInsetRow(string axis, int overviewMetricIndex)
	{
		var border = new Border
		{
			Background = HmiVisualTheme.InsetBg,
			BorderBrush = HmiVisualTheme.InsetBorder,
			BorderThickness = new Thickness(1),
			Margin = new Thickness(0, 4, 0, 4),
			Padding = new Thickness(10, 6, 10, 6)
		};
		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		var label = new TextBlock { Text = axis, Foreground = HmiVisualTheme.TextOnDark, FontWeight = FontWeights.Bold, FontSize = 16 };
		var val = new TextBlock { Foreground = HmiVisualTheme.TextOnDark, FontWeight = FontWeights.Bold, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Right };
		if (overviewMetricIndex < _viewModel.OverviewMetrics.Count)
		{
			val.SetBinding(TextBlock.TextProperty, new Binding($"[{overviewMetricIndex}].Value") { Source = _viewModel.OverviewMetrics });
		}
		Grid.SetColumn(val, 1);
		grid.Children.Add(label);
		grid.Children.Add(val);
		border.Child = grid;
		return border;
	}

	private Border BuildLargeValueTile(string label, int overviewMetricIndex)
	{
		var border = new Border
		{
			Background = HmiVisualTheme.MetricTileBg,
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(1),
			Padding = new Thickness(10, 8, 10, 8),
			Margin = new Thickness(0, 4, 0, 4)
		};
		var stack = new StackPanel();
		stack.Children.Add(new TextBlock { Text = label, Foreground = HmiVisualTheme.TextSecondary, FontSize = 12 });
		var valueBlock = new TextBlock { Foreground = HmiVisualTheme.ValueAccent, FontSize = 24, FontWeight = FontWeights.Bold };
		if (overviewMetricIndex < _viewModel.OverviewMetrics.Count)
		{
			valueBlock.SetBinding(TextBlock.TextProperty, new Binding($"[{overviewMetricIndex}].Value") { Source = _viewModel.OverviewMetrics });
		}
		stack.Children.Add(valueBlock);
		border.Child = stack;
		return border;
	}

	private static Border WrapPanel(UIElement child) =>
		new()
		{
			Background = HmiVisualTheme.PanelBg,
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(1),
			Margin = new Thickness(3),
			Padding = new Thickness(8),
			VerticalAlignment = VerticalAlignment.Stretch,
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
		prodControls.Children.Add(MakeCommandButton("Neuen Maschinenlauf starten", _viewModel.ResetMachineCommand));
		stack.Children.Add(prodControls);

		stack.Children.Add(MakeSectionTitle("Betriebszustand"));
		stack.Children.Add(MakeBoundBlock("StatusBadge", 18, FontWeights.Bold));

		stack.Children.Add(MakeSectionTitle("Aktiver Auftrag"));
		stack.Children.Add(MakeBoundBlock("JobName", 14, FontWeights.Normal, "Job: {0}"));
		stack.Children.Add(MakeBoundBlock("PartName", 14, FontWeights.Normal, "Teil: {0}"));
		stack.Children.Add(MakeBoundBlock("CounterText", 14, FontWeights.Normal, "Ist/Soll: {0}"));

		stack.Children.Add(MakeSectionTitle("Aufträge"));
		stack.Children.Add(MakeBoundBlock("NextJobText", 13, FontWeights.Normal, "Nächster: {0}"));
		stack.Children.Add(MakeBoundBlock("JobPoolText", 13, FontWeights.Normal, "Pool: {0}"));
		stack.Children.Add(MakeBoundBlock("JobChangeText", 13, FontWeights.Normal, "{0}"));
		stack.Children.Add(MakeBoundBlock("JobChangeRemainingText", 13, FontWeights.Normal, "{0}"));
		stack.Children.Add(MakeCommandButton("Nächsten Job laden", _viewModel.ChangeJobCommand));
		stack.Children.Add(MakeCommandButton("Auftrag wählen", _viewModel.SelectJobCommand));

		stack.Children.Add(MakeSectionTitle("Simulation"));
		stack.Children.Add(MakeBoundBlock("SimulationSpeedText", 13, FontWeights.Normal, "Zeitfaktor: {0}"));
		stack.Children.Add(MakeBoundBlock("RandomSeedText", 13, FontWeights.Normal, "Seed: {0}"));
		stack.Children.Add(MakeBoundBlock("ProductionSpeedText", 13, FontWeights.Normal, "Prod.-Geschw.: {0}"));
		var speedRow = new WrapPanel();
		speedRow.Children.Add(MakeCommandButton("1x", _viewModel.SetSimulationSpeed1xCommand));
		speedRow.Children.Add(MakeCommandButton("2x", _viewModel.SetSimulationSpeed2xCommand));
		speedRow.Children.Add(MakeCommandButton("5x", _viewModel.SetSimulationSpeed5xCommand));
		speedRow.Children.Add(MakeCommandButton("10x", _viewModel.SetSimulationSpeed10xCommand));
		stack.Children.Add(speedRow);

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
			Background = HmiVisualTheme.SimPanelBg,
			BorderBrush = new SolidColorBrush(Color.FromRgb(140, 110, 80)),
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
			Margin = new Thickness(0, 4, 0, 4)
		};
		HmiVisualTheme.ApplyListBoxStyle(list);
		list.SelectionChanged += (_, _) => _viewModel.SetSelectedFaultScenario(
			list.SelectedItem as Werkflow.OpcUaSimulator.App.ViewModels.FaultScenarioListItem);
		simStack.Children.Add(list);

		simStack.Children.Add(MakeSectionTitle("Intensität / Zeitfaktor"));
		var intensitySlider = new Slider
		{
			Minimum = 0.1,
			Maximum = 2.0,
			Value = _viewModel.FaultIntensity,
			Margin = new Thickness(0, 4, 0, 4)
		};
		intensitySlider.ValueChanged += (_, e) => _viewModel.SetFaultIntensity(e.NewValue);
		simStack.Children.Add(intensitySlider);
		var timeFactorSlider = new Slider
		{
			Minimum = 1,
			Maximum = 50,
			Value = _viewModel.FaultTimeFactor,
			Margin = new Thickness(0, 4, 0, 8)
		};
		timeFactorSlider.ValueChanged += (_, e) => _viewModel.SetFaultTimeFactor(e.NewValue);
		simStack.Children.Add(timeFactorSlider);

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
				Background = HmiVisualTheme.PanelBg,
				BorderBrush = HmiVisualTheme.PanelBorder,
				BorderThickness = new Thickness(1)
			};
			var stack = new StackPanel();
			var title = new TextBlock
			{
				FontSize = 16,
				FontWeight = FontWeights.Bold,
				Foreground = HmiVisualTheme.TextPrimary,
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
		grid.Children.Add(new TextBlock { Text = label, Foreground = HmiVisualTheme.TextSecondary, FontSize = 12 });
		var val = new TextBlock
		{
			Foreground = HmiVisualTheme.TextPrimary,
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
						: HmiVisualTheme.PanelBg,
				BorderBrush = HmiVisualTheme.PanelBorder,
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
				Foreground = HmiVisualTheme.TextSecondary,
				Margin = new Thickness(0, 3, 8, 3),
				FontSize = 13
			};
			var value = new TextBlock
			{
				Foreground = HmiVisualTheme.TextPrimary,
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
			Background = HmiVisualTheme.MetricTileBg,
			BorderBrush = HmiVisualTheme.PanelBorder,
			BorderThickness = new Thickness(1)
		};
		var stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = metric.Label,
			Foreground = HmiVisualTheme.TextSecondary,
			FontSize = 12
		});
		var valueBlock = new TextBlock
		{
			Foreground = HmiVisualTheme.ValueAccent,
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
		var label = new TextBlock { Text = metric.Label, Foreground = HmiVisualTheme.TextSecondary, FontSize = 13 };
		var value = new TextBlock { Foreground = HmiVisualTheme.TextPrimary, FontSize = 13, FontWeight = FontWeights.SemiBold };
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
			Background = HmiVisualTheme.PanelBg,
			Foreground = HmiVisualTheme.TextPrimary,
			BorderBrush = HmiVisualTheme.PanelBorder,
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
			Foreground = HmiVisualTheme.TextPrimary,
			Margin = new Thickness(0, 0, 0, 2)
		};

	private static TextBlock MakeBoundBlock(string property, double size, FontWeight weight, string? format = null)
	{
		var block = new TextBlock
		{
			FontSize = size,
			FontWeight = weight,
			Foreground = HmiVisualTheme.TextPrimary,
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
			Foreground = HmiVisualTheme.SectionTitle,
			Margin = new Thickness(0, 8, 0, 4)
		};

	private static Button MakeButton(string text, RoutedEventHandler onClick)
	{
		var button = new Button
		{
			Content = text,
			Margin = new Thickness(4)
		};
		HmiVisualTheme.ApplyButtonStyle(button);
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
