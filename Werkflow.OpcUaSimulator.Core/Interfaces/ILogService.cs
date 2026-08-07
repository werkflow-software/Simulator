using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface ILogService
{
	IReadOnlyList<SimulationLogEntry> Entries { get; }

	event EventHandler<SimulationLogEntry>? EntryAdded;

	void Log(LogCategory category, string message, string? machineName = null, string? previousValue = null, string? newValue = null);

	void Clear();

	Task ExportCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));
}
