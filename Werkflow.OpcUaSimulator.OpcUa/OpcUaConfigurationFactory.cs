using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Configuration;
using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.Models;

namespace Werkflow.OpcUaSimulator.OpcUa;

internal static class OpcUaConfigurationFactory
{
	public static async Task<ApplicationConfiguration> CreateAsync(MachineConfiguration machine, CancellationToken cancellationToken = default(CancellationToken))
	{
		string pkiRoot = GetMachinePkiRoot(machine);
		string ownPath = Path.Combine(pkiRoot, "own");
		string trustedPath = Path.Combine(pkiRoot, "trusted");
		string issuerPath = Path.Combine(pkiRoot, "issuer");
		string rejectedPath = Path.Combine(pkiRoot, "rejected");
		Directory.CreateDirectory(ownPath);
		Directory.CreateDirectory(trustedPath);
		Directory.CreateDirectory(issuerPath);
		Directory.CreateDirectory(rejectedPath);
		string endpoint = machine.Endpoint.TrimEnd('/');
		ApplicationConfiguration config = new ApplicationConfiguration
		{
			ApplicationName = "Werkflow OpcUa Simulator - " + machine.Name,
			ApplicationType = ApplicationType.Server,
			ApplicationUri = machine.NamespaceUri,
			ProductUri = "urn:werkflow:opcua-simulator",
			ServerConfiguration = new ServerConfiguration
			{
				BaseAddresses = { endpoint },
				SecurityPolicies = new ServerSecurityPolicyCollection
				{
					new ServerSecurityPolicy
					{
						SecurityMode = MessageSecurityMode.None,
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None"
					},
					new ServerSecurityPolicy
					{
						SecurityMode = MessageSecurityMode.Sign,
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
					},
					new ServerSecurityPolicy
					{
						SecurityMode = MessageSecurityMode.SignAndEncrypt,
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
					}
				},
				UserTokenPolicies = new UserTokenPolicyCollection
				{
					new UserTokenPolicy(UserTokenType.Anonymous)
					{
						SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
					}
				},
				MinRequestThreadCount = 5,
				MaxRequestThreadCount = 100,
				MaxQueuedRequestCount = 200,
				MaxSessionCount = 100,
				MinSessionTimeout = 10000,
				MaxSessionTimeout = 3600000
			},
			SecurityConfiguration = new SecurityConfiguration
			{
				ApplicationCertificate = new CertificateIdentifier
				{
					StoreType = "Directory",
					StorePath = ownPath,
					SubjectName = Utils.Format("CN=Werkflow OpcUa Simulator {0}", machine.Name)
				},
				TrustedIssuerCertificates = new CertificateTrustList
				{
					StoreType = "Directory",
					StorePath = issuerPath
				},
				TrustedPeerCertificates = new CertificateTrustList
				{
					StoreType = "Directory",
					StorePath = trustedPath
				},
				RejectedCertificateStore = new CertificateTrustList
				{
					StoreType = "Directory",
					StorePath = rejectedPath
				},
				AutoAcceptUntrustedCertificates = true,
				AddAppCertToTrustedStore = true,
				SuppressNonceValidationErrors = true,
				MinimumCertificateKeySize = 2048
			},
			TransportQuotas = new TransportQuotas
			{
				OperationTimeout = 120000,
				MaxStringLength = 1048576,
				MaxByteStringLength = 1048576,
				MaxArrayLength = 65535,
				MaxMessageSize = 4194304,
				MaxBufferSize = 65535,
				ChannelLifetime = 300000,
				SecurityTokenLifetime = 3600000
			},
			TraceConfiguration = new TraceConfiguration()
		};
		await config.Validate(ApplicationType.Server).ConfigureAwait(continueOnCapturedContext: false);
		return config;
	}

