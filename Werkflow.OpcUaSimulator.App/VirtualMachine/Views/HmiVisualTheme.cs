using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

/// <summary>
/// Industrial HMI theme: light gray / silver base, dark text, status colors (green/yellow/red/gray).
/// </summary>
internal static class HmiVisualTheme
{
	// Base surfaces — light metallic gray (reference HMI style)
	public static readonly Brush BgDark = Freeze(new SolidColorBrush(Color.FromRgb(196, 200, 204)));
	public static readonly Brush PanelBg = Freeze(new SolidColorBrush(Color.FromRgb(228, 230, 234)));
	public static readonly Brush HeaderBg = Freeze(new SolidColorBrush(Color.FromRgb(176, 184, 192)));
	public static readonly Brush PanelBorder = Freeze(new SolidColorBrush(Color.FromRgb(88, 96, 104)));
	public static readonly Brush InsetBg = Freeze(new SolidColorBrush(Color.FromRgb(48, 52, 58)));
	public static readonly Brush InsetBorder = Freeze(new SolidColorBrush(Color.FromRgb(32, 36, 42)));

	// Typography — dark on light
	public static readonly Brush TextPrimary = Freeze(new SolidColorBrush(Color.FromRgb(28, 32, 38)));
	public static readonly Brush TextSecondary = Freeze(new SolidColorBrush(Color.FromRgb(56, 64, 72)));
	public static readonly Brush TextMuted = Freeze(new SolidColorBrush(Color.FromRgb(96, 104, 112)));
	public static readonly Brush TextOnDark = Freeze(new SolidColorBrush(Color.FromRgb(248, 250, 252)));
	public static readonly Brush SectionTitle = Freeze(new SolidColorBrush(Color.FromRgb(0, 72, 108)));
	public static readonly Brush ValueAccent = Freeze(new SolidColorBrush(Color.FromRgb(0, 56, 88)));
	public static readonly Brush AccentLine = Freeze(new SolidColorBrush(Color.FromRgb(0, 140, 168)));

	// Navigation & controls
	public static readonly Brush NavSelectedBg = Freeze(new SolidColorBrush(Color.FromRgb(148, 188, 212)));
	public static readonly Brush NavNormalBg = Freeze(new SolidColorBrush(Color.FromRgb(200, 206, 214)));
	public static readonly Brush ButtonBg = Freeze(new SolidColorBrush(Color.FromRgb(210, 216, 224)));
	public static readonly Brush ButtonHoverBg = Freeze(new SolidColorBrush(Color.FromRgb(188, 198, 210)));
	public static readonly Brush ButtonPressedBg = Freeze(new SolidColorBrush(Color.FromRgb(168, 178, 190)));
	public static readonly Brush ButtonDisabledBg = Freeze(new SolidColorBrush(Color.FromRgb(184, 188, 194)));
	public static readonly Brush ButtonDisabledFg = Freeze(new SolidColorBrush(Color.FromRgb(128, 136, 144)));

	// Panels
	public static readonly Brush SimPanelBg = Freeze(new SolidColorBrush(Color.FromRgb(218, 212, 200)));
	public static readonly Brush MetricTileBg = Freeze(new SolidColorBrush(Color.FromRgb(236, 238, 242)));
	public static readonly Brush ListItemBg = Freeze(new SolidColorBrush(Color.FromRgb(224, 228, 234)));
	public static readonly Brush ListItemSelectedBg = Freeze(new SolidColorBrush(Color.FromRgb(148, 188, 212)));

	// Status tones
	public static readonly Brush StatusRunning = Freeze(new SolidColorBrush(Color.FromRgb(72, 148, 72)));
	public static readonly Brush StatusSetup = Freeze(new SolidColorBrush(Color.FromRgb(200, 168, 48)));
	public static readonly Brush StatusError = Freeze(new SolidColorBrush(Color.FromRgb(176, 56, 48)));
	public static readonly Brush StatusIdle = Freeze(new SolidColorBrush(Color.FromRgb(140, 148, 156)));
	public static readonly Brush StatusBarRunning = Freeze(new SolidColorBrush(Color.FromRgb(56, 132, 56)));
	public static readonly Brush StatusBarSetup = Freeze(new SolidColorBrush(Color.FromRgb(184, 152, 40)));
	public static readonly Brush StatusBarIdle = Freeze(new SolidColorBrush(Color.FromRgb(120, 128, 136)));

	public static Brush PhaseBannerBrush(string tone) =>
		tone switch
		{
			"running" => StatusRunning,
			"cutting" => StatusRunning,
			"setup" => StatusSetup,
			"error" => StatusError,
			_ => StatusIdle
		};

	public static Style CreateButtonStyle()
	{
		var style = new Style(typeof(Button));
		style.Setters.Add(new Setter(Control.BackgroundProperty, ButtonBg));
		style.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, PanelBorder));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 6, 10, 6)));
		style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
		style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));

		var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, ButtonHoverBg));
		style.Triggers.Add(hover);

		var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
		pressed.Setters.Add(new Setter(Control.BackgroundProperty, ButtonPressedBg));
		style.Triggers.Add(pressed);

		var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
		disabled.Setters.Add(new Setter(Control.BackgroundProperty, ButtonDisabledBg));
		disabled.Setters.Add(new Setter(Control.ForegroundProperty, ButtonDisabledFg));
		style.Triggers.Add(disabled);

		return style;
	}

	public static void ApplyButtonStyle(Button button) => button.Style = CreateButtonStyle();

	public static void ApplyListBoxStyle(ListBox listBox)
	{
		listBox.Background = ListItemBg;
		listBox.Foreground = TextPrimary;
		listBox.BorderBrush = PanelBorder;
		listBox.BorderThickness = new Thickness(1);

		var itemStyle = new Style(typeof(ListBoxItem));
		itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, ListItemBg));
		itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
		itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 4, 6, 4)));

		var selected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, ListItemSelectedBg));
		itemStyle.Triggers.Add(selected);

		var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, ButtonHoverBg));
		itemStyle.Triggers.Add(hover);

		listBox.ItemContainerStyle = itemStyle;
	}

	private static ControlTemplate CreateButtonTemplate()
	{
		var template = new ControlTemplate(typeof(Button));
		var borderFactory = new FrameworkElementFactory(typeof(Border));
		borderFactory.Name = "RootBorder";
		borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
		borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

		var content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(TextElement.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
		borderFactory.AppendChild(content);
		template.VisualTree = borderFactory;
		return template;
	}

	private static Brush Freeze(SolidColorBrush brush)
	{
		if (brush.CanFreeze)
		{
			brush.Freeze();
		}
		return brush;
	}
}
