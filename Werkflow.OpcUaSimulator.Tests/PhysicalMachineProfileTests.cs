using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Xunit;

namespace Werkflow.OpcUaSimulator.Tests;

public class PhysicalMachineProfileTests
{
	private sealed class SignalDefinitionBuilder
	{
		public string SignalId { get; set; }

		public string NodeId { get; set; }

		public string BrowseName { get; set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }

		public SignalCategory Category { get; set; }

		public PhysicalSignalDataType DataType { get; set; }

		public string EngineeringUnit { get; set; }

		public double NormalMinimum { get; set; }

		public double NormalMaximum { get; set; }

		public double NominalValue { get; set; }

		public double HardMinimum { get; set; }

		public double HardMaximum { get; set; }

		public NoiseModel NoiseModel { get; set; }

		public double NoiseAmplitude { get; set; }

		public TimeSpan UpdateInterval { get; set; }

		public int DecimalPlaces { get; set; }

		public double ResponseInertia { get; set; }

		public double InitialValue { get; set; }

		public bool IsEnabled { get; set; }

		public bool IsWritable { get; set; }

		public IReadOnlyList<string> HiddenProcessInputs { get; set; }

		public SignalDefinitionBuilder(SignalDefinition source)
		{
			SignalId = source.SignalId;
			NodeId = source.NodeId;
			BrowseName = source.BrowseName;
			DisplayName = source.DisplayName;
			Description = source.Description;
			Category = source.Category;
			DataType = source.DataType;
			EngineeringUnit = source.EngineeringUnit;
			NormalMinimum = source.NormalMinimum;
			NormalMaximum = source.NormalMaximum;
			NominalValue = source.NominalValue;
			HardMinimum = source.HardMinimum;
			HardMaximum = source.HardMaximum;
			NoiseModel = source.NoiseModel;
			NoiseAmplitude = source.NoiseAmplitude;
			UpdateInterval = source.UpdateInterval;
			DecimalPlaces = source.DecimalPlaces;
			ResponseInertia = source.ResponseInertia;
			InitialValue = source.InitialValue;
			IsEnabled = source.IsEnabled;
			IsWritable = source.IsWritable;
			HiddenProcessInputs = source.HiddenProcessInputs.ToList();
		}

		public SignalDefinition Build()
		{
			return new SignalDefinition
			{
				SignalId = SignalId,
				NodeId = NodeId,
				BrowseName = BrowseName,
				DisplayName = DisplayName,
				Description = Description,
				Category = Category,
				DataType = DataType,
				EngineeringUnit = EngineeringUnit,
				NormalMinimum = NormalMinimum,
				NormalMaximum = NormalMaximum,
				NominalValue = NominalValue,
				HardMinimum = HardMinimum,
				HardMaximum = HardMaximum,
				NoiseModel = NoiseModel,
				NoiseAmplitude = NoiseAmplitude,
				UpdateInterval = UpdateInterval,
				DecimalPlaces = DecimalPlaces,
				ResponseInertia = ResponseInertia,
				InitialValue = InitialValue,
				IsEnabled = IsEnabled,
				IsWritable = IsWritable,
				HiddenProcessInputs = HiddenProcessInputs
			};
		}
	}

	private static string ReferenceProfilePath => Path.Combine(AppContext.BaseDirectory, "TestData", "ReferenceMachine.json");

	private static IPhysicalMachineProfileLoader CreateLoader()
	{
		return new JsonPhysicalMachineProfileLoader(new PhysicalMachineProfileValidator());
	}

	private static PhysicalMachineProfile CloneWithSignals(PhysicalMachineProfile source, IReadOnlyList<SignalDefinition> signals)
	{
		return new PhysicalMachineProfile
		{
			ProfileId = source.ProfileId,
			ProfileVersion = source.ProfileVersion,
			DisplayName = source.DisplayName,
			Description = source.Description,
			MachineType = source.MachineType,
			Manufacturer = source.Manufacturer,
			DefaultUpdateInterval = source.DefaultUpdateInterval,
			Signals = signals,
			HiddenProcessStates = source.HiddenProcessStates,
			Dependencies = source.Dependencies,
			Metadata = source.Metadata
		};
	}

	private static SignalDefinition CloneSignal(SignalDefinition s, Action<SignalDefinitionBuilder> configure)
	{
		SignalDefinitionBuilder signalDefinitionBuilder = new SignalDefinitionBuilder(s);
		configure(signalDefinitionBuilder);
		return signalDefinitionBuilder.Build();
	}

