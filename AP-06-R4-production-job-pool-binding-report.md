# AP-06-R4 – Production Job/Part Pool Binding

**Machine:** Werkflow Virtual Laser 01 (Machine 1, port 4840)  
**Date:** 2026-08-11  
**Status:** Implemented – pending Product Owner visual verification

---

## Root Cause

Three interacting defects prevented production job binding and counter ticks in standalone Virtual Machine HMI mode:

1. **`StartMachineServerAsync` used `assignJob: false`** – starting the VM OPC UA server never pulled a job from the pool.
2. **`AssignJobIfMissingAsync` treated `"—"` as assigned** – runtime defaults use em-dash placeholders for `JobName`/`PartName` with `TargetCounter = 100`. The old check `IsNullOrWhiteSpace(JobName) || TargetCounter <= 0` never matched, so reassignment was skipped.
3. **Production loop required global `SimulationState.Running`** – HMI standalone start sets `IsProducing = true` but leaves global state `Stopped`, so counters never incremented.

## Job Pool (unchanged, reused)

| Source | Content |
|--------|---------|
| `FixedSimulationCatalog.cs` | 20 jobs `Job-001`…`Job-020`, parts `Part-001`…`Part-020`, batch sizes 25–1000 |
| `JobDispatcher.cs` | Assigns pending jobs per machine; reuses completed when configured |
| `JobGenerator.cs` | Regenerates pool when empty |
| `ConfigurationService.InitializeAsync` | Loads `jobs.json` or seeds from catalog |

## Binding Fix

| Change | Location |
|--------|----------|
| `IsPlaceholderValue` / `IsJobUnassigned` | Treats `—`, `-`, `–`, empty, null as unassigned; requires no `AssignedJobId` |
| `EnsureJobPoolReady` | Regenerates jobs via `JobGenerator` when no pending/assigned/completed-reusable jobs exist |
| `EnsureStandaloneEngineReady` | Creates persistent `_globalCts`, seeds RNG, ensures pool – for VM standalone |
| `StartMachineServerAsync` | VM (`VirtualMachineContract.MachineId`): `assignJob: true` + standalone engine token |
| `AssignJobIfMissingAsync` | Uses `IsJobUnassigned`, calls `EnsureJobPoolReady`, publishes after assign |
| `ShouldTickProduction` | Ticks when global `Running` **or** standalone (`Stopped` + server online + producing) |

## Job/Part Behavior After Fix

- **Start Machine (HMI):** OPC UA server starts → job assigned from pool → `JobName`/`PartName`/`TargetCounter` populated → `IsProducing = true`, state `Running`.
- **Start Production:** Ensures job if missing, then starts production (unchanged HMI flow, now functional).
- **OPC UA:** `PublishAll` publishes `JobName`, `PartName`, `ActualCounter`, `TargetCounter`, `LastProductionChange` via existing node mappings.
- **HMI:** `HmiSemanticResolver` derives `RemainingCounter` from `TargetCounter - ActualCounter` at refresh time.

## Counters

- `IncrementCounter` updates `ActualCounter`, `LastProductionChange`, linked `SimulationJob.ActualCounter`, and publishes via OPC UA.
- Ticks occur on configured `ProductionIntervalMs` when `ShouldTickProduction` is true.
- Job completion at `ActualCounter >= TargetCounter` triggers existing `HandleJobCompletion` (idle, job marked completed, optional auto-restart).

## Stop / Pause / Reset

| Action | Behavior |
|--------|----------|
| **Pause production** | `IsProducing = false`, state `Paused` – counters stop |
| **Resume production** | `IsProducing = true`, state `Running` – counters resume |
| **Stop production (HMI)** | Pause + manual idle |
| **Shutdown machine** | Stops server, loop cancelled via CTS |
| **Global simulation stop** | Cancels `_globalCts`, resets runtime (unchanged) |
| **Reset counters** | `ResetCountersAsync` zeros `ActualCounter` (unchanged) |

## Changed Files

```
Werkflow.OpcUaSimulator.Core/Services/SimulationEngine.cs
AP-06-R4-production-job-pool-binding-report.md
handoff/ap-06-r4-production-pool/AP-06-R4-production-job-pool-binding-report.md
handoff/ap-06-r4-production-pool/changed-files.txt
handoff/ap-06-r4-production-pool/build-proof.txt
handoff/ap-06-r4-production-pool/git-closure.md
```

**Not changed:** physics, faults, ground truth, 309 signals, NodeIds, port 4840, HMI view models.

## Build Result

```
dotnet build Werkflow.OpcUaSimulator.sln -c Release
→ 0 Fehler / 0 Errors
→ Pre-existing warnings only (no new errors from R4 changes)
```

## Plausibility Check (static, short)

| Check | Result |
|-------|--------|
| VM start path sets `assignJob: true` | ✓ `machineId == VirtualMachineContract.MachineId` |
| Placeholder `"—"` detected as unassigned | ✓ `IsPlaceholderValue` |
| Pool refill when empty | ✓ `EnsureJobPoolReady` → `JobGenerator.RegenerateJobs` |
| Standalone counter tick without global Running | ✓ `ShouldTickProduction` allows `Stopped + IsServerOnline` |
| Global pause blocks ticks | ✓ `ShouldTickProduction` excludes `Paused` |
| OPC UA publish on assign | ✓ `PublishMachine` in `AssignJobIfMissingAsync` + `AssignJobToMachine` |
| `LastProductionChange` on increment | ✓ existing `IncrementCounter` (unchanged) |

## Commit

Message: `Bind virtual machine production job and part pool`  
(See `handoff/ap-06-r4-production-pool/git-closure.md` for hash after commit.)

## Git Status After Commit

Only R4-related files staged and committed. Unrelated handoff modifications remain unstaged.

## Product Owner Verification Note

**Manual visual verification required:**

1. Launch app → open Virtual Machine HMI for **Werkflow Virtual Laser 01**.
2. Click **Start Machine** – confirm Job/Part show real values (e.g. `Job-003` / `Part-003`), not `—`.
3. Confirm counter increments (Actual / Target) in HMI overview and Production tab.
4. Connect OPC UA client to `opc.tcp://localhost:4840` – verify `JobName`, `PartName`, counters, `LastProductionChange` update.
5. Test Pause → Resume → counters pause/resume.
6. Test Shutdown → restart → new job assigned from pool.
