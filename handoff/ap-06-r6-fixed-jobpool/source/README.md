# Source snapshots

Exact copies of R6-relevant source files from repository root at handoff time.

Files are flat-named in this folder (no subdirectories).

| File | Role |
|------|------|
| FixedProductionJobDefinition.cs | Job definition record + load/feed factors |
| FixedSimulationCatalog.cs | 20-job catalog + pause constants |
| SimulationJob.cs | Extended job model |
| MachineRuntimeState.cs | Catalog index + job-change runtime fields |
| PhysicalJobState.cs | Physical job mirror fields |
| PhysicalSimulationContext.cs | Job-change pause flags |
| PhysicalJobCoordinator.cs | ApplyDefinition, stable signals, counter sync |
| ProcessPhaseScheduler.cs | Setup duration override, pause gate |
| HiddenProcessStateEngine.cs | ProcessLoadFactor on ProcessDemand |
| IJobDispatcher.cs | GetJobByCatalogIndex |
| JobDispatcher.cs | Catalog-index job resolution |
| IPhysicalSignalPublishingCoordinator.cs | BeginJobChange / ApplyProductionJob |
| SimulationEngine.cs | ScheduleJobChange, sequential assignment |
| PhysicalSignalPublishingCoordinator.cs | Physical pause + job apply |
| VirtualMachineHmiViewModel.cs | HMI job change display |
| VirtualMachineHmiWindow.cs | „Nächsten Job laden“ UI |

Canonical paths: `Werkflow.OpcUaSimulator.*` under repository root.
