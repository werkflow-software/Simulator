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

	public FaultThresholdSourceType? SafeRecoverySourceType { get; set; }

	public string? SafeRecoverySourceId { get; set; }

	public FaultThresholdComparison? SafeRecoveryComparison { get; set; }

	public double? SafeRecoveryThreshold { get; set; }

	public double SafeRecoveryTolerance { get; set; } = 1.0;

	public bool HasSafeRecoveryThreshold =>
		SafeRecoveryThreshold.HasValue
		&& !string.IsNullOrWhiteSpace(SafeRecoverySourceId)
		&& SafeRecoveryComparison.HasValue;
}
