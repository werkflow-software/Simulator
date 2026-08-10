using System.Collections.Generic;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Vigil;

public sealed class RecordedVigilEventSource : IVigilEventSource
{
	private readonly Dictionary<string, List<VigilEvent>> _events = new(StringComparer.OrdinalIgnoreCase);

	public bool IsConnected => true;

	public void AddEvents(string experimentId, IEnumerable<VigilEvent> events)
	{
		if (!_events.TryGetValue(experimentId, out var list))
		{
			list = [];
			_events[experimentId] = list;
		}
		list.AddRange(events);
	}

	public IReadOnlyList<VigilEvent> GetEvents(string experimentId) =>
		_events.TryGetValue(experimentId, out var list) ? list : [];
}
