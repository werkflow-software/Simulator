using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.Converters;

public sealed class MachineStateBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is MachineState machineState)
		{
			if (1 == 0)
			{
			}
			SolidColorBrush result = machineState switch
			{
				MachineState.Offline => new SolidColorBrush(Color.FromRgb(120, 120, 120)), 
				MachineState.Idle => new SolidColorBrush(Color.FromRgb(90, 140, 200)), 
				MachineState.Running => new SolidColorBrush(Color.FromRgb(46, 139, 87)), 
				MachineState.Warning => new SolidColorBrush(Color.FromRgb(218, 165, 32)), 
				MachineState.Error => new SolidColorBrush(Color.FromRgb(200, 60, 60)), 
				MachineState.Paused => new SolidColorBrush(Color.FromRgb(100, 100, 160)), 
				MachineState.Setup => new SolidColorBrush(Color.FromRgb(130, 100, 180)), 
				_ => Brushes.Gray, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
		return Brushes.Gray;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
