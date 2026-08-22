using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Profiles;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;

public sealed class PhysicalMachineSessionFactory : IPhysicalMachineSessionFactory
{
	private readonly IPhysicalMachineProfileLoader _loader;

	private readonly IPhysicalMachineProfileValidator _validator;

	private readonly IPhysicalMachineRuntimeFactory _runtimeFactory;

	private readonly Dictionary<string, PhysicalMachineProfile> _profiles = new Dictionary<string, PhysicalMachineProfile>(StringComparer.OrdinalIgnoreCase);

	public PhysicalMachineSessionFactory(IPhysicalMachineProfileLoader loader, IPhysicalMachineProfileValidator validator, IPhysicalMachineRuntimeFactory runtimeFactory)
	{
		_loader = loader;
		_validator = validator;
		_runtimeFactory = runtimeFactory;
		LoadBuiltInProfiles();
	}

	public PhysicalMachineSession? TryCreateSession(Guid machineId, string machineName, string? physicalProfileId)
	{
		if (string.IsNullOrWhiteSpace(physicalProfileId))
		{
			return null;
		}
		if (!_profiles.TryGetValue(physicalProfileId, out PhysicalMachineProfile value))
		{
			return null;
		}
		PhysicalProfileValidationResult physicalProfileValidationResult = _validator.Validate(value);
		if (!physicalProfileValidationResult.IsValid)
		{
			throw new InvalidOperationException("Physisches Profil '" + physicalProfileId + "' ist ungültig: " + string.Join("; ", physicalProfileValidationResult.Errors.Select((PhysicalProfileIssue e) => e.Message)));
		}
		return new PhysicalMachineSession
		{
			MachineId = machineId,
			MachineName = machineName,
			Profile = value,
			Runtime = _runtimeFactory.Create(value, null)
		};
	}

	public PhysicalMachineProfile? ResolveProfile(string profileId)
	{
		PhysicalMachineProfile value;
		return _profiles.TryGetValue(profileId, out value) ? value : null;
	}

	public IReadOnlyList<string> GetAvailableProfileIds()
	{
		return _profiles.Keys.OrderBy<string, string>((string k) => k, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private void LoadBuiltInProfiles()
	{
		RegisterProfile(TechnicalLearningMachine300ProfileFactory.Create());
		RegisterProfile(LaserProcessingMachine300ProfileFactory.Create());
		RegisterProfile(VigilLabLaserReducedProfileFactory.Create());
		RegisterProfile(VigilPressBrakeReducedProfileFactory.Create());
		RegisterProfile(BendingHydraulicMachine300ProfileFactory.Create());
		string path = PhysicalMachineProfilePaths.ResolveProfilesDirectory();
		if (!Directory.Exists(path))
		{
			return;
		}
		string[] files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
		foreach (string filePath in files)
		{
			try
			{
				string json = File.ReadAllText(filePath);
				PhysicalMachineProfile profile = _loader.Deserialize(json, filePath);
				RegisterProfile(profile);
			}
			catch
			{
			}
		}
	}

	private void RegisterProfile(PhysicalMachineProfile profile)
	{
		if (!string.IsNullOrWhiteSpace(profile.ProfileId))
		{
			_profiles[profile.ProfileId] = profile;
		}
	}
}
