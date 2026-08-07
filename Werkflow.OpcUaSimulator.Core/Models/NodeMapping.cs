namespace Werkflow.OpcUaSimulator.Core.Models;

public class NodeMapping
{
	public NodeSemanticType SemanticType { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	public string BrowseName { get; set; } = string.Empty;

	public string NodeId { get; set; } = string.Empty;

	public OpcUaDataType DataType { get; set; }

	public string InitialValue { get; set; } = string.Empty;

	public bool IsEnabled { get; set; } = true;

	public NodeMapping Clone()
	{
		return new NodeMapping
		{
			SemanticType = SemanticType,
			DisplayName = DisplayName,
			BrowseName = BrowseName,
			NodeId = NodeId,
			DataType = DataType,
			InitialValue = InitialValue,
			IsEnabled = IsEnabled
		};
	}
}
