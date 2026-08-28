using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class ConfigurationService : IConfigurationService
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	private readonly ILogService _logService;

	private readonly IJobGenerator _jobGenerator;

	public AppConfiguration Configuration { get; private set; } = new AppConfiguration();

	public ApplicationOperatingMode OperatingMode { get; private set; } = ApplicationOperatingMode.ClassicSimulator;

	public string ConfigurationDirectory { get; }

	public event EventHandler? ConfigurationChanged;

	public ConfigurationService(ILogService logService, IJobGenerator jobGenerator)
	{
		_logService = logService;
		_jobGenerator = jobGenerator;
		ConfigurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Werkflow", "OpcUaSimulator");
	}

	public async Task InitializeAsync(ApplicationOperatingMode operatingMode, CancellationToken cancellationToken = default(CancellationToken))
	{
		OperatingMode = operatingMode;
		Directory.CreateDirectory(ConfigurationDirectory);
		AppConfiguration appConfiguration = new AppConfiguration();
		AppConfiguration appConfiguration2 = appConfiguration;
		appConfiguration2.Settings = await LoadAsync("settings.json", FixedSimulationCatalog.CreateDefaultSettings, cancellationToken);
		AppConfiguration appConfiguration3 = appConfiguration;
		appConfiguration3.Machines = await LoadAsync("machines.json", DefaultMachines.Create, cancellationToken);
		AppConfiguration appConfiguration4 = appConfiguration;
		appConfiguration4.Jobs = await LoadAsync("jobs.json", FixedSimulationCatalog.CreateJobs, cancellationToken);
		AppConfiguration appConfiguration5 = appConfiguration;
		appConfiguration5.Events = await LoadAsync("error-messages.json", FixedSimulationCatalog.CreateDefaultEvents, cancellationToken);
		Configuration = appConfiguration;
		if (operatingMode == ApplicationOperatingMode.VirtualMachine)
		{
			ApplyVirtualMachineMachineFilter();
		}
		else if (Configuration.Machines.Count < 4)
		{
			Configuration.Machines = DefaultMachines.Create();
		}
		if (Configuration.Jobs.Count == 0)
		{
			_jobGenerator.RegenerateJobs(Configuration, new Random(Configuration.Settings.RandomSeed));
		}
		foreach (MachineConfiguration machine in Configuration.Machines)
		{
			machine.UpdateEndpointFromHostPort();
			NormalizeMachine(machine);
		}
		NormalizeEvents(Configuration.Events);
		ILogService logService = _logService;
		if (logService is LogService logService2)
		{
			logService2.SetMaxEntries(Configuration.Settings.LogMaxEntries);
		}
		this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
	}

	public async Task SaveAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		await SaveSettingsAsync(cancellationToken);
		await SaveMachinesAsync(cancellationToken);
		await SaveJobsAsync(cancellationToken);
		await SaveEventsAsync(cancellationToken);
	}

	public Task SaveMachinesAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return SaveAsync("machines.json", Configuration.Machines, cancellationToken);
	}

	public Task SaveJobsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return SaveAsync("jobs.json", Configuration.Jobs, cancellationToken);
	}

	public Task SaveSettingsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return SaveAsync("settings.json", Configuration.Settings, cancellationToken);
	}

	public Task SaveEventsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return SaveAsync("error-messages.json", Configuration.Events, cancellationToken);
	}

	public async Task RestoreFactoryDefaultsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Configuration = new AppConfiguration
		{
			Settings = FixedSimulationCatalog.CreateDefaultSettings(),
			Machines = DefaultMachines.Create(),
			Jobs = FixedSimulationCatalog.CreateJobs(),
			Events = FixedSimulationCatalog.CreateDefaultEvents()
		};
		await SaveAllAsync(cancellationToken);
		this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
		_logService.Log(LogCategory.Configuration, "Werkseinstellungen wiederhergestellt");
	}

	public async Task ExportAllAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		string json = JsonSerializer.Serialize(Configuration, JsonOptions);
		await File.WriteAllTextAsync(filePath, json, cancellationToken);
	}

	public async Task ImportAllAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		AppConfiguration imported = JsonSerializer.Deserialize<AppConfiguration>(await File.ReadAllTextAsync(filePath, cancellationToken), JsonOptions) ?? throw new InvalidOperationException("Importdatei konnte nicht gelesen werden.");
		Configuration = imported;
		await SaveAllAsync(cancellationToken);
		this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
		_logService.Log(LogCategory.Configuration, "Konfiguration importiert", null, null, filePath);
	}

	public void OpenConfigurationDirectory()
	{
		Directory.CreateDirectory(ConfigurationDirectory);
		Process.Start(new ProcessStartInfo
		{
			FileName = ConfigurationDirectory,
			UseShellExecute = true
		});
	}

	private void ApplyVirtualMachineMachineFilter()
	{
		MachineConfiguration existingLaser = ResolveVirtualMachineMachine(VirtualMachineContract.MachineId, VirtualMachineContract.Port)
			?? DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		MachineConfiguration vigilLabLaser = ResolveVirtualMachineMachine(VigilLabMachineContract.MachineId, VigilLabMachineContract.Port)
			?? DefaultMachines.CreateVigilLabMachine();
		MachineConfiguration pressBrake = ResolveVirtualMachineMachine(VirtualPressBrakeContract.MachineId, VirtualPressBrakeContract.Port)
			?? DefaultMachines.Create().First(m => m.Port == VirtualPressBrakeContract.Port);

		existingLaser.IsActive = true;
		vigilLabLaser.IsActive = true;
		pressBrake.IsActive = true;
		NormalizeMachine(existingLaser);
		NormalizeMachine(vigilLabLaser);
		NormalizeMachine(pressBrake);
		Configuration.Machines = [existingLaser, pressBrake, vigilLabLaser];
	}

	private MachineConfiguration? ResolveVirtualMachineMachine(Guid machineId, int port) =>
		Configuration.Machines.FirstOrDefault(m => m.Id == machineId || m.Port == port);

	private static void NormalizeMachine(MachineConfiguration machine)
	{
		if (machine.Port == VirtualMachineContract.Port)
		{
			machine.Id = VirtualMachineContract.MachineId;
			machine.Name = VirtualMachineContract.DisplayName;
			machine.PhysicalProfileId = VirtualMachineContract.PhysicalProfileId;
			machine.Host = "localhost";
			machine.UpdateEndpointFromHostPort();
		}
		else if (machine.Port == VigilLabMachineContract.Port)
		{
			machine.Id = VigilLabMachineContract.MachineId;
			machine.Name = VigilLabMachineContract.DisplayName;
			machine.PhysicalProfileId = VigilLabMachineContract.PhysicalProfileId;
			machine.NamespaceUri = VigilLabMachineContract.NamespaceUri;
			machine.Host = "localhost";
			machine.ErrorProbabilityPercent = 0.0;
			machine.DisconnectProbabilityPercent = 0.0;
			machine.UpdateEndpointFromHostPort();
		}
		else if (machine.Port == VirtualPressBrakeContract.Port || machine.Id == VirtualPressBrakeContract.MachineId)
		{
			machine.Id = VirtualPressBrakeContract.MachineId;
			machine.Name = VirtualPressBrakeContract.DisplayName;
			machine.PhysicalProfileId = VirtualPressBrakeContract.PhysicalProfileId;
			machine.NamespaceUri = VirtualPressBrakeContract.NamespaceUri;
			machine.Host = "localhost";
			machine.ErrorProbabilityPercent = 0.0;
			machine.DisconnectProbabilityPercent = 0.0;
			machine.UpdateEndpointFromHostPort();
		}
		else if (machine.Port == VirtualAutonomousProductionCellContract.Port || machine.Id == VirtualAutonomousProductionCellContract.MachineId)
		{
			machine.Id = VirtualAutonomousProductionCellContract.MachineId;
			machine.Name = VirtualAutonomousProductionCellContract.DisplayName;
			machine.PhysicalProfileId ??= VirtualAutonomousProductionCellContract.PhysicalProfileIdCore24;
			machine.NamespaceUri = VirtualAutonomousProductionCellContract.NamespaceUri;
			machine.Host = "localhost";
			machine.ErrorProbabilityPercent = 0.0;
			machine.DisconnectProbabilityPercent = 0.0;
			machine.UpdateEndpointFromHostPort();
		}

		machine.ErrorProbabilityPercent = Math.Min(machine.ErrorProbabilityPercent, 1.5);
		machine.DisconnectProbabilityPercent = Math.Min(machine.DisconnectProbabilityPercent, 1.5);
		machine.MinErrorDurationMs = SimulationErrorPolicy.CapDisruptedDuration(machine.MinErrorDurationMs);
		machine.MaxErrorDurationMs = SimulationErrorPolicy.CapDisruptedDuration(machine.MaxErrorDurationMs);
		machine.MinOfflineDurationMs = SimulationErrorPolicy.CapDisruptedDuration(machine.MinOfflineDurationMs);
		machine.MaxOfflineDurationMs = SimulationErrorPolicy.CapDisruptedDuration(machine.MaxOfflineDurationMs);
		if (machine.MinErrorDurationMs > machine.MaxErrorDurationMs)
		{
			machine.MinErrorDurationMs = 3000;
			machine.MaxErrorDurationMs = 60000;
		}
		if (machine.MinOfflineDurationMs > machine.MaxOfflineDurationMs)
		{
			machine.MinOfflineDurationMs = 3000;
			machine.MaxOfflineDurationMs = 60000;
		}
		machine.StartInErrorState = false;
		MachineState baseState = machine.BaseState;
		if ((baseState == MachineState.Offline || (uint)(baseState - 3) <= 1u) ? true : false)
		{
			machine.BaseState = MachineState.Idle;
		}
	}

	private static void NormalizeEvents(EventSettings events)
	{
		foreach (EventTypeSettings @event in events.Events)
		{
			switch (@event.EventType)
			{
			case SimulationEventType.Error:
				@event.ProbabilityPercent = Math.Min(@event.ProbabilityPercent, 0.5);
				break;
			case SimulationEventType.Warning:
				@event.ProbabilityPercent = Math.Min(@event.ProbabilityPercent, 1.0);
				break;
			case SimulationEventType.OpcUaDisconnect:
				@event.ProbabilityPercent = Math.Min(@event.ProbabilityPercent, 0.5);
				break;
			default:
				continue;
			}
			@event.MinDurationMs = SimulationErrorPolicy.CapDisruptedDuration(@event.MinDurationMs);
			@event.MaxDurationMs = SimulationErrorPolicy.CapDisruptedDuration(@event.MaxDurationMs);
			if (@event.MinDurationMs > @event.MaxDurationMs)
			{
				@event.MinDurationMs = 3000;
				@event.MaxDurationMs = 60000;
			}
		}
	}

	private async Task<T> LoadAsync<T>(string fileName, Func<T> factory, CancellationToken cancellationToken)
	{
		string path = Path.Combine(ConfigurationDirectory, fileName);
		if (!File.Exists(path))
		{
			T value = factory();
			await SaveAsync(fileName, value, cancellationToken);
			return value;
		}
		try
		{
			T val = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path, cancellationToken), JsonOptions);
			return (val != null) ? val : factory();
		}
		catch (Exception ex)
		{
			string backup = path + $".backup-{DateTime.Now:yyyyMMddHHmmss}";
			File.Copy(path, backup, overwrite: true);
			_logService.Log(LogCategory.Configuration, "Beschädigte Datei '" + fileName + "' gesichert und Standard geladen: " + ex.Message);
			T value2 = factory();
			await SaveAsync(fileName, value2, cancellationToken);
			return value2;
		}
	}

	private async Task SaveAsync<T>(string fileName, T data, CancellationToken cancellationToken)
	{
		Directory.CreateDirectory(ConfigurationDirectory);
		string path = Path.Combine(ConfigurationDirectory, fileName);
		string json = JsonSerializer.Serialize(data, JsonOptions);
		await File.WriteAllTextAsync(path, json, cancellationToken);
	}
}
