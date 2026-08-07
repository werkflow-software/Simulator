using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public static class PhysicalMachineProfileJsonExporter
{
	private static readonly JsonSerializerOptions JsonOptions;

	static PhysicalMachineProfileJsonExporter()
	{
		JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
	}

	public static string Serialize(PhysicalMachineProfile profile)
	{
		return JsonSerializer.Serialize(Map(profile), JsonOptions);
	}

	public static async Task ExportToFileAsync(PhysicalMachineProfile profile, string filePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		string json = Serialize(profile);
		await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static object Map(PhysicalMachineProfile profile)
	{
		return new
		{
			profileId = profile.ProfileId,
			profileVersion = profile.ProfileVersion,
			displayName = profile.DisplayName,
			description = profile.Description,
			machineType = profile.MachineType,
			manufacturer = profile.Manufacturer,
			defaultUpdateInterval = FormatInterval(profile.DefaultUpdateInterval),
			metadata = profile.Metadata,
			signals = profile.Signals.Select(MapSignal).ToList(),
			hiddenProcessStates = profile.HiddenProcessStates.Select(MapState).ToList(),
			dependencies = profile.Dependencies.Select(MapDependency).ToList(),
			hiddenStateDependencies = profile.HiddenStateDependencies.Select(MapHiddenStateDependency).ToList()
		};
	}

	private static object MapSignal(SignalDefinition signal)
	{
		return new
		{
			signalId = signal.SignalId,
			nodeId = signal.NodeId,
			browseName = signal.BrowseName,
			displayName = signal.DisplayName,
			description = signal.Description,
			category = signal.Category.ToString(),
			dataType = signal.DataType.ToString(),
			engineeringUnit = signal.EngineeringUnit,
			normalMinimum = signal.NormalMinimum,
			normalMaximum = signal.NormalMaximum,
			nominalValue = signal.NominalValue,
			hardMinimum = signal.HardMinimum,
			hardMaximum = signal.HardMaximum,
			noiseModel = signal.NoiseModel.ToString(),
			noiseAmplitude = signal.NoiseAmplitude,
			updateInterval = FormatInterval(signal.UpdateInterval),
			decimalPlaces = signal.DecimalPlaces,
			responseInertia = signal.ResponseInertia,
			initialValue = signal.InitialValue,
			isEnabled = signal.IsEnabled,
			isWritable = signal.IsWritable,
			technicalBehavior = signal.TechnicalBehavior.ToString(),
			counterStepSize = signal.CounterStepSize,
			initialStringValue = (string.IsNullOrWhiteSpace(signal.InitialStringValue) ? null : signal.InitialStringValue),
			initialDateTimeUtc = signal.InitialDateTimeUtc?.ToString("o", CultureInfo.InvariantCulture),
			allowedValues = ((signal.AllowedValues.Count > 0) ? signal.AllowedValues : null),
			hiddenProcessInputs = signal.HiddenProcessInputs
		};
	}

	private static object MapState(HiddenProcessStateDefinition state)
	{
		return new
		{
			stateId = state.StateId,
			displayName = state.DisplayName,
			description = state.Description,
			normalMinimum = state.NormalMinimum,
			normalMaximum = state.NormalMaximum,
			nominalValue = state.NominalValue,
			hardMinimum = state.HardMinimum,
			hardMaximum = state.HardMaximum,
			initialValue = state.InitialValue,
			responseInertia = state.ResponseInertia,
			naturalDrift = state.NaturalDrift,
			noiseAmplitude = state.NoiseAmplitude
		};
	}

	private static object MapDependency(SignalDependencyDefinition dependency)
	{
		return new
		{
			dependencyId = dependency.DependencyId,
			sourceStateId = dependency.SourceStateId,
			targetSignalId = dependency.TargetSignalId,
			dependencyType = dependency.DependencyType.ToString(),
			weight = dependency.Weight,
			offset = dependency.Offset,
			responseDelay = FormatInterval(dependency.ResponseDelay),
			responseInertia = dependency.ResponseInertia,
			minimumEffect = dependency.MinimumEffect,
			maximumEffect = dependency.MaximumEffect,
			thresholdValue = dependency.ThresholdValue,
			isEnabled = dependency.IsEnabled
		};
	}

	private static object MapHiddenStateDependency(HiddenStateDependencyDefinition dependency)
	{
		return new
		{
			dependencyId = dependency.DependencyId,
			sourceStateId = dependency.SourceStateId,
			targetStateId = dependency.TargetStateId,
			dependencyType = dependency.DependencyType.ToString(),
			weight = dependency.Weight,
			offset = dependency.Offset,
			responseDelay = FormatInterval(dependency.ResponseDelay),
			responseInertia = dependency.ResponseInertia,
			minimumEffect = dependency.MinimumEffect,
			maximumEffect = dependency.MaximumEffect,
			thresholdValue = dependency.ThresholdValue,
			isEnabled = dependency.IsEnabled
		};
	}

	private static string FormatInterval(TimeSpan interval)
	{
		return interval.ToString("c", CultureInfo.InvariantCulture);
	}
}
