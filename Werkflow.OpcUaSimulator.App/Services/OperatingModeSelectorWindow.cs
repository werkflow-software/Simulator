using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Werkflow.OpcUaSimulator.App;

namespace Werkflow.OpcUaSimulator.App.Services;

public sealed class OperatingModeSelectorWindow : Window
{
	private readonly ApplicationSessionCoordinator _sessionCoordinator;

	public OperatingModeSelectorWindow(ApplicationSessionCoordinator sessionCoordinator)
	{
		_sessionCoordinator = sessionCoordinator;
		Title = "Werkflow OPC UA Simulator";
		Width = 520;
		Height = 420;
		MinWidth = 480;
		MinHeight = 380;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		ResizeMode = ResizeMode.NoResize;
		Background = new SolidColorBrush(Color.FromRgb(34, 38, 44));
		Content = BuildContent();
	}

	public void ResetForDisplay()
	{
		ShowInTaskbar = true;
		Visibility = Visibility.Visible;
	}

	private UIElement BuildContent()
	{
		var root = new StackPanel
		{
			Margin = new Thickness(28, 24, 28, 24),
			VerticalAlignment = VerticalAlignment.Center
		};

		root.Children.Add(new TextBlock
		{
			Text = "Werkflow OPC UA Simulator",
			FontSize = 22,
			FontWeight = FontWeights.SemiBold,
			Foreground = Brushes.White,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 8)
		});
		root.Children.Add(new TextBlock
		{
			Text = "Betriebsmodus wählen",
			FontSize = 14,
			Foreground = new SolidColorBrush(Color.FromRgb(180, 188, 198)),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 20)
		});

		root.Children.Add(CreateModeButton(
			"SIMULATOR",
			"4 Maschinen – technisches Testumfeld",
			async () =>
			{
				Hide();
				ShowInTaskbar = false;
				await _sessionCoordinator.StartClassicSimulatorAsync();
			}));

		root.Children.Add(CreateModeButton(
			"VIRTUAL MACHINE",
			"Werkflow Virtual Laser 01 – realistisches HMI",
			async () =>
			{
				Hide();
				ShowInTaskbar = false;
				await _sessionCoordinator.StartVirtualMachineAsync();
			}));

		var exitButton = new Button
		{
			Content = "Beenden",
			Margin = new Thickness(0, 16, 0, 0),
			Padding = new Thickness(16, 8, 16, 8),
			HorizontalAlignment = HorizontalAlignment.Center,
			Background = new SolidColorBrush(Color.FromRgb(52, 58, 66)),
			Foreground = new SolidColorBrush(Color.FromRgb(200, 206, 214)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(72, 80, 90))
		};
		exitButton.Click += (_, _) => Application.Current.Shutdown();
		root.Children.Add(exitButton);

		return root;
	}

	private static Button CreateModeButton(string title, string subtitle, Func<Task> onClick)
	{
		var panel = new StackPanel();
		panel.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 18,
			FontWeight = FontWeights.Bold,
			Foreground = Brushes.White
		});
		panel.Children.Add(new TextBlock
		{
			Text = subtitle,
			FontSize = 13,
			Foreground = new SolidColorBrush(Color.FromRgb(170, 180, 192)),
			Margin = new Thickness(0, 4, 0, 0)
		});

		var button = new Button
		{
			Content = panel,
			Margin = new Thickness(0, 0, 0, 12),
			Padding = new Thickness(18, 14, 18, 14),
			HorizontalContentAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Color.FromRgb(46, 54, 64)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(72, 82, 96)),
			Cursor = System.Windows.Input.Cursors.Hand
		};
		button.Click += async (_, _) => await onClick();
		return button;
	}
}
