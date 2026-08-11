# Git closure – AP-06-R6 (handoff documentation state)

## Branch

`feature/physical-learning-simulator`

## HEAD at documentation capture

| Field | Value |
|-------|--------|
| SHA | `da9499485de04dd842c192a8f84740fa53b119db` |
| Message | `Separate classic simulator and virtual machine operating modes` |

**Note:** AP-06-R6 **implementation** was **not committed** at this capture. HEAD still points to AP-06-R5.

## Intended R6 implementation commit (not yet applied)

```
Add fixed production job pool and realistic job changes
```

## Worktree status

**NOT clean.**

### R6-related uncommitted source (modified / new)

- Werkflow.OpcUaSimulator.Core/Defaults/FixedProductionJobDefinition.cs (new)
- Werkflow.OpcUaSimulator.Core/Defaults/FixedSimulationCatalog.cs
- Werkflow.OpcUaSimulator.Core/Models/SimulationJob.cs
- Werkflow.OpcUaSimulator.Core/Models/MachineRuntimeState.cs
- Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Models/PhysicalJobState.cs
- Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Models/PhysicalSimulationContext.cs
- Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Services/PhysicalJobCoordinator.cs
- Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Services/ProcessPhaseScheduler.cs
- Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Services/HiddenProcessStateEngine.cs
- Werkflow.OpcUaSimulator.Core/Interfaces/IJobDispatcher.cs
- Werkflow.OpcUaSimulator.Core/Services/JobDispatcher.cs
- Werkflow.OpcUaSimulator.Core/Interfaces/IPhysicalSignalPublishingCoordinator.cs
- Werkflow.OpcUaSimulator.Core/Services/SimulationEngine.cs
- Werkflow.OpcUaSimulator.OpcUa/PhysicalSignals/PhysicalSignalPublishingCoordinator.cs
- Werkflow.OpcUaSimulator.App/VirtualMachine/ViewModels/VirtualMachineHmiViewModel.cs
- Werkflow.OpcUaSimulator.App/VirtualMachine/Views/VirtualMachineHmiWindow.cs
- Werkflow.OpcUaSimulator.Tests/JobGeneratorTests.cs

### R6 handoff (this correction)

- handoff/ap-06-r6-fixed-jobpool/** (report, build-proof, git-closure, changed-files, source/)
- AP-06-R6-fixed-jobpool-and-realistic-job-change-report.md (root)

### Other unrelated dirty paths

Many modified files under `handoff/ap-04*`, `handoff/ap-05*`, contrast/HMI polish files, etc. — **not part of R6**.

## Handoff documentation commit (optional)

If committed separately:

```
Complete AP-06-R6 handoff documentation
```

(stages handoff folder + root report only; no R6 source at repo root)
