using System;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.Core.Interfaces;

public interface IConfigurationService
{
	ApplicationOperatingMode OperatingMode { get; }
	AppConfiguration Configuration { get; }

	string ConfigurationDirectory { get; }

	event EventHandler? ConfigurationChanged;

	Task InitializeAsync(ApplicationOperatingMode operatingMode, CancellationToken cancellationToken = default(CancellationToken));

	Task SaveAllAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task SaveMachinesAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task SaveJobsAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task SaveSettingsAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task SaveEventsAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task RestoreFactoryDefaultsAsync(CancellationToken cancellationToken = default(CancellationToken));

	Task ExportAllAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

	Task ImportAllAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken));

	void OpenConfigurationDirectory();
}
