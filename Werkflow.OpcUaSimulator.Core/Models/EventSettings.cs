using System;
using System.Collections.Generic;
using System.Linq;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class EventSettings
{
	public List<EventTypeSettings> Events { get; set; } = CreateDefaults();

	public List<string> ErrorMessages { get; set; } = DefaultErrorMessages.Create();

	public static List<EventTypeSettings> CreateDefaults()
	{
		return Enum.GetValues<SimulationEventType>().Select(delegate(SimulationEventType t)
		{
			EventTypeSettings eventTypeSettings = new EventTypeSettings
			{
				EventType = t,
				IsEnabled = (t == SimulationEventType.ProductionStop)
			};
			EventTypeSettings eventTypeSettings2 = eventTypeSettings;
			if (1 == 0)
			{
			}
			double probabilityPercent = t switch
			{
				SimulationEventType.Error => 0.5, 
				SimulationEventType.Warning => 1.0, 
				SimulationEventType.ProductionStop => 3.0, 
				SimulationEventType.OpcUaDisconnect => 0.5, 
				_ => 1.0, 
			};
			if (1 == 0)
			{
			}
			eventTypeSettings2.ProbabilityPercent = probabilityPercent;
			EventTypeSettings eventTypeSettings3 = eventTypeSettings;
			bool flag = (((uint)t <= 1u || t == SimulationEventType.OpcUaDisconnect) ? true : false);
			eventTypeSettings3.MinDurationMs = (flag ? 3000 : 1000);
			EventTypeSettings eventTypeSettings4 = eventTypeSettings;
			bool flag2 = (((uint)t <= 1u || t == SimulationEventType.OpcUaDisconnect) ? true : false);
			eventTypeSettings4.MaxDurationMs = (flag2 ? 60000 : 10000);
			return eventTypeSettings;
		}).ToList();
	}
}
