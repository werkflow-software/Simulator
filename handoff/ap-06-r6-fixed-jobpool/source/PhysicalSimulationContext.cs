using System;
using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

public sealed class PhysicalSimulationContext
{
	public SignalGenerationMode GenerationMode { get; set; } = SignalGenerationMode.Technical;

	public PhysicalVerificationMode VerificationMode { get; set; } = PhysicalVerificationMode.Normal;

	public ProcessPhase CurrentPhase { get; set; } = ProcessPhase.Idle;

	public DateTimeOffset PhaseStartedAt { get; set; } = DateTimeOffset.UtcNow;

	public int Seed { get; set; }

	public TimeSpan SimulationTime { get; set; }

	public TimeSpan PhaseElapsedSimulationTime { get; set; }

	public double TimeFactor { get; set; } = 1.0;

	public bool IsEngineActive { get; set; }

	public bool DiagnosisModeEnabled { get; set; }

	public PhysicalSimulationMetrics Metrics { get; } = new PhysicalSimulationMetrics();

	public PhysicalJobState Job { get; set; } = new PhysicalJobState();

	public List<ProcessPhaseTransition> PhaseTransitions { get; } = new List<ProcessPhaseTransition>();

	public PerMachinePhysicsState PhysicsState { get; } = new PerMachinePhysicsState();

	public DateTimeOffset? LastCalculationAt { get; set; }

	public FaultScenarioMachineContext FaultScenarios { get; } = new FaultScenarioMachineContext();

	public bool ProductionDrivenJobs { get; set; } = true;

	public bool IsJobChangePauseActive { get; set; }

	public TimeSpan JobChangePauseUntil { get; set; }

	public FixedProductionJobDefinition? PendingJobDefinition { get; set; }

	public TimeSpan? OverrideSetupDuration { get; set; }

	public void ResetPhaseState()
	{
		CurrentPhase = ProcessPhase.Idle;
		PhaseStartedAt = DateTimeOffset.UtcNow;
		PhaseElapsedSimulationTime = TimeSpan.Zero;
		PhaseTransitions.Clear();
		Metrics.PhaseChanges = 0;
	}
}
