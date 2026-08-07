using System;
using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.App.ViewModels;

public class MachineCardViewModel
{
	public Guid MachineId { get; }

	public string Name { get; }

	public string Endpoint { get; }

	public bool IsServerOnline { get; }

	public MachineState State { get; }

	public string StateLabel => State.ToGermanLabel();

	public string PartName { get; }

	public string JobName { get; }

	public int ActualCounter { get; }

	public int TargetCounter { get; }

	public double Progress { get; }

	public string ErrorMessage { get; }

	public int Speed { get; }

	public int ActiveScenarioCount { get; }

	public string HighestScenarioSeverity { get; }

	public string ScenarioStatus { get; }

	public bool IsFaultedOrRecovering { get; }

	public MachineCardViewModel(MachineConfiguration machine, MachineRuntimeState? runtime, IReadOnlyList<FaultScenarioRuntimeInfo> activeScenarios)
	{
		MachineId = machine.Id;
		Name = machine.Name;
		Endpoint = machine.Endpoint;
		IsServerOnline = runtime?.IsServerOnline ?? false;
		State = runtime?.State ?? MachineState.Offline;
		PartName = runtime?.PartName ?? "â€”";
		JobName = runtime?.JobName ?? "â€”";
		ActualCounter = runtime?.ActualCounter ?? 0;
		TargetCounter = runtime?.TargetCounter ?? 0;
		Progress = runtime?.ProgressPercent ?? 0.0;
		ErrorMessage = runtime?.ErrorMessage ?? string.Empty;
		Speed = machine.ProductionIntervalMs;
		ActiveScenarioCount = activeScenarios.Count;
		HighestScenarioSeverity = activeScenarios.OrderByDescending((FaultScenarioRuntimeInfo s) => s.Severity).FirstOrDefault()?.Severity.ToString() ?? "â€”";
		ScenarioStatus = activeScenarios.FirstOrDefault()?.LifecycleState.ToString() ?? "â€”";
		IsFaultedOrRecovering = activeScenarios.Any(delegate(FaultScenarioRuntimeInfo s)
		{
			FaultScenarioLifecycleState lifecycleState = s.LifecycleState;
			return (uint)(lifecycleState - 4) <= 1u;
		});
	}
}
