using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultRecoveryDefinition
{
	public FaultRecoveryType RecoveryType { get; set; } = FaultRecoveryType.Exponential;

	public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(3.0);

	public double Rate { get; set; } = 0.15;

	public double TargetNormalState { get; set; } = 0.0;

	public bool KeepMachineFaultedUntilRecovered { get; set; } = true;

	public bool ClearErrorAtRecoveryStart { get; set; } = false;

	public bool ClearErrorAtRecoveryEnd { get; set; } = true;

	public bool ResumeProductionAfterRecovery { get; set; } = true;

	public TimeSpan MinimumStableDuration { get; set; } = TimeSpan.FromSeconds(30.0);
}
