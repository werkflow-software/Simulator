using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Werkflow.OpcUaSimulator.Core.Defaults;
using Werkflow.OpcUaSimulator.Core.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Models;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Werkflow.OpcUaSimulator.OpcUa;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;

namespace Werkflow.OpcUaSimulator.Tests;

public static class PhysicalSignalVerificationHarness
{
	public static async Task<VerificationReport> RunAsync(VerificationOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		VerificationReport report = new VerificationReport
		{
			StartedAtUtc = DateTime.UtcNow,
			Options = options
		};
		TestLogService log = new TestLogService();
		new PhysicalMachineSessionFactory(new JsonPhysicalMachineProfileLoader(new PhysicalMachineProfileValidator()), new PhysicalMachineProfileValidator(), new PhysicalMachineRuntimeFactory());
		PhysicalSignalPublishingCoordinator coordinator = PhysicalTestServiceFactory.CreateCoordinator(log);
		MachineServerService serverService = new MachineServerService(log, coordinator);
		List<MachineConfiguration> machines = BuildMachines(options.MachineCount);
		report.MemoryStartMb = GetMemoryMb();
		try
		{
			foreach (MachineConfiguration machine in machines)
			{
				coordinator.PrepareMachine(machine, options.Seed);
				MachineRuntimeState runtime = new MachineRuntimeState
				{
					MachineId = machine.Id
				};
				await serverService.StartServerAsync(machine, runtime, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				report.ActiveServers++;
			}
			report.SignalCountPerMachine = (from s in coordinator.GetSessions()
				select s.Profile.Signals.Count((SignalDefinition s) => s.IsEnabled)).ToList();
			report.RegisteredNodesPerMachine = (from s in coordinator.GetSessions()
				select s.OpcUaNodeCount).ToList();
			report.Endpoints = machines.Select((MachineConfiguration m) => m.Endpoint).ToList();
			report.MachineNames = machines.Select((MachineConfiguration m) => m.Name).ToList();
			DateTime publishStart = DateTime.UtcNow;
			while (DateTime.UtcNow - publishStart < options.PublishDuration)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await Task.Delay(1000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			foreach (PhysicalMachineSession session in coordinator.GetSessions())
			{
				report.UpdatesPerSecondPerMachine.Add(session.Metrics.UpdatesPerSecond);
				report.AveragePublishDurationMsPerMachine.Add(session.Metrics.AveragePublishDurationMs);
				report.MaxPublishDurationMsPerMachine.Add(session.Metrics.MaxPublishDurationMs);
				report.FailedUpdatesPerMachine.Add(session.Metrics.FailedUpdates);
				report.SkippedIdenticalPerMachine.Add(session.Metrics.SkippedIdenticalValues);
			}
			if (options.MachineCount >= 2 && options.TestMachineIsolation)
			{
				MachineConfiguration machine2 = machines[0];
				PhysicalMachineSession session2Before = coordinator.GetSession(machines[1].Id);
				double ups2Before = session2Before.Metrics.UpdatesPerSecond;
				await serverService.StopServerAsync(machine2.Id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				await Task.Delay(2000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				PhysicalMachineSession session2During = coordinator.GetSession(machines[1].Id);
				report.Machine2UpdatesWhileMachine1Stopped = session2During.Metrics.UpdatesPerSecond;
				report.Machine2StillUpdatingWhileMachine1Stopped = session2During.Metrics.UpdatesPerSecond >= ups2Before * 0.5;
				report.Machine1StoppedSuccessfully = coordinator.GetSession(machine2.Id) == null;
				coordinator.PrepareMachine(machine2, options.Seed);
				await serverService.StartServerAsync(machine2, new MachineRuntimeState
				{
					MachineId = machine2.Id,
					IsServerOnline = true
				}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				report.Machine1RestartNodeCount = coordinator.GetSession(machine2.Id)?.OpcUaNodeCount ?? 0;
				report.Machine1RestartSameNodeCount = report.Machine1RestartNodeCount == report.RegisteredNodesPerMachine[0];
				report.PublisherCountAfterMachine1Restart = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Running);
			}
			if (options.TestPauseResume)
			{
				await coordinator.PauseAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				report.PublishersDuringPause = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Paused);
				await Task.Delay(500, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				Dictionary<string, string> pausedValues1 = SampleStableValues(coordinator, machines[0].Id);
				await Task.Delay(3000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				Dictionary<string, string> pausedValues2 = SampleStableValues(coordinator, machines[0].Id);
				report.ValuesStableDuringPause = pausedValues1.SequenceEqual(pausedValues2);
				report.ServerReachableDuringPause = serverService.IsRunning(machines[0].Id);
				await coordinator.ResumeAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				await Task.Delay(2000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				report.PublisherCountAfterResume = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Running);
				report.NoDuplicatePublishersAfterResume = report.PublisherCountAfterResume == coordinator.GetSessions().Count;
			}
			if (options.TestStopRestartCycles > 0)
			{
				VerificationReport verificationReport = report;
				verificationReport.StopRestartCycles = await RunStopRestartCyclesAsync(serverService, coordinator, machines[0], options, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (options.TestDataChange)
			{
				VerificationReport verificationReport2 = report;
				verificationReport2.DataChangeResults = await RunDataChangeClientAsync(machines[0], cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			report.ActivePublishers = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Running);
			report.Exceptions = (from e in log.Entries
				where e.Category == LogCategory.Error
				select e.Message).ToList();
		}
		finally
		{
			await coordinator.StopAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await serverService.StopAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			report.MemoryEndMb = GetMemoryMb();
			report.EndedAtUtc = DateTime.UtcNow;
			report.Duration = report.EndedAtUtc - report.StartedAtUtc;
		}
		return report;
	}

	private static async Task<List<StopRestartCycleResult>> RunStopRestartCyclesAsync(MachineServerService serverService, PhysicalSignalPublishingCoordinator coordinator, MachineConfiguration machine, VerificationOptions options, CancellationToken cancellationToken)
	{
		List<StopRestartCycleResult> cycles = new List<StopRestartCycleResult>();
		for (int cycle = 1; cycle <= options.TestStopRestartCycles; cycle++)
		{
			StopRestartCycleResult result = new StopRestartCycleResult
			{
				Cycle = cycle
			};
			result.NodesBeforeStop = coordinator.GetSession(machine.Id)?.OpcUaNodeCount ?? 0;
			result.PublishersBeforeStop = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Running);
			await serverService.StopServerAsync(machine.Id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			result.RegistryCleared = coordinator.GetSession(machine.Id) == null;
			result.ServerStopped = !serverService.IsRunning(machine.Id);
			await Task.Delay(1000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			coordinator.PrepareMachine(machine, options.Seed);
			await serverService.StartServerAsync(machine, new MachineRuntimeState
			{
				MachineId = machine.Id,
				IsServerOnline = true
			}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			result.NodesAfterRestart = coordinator.GetSession(machine.Id)?.OpcUaNodeCount ?? 0;
			result.SameNodeCount = result.NodesAfterRestart == result.NodesBeforeStop;
			result.PublisherCount = coordinator.GetSessions().Count((PhysicalMachineSession s) => s.Metrics.State == PhysicalPublisherState.Running);
			result.SinglePublisher = result.PublisherCount == 1;
			result.PortAvailable = serverService.IsRunning(machine.Id);
			cycles.Add(result);
		}
		return cycles;
	}

	private static List<MachineConfiguration> BuildMachines(int count)
	{
		List<MachineConfiguration> list = DefaultMachines.Create().Take(count).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].PhysicalProfileId = "technical-learning-machine-300";
			list[i].Port = 14850 + i;
			list[i].UpdateEndpointFromHostPort();
		}
		return list;
	}

	private static Dictionary<string, string> SampleStableValues(PhysicalSignalPublishingCoordinator coordinator, Guid machineId)
	{
		PhysicalMachineSession session = coordinator.GetSession(machineId);
		return session.Profile.Signals.Where(delegate(SignalDefinition s)
		{
			bool isEnabled = s.IsEnabled;
			bool flag = isEnabled;
			if (flag)
			{
				TechnicalSignalBehavior technicalBehavior = s.TechnicalBehavior;
				bool flag2 = (((uint)(technicalBehavior - 2) <= 3u || technicalBehavior == TechnicalSignalBehavior.Stable) ? true : false);
				flag = flag2;
			}
			return flag;
		}).Take(20).ToDictionary((SignalDefinition s) => s.SignalId, (SignalDefinition s) => SignalRuntimeValueHelper.GetCurrentValue(s, session.Runtime.Signals.First((SignalRuntimeState r) => r.SignalId == s.SignalId))?.ToString() ?? "");
	}

	private static Dictionary<string, string> SampleValues(PhysicalSignalPublishingCoordinator coordinator, Guid machineId)
	{
		PhysicalMachineSession session = coordinator.GetSession(machineId);
		return session.Profile.Signals.Where((SignalDefinition s) => s.IsEnabled).Take(20).ToDictionary((SignalDefinition s) => s.SignalId, (SignalDefinition s) => SignalRuntimeValueHelper.GetCurrentValue(s, session.Runtime.Signals.First((SignalRuntimeState r) => r.SignalId == s.SignalId))?.ToString() ?? "");
	}

	private static double GetMemoryMb()
	{
		return (double)Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
	}

	public static Task<ApplicationConfiguration> CreateClientConfigurationForTestsAsync(CancellationToken cancellationToken)
	{
		return CreateClientConfigurationAsync(cancellationToken);
	}

	private static async Task<ApplicationConfiguration> CreateClientConfigurationAsync(CancellationToken cancellationToken)
	{
		string pkiRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Werkflow", "OpcUaSimulator", "pki", "verification-client");
		string ownPath = Path.Combine(pkiRoot, "own");
		string trustedPath = Path.Combine(pkiRoot, "trusted");
		string issuerPath = Path.Combine(pkiRoot, "issuer");
		string rejectedPath = Path.Combine(pkiRoot, "rejected");
		Directory.CreateDirectory(ownPath);
		Directory.CreateDirectory(trustedPath);
		Directory.CreateDirectory(issuerPath);
		Directory.CreateDirectory(rejectedPath);
		ApplicationConfiguration config = new ApplicationConfiguration
		{
			ApplicationName = "PhysicalSignalVerificationClient",
			ApplicationUri = "urn:werkflow:verification-client",
			ApplicationType = ApplicationType.Client,
			SecurityConfiguration = new SecurityConfiguration
			{
				ApplicationCertificate = new CertificateIdentifier
				{
					StoreType = "Directory",
					StorePath = ownPath,
					SubjectName = "CN=PhysicalSignalVerificationClient"
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
				RejectSHA1SignedCertificates = false,
				AddAppCertToTrustedStore = true
			},
			ClientConfiguration = new ClientConfiguration
			{
				DefaultSessionTimeout = 60000
			},
			TransportQuotas = new TransportQuotas
			{
				OperationTimeout = 15000
			}
		};
		await config.Validate(ApplicationType.Client).ConfigureAwait(continueOnCapturedContext: false);
		ApplicationInstance application = new ApplicationInstance
		{
			ApplicationName = config.ApplicationName,
			ApplicationType = ApplicationType.Client,
			ApplicationConfiguration = config
		};
		if (!(await application.CheckApplicationInstanceCertificates(silent: false, 2048, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)))
		{
			throw new InvalidOperationException("Verification client certificate could not be created.");
		}
		ApplicationConfiguration applicationConfiguration = config;
		if (applicationConfiguration.CertificateValidator == null)
		{
			applicationConfiguration.CertificateValidator = new CertificateValidator();
		}
		await config.CertificateValidator.UpdateAsync(config.SecurityConfiguration).ConfigureAwait(continueOnCapturedContext: false);
		return config;
	}

	private static async Task<List<DataChangeSample>> RunDataChangeClientAsync(MachineConfiguration machine, CancellationToken cancellationToken)
	{
		List<DataChangeSample> samples = new List<DataChangeSample>();
		ApplicationConfiguration config = await CreateClientConfigurationAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		EndpointDescription selected = CoreClientUtils.SelectEndpoint(config, machine.Endpoint, useSecurity: false);
		ConfiguredEndpoint endpointConfig = new ConfiguredEndpoint(null, selected, EndpointConfiguration.Create(config));
		using Session session = await Session.Create(config, endpointConfig, updateBeforeConnect: false, "Verification", 60000u, new UserIdentity(), null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		(string NodePath, PhysicalSignalDataType Type)[] signalIds = new(string, PhysicalSignalDataType)[7]
		{
			("Process.SpindleSpeed", PhysicalSignalDataType.Double),
			("Production.OeeAvailability", PhysicalSignalDataType.Float),
			("Production.QueueLength", PhysicalSignalDataType.Int32),
			("Production.CycleCounter", PhysicalSignalDataType.Int64),
			("Production.AutomaticMode", PhysicalSignalDataType.Boolean),
			("Production.ActiveProgram", PhysicalSignalDataType.String),
			("Production.LastCycleStartUtc", PhysicalSignalDataType.DateTime)
		};
		int nsIndex = session.NamespaceUris.GetIndex(machine.NamespaceUri);
		ConcurrentDictionary<string, DataValue> notifications = new ConcurrentDictionary<string, DataValue>();
		Subscription subscription = new Subscription(session.DefaultSubscription)
		{
			PublishingInterval = 500
		};
		session.AddSubscription(subscription);
		subscription.Create();
		(string NodePath, PhysicalSignalDataType Type)[] array = signalIds;
		for (int i = 0; i < array.Length; i++)
		{
			(string, PhysicalSignalDataType) tuple = array[i];
			string path = tuple.Item1;
			NodeId nodeId = new NodeId(path, (ushort)nsIndex);
			MonitoredItem item = new MonitoredItem(subscription.DefaultItem)
			{
				StartNodeId = nodeId,
				AttributeId = 13u,
				SamplingInterval = 500,
				QueueSize = 10u,
				DiscardOldest = true
			};
			item.Notification += delegate(MonitoredItem _, MonitoredItemNotificationEventArgs e)
			{
				if (e.NotificationValue is MonitoredItemNotification monitoredItemNotification)
				{
					notifications[path] = monitoredItemNotification.Value;
				}
			};
			subscription.AddItem(item);
		}
		subscription.ApplyChanges();
		await Task.Delay(8000, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		(string NodePath, PhysicalSignalDataType Type)[] array2 = signalIds;
		for (int j = 0; j < array2.Length; j++)
		{
			(string, PhysicalSignalDataType) tuple = array2[j];
			string path2 = tuple.Item1;
			PhysicalSignalDataType type = tuple.Item2;
			NodeId nodeId2 = new NodeId(path2, (ushort)nsIndex);
			DataValue initial = session.ReadValue(nodeId2);
			notifications.TryGetValue(path2, out DataValue notified);
			DataValue later = session.ReadValue(nodeId2);
			bool counterMonotonic = type != PhysicalSignalDataType.Int64 || (initial.Value is long a && later.Value is long b && b >= a);
			samples.Add(new DataChangeSample
			{
				NodePath = path2,
				DataType = type.ToString(),
				InitialValue = initial.Value?.ToString(),
				LaterValue = later.Value?.ToString(),
				InitialSourceTimestamp = initial.SourceTimestamp,
				LaterSourceTimestamp = later.SourceTimestamp,
				SubscriptionReceived = (notified != null),
				SourceTimestampUpdated = (later.SourceTimestamp > initial.SourceTimestamp || notified?.SourceTimestamp > initial.SourceTimestamp),
				TypeMatches = (initial.Value != null && MatchesType(initial.Value, type)),
				CounterMonotonic = counterMonotonic
			});
			notified = null;
		}
		subscription.Delete(silent: true);
		return samples;
	}

	private static bool MatchesType(object value, PhysicalSignalDataType type)
	{
		if (1 == 0)
		{
		}
		bool result = type switch
		{
			PhysicalSignalDataType.Double => value is double, 
			PhysicalSignalDataType.Float => value is float, 
			PhysicalSignalDataType.Int32 => value is int, 
			PhysicalSignalDataType.Int64 => value is long, 
			PhysicalSignalDataType.Boolean => value is bool, 
			PhysicalSignalDataType.String => value is string, 
			PhysicalSignalDataType.DateTime => value is DateTime, 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
