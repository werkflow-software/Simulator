using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class MachineListItemViewModel : ObservableObject
{
	private readonly ISimulationEngine _simulationEngine;

	private string _name;

	private string _description;

	private string _host;

	private int _port;

	private string _endpoint;

	private bool _useCustomEndpoint;

	private string _namespaceUri;

	private bool _isActive;

	private double _productionSpeedFactor;

	private double _errorProbabilityPercent;

	private double _disconnectProbabilityPercent;

	private bool _isServerOnline;

	private MachineState _state;

	private int _connectedClients;

	public Guid Id { get; }

	public string StateLabel => State.ToGermanLabel();

	public string ServerLabel => IsServerOnline ? "Online" : "Offline";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Name
	{
		get
		{
			return _name;
		}
		[MemberNotNull("_name")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_name, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Name);
				_name = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Name);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Description
	{
		get
		{
			return _description;
		}
		[MemberNotNull("_description")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_description, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Description);
				_description = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Description);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Host
	{
		get
		{
			return _host;
		}
		[MemberNotNull("_host")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_host, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Host);
				_host = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Host);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_port, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Port);
				_port = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Port);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Endpoint
	{
		get
		{
			return _endpoint;
		}
		[MemberNotNull("_endpoint")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_endpoint, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Endpoint);
				_endpoint = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Endpoint);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool UseCustomEndpoint
	{
		get
		{
			return _useCustomEndpoint;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_useCustomEndpoint, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UseCustomEndpoint);
				_useCustomEndpoint = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UseCustomEndpoint);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string NamespaceUri
	{
		get
		{
			return _namespaceUri;
		}
		[MemberNotNull("_namespaceUri")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_namespaceUri, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NamespaceUri);
				_namespaceUri = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NamespaceUri);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isActive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsActive);
				_isActive = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsActive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double ProductionSpeedFactor
	{
		get
		{
			return _productionSpeedFactor;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_productionSpeedFactor, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProductionSpeedFactor);
				_productionSpeedFactor = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProductionSpeedFactor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double ErrorProbabilityPercent
	{
		get
		{
			return _errorProbabilityPercent;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_errorProbabilityPercent, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ErrorProbabilityPercent);
				_errorProbabilityPercent = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ErrorProbabilityPercent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double DisconnectProbabilityPercent
	{
		get
		{
			return _disconnectProbabilityPercent;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_disconnectProbabilityPercent, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DisconnectProbabilityPercent);
				_disconnectProbabilityPercent = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DisconnectProbabilityPercent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsServerOnline
	{
		get
		{
			return _isServerOnline;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isServerOnline, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsServerOnline);
				_isServerOnline = value;
				OnIsServerOnlineChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsServerOnline);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public MachineState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (!EqualityComparer<MachineState>.Default.Equals(_state, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.State);
				_state = value;
				OnStateChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.State);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ConnectedClients
	{
		get
		{
			return _connectedClients;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_connectedClients, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConnectedClients);
				_connectedClients = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConnectedClients);
			}
		}
	}

	public MachineListItemViewModel(MachineConfiguration machine, MachineRuntimeState? runtime, ISimulationEngine simulationEngine)
	{
		_simulationEngine = simulationEngine;
		Id = machine.Id;
		Name = machine.Name;
		Description = machine.Description;
		Host = machine.Host;
		Port = machine.Port;
		Endpoint = machine.Endpoint;
		UseCustomEndpoint = machine.UseCustomEndpoint;
		NamespaceUri = machine.NamespaceUri;
		IsActive = machine.IsActive;
		ProductionSpeedFactor = machine.ProductionSpeedFactor;
		ErrorProbabilityPercent = machine.ErrorProbabilityPercent;
		DisconnectProbabilityPercent = machine.DisconnectProbabilityPercent;
		IsServerOnline = runtime?.IsServerOnline ?? false;
		State = runtime?.State ?? MachineState.Offline;
		ConnectedClients = runtime?.ConnectedClients ?? 0;
	}

	public void SyncFromConfiguration(MachineConfiguration machine)
	{
		Name = machine.Name;
		Description = machine.Description;
		Host = machine.Host;
		Port = machine.Port;
		Endpoint = machine.Endpoint;
		UseCustomEndpoint = machine.UseCustomEndpoint;
		NamespaceUri = machine.NamespaceUri;
		IsActive = machine.IsActive;
		ProductionSpeedFactor = machine.ProductionSpeedFactor;
		ErrorProbabilityPercent = machine.ErrorProbabilityPercent;
		DisconnectProbabilityPercent = machine.DisconnectProbabilityPercent;
	}

	public void SyncRuntimeState(MachineRuntimeState? runtime)
	{
		IsServerOnline = runtime?.IsServerOnline ?? false;
		State = runtime?.State ?? MachineState.Offline;
		ConnectedClients = runtime?.ConnectedClients ?? 0;
	}

	public void ApplyTo(MachineConfiguration machine)
	{
		machine.Name = Name;
		machine.Description = Description;
		machine.Host = Host;
		machine.Port = Port;
		machine.Endpoint = Endpoint;
		machine.UseCustomEndpoint = UseCustomEndpoint;
		machine.NamespaceUri = NamespaceUri;
		machine.IsActive = IsActive;
		machine.ProductionSpeedFactor = ProductionSpeedFactor;
		machine.ErrorProbabilityPercent = ErrorProbabilityPercent;
		machine.DisconnectProbabilityPercent = DisconnectProbabilityPercent;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnIsServerOnlineChanged(bool value)
	{
		OnPropertyChanged("ServerLabel");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnStateChanged(MachineState value)
	{
		OnPropertyChanged("StateLabel");
	}
}
