using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Models;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;

public sealed class JsonFaultScenarioRepository : IFaultScenarioRepository
{
	private sealed class FaultScenarioDefinitionDto
	{
		public string? ScenarioId { get; set; }

		public string? ScenarioVersion { get; set; }

		public string? DisplayName { get; set; }

		public string? Description { get; set; }

		public List<string>? MachineProfileIds { get; set; }

		public string? Category { get; set; }

		public string? Severity { get; set; }

		public string? DefaultDuration { get; set; }

		public string? MinimumDuration { get; set; }

		public string? MaximumDuration { get; set; }

		public double DefaultIntensity { get; set; }

		public double MinimumIntensity { get; set; }

		public double MaximumIntensity { get; set; }

		public List<PhaseDto>? Phases { get; set; }

		public List<EffectDto>? Effects { get; set; }

		public List<RuleDto>? ThresholdRules { get; set; }

		public RecoveryDto? Recovery { get; set; }

		public bool CanRunInParallel { get; set; } = true;

		public List<string>? MutuallyExclusiveScenarioIds { get; set; }

		public string? RequiredMachinePhase { get; set; }

		public List<string>? AllowedMachinePhases { get; set; }

		public bool IsEnabled { get; set; } = true;

		public List<string>? Tags { get; set; }

		public Dictionary<string, string>? Metadata { get; set; }

		public bool SupportsNonFaultingControlRun { get; set; }

		public int Priority { get; set; }
	}

	private sealed class PhaseDto
	{
		public string? Phase { get; set; }

		public string? Duration { get; set; }

		public double DurationFraction { get; set; }
	}

	private sealed class EffectDto
	{
		public string? EffectId { get; set; }

		public string? TargetType { get; set; }

		public string? TargetId { get; set; }

		public string? EffectType { get; set; }

		public string? StartPhase { get; set; }

		public string? EndPhase { get; set; }

		public string? Direction { get; set; }

		public double Magnitude { get; set; }

		public double RatePerSimulationMinute { get; set; }

		public string? Delay { get; set; }

		public double Inertia { get; set; }

		public double MinimumEffect { get; set; }

		public double MaximumEffect { get; set; }

		public double NoiseModifier { get; set; }

		public bool IsEnabled { get; set; } = true;

		public double OscillationFrequencyHz { get; set; }

		public double PulseIntervalSeconds { get; set; }

		public double PulseDurationSeconds { get; set; }
	}

	private sealed class RuleDto
	{
		public string? RuleId { get; set; }

		public string? SourceType { get; set; }

		public string? SourceId { get; set; }

		public string? Comparison { get; set; }

		public double ThresholdValue { get; set; }

		public double? ThresholdValueSecondary { get; set; }

		public string? MinimumDuration { get; set; }

		public string? FaultCode { get; set; }

		public string? FaultMessage { get; set; }

		public bool SetErrorActive { get; set; } = true;

		public bool SetMachineStateError { get; set; } = true;

		public bool StopProduction { get; set; } = true;

		public bool KeepServerOnline { get; set; } = true;

		public bool AutoRecover { get; set; }

		public bool IsEnabled { get; set; } = true;

		public bool DisabledInControlRun { get; set; } = true;
	}

	private sealed class RecoveryDto
	{
		public string? RecoveryType { get; set; }

		public string? Duration { get; set; }

		public double Rate { get; set; }

		public double TargetNormalState { get; set; }

		public bool KeepMachineFaultedUntilRecovered { get; set; } = true;

		public bool ClearErrorAtRecoveryStart { get; set; }

		public bool ClearErrorAtRecoveryEnd { get; set; } = true;

		public bool ResumeProductionAfterRecovery { get; set; } = true;

		public string? MinimumStableDuration { get; set; }

		public string? SafeRecoverySourceType { get; set; }

		public string? SafeRecoverySourceId { get; set; }

		public string? SafeRecoveryComparison { get; set; }

		public double? SafeRecoveryThreshold { get; set; }

		public double SafeRecoveryTolerance { get; set; } = 1.0;
	}

	private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

	private readonly string _baseDirectory;

	private List<FaultScenarioDefinition> _scenarios = new List<FaultScenarioDefinition>();

	public JsonFaultScenarioRepository(string? baseDirectory = null)
	{
		_baseDirectory = baseDirectory ?? FaultScenarioPaths.ResolveDirectory();
	}

