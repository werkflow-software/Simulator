using System;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

public sealed class FaultThresholdRule
{
	public string RuleId { get; set; } = string.Empty;

	public FaultThresholdSourceType SourceType { get; set; }

	public string SourceId { get; set; } = string.Empty;

	public FaultThresholdComparison Comparison { get; set; }

	public double ThresholdValue { get; set; }

	public double? ThresholdValueSecondary { get; set; }

	public TimeSpan MinimumDuration { get; set; } = TimeSpan.FromSeconds(10.0);

	public string FaultCode { get; set; } = string.Empty;

	public string FaultMessage { get; set; } = string.Empty;

	public bool SetErrorActive { get; set; } = true;

	public bool SetMachineStateError { get; set; } = true;

	public bool StopProduction { get; set; } = true;

	public bool KeepServerOnline { get; set; } = true;

	public bool AutoRecover { get; set; } = false;

	public bool IsEnabled { get; set; } = true;

	public bool DisabledInControlRun { get; set; } = true;
}
