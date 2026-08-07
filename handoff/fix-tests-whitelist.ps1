$testsDir = 'C:\WerkFlow\Coding\Simulation\Werkflow.OpcUaSimulator.Tests'
$keep = @(
    'PhysicalAp3R3ModelTests.cs',
    'PhysicalAp3R4CorrelationEvaluatorTests.cs',
    'PhysicalAp3R4SegmentTests.cs',
    'PhysicalAp4FaultScenarioTests.cs',
    'PhysicalAp4VerificationHarness.cs',
    'PhysicalCorrelationEvaluator.cs',
    'PhysicalCorrelationRecorder.cs',
    'PhysicalPhaseRangeExpectations.cs',
    'PhysicalPhaseSegmentRecorder.cs',
    'PhysicalPhysicsR2VerificationHarness.cs',
    'PhysicalPhysicsR2VerificationTests.cs',
    'PhysicalPhysicsR3VerificationHarness.cs',
    'PhysicalPhysicsR3VerificationTests.cs',
    'PhysicalPhysicsR4VerificationHarness.cs',
    'PhysicalPhysicsR4VerificationTests.cs',
    'PhysicalPhysicsVerificationTests.cs',
    'PhysicalSimulationEngineTests.cs',
    'PhysicalStatisticsRecorder.cs',
    'PhysicalTestServiceFactory.cs',
    'TestLogService.cs',
    'PhysicalSignalVerificationHarness.cs',
    'PhysicalPhysicsR1VerificationHarness.cs',
    'PhysicalSignalHierarchyTests.cs',
    'PhysicalSignalRegistryTests.cs',
    'PhysicalSignalTypeMapperTests.cs',
    'PhysicalSignalVerificationTests.cs',
    'PhysicalAp3R1ProfileTests.cs',
    'TechnicalLearningMachine300R1ProfileTests.cs',
    'JobGeneratorTests.cs',
    'NodeIdParserTests.cs',
    'ValidationServiceTests.cs',
    'PhysicalMachineProfileTests.cs',
    'R1LongRunReport.cs',
    'R1MachineReport.cs'
)

Get-ChildItem -Path $testsDir -Filter '*.cs' -File | Where-Object { $keep -notcontains $_.Name } | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "Removed $($_.Name)"
}
