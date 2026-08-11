using System;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class PhysicalJobCoordinator
{
	public static void Initialize(PhysicalSimulationContext context)
	{
		ApplyDefinition(context, FixedSimulationCatalog.GetDefinition(0), null);
	}

	public static void ApplyDefinition(
		PhysicalSimulationContext context,
		FixedProductionJobDefinition definition,
		PhysicalMachineRuntime? runtime)
	{
		context.Job.CatalogIndex = definition.CatalogIndex;
		context.Job.JobIndex = definition.CatalogIndex + 1;
		context.Job.JobName = definition.JobName;
		context.Job.PartName = definition.PartName;
		context.Job.TargetQuantity = definition.TargetQuantity;
		context.Job.ProducedQuantity = 0;
		context.Job.JobStartedAtUtc = DateTimeOffset.UtcNow;
		context.Job.MaterialName = definition.MaterialName;
		context.Job.MaterialThicknessMm = definition.MaterialThicknessMm;
		context.Job.RecipeName = definition.RecipeName;
		context.Job.ProgramName = definition.ProgramName;
		context.Job.ProcessLoadFactor = definition.ProcessLoadFactor;
		context.Job.FeedRateFactor = definition.FeedRateFactor;
		context.Metrics.JobChanges++;
		if (runtime != null)
		{
			ApplyStableSignals(runtime, definition);
		}
	}

	public static void SyncProductionCounters(PhysicalSimulationContext context, int actualCounter, int targetCounter)
	{
		context.Job.ProducedQuantity = actualCounter;
		context.Job.TargetQuantity = targetCounter;
	}

	public static void TickProductionCounters(PhysicalSimulationContext context)
	{
		if (context.IsJobChangePauseActive)
		{
			return;
		}

		ProcessPhase currentPhase = context.CurrentPhase;
		if ((uint)(currentPhase - 3) <= 1u)
		{
			context.Job.ProducedQuantity++;
		}
	}

	private static void ApplyStableSignals(PhysicalMachineRuntime runtime, FixedProductionJobDefinition definition)
	{
		foreach (SignalRuntimeState signal in runtime.Signals)
		{
			switch (signal.SignalId)
			{
			case "Process.MaterialThickness":
				signal.CurrentValue = definition.MaterialThicknessMm;
				signal.TargetValue = definition.MaterialThicknessMm;
				break;
			case "Process.MaterialName":
				signal.CurrentStringValue = definition.MaterialName;
				break;
			case "Process.RecipeName":
				signal.CurrentStringValue = definition.RecipeName;
				break;
			case "Production.RecipeName":
				signal.CurrentStringValue = definition.RecipeName;
				break;
			case "Production.ActiveProgram":
				signal.CurrentStringValue = definition.ProgramName;
				break;
			case "Production.MaterialDesignation":
				signal.CurrentStringValue = $"Sheet-{definition.MaterialThicknessMm:0.#}mm";
				break;
			case "Process.FeedRate":
			case "Process.FeedRateTarget":
				double feed = 1200.0 * definition.FeedRateFactor;
				signal.CurrentValue = feed;
				signal.TargetValue = feed;
				break;
			}
		}
	}
}
