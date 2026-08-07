using System;
using System.Collections.Generic;
using System.Linq;

namespace Werkflow.OpcUaSimulator.Core.Models;

public class MachineConfiguration
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public bool IsActive { get; set; } = true;

	public string Name { get; set; } = "Maschine";

	public string Description { get; set; } = string.Empty;

	public string Host { get; set; } = "localhost";

	public int Port { get; set; } = 4840;

	public string Endpoint { get; set; } = "opc.tcp://localhost:4840";

	public bool UseCustomEndpoint { get; set; }

	public string NamespaceUri { get; set; } = "urn:werkflow:simulator:machine";

	public double ProductionSpeedFactor { get; set; } = 1.0;

	public MachineState BaseState { get; set; } = MachineState.Idle;

	public double ErrorProbabilityPercent { get; set; } = 0.5;

	public double DisconnectProbabilityPercent { get; set; } = 0.5;

	public int MinErrorDurationMs { get; set; } = 3000;

	public int MaxErrorDurationMs { get; set; } = 60000;

	public int MinOfflineDurationMs { get; set; } = 3000;

	public int MaxOfflineDurationMs { get; set; } = 60000;

	public int ProductionIntervalMs { get; set; } = 2000;

	public int ProductionStepSize { get; set; } = 1;

	public bool ContinueOnWarning { get; set; }

	public bool ContinueOnError { get; set; }

	public bool StartInErrorState { get; set; }

	public string? PhysicalProfileId { get; set; }

	public List<NodeMapping> Nodes { get; set; } = (from n in NodeSemanticDefaults.CreateDefaultMappings()
		select n.Clone()).ToList();

	public void UpdateEndpointFromHostPort()
	{
		if (!UseCustomEndpoint)
		{
			Endpoint = $"opc.tcp://{Host}:{Port}";
		}
	}

	public MachineConfiguration Clone()
	{
		return new MachineConfiguration
		{
			Id = Guid.NewGuid(),
			IsActive = IsActive,
			Name = Name + " (Kopie)",
			Description = Description,
			Host = Host,
			Port = Port,
			Endpoint = Endpoint,
			UseCustomEndpoint = UseCustomEndpoint,
			NamespaceUri = NamespaceUri + "-copy",
			ProductionSpeedFactor = ProductionSpeedFactor,
			BaseState = BaseState,
			ErrorProbabilityPercent = ErrorProbabilityPercent,
			DisconnectProbabilityPercent = DisconnectProbabilityPercent,
			MinErrorDurationMs = MinErrorDurationMs,
			MaxErrorDurationMs = MaxErrorDurationMs,
			MinOfflineDurationMs = MinOfflineDurationMs,
			MaxOfflineDurationMs = MaxOfflineDurationMs,
			ProductionIntervalMs = ProductionIntervalMs,
			ProductionStepSize = ProductionStepSize,
			ContinueOnWarning = ContinueOnWarning,
			ContinueOnError = ContinueOnError,
			StartInErrorState = StartInErrorState,
			PhysicalProfileId = PhysicalProfileId,
			Nodes = Nodes.Select((NodeMapping n) => n.Clone()).ToList()
		};
	}
}
