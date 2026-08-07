using System;
using System.IO;
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
		string pkiRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Werkflow", "OpcUaSimulator", "pki", machine.Id.ToString("N"));
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

	public static async Task EnsureCertificateAsync(ApplicationInstance application, ILogService logService, string machineName, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!(await application.CheckApplicationInstanceCertificates(silent: false, 2048, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
		{
			throw new InvalidOperationException("Anwendungszertifikat für '" + machineName + "' konnte nicht erstellt oder geladen werden.");
		}
		ApplicationConfiguration config = application.ApplicationConfiguration ?? throw new InvalidOperationException("ApplicationConfiguration fehlt.");
		ApplicationConfiguration applicationConfiguration = config;
		if (applicationConfiguration.CertificateValidator == null)
		{
			applicationConfiguration.CertificateValidator = new CertificateValidator();
		}
		await config.CertificateValidator.UpdateAsync(config.SecurityConfiguration).ConfigureAwait(continueOnCapturedContext: false);
		X509Certificate2 certificate = await config.SecurityConfiguration.ApplicationCertificate.Find(needPrivateKey: true).ConfigureAwait(continueOnCapturedContext: false);
		if (certificate == null)
		{
			throw new InvalidOperationException("Kein gültiges Serverzertifikat für '" + machineName + "' im Zertifikatsspeicher gefunden.");
		}
		logService.Log(LogCategory.Server, "Serverzertifikat geladen: " + certificate.Subject, machineName);
	}
}
