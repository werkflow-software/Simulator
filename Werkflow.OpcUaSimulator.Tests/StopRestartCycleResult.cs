namespace Werkflow.OpcUaSimulator.Tests;

public sealed class StopRestartCycleResult
{
	public int Cycle { get; set; }

	public int NodesBeforeStop { get; set; }

	public bool RegistryCleared { get; set; }

	public bool ServerStopped { get; set; }

	public int NodesAfterRestart { get; set; }

	public bool SameNodeCount { get; set; }

	public int PublisherCount { get; set; }

	public bool SinglePublisher { get; set; }

	public int PublishersBeforeStop { get; set; }

	public bool PortAvailable { get; set; }
}