	public async Task LoadAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!Directory.Exists(_baseDirectory))
		{
			_scenarios = new List<FaultScenarioDefinition>();
			return;
		}
		string[] files = Directory.GetFiles(_baseDirectory, "*.json", SearchOption.AllDirectories).OrderBy<string, string>((string f) => f, StringComparer.OrdinalIgnoreCase).ToArray();
		List<FaultScenarioDefinition> loaded = new List<FaultScenarioDefinition>(files.Length);
		string[] array = files;
		foreach (string file in array)
		{
			cancellationToken.ThrowIfCancellationRequested();
			FaultScenarioDefinitionDto dto = JsonSerializer.Deserialize<FaultScenarioDefinitionDto>(await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), JsonOptions);
			if (dto != null)
			{
				loaded.Add(Map(dto));
			}
		}
		_scenarios = loaded;
	}

	public IReadOnlyList<FaultScenarioDefinition> GetAll()
	{
		return _scenarios;
	}

	public FaultScenarioDefinition? GetById(string scenarioId)
	{
		return _scenarios.FirstOrDefault((FaultScenarioDefinition s) => s.ScenarioId.Equals(scenarioId, StringComparison.OrdinalIgnoreCase));
	}

	private static FaultScenarioDefinition Map(FaultScenarioDefinitionDto dto)
	{
		return new FaultScenarioDefinition
		{
			ScenarioId = (dto.ScenarioId ?? string.Empty),
			ScenarioVersion = (dto.ScenarioVersion ?? "1.0"),
			DisplayName = (dto.DisplayName ?? string.Empty),
			Description = (dto.Description ?? string.Empty),
			MachineProfileIds = (dto.MachineProfileIds ?? new List<string>()),
			Category = ParseEnum(dto.Category, FaultScenarioCategory.Mechanical),
			Severity = ParseEnum(dto.Severity, FaultScenarioSeverity.Medium),
			DefaultDuration = ParseTime(dto.DefaultDuration, TimeSpan.FromMinutes(5.0)),
			MinimumDuration = ParseTime(dto.MinimumDuration, TimeSpan.FromSeconds(30.0)),
			MaximumDuration = ParseTime(dto.MaximumDuration, TimeSpan.FromMinutes(30.0)),
			DefaultIntensity = ((dto.DefaultIntensity > 0.0) ? dto.DefaultIntensity : 1.0),
			MinimumIntensity = ((dto.MinimumIntensity > 0.0) ? dto.MinimumIntensity : 0.25),
			MaximumIntensity = ((dto.MaximumIntensity > 0.0) ? dto.MaximumIntensity : 1.5),
			Phases = (dto.Phases ?? new List<PhaseDto>()).Select((PhaseDto p) => new FaultScenarioPhaseTiming
			{
				Phase = ParseEnum(p.Phase, FaultScenarioPhase.Initiating),
				Duration = ParseTime(p.Duration, TimeSpan.Zero),
				DurationFraction = p.DurationFraction
			}).ToList(),
			Effects = (dto.Effects ?? new List<EffectDto>()).Select((EffectDto e) => new FaultEffectDefinition
			{
				EffectId = (e.EffectId ?? string.Empty),
				TargetType = ParseEnum(e.TargetType, FaultEffectTargetType.HiddenState),
				TargetId = (e.TargetId ?? string.Empty),
				EffectType = ParseEnum(e.EffectType, FaultEffectType.AdditiveDrift),
				StartPhase = ParseEnum(e.StartPhase, FaultScenarioPhase.Initiating),
				EndPhase = ParseEnum(e.EndPhase, FaultScenarioPhase.Faulted),
				Direction = ParseEnum(e.Direction, FaultEffectDirection.Increase),
				Magnitude = e.Magnitude,
				RatePerSimulationMinute = e.RatePerSimulationMinute,
				Delay = ParseTime(e.Delay, TimeSpan.Zero),
				Inertia = ((e.Inertia > 0.0) ? e.Inertia : 1.0),
				MinimumEffect = e.MinimumEffect,
				MaximumEffect = ((e.MaximumEffect > 0.0) ? e.MaximumEffect : 1.0),
				NoiseModifier = ((e.NoiseModifier > 0.0) ? e.NoiseModifier : 1.0),
				IsEnabled = e.IsEnabled,
				OscillationFrequencyHz = ((e.OscillationFrequencyHz > 0.0) ? e.OscillationFrequencyHz : 0.5),
				PulseIntervalSeconds = ((e.PulseIntervalSeconds > 0.0) ? e.PulseIntervalSeconds : 10.0),
				PulseDurationSeconds = ((e.PulseDurationSeconds > 0.0) ? e.PulseDurationSeconds : 2.0)
			}).ToList(),
			ThresholdRules = (dto.ThresholdRules ?? new List<RuleDto>()).Select((RuleDto r) => new FaultThresholdRule
			{
				RuleId = (r.RuleId ?? string.Empty),
				SourceType = ParseEnum(r.SourceType, FaultThresholdSourceType.Signal),
				SourceId = (r.SourceId ?? string.Empty),
				Comparison = ParseEnum(r.Comparison, FaultThresholdComparison.GreaterThan),
				ThresholdValue = r.ThresholdValue,
				ThresholdValueSecondary = r.ThresholdValueSecondary,
				MinimumDuration = ParseTime(r.MinimumDuration, TimeSpan.FromSeconds(10.0)),
				FaultCode = (r.FaultCode ?? string.Empty),
				FaultMessage = (r.FaultMessage ?? string.Empty),
				SetErrorActive = r.SetErrorActive,
				SetMachineStateError = r.SetMachineStateError,
				StopProduction = r.StopProduction,
				KeepServerOnline = r.KeepServerOnline,
				AutoRecover = r.AutoRecover,
				IsEnabled = r.IsEnabled,
				DisabledInControlRun = r.DisabledInControlRun
			}).ToList(),
			Recovery = ((dto.Recovery == null) ? new FaultRecoveryDefinition() : new FaultRecoveryDefinition
			{
				RecoveryType = ParseEnum(dto.Recovery.RecoveryType, FaultRecoveryType.Exponential),
				Duration = ParseTime(dto.Recovery.Duration, TimeSpan.FromMinutes(3.0)),
				Rate = ((dto.Recovery.Rate > 0.0) ? dto.Recovery.Rate : 0.15),
				TargetNormalState = dto.Recovery.TargetNormalState,
				KeepMachineFaultedUntilRecovered = dto.Recovery.KeepMachineFaultedUntilRecovered,
				ClearErrorAtRecoveryStart = dto.Recovery.ClearErrorAtRecoveryStart,
				ClearErrorAtRecoveryEnd = dto.Recovery.ClearErrorAtRecoveryEnd,
				ResumeProductionAfterRecovery = dto.Recovery.ResumeProductionAfterRecovery,
				MinimumStableDuration = ParseTime(dto.Recovery.MinimumStableDuration, TimeSpan.FromSeconds(30.0)),
				SafeRecoverySourceType = ParseNullableEnum(dto.Recovery.SafeRecoverySourceType, FaultThresholdSourceType.Signal),
				SafeRecoverySourceId = dto.Recovery.SafeRecoverySourceId,
				SafeRecoveryComparison = ParseNullableEnum(dto.Recovery.SafeRecoveryComparison, FaultThresholdComparison.LessThan),
				SafeRecoveryThreshold = dto.Recovery.SafeRecoveryThreshold,
				SafeRecoveryTolerance = ((dto.Recovery.SafeRecoveryTolerance > 0.0) ? dto.Recovery.SafeRecoveryTolerance : 1.0)
			}),
			CanRunInParallel = dto.CanRunInParallel,
			MutuallyExclusiveScenarioIds = (dto.MutuallyExclusiveScenarioIds ?? new List<string>()),
			RequiredMachinePhase = dto.RequiredMachinePhase,
			AllowedMachinePhases = (dto.AllowedMachinePhases ?? new List<string>()),
			IsEnabled = dto.IsEnabled,
			Tags = (dto.Tags ?? new List<string>()),
			Metadata = (dto.Metadata ?? new Dictionary<string, string>()),
			SupportsNonFaultingControlRun = dto.SupportsNonFaultingControlRun,
			Priority = ((dto.Priority > 0) ? dto.Priority : CategoryPriority(ParseEnum(dto.Category, FaultScenarioCategory.Mechanical)))
		};
	}

	private static int CategoryPriority(FaultScenarioCategory category)
	{
		if (1 == 0)
		{
		}
		int result = category switch
		{
			FaultScenarioCategory.Communication => 2, 
			FaultScenarioCategory.Thermal => 3, 
			FaultScenarioCategory.Hydraulic => 4, 
			FaultScenarioCategory.Electrical => 5, 
			FaultScenarioCategory.Mechanical => 6, 
			FaultScenarioCategory.Tooling => 7, 
			FaultScenarioCategory.Process => 8, 
			FaultScenarioCategory.Sensor => 9, 
			_ => 5, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static TimeSpan ParseTime(string? value, TimeSpan fallback)
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
		return fallback;
	}

	private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return fallback;
		}
		TEnum result;
		return (Enum.TryParse<TEnum>(value, ignoreCase: true, out result) && Enum.IsDefined(result)) ? result : fallback;
	}

	private static TEnum? ParseNullableEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		TEnum result;
		return (Enum.TryParse<TEnum>(value, ignoreCase: true, out result) && Enum.IsDefined(result)) ? result : fallback;
	}

	private static JsonSerializerOptions CreateOptions()
	{
		JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
		return jsonSerializerOptions;
	}
}
