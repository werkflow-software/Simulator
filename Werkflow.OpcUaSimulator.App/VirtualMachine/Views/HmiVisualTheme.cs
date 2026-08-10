using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

/// <summary>
/// Central contrast-safe brushes and button styles for the virtual machine HMI.
/// </summary>
internal static class HmiVisualTheme
{
	public static readonly Brush BgDark = Freeze(new SolidColorBrush(Color.FromRgb(28, 32, 38)));
	public static readonly Brush PanelBg = Freeze(new SolidColorBrush(Color.FromRgb(38, 44, 52)));
	public static readonly Brush HeaderBg = Freeze(new SolidColorBrush(Color.FromRgb(20, 24, 30)));
	public static readonly Brush Border = Freeze(new SolidColorBrush(Color.FromRgb(90, 102, 118)));
	public static readonly Brush TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(244, 246, 248)));
	public static readonly Brush TextSecondary = Freeze(new SolidColorBrush(Color.FromRgb(210, 218, 228)));
	public static readonly Brush TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(168, 178, 192)));
	public static readonly Brush SectionTitle = Freeze(new SolidColorBrush(Color.FromRgb(196, 218, 244)));
	public static readonly Brush ValueAccent = Freeze(new SolidColorBrush(Color.FromRgb(126, 200, 255)));
	public static readonly Brush NavSelectedBg = Freeze(new SolidColorBrush(Color.FromRgb(74, 106, 138)));
	public static readonly Brush NavNormalBg = Freeze(new SolidColorBrush(Color.FromRgb(48, 56, 66)));
	public static readonly Brush ButtonBg = Freeze(new SolidColorBrush(Color.FromRgb(58, 68, 82)));
	public static readonly Brush ButtonDisabledBg = Freeze(new SolidColorBrush(Color.FromRgb(42, 48, 58)));
	public static readonly Brush ButtonDisabledFg = Freeze(new SolidColorBrush(Color.FromRgb(156, 168, 184)));
	public static readonly Brush SimPanelBg = Freeze(new SolidColorBrush(Color.FromRgb(52, 42, 36)));
	public static readonly Brush MetricTileBg = Freeze(new SolidColorBrush(Color.FromRgb(46, 54, 64)));

	public static Style CreateButtonStyle()
	{
		var style = new Style(typeof(Button));
		style.Setters.Add(new Setter(Control.BackgroundProperty, ButtonBg));
		style.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Border));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
		style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));

		var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
		disabled.Setters.Add(new Setter(Control.BackgroundProperty, ButtonDisabledBg));
		disabled.Setters.Add(new Setter(Control.ForegroundProperty, ButtonDisabledFg));
		disabled.Setters.Add(new Setter(Control.BorderBrushProperty, Border));
		style.Triggers.Add(disabled);

		return style;
	}

	public static void ApplyButtonStyle(Button button) => button.Style = CreateButtonStyle();

	private static Brush Freeze(SolidColorBrush brush)
	{
		if (brush.CanFreeze)
		{
			brush.Freeze();
		}
		return brush;
	}
}
