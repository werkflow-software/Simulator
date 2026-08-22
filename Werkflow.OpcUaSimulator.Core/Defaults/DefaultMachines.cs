using System.Collections.Generic;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;

namespace Werkflow.OpcUaSimulator.Core.Defaults;

public static class DefaultMachines
{
	public static List<MachineConfiguration> Create()
	{
		return new List<MachineConfiguration>
		{
			CreateVirtualMachine(),
			CreateVirtualPressBrakeMachine(),
			CreateMachine(3, 4842, "Langsam", "Langsame, stabile Produktion", 8000, 0.4, 0.3, 10000, 60000, 10000, 60000, 0.5, MachineState.Idle),
			CreateMachine(4, 4843, "Störanfällig", "Leicht erhöhte Störwahrscheinlichkeit, global auf 25 % begrenzt", 2500, 1.0, 1.0, 10000, 90000, 10000, 90000, 1.0, MachineState.Idle)
		};
	}

	private static MachineConfiguration CreateVirtualMachine()
	{
		MachineConfiguration machine = CreateMachine(
			1,
			VirtualMachineContract.Port,
			"Schnell",
			"Virtuelle Laser-Produktionsmaschine für inMotion- und VIGIL-Lernversuche",
			1000,
			0.3,
			0.3,
			5000,
			30000,
			5000,
			30000,
			2.0,
			MachineState.Idle);
		machine.Id = VirtualMachineContract.MachineId;
		machine.Name = VirtualMachineContract.DisplayName;
		machine.PhysicalProfileId = VirtualMachineContract.PhysicalProfileId;
		machine.UpdateEndpointFromHostPort();
		return machine;
	}

	private static MachineConfiguration CreateVirtualPressBrakeMachine()
	{
		MachineConfiguration machine = CreateMachine(
			2,
			VirtualPressBrakeContract.Port,
			"Normal",
			"Virtuelle Biegemaschine für VIGIL-Generalisierungsvalidierung (Machine 2)",
			1200,
			0.0,
			0.0,
			5000,
			30000,
			5000,
			30000,
			2.0,
			MachineState.Idle);
		machine.Id = VirtualPressBrakeContract.MachineId;
		machine.Name = VirtualPressBrakeContract.DisplayName;
		machine.PhysicalProfileId = VirtualPressBrakeContract.PhysicalProfileId;
		machine.NamespaceUri = VirtualPressBrakeContract.NamespaceUri;
		machine.ErrorProbabilityPercent = 0.0;
		machine.DisconnectProbabilityPercent = 0.0;
		machine.UpdateEndpointFromHostPort();
		return machine;
	}

	public static MachineConfiguration CreateVigilLabMachine()
	{
		MachineConfiguration machine = CreateMachine(
			5,
			VigilLabMachineContract.Port,
			"VIGIL LAB",
			"VIGIL interner Lernversuch-Laser mit reduziertem OPC-UA-Signalvertrag",
			1000,
			0.0,
			0.0,
			5000,
			30000,
			5000,
			30000,
			2.0,
			MachineState.Idle);
		machine.Id = VigilLabMachineContract.MachineId;
		machine.Name = VigilLabMachineContract.DisplayName;
		machine.PhysicalProfileId = VigilLabMachineContract.PhysicalProfileId;
		machine.NamespaceUri = VigilLabMachineContract.NamespaceUri;
		machine.ErrorProbabilityPercent = 0.0;
		machine.DisconnectProbabilityPercent = 0.0;
		machine.UpdateEndpointFromHostPort();
		return machine;
	}

	private static MachineConfiguration CreateMachine(int index, int port, string profile, string description, int productionIntervalMs, double errorProb, double disconnectProb, int minError, int maxError, int minOffline, int maxOffline, double speedFactor, MachineState baseState, bool startInError = false)
	{
		MachineConfiguration machineConfiguration = new MachineConfiguration
		{
			Name = $"Maschine {index}",
			Description = description,
			Host = "localhost",
			Port = port,
			NamespaceUri = $"urn:werkflow:simulator:machine{index}",
			ProductionIntervalMs = productionIntervalMs,
			ErrorProbabilityPercent = errorProb,
			DisconnectProbabilityPercent = disconnectProb,
			MinErrorDurationMs = minError,
			MaxErrorDurationMs = maxError,
			MinOfflineDurationMs = minOffline,
			MaxOfflineDurationMs = maxOffline,
			ProductionSpeedFactor = speedFactor,
			BaseState = baseState,
			StartInErrorState = startInError,
			Nodes = NodeMappingPresets.GetDefaultForMachine(index)
		};
		if ((uint)(index - 1) <= 1u && index != 2)
		{
			machineConfiguration.PhysicalProfileId = index == 1 ? "laser-processing-machine-300" : "bending-hydraulic-machine-300";
		}
		machineConfiguration.UpdateEndpointFromHostPort();
		return machineConfiguration;
	}
}
