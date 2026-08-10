using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Models;

public sealed class HmiMetricItem : INotifyPropertyChanged
{
	private string _label;
	private string _value;
	private string _unit;
	private bool _isWarning;
	private bool _isError;

	public HmiMetricItem(string label, string unit = "")
	{
		_label = label;
		_unit = unit;
		_value = "—";
	}

	public string Label => _label;

	public string Unit => _unit;

	public string Value
	{
		get => _value;
		set
		{
			if (_value != value)
			{
				_value = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsWarning
	{
		get => _isWarning;
		set
		{
			if (_isWarning != value)
			{
				_isWarning = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsError
	{
		get => _isError;
		set
		{
			if (_isError != value)
			{
				_isError = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
