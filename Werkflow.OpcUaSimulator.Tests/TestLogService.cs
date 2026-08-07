using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed class TestLogService : ILogService
{
	private readonly List<SimulationLogEntry> _entries = new List<SimulationLogEntry>();

	public IReadOnlyList<SimulationLogEntry> Entries => _entries;

	public event EventHandler<SimulationLogEntry>? EntryAdded;

	public void Log(LogCategory category, string message, string? machineName = null, string? previousValue = null, string? newValue = null)
	{
		SimulationLogEntry simulationLogEntry = new SimulationLogEntry
		{
			Category = category,
			Message = message,
			MachineName = (machineName ?? string.Empty),
			Timestamp = DateTime.UtcNow
		};
		_entries.Add(simulationLogEntry);
		this.EntryAdded?.Invoke(this, simulationLogEntry);
	}

	public void Clear()
	{
		_entries.Clear();
	}

	public Task ExportCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		return Task.CompletedTask;
	}
}
