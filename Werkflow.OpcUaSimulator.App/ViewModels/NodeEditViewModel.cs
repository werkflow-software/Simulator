using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class NodeEditViewModel : ObservableObject
{
	private string _displayName;

	private string _browseName;

	private string _nodeId;

	private bool _isEnabled;

	private string _liveValue;

	public NodeSemanticType SemanticType { get; }

	public string SemanticLabel { get; }

	public string DataType { get; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string DisplayName
	{
		get
		{
			return _displayName;
		}
		[MemberNotNull("_displayName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_displayName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DisplayName);
				_displayName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DisplayName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string BrowseName
	{
		get
		{
			return _browseName;
		}
		[MemberNotNull("_browseName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_browseName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BrowseName);
				_browseName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BrowseName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string NodeId
	{
		get
		{
			return _nodeId;
		}
		[MemberNotNull("_nodeId")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_nodeId, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NodeId);
				_nodeId = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NodeId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsEnabled);
				_isEnabled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string LiveValue
	{
		get
		{
			return _liveValue;
		}
		[MemberNotNull("_liveValue")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_liveValue, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LiveValue);
				_liveValue = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LiveValue);
			}
		}
	}

	public NodeEditViewModel(NodeMapping mapping, string liveValue)
	{
		SemanticType = mapping.SemanticType;
		SemanticLabel = NodeSemanticDefaults.GetSemanticLabel(mapping.SemanticType);
		DisplayName = mapping.DisplayName;
		BrowseName = mapping.BrowseName;
		NodeId = mapping.NodeId;
		DataType = mapping.DataType.ToString();
		IsEnabled = mapping.IsEnabled;
		LiveValue = liveValue;
	}

	public void ApplyTo(NodeMapping mapping)
	{
		mapping.DisplayName = DisplayName;
		mapping.BrowseName = BrowseName;
		mapping.NodeId = NodeId;
		mapping.IsEnabled = IsEnabled;
	}
}
