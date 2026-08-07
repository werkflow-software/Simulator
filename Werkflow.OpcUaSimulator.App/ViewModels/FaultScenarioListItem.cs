using System;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public sealed class FaultScenarioListItem
{
	public string ScenarioId { get; }

	public string DisplayName { get; }

	public string Category { get; }

	public string Severity { get; }

	public string Description { get; }

	public TimeSpan DefaultDuration { get; }

	public bool HasThreshold { get; }

	public string RecoveryType { get; }

	public bool SupportsControlRun { get; }

	public FaultScenarioListItem(FaultScenarioDefinition definition)
	{
		ScenarioId = definition.ScenarioId;
		DisplayName = definition.DisplayName;
		Category = definition.Category.ToString();
		Severity = definition.Severity.ToString();
		Description = definition.Description;
		DefaultDuration = definition.DefaultDuration;
		HasThreshold = definition.ThresholdRules.Any((FaultThresholdRule r) => r.IsEnabled);
		RecoveryType = definition.Recovery.RecoveryType.ToString();
		SupportsControlRun = definition.SupportsNonFaultingControlRun;
	}
}
