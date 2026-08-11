using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Werkflow.OpcUaSimulator.App.VirtualMachine.Views;
using Werkflow.OpcUaSimulator.Core.Defaults;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Views;

public sealed class JobSelectionWindow : Window
{
	public int? SelectedCatalogIndex { get; private set; }

	public JobSelectionWindow()
	{
		Title = "Auftrag wählen";
		Width = 520;
		Height = 480;
		MinWidth = 420;
		MinHeight = 360;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = HmiVisualTheme.PanelBg;

		var root = new Grid { Margin = new Thickness(12) };
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		var title = new TextBlock
		{
			Text = "Festen Job aus Pool auswählen",
			FontSize = 16,
			FontWeight = FontWeights.SemiBold,
			Foreground = HmiVisualTheme.TextPrimary,
			Margin = new Thickness(0, 0, 0, 8)
		};
		root.Children.Add(title);

		var list = new ListBox
		{
			Margin = new Thickness(0, 0, 0, 8),
			ItemTemplate = CreateJobItemTemplate()
		};
		HmiVisualTheme.ApplyListBoxStyle(list);
		foreach (FixedProductionJobDefinition definition in FixedSimulationCatalog.GetDefinitions())
		{
			list.Items.Add(new JobListItem(definition));
		}
		Grid.SetRow(list, 1);
		root.Children.Add(list);

		var buttons = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		var loadBtn = new Button { Content = "Laden", Margin = new Thickness(4), Padding = new Thickness(16, 6, 16, 6), IsDefault = true };
		var cancelBtn = new Button { Content = "Abbrechen", Margin = new Thickness(4), Padding = new Thickness(16, 6, 16, 6), IsCancel = true };
		HmiVisualTheme.ApplyButtonStyle(loadBtn);
		HmiVisualTheme.ApplyButtonStyle(cancelBtn);
		loadBtn.Click += (_, _) =>
		{
			if (list.SelectedItem is JobListItem item)
			{
				SelectedCatalogIndex = item.CatalogIndex;
				DialogResult = true;
			}
		};
		cancelBtn.Click += (_, _) => DialogResult = false;
		buttons.Children.Add(loadBtn);
		buttons.Children.Add(cancelBtn);
		Grid.SetRow(buttons, 2);
		root.Children.Add(buttons);

		Content = root;
	}

	private static DataTemplate CreateJobItemTemplate()
	{
		var template = new DataTemplate();
		var factory = new FrameworkElementFactory(typeof(TextBlock));
		factory.SetBinding(TextBlock.TextProperty, new Binding(nameof(JobListItem.DisplayText)));
		factory.SetValue(TextBlock.FontSizeProperty, 13.0);
		factory.SetValue(TextBlock.MarginProperty, new Thickness(2));
		template.VisualTree = factory;
		return template;
	}

	private sealed class JobListItem(FixedProductionJobDefinition definition)
	{
		public int CatalogIndex => definition.CatalogIndex;

		public string DisplayText =>
			$"{definition.JobName}\n{definition.PartName}\n{definition.TargetQuantity} Teile\n{definition.MaterialName} {definition.MaterialThicknessMm:0.#} mm";
	}
}
