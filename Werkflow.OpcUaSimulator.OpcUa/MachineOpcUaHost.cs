using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.OpcUa;

internal sealed class MachineOpcUaHost
{
	private readonly MachineConfiguration _machine;

	private readonly ILogService _logService;

	private readonly PhysicalSignalPublishingCoordinator? _physicalCoordinator;

	private SimulatorServer? _server;

	private ApplicationInstance? _application;

	public MachineRuntimeState Runtime { get; }

	public string MachineName => _machine.Name;

	public bool IsRunning => _server?.CurrentInstance != null;

	public int ConnectedClients => (_server?.CurrentInstance?.SessionManager?.GetSessions().Count).GetValueOrDefault();

	public ushort CustomNamespaceIndex => _server?.NodeManager?.CustomNamespaceIndex ?? 1;

	public event EventHandler<int>? ClientCountChanged;

	public MachineOpcUaHost(MachineConfiguration machine, MachineRuntimeState runtime, ILogService logService, PhysicalSignalPublishingCoordinator? physicalCoordinator = null)
	{
		_machine = machine;
		Runtime = runtime;
		_logService = logService;
		_physicalCoordinator = physicalCoordinator;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_application = await OpcUaConfigurationFactory.CreateApplicationInstanceAsync(_machine, _logService, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_server = new SimulatorServer(_machine, Runtime, _logService, _physicalCoordinator);
		await _application.Start(_server).ConfigureAwait(continueOnCapturedContext: false);
		if (_server.CurrentInstance?.SessionManager != null)
		{
			_server.CurrentInstance.SessionManager.SessionActivated += OnSessionChanged;
			_server.CurrentInstance.SessionManager.SessionClosing += OnSessionChanged;
		}
		_logService.Log(LogCategory.Server, "OPC-UA-Server bereit auf " + _machine.Endpoint, _machine.Name);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		try
		{
			if (_server?.CurrentInstance?.SessionManager != null)
			{
				_server.CurrentInstance.SessionManager.SessionActivated -= OnSessionChanged;
				_server.CurrentInstance.SessionManager.SessionClosing -= OnSessionChanged;
			}
			_server?.Stop();
			_server = null;
			_application = null;
		}
		catch (Exception ex)
		{
			_logService.Log(LogCategory.Server, "Fehler beim Stoppen: " + ex.Message, _machine.Name);
		}
		return Task.CompletedTask;
	}

	public void PublishAll(MachineRuntimeState state, IReadOnlyList<NodeMapping> nodes)
	{
		_server?.NodeManager?.PublishAll(state, nodes);
	}

	public void PublishValue(NodeSemanticType semanticType, object? value, IReadOnlyList<NodeMapping> nodes)
	{
		_server?.NodeManager?.PublishValue(semanticType, value, nodes);
	}

	public object? GetLiveValue(NodeSemanticType semanticType)
	{
		return _server?.NodeManager?.GetLiveValue(semanticType);
	}

	private void OnSessionChanged(Session session, SessionEventReason reason)
	{
		try
		{
			int connectedClients = ConnectedClients;
			_logService.Log(LogCategory.Connection, $"Client-Session {reason}: {session.SessionDiagnostics.SessionName} ({connectedClients} aktiv)", _machine.Name);
			this.ClientCountChanged?.Invoke(this, connectedClients);
		}
		catch (Exception ex)
		{
			_logService.Log(LogCategory.Server, "Clientstatus-Aktualisierung fehlgeschlagen: " + ex.Message, _machine.Name);
		}
	}
}
