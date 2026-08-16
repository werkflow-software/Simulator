using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

internal static class VigilLabVirtualMachineP03R1ReproHarness
{
	public static async Task<DualOpcUaReproResult> ReproduceDualServerStartupAsync(
		int firstPort = 48640,
		int secondPort = 48644,
		CancellationToken cancellationToken = default)
	{
		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var serverService = new MachineServerService(log, stack.Coordinator);

		var existingLaser = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		existingLaser.Host = "localhost";
		existingLaser.Port = firstPort;
		existingLaser.UpdateEndpointFromHostPort();

		var vigilLab = DefaultMachines.CreateVigilLabMachine();
		vigilLab.Host = "localhost";
		vigilLab.Port = secondPort;
		vigilLab.UpdateEndpointFromHostPort();

		OpcUaCertificateLifecycleHarness.DeletePkiRoot(existingLaser);
		OpcUaCertificateLifecycleHarness.DeletePkiRoot(vigilLab);

		var result = new DualOpcUaReproResult
		{
			FirstEndpoint = existingLaser.Endpoint,
			SecondEndpoint = vigilLab.Endpoint
		};

		try
		{
			await StartMachineAsync(serverService, stack.Coordinator, existingLaser, cancellationToken);
			result.FirstServerStarted = serverService.IsRunning(existingLaser.Id);
			result.FirstReadAfterStart = await TryReadMachineStateAsync(existingLaser, cancellationToken);

			await StartMachineAsync(serverService, stack.Coordinator, vigilLab, cancellationToken);
			result.SecondServerStarted = serverService.IsRunning(vigilLab.Id);
			result.FirstStillRunningAfterSecondStart = serverService.IsRunning(existingLaser.Id);
			result.FirstReadAfterSecondStart = await TryReadMachineStateAsync(existingLaser, cancellationToken);
			result.SecondReadAfterSecondStart = await TryReadMachineStateAsync(vigilLab, cancellationToken);

			await serverService.StopServerAsync(existingLaser.Id, cancellationToken);
			result.SecondStillRunningAfterFirstStop = serverService.IsRunning(vigilLab.Id);
			result.SecondReadAfterFirstStop = await TryReadMachineStateAsync(vigilLab, cancellationToken);
		}
		catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadServerHalted)
		{
			result.BadServerHalted = true;
			result.ExceptionMessage = ex.Message;
			result.ExceptionStack = ex.StackTrace;
		}
		catch (Exception ex)
		{
			result.ExceptionMessage = ex.ToString();
			result.ExceptionStack = ex.StackTrace;
		}
		finally
		{
			await stack.Coordinator.StopAllAsync(cancellationToken);
			await serverService.StopAllAsync(cancellationToken);
		}

		return result;
	}

	private static async Task StartMachineAsync(
		MachineServerService serverService,
		PhysicalSignalPublishingCoordinator coordinator,
		MachineConfiguration machine,
		CancellationToken cancellationToken)
	{
		coordinator.PrepareMachine(machine, 42);
		var runtime = new MachineRuntimeState
		{
			MachineId = machine.Id,
			IsServerOnline = true,
			State = MachineState.Idle
		};
		await serverService.StartServerAsync(machine, runtime, cancellationToken);
		coordinator.TrySetGenerationMode(machine.Id, SignalGenerationMode.Physical);
	}

	public static async Task<OpcReadResult> ReadMachineStateAsync(MachineConfiguration machine, CancellationToken cancellationToken) =>
		await TryReadMachineStateAsync(machine, cancellationToken);

	private static async Task<OpcReadResult> TryReadMachineStateAsync(MachineConfiguration machine, CancellationToken cancellationToken)
	{
		var read = new OpcReadResult { Endpoint = machine.Endpoint };
		try
		{
			var config = await PhysicalSignalVerificationHarness.CreateClientConfigurationForTestsAsync(cancellationToken);
			EndpointDescription selected = CoreClientUtils.SelectEndpoint(config, machine.Endpoint, useSecurity: false);
			ConfiguredEndpoint endpointConfig = new ConfiguredEndpoint(null, selected, EndpointConfiguration.Create(config));
			using Session session = await Session.Create(
				config,
				endpointConfig,
				updateBeforeConnect: false,
				"P03R1DualServerRepro",
				60000u,
				new UserIdentity(),
				null,
				cancellationToken);

			int nsIndex = session.NamespaceUris.GetIndex(machine.NamespaceUri);
			var value = session.ReadValue(new NodeId("Machine.MachineState", (ushort)nsIndex));
			read.Success = StatusCode.IsGood(value.StatusCode);
			read.StatusCode = value.StatusCode.ToString();
			read.Value = value.Value?.ToString();
		}
		catch (ServiceResultException ex)
		{
			read.Success = false;
			read.StatusCode = ex.StatusCode.ToString();
			read.Value = ex.Message;
		}
		catch (Exception ex)
		{
			read.Success = false;
			read.StatusCode = "Exception";
			read.Value = ex.Message;
		}

		return read;
	}
}

internal sealed class DualOpcUaReproResult
{
	public string FirstEndpoint { get; set; } = string.Empty;
	public string SecondEndpoint { get; set; } = string.Empty;
	public bool FirstServerStarted { get; set; }
	public bool SecondServerStarted { get; set; }
	public bool FirstStillRunningAfterSecondStart { get; set; }
	public bool SecondStillRunningAfterFirstStop { get; set; }
	public bool BadServerHalted { get; set; }
	public OpcReadResult? FirstReadAfterStart { get; set; }
	public OpcReadResult? FirstReadAfterSecondStart { get; set; }
	public OpcReadResult? SecondReadAfterSecondStart { get; set; }
	public OpcReadResult? SecondReadAfterFirstStop { get; set; }
	public string? ExceptionMessage { get; set; }
	public string? ExceptionStack { get; set; }
}

internal sealed class OpcReadResult
{
	public string Endpoint { get; set; } = string.Empty;
	public bool Success { get; set; }
	public string? StatusCode { get; set; }
	public string? Value { get; set; }
}
