using System;
using Opc.Ua;
using Opc.Ua.Server;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.OpcUa;

internal sealed class SimulatorServer : StandardServer
{
	private readonly MachineConfiguration _machine;

	private readonly MachineRuntimeState _runtime;

	private readonly ILogService _logService;

	private readonly PhysicalSignalPublishingCoordinator? _physicalCoordinator;

	public SimulatorNodeManager? NodeManager { get; private set; }

	public SimulatorServer(MachineConfiguration machine, MachineRuntimeState runtime, ILogService logService, PhysicalSignalPublishingCoordinator? physicalCoordinator = null)
	{
		_machine = machine;
		_runtime = runtime;
		_logService = logService;
		_physicalCoordinator = physicalCoordinator;
	}

	protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
	{
		NodeManager = new SimulatorNodeManager(server, configuration, _machine, _runtime, _logService, _physicalCoordinator);
		return new MasterNodeManager(server, configuration, null, NodeManager);
	}

	protected override ServerProperties LoadServerProperties()
	{
		return new ServerProperties
		{
			ManufacturerName = "Werkflow",
			ProductName = "OPC UA Simulator",
			ProductUri = "urn:werkflow:opcua-simulator",
			SoftwareVersion = "1.0.0",
			BuildNumber = "1",
			BuildDate = DateTime.UtcNow
		};
	}

	protected override void ValidateRequest(RequestHeader requestHeader)
	{
		try
		{
			base.ValidateRequest(requestHeader);
		}
		catch (Exception ex)
		{
			_logService.Log(LogCategory.Server, "Request-Validierung fehlgeschlagen: " + ex.Message, _machine.Name);
			throw;
		}
	}
}
