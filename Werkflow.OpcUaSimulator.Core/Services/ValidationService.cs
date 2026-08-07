using System.Collections.Generic;
using System.Linq;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.Utilities;

namespace Werkflow.OpcUaSimulator.Core.Services;

public sealed class ValidationService : IValidationService
{
	public ValidationResult ValidateForSimulationStart(AppConfiguration config)
	{
		ValidationResult validationResult = new ValidationResult();
		if (config.Machines.Count((MachineConfiguration m) => m.IsActive) == 0)
		{
			validationResult.AddError("Mindestens eine aktive Maschine ist erforderlich.");
		}
		validationResult.Errors.AddRange(ValidatePorts(config.Machines).Errors);
		foreach (MachineConfiguration item in config.Machines.Where((MachineConfiguration m) => m.IsActive))
		{
			ValidationResult validationResult2 = ValidateMachine(item, config.Machines);
			validationResult.Errors.AddRange(validationResult2.Errors);
			validationResult.Warnings.AddRange(validationResult2.Warnings);
		}
		if (config.Settings.MinBatchSize > config.Settings.MaxBatchSize)
		{
			validationResult.AddError("Minimale Losgröße darf nicht größer als maximale Losgröße sein.");
		}
		foreach (EventTypeSettings @event in config.Events.Events)
		{
			double probabilityPercent = @event.ProbabilityPercent;
			if ((probabilityPercent < 0.0 || probabilityPercent > 100.0) ? true : false)
			{
				validationResult.AddError("Ereignis '" + @event.EventType.ToGermanLabel() + "': Wahrscheinlichkeit muss zwischen 0 und 100 % liegen.");
			}
			if (@event.MinDurationMs > @event.MaxDurationMs)
			{
				validationResult.AddError("Ereignis '" + @event.EventType.ToGermanLabel() + "': Mindestdauer darf nicht größer als Maximaldauer sein.");
			}
			if (@event.MinCooldownMs > @event.MaxCooldownMs)
			{
				validationResult.AddError("Ereignis '" + @event.EventType.ToGermanLabel() + "': Mindestwartezeit darf nicht größer als Maximalwartezeit sein.");
			}
		}
		return validationResult;
	}

	public ValidationResult ValidateMachine(MachineConfiguration machine, IReadOnlyList<MachineConfiguration> allMachines)
	{
		ValidationResult validationResult = new ValidationResult();
		if (string.IsNullOrWhiteSpace(machine.Name))
		{
			validationResult.AddError($"Maschine (Port {machine.Port}): Name fehlt.");
		}
		if (string.IsNullOrWhiteSpace(machine.Host))
		{
			validationResult.AddError("Maschine '" + machine.Name + "': Host fehlt.");
		}
		int port = machine.Port;
		if ((port < 1 || port > 65535) ? true : false)
		{
			validationResult.AddError($"Maschine '{machine.Name}': Port {machine.Port} ist ungültig.");
		}
		if (string.IsNullOrWhiteSpace(machine.NamespaceUri))
		{
			validationResult.AddError("Maschine '" + machine.Name + "': Namespace URI fehlt.");
		}
		ValidationResult validationResult2 = NodeIdParser.ValidateNodeMappings(machine.Nodes);
		foreach (string error in validationResult2.Errors)
		{
			validationResult.AddError("Maschine '" + machine.Name + "': " + error);
		}
		List<string> list = (from m in allMachines
			where m.Port == machine.Port && m.Id != machine.Id
			select m.Name).ToList();
		if (list.Count > 0)
		{
			validationResult.AddError($"Maschine '{machine.Name}': Port {machine.Port} wird auch von '{string.Join(", ", list)}' verwendet.");
		}
		return validationResult;
	}

	public ValidationResult ValidatePorts(IReadOnlyList<MachineConfiguration> machines)
	{
		ValidationResult validationResult = new ValidationResult();
		List<IGrouping<int, MachineConfiguration>> list = (from m in machines
			group m by m.Port into g
			where g.Count() > 1
			select g).ToList();
		foreach (IGrouping<int, MachineConfiguration> item in list)
		{
			string value = string.Join(", ", item.Select((MachineConfiguration m) => m.Name));
			validationResult.AddError($"Portkonflikt {item.Key}: {value}");
		}
		return validationResult;
	}
}
