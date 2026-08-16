using System.Security.Cryptography.X509Certificates;
using Opc.Ua;
using Opc.Ua.Configuration;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.VirtualMachine;
using Werkflow.OpcUaSimulator.OpcUa;

namespace Werkflow.OpcUaSimulator.Tests;

internal static class OpcUaCertificateLifecycleHarness
{
	public const int VigilLabTestPort = 58644;
	public const int ExistingLaserTestPort = 58640;

	public static MachineConfiguration CreateVigilLabTestMachine(int port = VigilLabTestPort)
	{
		var machine = DefaultMachines.CreateVigilLabMachine();
		machine.Host = "localhost";
		machine.Port = port;
		machine.UpdateEndpointFromHostPort();
		return machine;
	}

	public static MachineConfiguration CreateExistingLaserTestMachine(int port = ExistingLaserTestPort)
	{
		var machine = DefaultMachines.Create().First(m => m.Port == VirtualMachineContract.Port);
		machine.Host = "localhost";
		machine.Port = port;
		machine.UpdateEndpointFromHostPort();
		return machine;
	}

	public static string GetPkiRoot(MachineConfiguration machine) =>
		OpcUaConfigurationFactory.GetMachinePkiRoot(machine);

	public static async Task SeedStaleWrongUriCertificateAsync(MachineConfiguration machine, CancellationToken cancellationToken = default)
	{
		var config = await OpcUaConfigurationFactory.CreateAsync(machine, cancellationToken);
		var stale = CertificateFactory.CreateCertificate(
				"urn:werkflow:simulator:machine1",
				"stale",
				config.SecurityConfiguration.ApplicationCertificate.SubjectName,
				new[] { "localhost" })
			.SetRSAKeySize(2048)
			.CreateForRSA();
		try
		{
			using ICertificateStore store = config.SecurityConfiguration.ApplicationCertificate.OpenStore();
			await store.Add(stale, string.Empty);
		}
		finally
		{
			stale.Dispose();
		}
	}

	public static async Task<CertificateProbeResult> ProbeCertificateEnsureAsync(
		MachineConfiguration machine,
		CancellationToken cancellationToken = default)
	{
		var result = new CertificateProbeResult
		{
			MachineName = machine.Name,
			Endpoint = machine.Endpoint,
			PkiRoot = GetPkiRoot(machine)
		};

		var log = new TestLogService();
		try
		{
			var config = await OpcUaConfigurationFactory.CreateAsync(machine, cancellationToken);
			result.ApplicationUri = config.ApplicationUri;
			result.CertificateSubject = config.SecurityConfiguration.ApplicationCertificate.SubjectName;
			result.StorePath = config.SecurityConfiguration.ApplicationCertificate.StorePath;
			result.FilesBefore = CountPkiFiles(result.PkiRoot);

			var application = await OpcUaConfigurationFactory.CreateApplicationInstanceAsync(machine, log, cancellationToken);
			var activeConfig = application.ApplicationConfiguration
				?? throw new InvalidOperationException("ApplicationConfiguration fehlt nach Zertifikatserstellung.");
			var certificate = await activeConfig.SecurityConfiguration.ApplicationCertificate.Find(needPrivateKey: true);
			result.Success = certificate != null && certificate.HasPrivateKey;
			result.CertificateFound = certificate != null;
			result.HasPrivateKey = certificate?.HasPrivateKey ?? false;
			result.CertificateSubjectResolved = certificate?.Subject;
			result.FilesAfter = CountPkiFiles(result.PkiRoot);
			application.ApplicationConfiguration = null;
		}
		catch (ServiceResultException ex)
		{
			result.Success = false;
			result.StatusCode = ex.StatusCode.ToString();
			result.ExceptionMessage = ex.Message;
			result.ExceptionStack = ex.StackTrace;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ExceptionMessage = ex.ToString();
			result.ExceptionStack = ex.StackTrace;
		}

		return result;
	}

	public static async Task<ApplicationStartupProbeResult> ProbeProductionServerStartupAsync(
		MachineConfiguration machine,
		CancellationToken cancellationToken = default)
	{
		var result = new ApplicationStartupProbeResult
		{
			Endpoint = machine.Endpoint,
			MachineId = machine.Id
		};

		var log = new TestLogService();
		var stack = PhysicalTestServiceFactory.CreateFaultScenarioService(log);
		var serverService = new MachineServerService(log, stack.Coordinator);
		var runtime = new MachineRuntimeState
		{
			MachineId = machine.Id,
			IsServerOnline = true,
			State = MachineState.Idle
		};

		try
		{
			stack.Coordinator.PrepareMachine(machine, 42);
			await serverService.StartServerAsync(machine, runtime, cancellationToken);
			result.ServerStarted = serverService.IsRunning(machine.Id);
			result.ReadResult = await VigilLabVirtualMachineP03R1ReproHarness.ReadMachineStateAsync(machine, cancellationToken);
		}
		catch (Exception ex)
		{
			result.ServerStarted = false;
			result.ExceptionMessage = ex.ToString();
		}
		finally
		{
			await stack.Coordinator.StopAllAsync(cancellationToken);
			await serverService.StopAllAsync(cancellationToken);
		}

		result.Success = result.ServerStarted && (result.ReadResult?.Success ?? false);
		return result;
	}

	public static void DeletePkiRoot(MachineConfiguration machine)
	{
		var root = GetPkiRoot(machine);
		if (Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private static int CountPkiFiles(string pkiRoot) =>
		Directory.Exists(pkiRoot)
			? Directory.GetFiles(pkiRoot, "*", SearchOption.AllDirectories).Length
			: 0;
}

internal sealed class CertificateProbeResult
{
	public string MachineName { get; set; } = string.Empty;
	public string Endpoint { get; set; } = string.Empty;
	public string PkiRoot { get; set; } = string.Empty;
	public string? ApplicationUri { get; set; }
	public string? CertificateSubject { get; set; }
	public string? CertificateSubjectResolved { get; set; }
	public string? StorePath { get; set; }
	public int FilesBefore { get; set; }
	public int FilesAfter { get; set; }
	public bool Success { get; set; }
	public bool CertificateFound { get; set; }
	public bool HasPrivateKey { get; set; }
	public string? StatusCode { get; set; }
	public string? ExceptionMessage { get; set; }
	public string? ExceptionStack { get; set; }
}

internal sealed class ApplicationStartupProbeResult
{
	public Guid MachineId { get; set; }
	public string Endpoint { get; set; } = string.Empty;
	public bool ServerStarted { get; set; }
	public bool Success { get; set; }
	public OpcReadResult? ReadResult { get; set; }
	public string? ExceptionMessage { get; set; }
}
