using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Publishing;

public interface IPhysicalSignalPublisher
{
	PhysicalPublisherState State { get; }

	Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task PauseAsync();

	Task ResumeAsync();

	Task StopAsync();

	bool PublishSignal(string signalId, object? value, bool force = false);
}