	public static async Task<ApplicationInstance> CreateApplicationInstanceAsync(
		MachineConfiguration machine,
		ILogService logService,
		CancellationToken cancellationToken = default(CancellationToken))
	{
		ApplicationConfiguration config = await CreateAsync(machine, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await CreateValidatedApplicationInstanceAsync(machine, config, logService, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task EnsureCertificateAsync(ApplicationInstance application, ILogService logService, string machineName, CancellationToken cancellationToken = default(CancellationToken))
	{
		ApplicationConfiguration config = application.ApplicationConfiguration ?? throw new InvalidOperationException("ApplicationConfiguration fehlt.");
		await EnsureCertificateLoadedAsync(application, config, logService, machineName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<ApplicationInstance> CreateValidatedApplicationInstanceAsync(
		MachineConfiguration machine,
		ApplicationConfiguration config,
		ILogService logService,
		CancellationToken cancellationToken)
	{
		ApplicationInstance application = CreateApplicationInstance(config);
		if (await TryCheckApplicationCertificateAsync(application, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			await CompleteCertificateLoadAsync(application, config, logService, machine.Name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return application;
		}

		logService.Log(LogCategory.Server, "Ungültiges Serverzertifikat erkannt; PKI-Speicher wird zurückgesetzt.", machine.Name);
		return await RecreateApplicationInstanceWithFreshCertificateAsync(machine, config, logService, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<ApplicationInstance> RecreateApplicationInstanceWithFreshCertificateAsync(
		MachineConfiguration machine,
		ApplicationConfiguration config,
		ILogService logService,
		CancellationToken cancellationToken)
	{
		ClearMachineCertificateStores(config.SecurityConfiguration);
		logService.Log(LogCategory.Server, "Ungültiges Serverzertifikat entfernt; PKI-Speicher zurückgesetzt.", machine.Name);

		ApplicationConfiguration freshConfig = await CreateAsync(machine, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		ApplicationInstance application = CreateApplicationInstance(freshConfig);
		if (!await TryCheckApplicationCertificateAsync(application, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			throw new InvalidOperationException("Anwendungszertifikat für '" + machine.Name + "' konnte nach PKI-Bereinigung nicht erstellt oder geladen werden.");
		}

		await CompleteCertificateLoadAsync(application, freshConfig, logService, machine.Name, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		logService.Log(LogCategory.Server, "Serverzertifikat neu erstellt und im PKI-Speicher abgelegt.", machine.Name);
		return application;
	}

	private static async Task EnsureCertificateLoadedAsync(
		ApplicationInstance application,
		ApplicationConfiguration config,
		ILogService logService,
		string machineName,
		CancellationToken cancellationToken,
		bool allowRegeneration = true)
	{
		if (!await TryCheckApplicationCertificateAsync(application, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			if (!allowRegeneration)
			{
				throw new InvalidOperationException("Anwendungszertifikat für '" + machineName + "' konnte nicht erstellt oder geladen werden.");
			}

			throw new InvalidOperationException("Anwendungszertifikat für '" + machineName + "' ist ungültig.");
		}

		await CompleteCertificateLoadAsync(application, config, logService, machineName, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task CompleteCertificateLoadAsync(
		ApplicationInstance application,
		ApplicationConfiguration config,
		ILogService logService,
		string machineName,
		CancellationToken cancellationToken)
	{
		_ = application;
		_ = cancellationToken;
		if (config.CertificateValidator == null)
		{
			config.CertificateValidator = new CertificateValidator();
		}
		await config.CertificateValidator.UpdateAsync(config.SecurityConfiguration).ConfigureAwait(continueOnCapturedContext: false);
		X509Certificate2 certificate = await config.SecurityConfiguration.ApplicationCertificate.Find(needPrivateKey: true).ConfigureAwait(continueOnCapturedContext: false)
			?? throw new InvalidOperationException("Kein gültiges Serverzertifikat für '" + machineName + "' im Zertifikatsspeicher gefunden.");
		if (!certificate.HasPrivateKey)
		{
			throw new InvalidOperationException("Serverzertifikat für '" + machineName + "' besitzt keinen privaten Schlüssel.");
		}

		logService.Log(LogCategory.Server, "Serverzertifikat geladen: " + certificate.Subject, machineName);
	}

	private static async Task<bool> TryCheckApplicationCertificateAsync(ApplicationInstance application, CancellationToken cancellationToken)
	{
		try
		{
			return await application.CheckApplicationInstanceCertificates(silent: false, 2048, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (ServiceResultException ex) when (ShouldRegenerateApplicationCertificate(ex))
		{
			return false;
		}
		catch (CryptographicException)
		{
			return false;
		}
	}

	private static ApplicationInstance CreateApplicationInstance(ApplicationConfiguration config) =>
		new()
		{
			ApplicationName = config.ApplicationName,
			ApplicationType = ApplicationType.Server,
			ApplicationConfiguration = config
		};

	private static bool ShouldRegenerateApplicationCertificate(Exception exception)
	{
		if (exception is ServiceResultException serviceResultException)
		{
			if (serviceResultException.StatusCode == StatusCodes.BadCertificateInvalid
				|| serviceResultException.StatusCode == StatusCodes.BadCertificateUriInvalid
				|| serviceResultException.StatusCode == StatusCodes.BadCertificateTimeInvalid)
			{
				return true;
			}
		}

		return exception.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
			|| exception.Message.Contains("Please update or delete the certificate", StringComparison.OrdinalIgnoreCase);
	}

	internal static string GetMachinePkiRoot(MachineConfiguration machine) =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Werkflow",
			"OpcUaSimulator",
			"pki",
			machine.Id.ToString("N"),
			machine.Port.ToString());

	internal static void ClearMachineCertificateStores(SecurityConfiguration securityConfiguration)
	{
		ClearCertificateStore(securityConfiguration.ApplicationCertificate.StorePath);
		ClearCertificateStore(securityConfiguration.TrustedIssuerCertificates?.StorePath);
		ClearCertificateStore(securityConfiguration.TrustedPeerCertificates?.StorePath);
		ClearCertificateStore(securityConfiguration.RejectedCertificateStore?.StorePath);
	}

	private static void ClearCertificateStore(string? storePath)
	{
		if (string.IsNullOrWhiteSpace(storePath) || !Directory.Exists(storePath))
		{
			return;
		}

		foreach (string file in Directory.EnumerateFiles(storePath, "*", SearchOption.AllDirectories))
		{
			File.Delete(file);
		}

		foreach (string directory in Directory.EnumerateDirectories(storePath, "*", SearchOption.AllDirectories).OrderByDescending(static path => path.Length))
		{
			Directory.Delete(directory, recursive: false);
		}
	}
}
