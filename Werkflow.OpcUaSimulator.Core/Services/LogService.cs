using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class LogService : ILogService
{
	private readonly object _sync = new object();

	private readonly List<SimulationLogEntry> _entries = new List<SimulationLogEntry>();

	private int _maxEntries = 5000;

	public IReadOnlyList<SimulationLogEntry> Entries
	{
		get
		{
			lock (_sync)
			{
				return _entries.ToList();
			}
		}
	}

	public event EventHandler<SimulationLogEntry>? EntryAdded;

	public void SetMaxEntries(int maxEntries)
	{
		_maxEntries = Math.Max(100, maxEntries);
	}

	public void Log(LogCategory category, string message, string? machineName = null, string? previousValue = null, string? newValue = null)
	{
		SimulationLogEntry simulationLogEntry = new SimulationLogEntry
		{
			Timestamp = DateTime.Now,
			MachineName = (machineName ?? "—"),
			Category = category,
			Message = message,
			PreviousValue = previousValue,
			NewValue = newValue
		};
		lock (_sync)
		{
			_entries.Insert(0, simulationLogEntry);
			if (_entries.Count > _maxEntries)
			{
				_entries.RemoveRange(_maxEntries, _entries.Count - _maxEntries);
			}
		}
		this.EntryAdded?.Invoke(this, simulationLogEntry);
	}

	public void Clear()
	{
		lock (_sync)
		{
			_entries.Clear();
		}
	}

	public Task ExportCsvAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		List<string> list = new List<string> { "Zeitstempel;Maschine;Kategorie;Meldung;Vorheriger Wert;Neuer Wert" };
		List<SimulationLogEntry> source;
		lock (_sync)
		{
			source = _entries.ToList();
		}
		foreach (SimulationLogEntry item in source.OrderBy((SimulationLogEntry e) => e.Timestamp))
		{
			cancellationToken.ThrowIfCancellationRequested();
			list.Add(string.Join(';', Escape(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)), Escape(item.MachineName), Escape(item.Category.ToGermanLabel()), Escape(item.Message), Escape(item.PreviousValue ?? ""), Escape(item.NewValue ?? "")));
		}
		return File.WriteAllLinesAsync(filePath, list, Encoding.UTF8, cancellationToken);
	}

	private static string Escape(string value)
	{
		return "\"" + value.Replace("\"", "\"\"") + "\"";
	}
}
