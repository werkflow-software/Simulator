using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Werkflow.OpcUaSimulator.App.ViewModels;

namespace Werkflow.OpcUaSimulator.App.Views;

public sealed class ExperimentsView : UserControl
{
	public ExperimentsView()
	{
		var root = new Grid { Margin = new Thickness(16) };
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var title = new TextBlock
		{
			Text = "Experimente",
			FontSize = 20,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 0, 0, 12)
		};
		root.Children.Add(title);

		var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
		controls.Children.Add(MakeCombo("Maschine", "SelectedMachineProfileId", "MachineProfiles"));
		controls.Children.Add(MakeCombo("Experiment", "SelectedExperimentId", "Experiments"));
		controls.Children.Add(MakeCombo("Modus", "SelectedMode", "Modes"));
		controls.Children.Add(MakeBoundText("TimeFactor", "TimeFactor"));
		controls.Children.Add(MakeButton("Start", "StartCommand"));
		controls.Children.Add(MakeButton("Pause", "PauseCommand"));
		controls.Children.Add(MakeButton("Fortsetzen", "ResumeCommand"));
		controls.Children.Add(MakeButton("Stop", "StopCommand"));
		Grid.SetRow(controls, 1);
		root.Children.Add(controls);

		var statusPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
		statusPanel.Children.Add(MakeStatusLine("Status", "Status"));
		statusPanel.Children.Add(MakeStatusLine("Run", "CurrentRun"));
		statusPanel.Children.Add(MakeStatusLine("RunType", "RunType"));
		statusPanel.Children.Add(MakeStatusLine("Wiederholung", "Repetition"));
		statusPanel.Children.Add(MakeStatusLine("Fortschritt", "Progress"));
		statusPanel.Children.Add(MakeStatusLine("Simulationszeit", "SimulationTime"));
		statusPanel.Children.Add(MakeStatusLine("Phase", "CurrentPhase"));
		statusPanel.Children.Add(MakeStatusLine("GroundTruthEvents", "GroundTruthEventCount"));
		statusPanel.Children.Add(MakeStatusLine("VIGIL", "VigilConnected"));
		statusPanel.Children.Add(new TextBlock
		{
			Text = "Ergebnis",
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 12, 0, 4)
		});
		var resultBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
		resultBlock.SetBinding(TextBlock.TextProperty, new Binding("ResultSummary"));
		statusPanel.Children.Add(resultBlock);
		Grid.SetRow(statusPanel, 2);
		root.Children.Add(statusPanel);

		Content = root;
	}

	private static FrameworkElement MakeCombo(string label, string selectedPath, string itemsPath)
	{
		var panel = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
		panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
		var combo = new ComboBox { MinWidth = 160 };
		combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath));
		combo.SetBinding(Selector.SelectedItemProperty, new Binding(selectedPath) { Mode = BindingMode.TwoWay });
		panel.Children.Add(combo);
		return panel;
	}

	private static FrameworkElement MakeBoundText(string label, string path)
	{
		var panel = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
		panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
		var box = new TextBox { MinWidth = 80 };
		box.SetBinding(TextBox.TextProperty, new Binding(path) { Mode = BindingMode.TwoWay });
		panel.Children.Add(box);
		return panel;
	}

	private static Button MakeButton(string text, string commandPath)
	{
		var button = new Button { Content = text, Margin = new Thickness(0, 18, 8, 0), MinWidth = 80 };
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		return button;
	}

	private static FrameworkElement MakeStatusLine(string label, string path)
	{
		var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
		panel.Children.Add(new TextBlock { Text = $"{label}: ", MinWidth = 140 });
		var value = new TextBlock();
		value.SetBinding(TextBlock.TextProperty, new Binding(path));
		panel.Children.Add(value);
		return panel;
	}
}
