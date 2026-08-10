using Werkflow.OpcUaSimulator.Core.Interfaces;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Calculation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.FaultScenarios.Validation;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Services;
using Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Validation;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals;
using Werkflow.OpcUaSimulator.OpcUa.PhysicalSignals.Mapping;

namespace Werkflow.OpcUaSimulator.Tests;

public sealed record FaultScenarioTestStack(
    PhysicalSignalPublishingCoordinator Coordinator,
    IFaultScenarioService FaultScenarioService,
    PhysicalRuntimeCoordinator RuntimeCoordinator,
    FaultScenarioEventHub EventHub,
    TestFaultScenarioSimulationBridge? Bridge = null);

internal static class PhysicalTestServiceFactory
{
    public static string ResolveFaultScenariosDirectory()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Werkflow.OpcUaSimulator.App", "FaultScenarios")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Werkflow.OpcUaSimulator.App", "bin", "Debug", "net8.0-windows", "FaultScenarios")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Werkflow.OpcUaSimulator.App", "bin", "Release", "net8.0-windows", "FaultScenarios")),
            FaultScenarioPaths.ResolveDirectory()
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    public static PhysicalSignalPublishingCoordinator CreateCoordinator(ILogService log) =>
        CreateFaultScenarioService(log).Coordinator;

    public static FaultScenarioTestStack CreateFaultScenarioService(ILogService log, TestFaultScenarioSimulationBridge? bridge = null)
    {
        var faultEffectCalculator = new FaultEffectCalculator();
        var faultRecoveryEngine = new FaultRecoveryEngine();
        var eventHub = new FaultScenarioEventHub();
        var faultScenarioEngine = new FaultScenarioEngine(faultEffectCalculator, faultRecoveryEngine, eventHub);
        var engine = new PhysicalSimulationEngine(
            new HiddenProcessStateEngine(),
            new SignalCalculationEngine(),
            new PhysicalModelValidator(),
            faultScenarioEngine,
            bridge);

        var runtimeCoordinator = new PhysicalRuntimeCoordinator(engine);
        var faultScenarioService = new FaultScenarioService(
            new JsonFaultScenarioRepository(ResolveFaultScenariosDirectory()),
            new FaultScenarioValidator(),
            new FaultScenarioRuntimeFactory(),
            faultRecoveryEngine,
            bridge,
            eventHub);

        var coordinator = new PhysicalSignalPublishingCoordinator(
            new PhysicalMachineSessionFactory(
                new JsonPhysicalMachineProfileLoader(new PhysicalMachineProfileValidator()),
                new PhysicalMachineProfileValidator(),
                new PhysicalMachineRuntimeFactory()),
            new PhysicalSignalTypeMapper(),
            new TechnicalSignalValueGenerator(),
            runtimeCoordinator,
            log,
            faultScenarioService);

        return new FaultScenarioTestStack(coordinator, faultScenarioService, runtimeCoordinator, eventHub, bridge);
    }

    public static FaultScenarioTestStack CreateFaultScenarioServiceWithServer(ILogService log, IMachineServerService serverService)
    {
        var bridge = new TestFaultScenarioSimulationBridge(serverService);
        return CreateFaultScenarioService(log, bridge);
    }

    public static PhysicalSimulationEngine CreateEngine() =>
        new(new HiddenProcessStateEngine(), new SignalCalculationEngine(), new PhysicalModelValidator());
}