	[Fact]
	public async Task ReferenceProfile_LoadsSuccessfully()
	{
		IPhysicalMachineProfileLoader loader = CreateLoader();
		PhysicalMachineProfile profile = await loader.LoadFromFileAsync(ReferenceProfilePath);
		Assert.Equal("reference-machine-v1", profile.ProfileId);
		Assert.Equal("1.0.0", profile.ProfileVersion);
		Assert.Equal(20, profile.Signals.Count);
		Assert.Equal(5, profile.HiddenProcessStates.Count);
		Assert.True(profile.Dependencies.Count >= 10);
	}

	[Fact]
	public async Task ReferenceProfile_PassesValidation()
	{
		IPhysicalMachineProfileLoader loader = CreateLoader();
		PhysicalMachineProfile profile = await loader.LoadFromFileAsync(ReferenceProfilePath);
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(profile);
		Assert.True(result.IsValid, string.Join("; ", result.Errors.Select((PhysicalProfileIssue e) => e.Message)));
		Assert.Empty(result.Errors);
	}

	[Fact]
	public async Task DuplicateSignalId_IsDetected()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		List<SignalDefinition> signals = profile.Signals.ToList();
		signals[1] = CloneSignal(signals[1], delegate(SignalDefinitionBuilder b)
		{
			b.SignalId = signals[0].SignalId;
		});
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(CloneWithSignals(profile, signals));
		Assert.False(result.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)result.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "SIGNAL_ID_DUPLICATE"));
	}

	[Fact]
	public async Task DuplicateNodeId_IsDetected()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		List<SignalDefinition> signals = profile.Signals.ToList();
		signals[1] = CloneSignal(signals[1], delegate(SignalDefinitionBuilder b)
		{
			b.NodeId = signals[0].NodeId;
		});
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(CloneWithSignals(profile, signals));
		Assert.False(result.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)result.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "SIGNAL_NODEID_DUPLICATE"));
	}

	[Fact]
	public async Task InvalidNormalRange_IsDetected()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		List<SignalDefinition> signals = profile.Signals.ToList();
		signals[0] = CloneSignal(signals[0], delegate(SignalDefinitionBuilder b)
		{
			b.NormalMinimum = 100.0;
			b.NormalMaximum = 10.0;
			b.NominalValue = 10.0;
			b.HardMinimum = 0.0;
			b.HardMaximum = 200.0;
			b.InitialValue = 50.0;
		});
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(CloneWithSignals(profile, signals));
		Assert.False(result.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)result.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "SIGNAL_NORMAL_RANGE_INVALID"));
	}

	[Fact]
	public async Task MissingHiddenProcessState_IsDetected()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		List<SignalDefinition> signals = profile.Signals.ToList();
		signals[0] = CloneSignal(signals[0], delegate(SignalDefinitionBuilder b)
		{
			b.HiddenProcessInputs = new[] { "DoesNotExist" };
		});
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(CloneWithSignals(profile, signals));
		Assert.False(result.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)result.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "SIGNAL_HIDDEN_INPUT_MISSING"));
	}

	[Fact]
	public async Task MissingDependencyTargetSignal_IsDetected()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		List<SignalDependencyDefinition> dependencies = profile.Dependencies.ToList();
		SignalDependencyDefinition first = dependencies[0];
		dependencies[0] = new SignalDependencyDefinition
		{
			DependencyId = first.DependencyId,
			SourceStateId = first.SourceStateId,
			TargetSignalId = "Missing.Signal",
			DependencyType = first.DependencyType,
			Weight = first.Weight,
			Offset = first.Offset,
			ResponseDelay = first.ResponseDelay,
			ResponseInertia = first.ResponseInertia,
			MinimumEffect = first.MinimumEffect,
			MaximumEffect = first.MaximumEffect,
			IsEnabled = first.IsEnabled
		};
		PhysicalMachineProfile mutated = new PhysicalMachineProfile
		{
			ProfileId = profile.ProfileId,
			ProfileVersion = profile.ProfileVersion,
			DisplayName = profile.DisplayName,
			Description = profile.Description,
			MachineType = profile.MachineType,
			Manufacturer = profile.Manufacturer,
			DefaultUpdateInterval = profile.DefaultUpdateInterval,
			Signals = profile.Signals,
			HiddenProcessStates = profile.HiddenProcessStates,
			Dependencies = dependencies,
			Metadata = profile.Metadata
		};
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(mutated);
		Assert.False(result.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)result.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "DEPENDENCY_TARGET_NOT_FOUND"));
	}

	[Fact]
	public async Task InvalidUpdateInterval_IsDetected()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		List<SignalDefinition> signals = profile.Signals.ToList();
		signals[0] = CloneSignal(signals[0], delegate(SignalDefinitionBuilder b)
		{
			b.UpdateInterval = TimeSpan.Zero;
		});
		PhysicalProfileValidationResult result = new PhysicalMachineProfileValidator().Validate(CloneWithSignals(profile, signals));
		Assert.False(result.IsValid);
		Assert.Contains((IEnumerable<PhysicalProfileIssue>)result.Errors, (Predicate<PhysicalProfileIssue>)((PhysicalProfileIssue e) => e.Code == "SIGNAL_UPDATE_INTERVAL_INVALID"));
	}

	[Fact]
	public async Task RuntimeFactory_CreatesSignalRuntimeStates()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		PhysicalMachineRuntime runtime = new PhysicalMachineRuntimeFactory().Create(profile, DateTimeOffset.UnixEpoch);
		Assert.Equal(profile.Signals.Count, runtime.Signals.Count);
		Assert.All(profile.Signals, delegate(SignalDefinition signal)
		{
			Assert.Contains((IEnumerable<SignalRuntimeState>)runtime.Signals, (Predicate<SignalRuntimeState>)((SignalRuntimeState r) => r.SignalId == signal.SignalId));
		});
	}

	[Fact]
	public async Task RuntimeFactory_CreatesHiddenProcessRuntimeStates()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		PhysicalMachineRuntime runtime = new PhysicalMachineRuntimeFactory().Create(profile, DateTimeOffset.UnixEpoch);
		Assert.Equal(profile.HiddenProcessStates.Count, runtime.HiddenProcessStates.Count);
		Assert.All(profile.HiddenProcessStates, delegate(HiddenProcessStateDefinition state)
		{
			Assert.Contains((IEnumerable<HiddenProcessRuntimeState>)runtime.HiddenProcessStates, (Predicate<HiddenProcessRuntimeState>)((HiddenProcessRuntimeState r) => r.StateId == state.StateId));
		});
	}

	[Fact]
	public async Task RuntimeFactory_AppliesInitialValues()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		PhysicalMachineRuntime runtime = new PhysicalMachineRuntimeFactory().Create(profile, DateTimeOffset.UnixEpoch);
		foreach (SignalDefinition signal in profile.Signals)
		{
			SignalRuntimeState state = runtime.Signals.Single((SignalRuntimeState s) => s.SignalId == signal.SignalId);
			Assert.Equal(signal.InitialValue, state.CurrentValue);
			Assert.Equal(signal.InitialValue, state.TargetValue);
			Assert.Equal(signal.InitialValue, state.PreviousValue);
			Assert.True(state.IsWithinHardLimits);
			Assert.Equal(0L, state.UpdateSequence);
		}
		foreach (HiddenProcessStateDefinition hidden in profile.HiddenProcessStates)
		{
			HiddenProcessRuntimeState state2 = runtime.HiddenProcessStates.Single((HiddenProcessRuntimeState s) => s.StateId == hidden.StateId);
			Assert.Equal(hidden.InitialValue, state2.CurrentValue);
			Assert.Equal(hidden.InitialValue, state2.TargetValue);
			Assert.Equal(hidden.InitialValue, state2.PreviousValue);
		}
	}

	[Fact]
	public async Task RuntimeFactory_IsDeterministicForIdenticalProfile()
	{
		PhysicalMachineProfile profile = await CreateLoader().LoadFromFileAsync(ReferenceProfilePath);
		PhysicalMachineRuntimeFactory factory = new PhysicalMachineRuntimeFactory();
		DateTimeOffset createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
		PhysicalMachineRuntime first = factory.Create(profile, createdAt);
		PhysicalMachineRuntime second = factory.Create(profile, createdAt);
		Assert.Equal(first.ProfileId, second.ProfileId);
		Assert.Equal(first.CreatedAt, second.CreatedAt);
		Assert.Equal(first.Signals.Select((SignalRuntimeState s) => (SignalId: s.SignalId, CurrentValue: s.CurrentValue, UpdateSequence: s.UpdateSequence, LastUpdatedAt: s.LastUpdatedAt)), second.Signals.Select((SignalRuntimeState s) => (SignalId: s.SignalId, CurrentValue: s.CurrentValue, UpdateSequence: s.UpdateSequence, LastUpdatedAt: s.LastUpdatedAt)));
		Assert.Equal(first.HiddenProcessStates.Select((HiddenProcessRuntimeState s) => (StateId: s.StateId, CurrentValue: s.CurrentValue, LastUpdatedAt: s.LastUpdatedAt)), second.HiddenProcessStates.Select((HiddenProcessRuntimeState s) => (StateId: s.StateId, CurrentValue: s.CurrentValue, LastUpdatedAt: s.LastUpdatedAt)));
	}
}
