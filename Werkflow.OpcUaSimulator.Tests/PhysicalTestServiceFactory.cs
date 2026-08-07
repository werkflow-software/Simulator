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

internal sealed record FaultScenarioTestStack(
    PhysicalSignalPublishingCoordinator Coordinator,
    IFaultScenarioService FaultScenarioService,
    PhysicalRuntimeCoordinator RuntimeCoordinator);

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

    public static FaultScenarioTestStack CreateFaultScenarioService(ILogService log)
    {
        var faultEffectCalculator = new FaultEffectCalculator();
        var faultRecoveryEngine = new FaultRecoveryEngine();
        var faultScenarioEngine = new FaultScenarioEngine(faultEffectCalculator, faultRecoveryEngine);
        var engine = new PhysicalSimulationEngine(
            new HiddenProcessStateEngine(),
            new SignalCalculationEngine(),
            new PhysicalModelValidator(),
            faultScenarioEngine);

        var runtimeCoordinator = new PhysicalRuntimeCoordinator(engine);
        var faultScenarioService = new FaultScenarioService(
            new JsonFaultScenarioRepository(ResolveFaultScenariosDirectory()),
            new FaultScenarioValidator(),
            new FaultScenarioRuntimeFactory(),
            faultRecoveryEngine);

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

        return new FaultScenarioTestStack(coordinator, faultScenarioService, runtimeCoordinator);
    }

    public static PhysicalSimulationEngine CreateEngine() =>
        new(new HiddenProcessStateEngine(), new SignalCalculationEngine(), new PhysicalModelValidator());
}
