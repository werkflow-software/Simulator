using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.OpcUa;

public sealed class MachineServerService : IMachineServerService, IMachineValuePublisher, IDisposable
{
	private readonly ILogService _logService;

	private readonly PhysicalSignalPublishingCoordinator? _physicalCoordinator;

	private readonly object _sync = new object();

	private readonly Dictionary<Guid, MachineOpcUaHost> _hosts = new Dictionary<Guid, MachineOpcUaHost>();

	public event EventHandler<(Guid MachineId, bool IsOnline, int ClientCount)>? ServerStatusChanged;

	public MachineServerService(ILogService logService, PhysicalSignalPublishingCoordinator? physicalCoordinator = null)
	{
		_logService = logService;
		_physicalCoordinator = physicalCoordinator;
	}

	public bool IsRunning(Guid machineId)
	{
		lock (_sync)
		{
			MachineOpcUaHost value;
			return _hosts.TryGetValue(machineId, out value) && value.IsRunning;
		}
	}

	public int GetConnectedClients(Guid machineId)
	{
		lock (_sync)
		{
			MachineOpcUaHost value;
			return _hosts.TryGetValue(machineId, out value) ? value.ConnectedClients : 0;
		}
	}

	public ushort? GetNamespaceIndex(Guid machineId)
	{
		lock (_sync)
		{
			MachineOpcUaHost value;
			return _hosts.TryGetValue(machineId, out value) ? new ushort?(value.CustomNamespaceIndex) : ((ushort?)null);
		}
	}

	public async Task StartServerAsync(MachineConfiguration machine, MachineRuntimeState runtime, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (_sync)
		{
			if (_hosts.TryGetValue(machine.Id, out MachineOpcUaHost existing) && existing.IsRunning)
			{
				return;
			}
		}
		MachineOpcUaHost host = new MachineOpcUaHost(machine, runtime, _logService, _physicalCoordinator);
		await host.StartAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (_physicalCoordinator != null)
		{
			await _physicalCoordinator.StartForMachineAsync(machine.Id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		lock (_sync)
		{
			_hosts[machine.Id] = host;
		}
		runtime.IsServerOnline = true;
		runtime.NamespaceIndex = host.CustomNamespaceIndex;
		host.ClientCountChanged += delegate(object? _, int count)
		{
			runtime.ConnectedClients = count;
			this.ServerStatusChanged?.Invoke(this, (machine.Id, true, count));
		};
		_logService.Log(LogCategory.Server, "Server gestartet: " + machine.Endpoint, machine.Name);
		this.ServerStatusChanged?.Invoke(this, (machine.Id, true, host.ConnectedClients));
	}

	public async Task StopServerAsync(Guid machineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		MachineOpcUaHost host;
		lock (_sync)
		{
			_hosts.TryGetValue(machineId, out host);
		}
		if (host != null)
		{
			await host.StopAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (_physicalCoordinator != null)
			{
				await _physicalCoordinator.StopForMachineAsync(machineId, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			lock (_sync)
			{
				_hosts.Remove(machineId);
			}
			host.Runtime.IsServerOnline = false;
			host.Runtime.ConnectedClients = 0;
			_logService.Log(LogCategory.Server, "Server gestoppt", host.MachineName);
			this.ServerStatusChanged?.Invoke(this, (machineId, false, 0));
		}
	}

	public async Task StopAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		List<Guid> ids;
		lock (_sync)
		{
			ids = _hosts.Keys.ToList();
		}
		foreach (Guid id in ids)
		{
			await StopServerAsync(id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void PublishAll(Guid machineId, MachineRuntimeState state, IReadOnlyList<NodeMapping> nodes)
	{
		if (TryGetHost(machineId, out MachineOpcUaHost host))
		{
			host.PublishAll(state, nodes);
		}
	}

	public void PublishValue(Guid machineId, NodeSemanticType semanticType, object? value, IReadOnlyList<NodeMapping> nodes)
	{
		if (TryGetHost(machineId, out MachineOpcUaHost host))
		{
			host.PublishValue(semanticType, value, nodes);
		}
	}

	public object? GetLiveValue(Guid machineId, NodeSemanticType semanticType)
	{
		MachineOpcUaHost host;
		return TryGetHost(machineId, out host) ? host.GetLiveValue(semanticType) : null;
	}

	private bool TryGetHost(Guid machineId, out MachineOpcUaHost host)
	{
		lock (_sync)
		{
			return _hosts.TryGetValue(machineId, out host);
		}
	}

	public void Dispose()
	{
		StopAllAsync().GetAwaiter().GetResult();
	}
}
