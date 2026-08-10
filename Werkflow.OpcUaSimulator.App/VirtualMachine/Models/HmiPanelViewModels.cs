using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.App.VirtualMachine.Models;

public sealed class HmiAxisPanelViewModel : INotifyPropertyChanged
{
	private string _position = "—";
	private string _targetPosition = "—";
	private string _speed = "—";
	private string _current = "—";
	private string _torque = "—";
	private string _temperature = "—";
	private string _load = "—";
	private string _positionError = "—";
	private string _servoState = "—";

	private string _axisName = "";

	public string AxisName
	{
		get => _axisName;
		set { _axisName = value; OnPropertyChanged(); }
	}

	public string Position
	{
		get => _position;
		set { _position = value; OnPropertyChanged(); }
	}

	public string TargetPosition
	{
		get => _targetPosition;
		set { _targetPosition = value; OnPropertyChanged(); }
	}

	public string Speed
	{
		get => _speed;
		set { _speed = value; OnPropertyChanged(); }
	}

	public string Current
	{
		get => _current;
		set { _current = value; OnPropertyChanged(); }
	}

	public string Torque
	{
		get => _torque;
		set { _torque = value; OnPropertyChanged(); }
	}

	public string Temperature
	{
		get => _temperature;
		set { _temperature = value; OnPropertyChanged(); }
	}

	public string Load
	{
		get => _load;
		set { _load = value; OnPropertyChanged(); }
	}

	public string PositionError
	{
		get => _positionError;
		set { _positionError = value; OnPropertyChanged(); }
	}

	public string ServoState
	{
		get => _servoState;
		set { _servoState = value; OnPropertyChanged(); }
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class HmiMotorGroupViewModel
{
	public required string GroupName { get; init; }

	public ObservableCollection<HmiMetricItem> Metrics { get; } = [];
}

public sealed class HmiTemperatureTileViewModel : INotifyPropertyChanged
{
	private string _value = "—";
	private string _normalRange = "";

	public required string Label { get; init; }

	public string Unit { get; init; } = "";

	public string Value
	{
		get => _value;
		set { _value = value; OnPropertyChanged(); }
	}

	public string NormalRange
	{
		get => _normalRange;
		set { _normalRange = value; OnPropertyChanged(); }
	}

	public bool IsWarning { get; set; }

	public bool IsError { get; set; }

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
