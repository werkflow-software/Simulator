using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Exceptions;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class JsonPhysicalMachineProfileLoader : IPhysicalMachineProfileLoader
{
	private sealed class PhysicalMachineProfileDto
	{
		public string? ProfileId { get; set; }

		public string? ProfileVersion { get; set; }

		public string? DisplayName { get; set; }

		public string? Description { get; set; }

		public string? MachineType { get; set; }

		public string? Manufacturer { get; set; }

		public string? DefaultUpdateInterval { get; set; }

		public List<SignalDefinitionDto>? Signals { get; set; }

		public List<HiddenProcessStateDefinitionDto>? HiddenProcessStates { get; set; }

		public List<SignalDependencyDefinitionDto>? Dependencies { get; set; }

		public List<HiddenStateDependencyDefinitionDto>? HiddenStateDependencies { get; set; }

		public Dictionary<string, string>? Metadata { get; set; }
	}

	private sealed class SignalDefinitionDto
	{
		public string? SignalId { get; set; }

		public string? NodeId { get; set; }

		public string? BrowseName { get; set; }

		public string? DisplayName { get; set; }

		public string? Description { get; set; }

		public string? Category { get; set; }

		public string? DataType { get; set; }

		public string? EngineeringUnit { get; set; }

		public double NormalMinimum { get; set; }

		public double NormalMaximum { get; set; }

		public double NominalValue { get; set; }

		public double HardMinimum { get; set; }

		public double HardMaximum { get; set; }

		public string? NoiseModel { get; set; }

		public double NoiseAmplitude { get; set; }

		public string? UpdateInterval { get; set; }

		public int DecimalPlaces { get; set; } = 2;

		public double ResponseInertia { get; set; }

		public double InitialValue { get; set; }

		public bool IsEnabled { get; set; } = true;

		public bool IsWritable { get; set; }

		public string? TechnicalBehavior { get; set; }

		public int CounterStepSize { get; set; } = 1;

		public string? InitialStringValue { get; set; }

		public string? InitialDateTimeUtc { get; set; }

		public List<string>? AllowedValues { get; set; }

		public List<string>? HiddenProcessInputs { get; set; }
	}

	private sealed class HiddenProcessStateDefinitionDto
	{
		public string? StateId { get; set; }

		public string? DisplayName { get; set; }

		public string? Description { get; set; }

		public double NormalMinimum { get; set; }

		public double NormalMaximum { get; set; }

		public double NominalValue { get; set; }

		public double HardMinimum { get; set; }

		public double HardMaximum { get; set; }

		public double InitialValue { get; set; }

		public double ResponseInertia { get; set; }

		public double NaturalDrift { get; set; }

		public double NoiseAmplitude { get; set; }
	}

	private sealed class SignalDependencyDefinitionDto
	{
		public string? DependencyId { get; set; }

		public string? SourceStateId { get; set; }

		public string? TargetSignalId { get; set; }

		public string? DependencyType { get; set; }

		public double Weight { get; set; } = 1.0;

		public double Offset { get; set; }

		public string? ResponseDelay { get; set; }

		public double ResponseInertia { get; set; }

		public double? MinimumEffect { get; set; }

		public double? MaximumEffect { get; set; }

		public double ThresholdValue { get; set; }

		public bool IsEnabled { get; set; } = true;
	}

	private sealed class HiddenStateDependencyDefinitionDto
	{
		public string? DependencyId { get; set; }

		public string? SourceStateId { get; set; }

		public string? TargetStateId { get; set; }

		public string? DependencyType { get; set; }

		public double Weight { get; set; } = 1.0;

		public double Offset { get; set; }

		public string? ResponseDelay { get; set; }

		public double ResponseInertia { get; set; }

		public double? MinimumEffect { get; set; }

		public double? MaximumEffect { get; set; }

		public double ThresholdValue { get; set; }

		public bool IsEnabled { get; set; } = true;
	}

	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private readonly IPhysicalMachineProfileValidator _validator;

	public JsonPhysicalMachineProfileLoader(IPhysicalMachineProfileValidator validator)
	{
		_validator = validator ?? throw new ArgumentNullException("validator");
	}

	public async Task<PhysicalMachineProfile> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("Dateipfad fehlt.", "filePath");
		}
		if (!File.Exists(filePath))
		{
			throw new PhysicalProfileException("Profildatei wurde nicht gefunden.", filePath);
		}
		string json;
		try
		{
			json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			throw new PhysicalProfileException("Profildatei konnte nicht gelesen werden.", ex, filePath);
		}
		return Deserialize(json, filePath);
	}

	public async Task<IReadOnlyList<PhysicalMachineProfile>> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			throw new ArgumentException("Verzeichnispfad fehlt.", "directoryPath");
		}
		if (!Directory.Exists(directoryPath))
		{
			throw new PhysicalProfileException("Profilverzeichnis wurde nicht gefunden.", directoryPath);
		}
		string[] files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly).OrderBy<string, string>((string f) => f, StringComparer.OrdinalIgnoreCase).ToArray();
		List<PhysicalMachineProfile> profiles = new List<PhysicalMachineProfile>(files.Length);
		string[] array = files;
		foreach (string file in array)
		{
			cancellationToken.ThrowIfCancellationRequested();
			List<PhysicalMachineProfile> list = profiles;
			list.Add(await LoadFromFileAsync(file, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		}
		return profiles;
	}

	public PhysicalMachineProfile Deserialize(string json, string? sourcePath = null)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			throw new PhysicalProfileException("JSON-Inhalt ist leer.", sourcePath);
		}
		PhysicalMachineProfileDto physicalMachineProfileDto;
		try
		{
			physicalMachineProfileDto = JsonSerializer.Deserialize<PhysicalMachineProfileDto>(json, JsonOptions);
		}
		catch (JsonException ex)
		{
			throw new PhysicalProfileException("JSON konnte nicht deserialisiert werden.", ex, sourcePath, ex.Path);
		}
		if (physicalMachineProfileDto == null)
		{
			throw new PhysicalProfileException("JSON ergab kein Profilobjekt.", sourcePath);
		}
		PhysicalMachineProfile physicalMachineProfile = Map(physicalMachineProfileDto);
		PhysicalProfileValidationResult physicalProfileValidationResult = _validator.Validate(physicalMachineProfile);
		if (!physicalProfileValidationResult.IsValid)
		{
			string text = string.Join("; ", physicalProfileValidationResult.Errors.Select((PhysicalProfileIssue e) => $"{e.Code}: {e.Message} ({e.FieldPath})"));
			throw new PhysicalProfileException("Profil ist ungültig: " + text, sourcePath);
		}
		return physicalMachineProfile;
	}

	private static PhysicalMachineProfile Map(PhysicalMachineProfileDto dto)
	{
		return new PhysicalMachineProfile
		{
			ProfileId = dto.ProfileId ?? string.Empty,
			ProfileVersion = dto.ProfileVersion ?? string.Empty,
			DisplayName = dto.DisplayName ?? string.Empty,
			Description = dto.Description ?? string.Empty,
			MachineType = dto.MachineType ?? string.Empty,
			Manufacturer = dto.Manufacturer ?? string.Empty,
			DefaultUpdateInterval = ParseInterval(dto.DefaultUpdateInterval, TimeSpan.FromSeconds(1.0), "defaultUpdateInterval"),
			Signals = (dto.Signals ?? new List<SignalDefinitionDto>()).Select(MapSignal).ToList(),
			HiddenProcessStates = (dto.HiddenProcessStates ?? new List<HiddenProcessStateDefinitionDto>()).Select(MapState).ToList(),
			Dependencies = (dto.Dependencies ?? new List<SignalDependencyDefinitionDto>()).Select(MapDependency).ToList(),
			HiddenStateDependencies = (dto.HiddenStateDependencies ?? new List<HiddenStateDependencyDefinitionDto>()).Select(MapHiddenStateDependency).ToList(),
			Metadata = dto.Metadata ?? new Dictionary<string, string>()
		};
	}

	private static SignalDefinition MapSignal(SignalDefinitionDto dto)
	{
		return new SignalDefinition
		{
			SignalId = (dto.SignalId ?? string.Empty),
			NodeId = (dto.NodeId ?? string.Empty),
			BrowseName = (dto.BrowseName ?? string.Empty),
			DisplayName = (dto.DisplayName ?? string.Empty),
			Description = (dto.Description ?? string.Empty),
			Category = ParseEnum(dto.Category, SignalCategory.Auxiliary, "category"),
			DataType = ParseEnum(dto.DataType, PhysicalSignalDataType.Double, "dataType"),
			EngineeringUnit = (dto.EngineeringUnit ?? string.Empty),
			NormalMinimum = dto.NormalMinimum,
			NormalMaximum = dto.NormalMaximum,
			NominalValue = dto.NominalValue,
			HardMinimum = dto.HardMinimum,
			HardMaximum = dto.HardMaximum,
			NoiseModel = ParseEnum(dto.NoiseModel, NoiseModel.None, "noiseModel"),
			NoiseAmplitude = dto.NoiseAmplitude,
			UpdateInterval = ParseInterval(dto.UpdateInterval, TimeSpan.FromSeconds(1.0), "updateInterval"),
			DecimalPlaces = dto.DecimalPlaces,
			ResponseInertia = dto.ResponseInertia,
			InitialValue = dto.InitialValue,
			IsEnabled = dto.IsEnabled,
			IsWritable = dto.IsWritable,
			TechnicalBehavior = ParseEnum(dto.TechnicalBehavior, TechnicalSignalBehavior.Continuous, "technicalBehavior"),
			CounterStepSize = ((dto.CounterStepSize <= 0) ? 1 : dto.CounterStepSize),
			InitialStringValue = (dto.InitialStringValue ?? string.Empty),
			InitialDateTimeUtc = ParseDateTime(dto.InitialDateTimeUtc),
			AllowedValues = (dto.AllowedValues ?? new List<string>()),
			HiddenProcessInputs = (dto.HiddenProcessInputs ?? new List<string>())
		};
	}

	private static HiddenProcessStateDefinition MapState(HiddenProcessStateDefinitionDto dto)
	{
		return new HiddenProcessStateDefinition
		{
			StateId = (dto.StateId ?? string.Empty),
			DisplayName = (dto.DisplayName ?? string.Empty),
			Description = (dto.Description ?? string.Empty),
			NormalMinimum = dto.NormalMinimum,
			NormalMaximum = dto.NormalMaximum,
			NominalValue = dto.NominalValue,
			HardMinimum = dto.HardMinimum,
			HardMaximum = dto.HardMaximum,
			InitialValue = dto.InitialValue,
			ResponseInertia = dto.ResponseInertia,
			NaturalDrift = dto.NaturalDrift,
			NoiseAmplitude = dto.NoiseAmplitude
		};
	}

	private static SignalDependencyDefinition MapDependency(SignalDependencyDefinitionDto dto)
	{
		return new SignalDependencyDefinition
		{
			DependencyId = (dto.DependencyId ?? string.Empty),
			SourceStateId = (dto.SourceStateId ?? string.Empty),
			TargetSignalId = (dto.TargetSignalId ?? string.Empty),
			DependencyType = ParseEnum(dto.DependencyType, DependencyType.Linear, "dependencyType"),
			Weight = dto.Weight,
			Offset = dto.Offset,
			ResponseDelay = ParseInterval(dto.ResponseDelay, TimeSpan.Zero, "responseDelay"),
			ResponseInertia = dto.ResponseInertia,
			MinimumEffect = dto.MinimumEffect,
			MaximumEffect = dto.MaximumEffect,
			ThresholdValue = dto.ThresholdValue,
			IsEnabled = dto.IsEnabled
		};
	}

	private static HiddenStateDependencyDefinition MapHiddenStateDependency(HiddenStateDependencyDefinitionDto dto)
	{
		return new HiddenStateDependencyDefinition
		{
			DependencyId = (dto.DependencyId ?? string.Empty),
			SourceStateId = (dto.SourceStateId ?? string.Empty),
			TargetStateId = (dto.TargetStateId ?? string.Empty),
			DependencyType = ParseEnum(dto.DependencyType, DependencyType.Linear, "dependencyType"),
			Weight = dto.Weight,
			Offset = dto.Offset,
			ResponseDelay = ParseInterval(dto.ResponseDelay, TimeSpan.Zero, "responseDelay"),
			ResponseInertia = dto.ResponseInertia,
			MinimumEffect = dto.MinimumEffect,
			MaximumEffect = dto.MaximumEffect,
			ThresholdValue = dto.ThresholdValue,
			IsEnabled = dto.IsEnabled
		};
	}

	private static TimeSpan ParseInterval(string? value, TimeSpan fallback, string fieldPath)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return fallback;
		}
		if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return TimeSpan.FromSeconds(result2);
		}
		throw new PhysicalProfileException("Ungültiges Intervall '" + value + "'.", (string?)null, fieldPath);
	}

	private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback, string fieldPath) where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return fallback;
		}
		if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) && Enum.IsDefined(result))
		{
			return result;
		}
		throw new PhysicalProfileException($"Ungültiger Enum-Wert '{value}' für {typeof(TEnum).Name}.", (string?)null, fieldPath);
	}

	private static DateTime? ParseDateTime(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
	}

	private static JsonSerializerOptions CreateOptions()
	{
		JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
		return jsonSerializerOptions;
	}
}
