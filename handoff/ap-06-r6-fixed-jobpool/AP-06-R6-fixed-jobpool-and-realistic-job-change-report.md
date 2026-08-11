# AP-06-R6 – Fixed Production Job Pool & Realistic Job Change

**AP:** 6 R6  
**Scope:** Fixed 20-job pool, sequential job change, 1–5 min setup pause, production ↔ physical coupling  
**Verification:** Final functional verification by Product Owner.

---

## 1. Summary

The Virtual Machine uses a **fixed catalog of exactly 20 jobs** (`JOB-001` … `JOB-020`). Jobs differ in part name, quantity, material, thickness, recipe, and program. Job changes are **sequential and cyclic** (`JOB-020` → `JOB-001`). Between jobs a **setup pause of 1–5 simulation minutes** runs with `MachineState.Setup`, counters held stable, and existing physical simulation in low-demand / cooling behaviour. Manual **„Nächsten Job laden“** and automatic completion both use the same pause + next-index logic (no immediate re-assignment of the same job).

---

## 2. Fixed job pool (all 20 jobs)

Source: `FixedSimulationCatalog.BuildDefinitions()` / `FixedProductionJobDefinition`.

| # | JobName | PartName | TargetQty | Material | Thickness (mm) | RecipeName | ProgramName |
|---|---------|----------|-----------|----------|----------------|------------|-------------|
| 1 | JOB-001 | Halter_01 | 50 | S235JR | 1.0 | LaserCut-Standard-A | PRG-12045 |
| 2 | JOB-002 | Flansch_02 | 75 | 1.4301 | 1.5 | LaserCut-Fine-B | PRG-12046 |
| 3 | JOB-003 | Abdeckung_03 | 100 | AlMg3-EN-AW5754 | 2.0 | LaserCut-Standard-A | PRG-12045 |
| 4 | JOB-004 | Grundplatte_04 | 140 | CuZn37 | 2.0 | Mill-Contour-C | PRG-22010 |
| 5 | JOB-005 | Seitenblech_05 | 180 | S235JR | 3.0 | LaserCut-Standard-A | PRG-12046 |
| 6 | JOB-006 | Traeger_06 | 220 | 1.4301 | 3.0 | LaserCut-Fine-B | PRG-33008 |
| 7 | JOB-007 | Winkel_07 | 275 | AlMg3-EN-AW5754 | 4.0 | LaserCut-Standard-A | PRG-12045 |
| 8 | JOB-008 | Konsole_08 | 320 | CuZn37 | 4.0 | Mill-Contour-C | PRG-22010 |
| 9 | JOB-009 | Rahmenplatte_09 | 400 | S235JR | 5.0 | LaserCut-Fine-B | PRG-12046 |
| 10 | JOB-010 | Verstaerkung_10 | 500 | 1.4301 | 5.0 | LaserCut-Standard-A | PRG-33008 |
| 11 | JOB-011 | Gehaeuseteil_11 | 600 | AlMg3-EN-AW5754 | 6.0 | LaserCut-Fine-B | PRG-12045 |
| 12 | JOB-012 | Montageplatte_12 | 750 | CuZn37 | 6.0 | Mill-Contour-C | PRG-22010 |
| 13 | JOB-013 | Halter_13 | 850 | S235JR | 8.0 | LaserCut-Standard-A | PRG-12046 |
| 14 | JOB-014 | Flansch_14 | 900 | 1.4301 | 8.0 | LaserCut-Fine-B | PRG-33008 |
| 15 | JOB-015 | Abdeckung_15 | 1000 | AlMg3-EN-AW5754 | 10.0 | LaserCut-Standard-A | PRG-12045 |
| 16 | JOB-016 | Grundplatte_16 | 125 | CuZn37 | 1.5 | Mill-Contour-C | PRG-22010 |
| 17 | JOB-017 | Seitenblech_17 | 250 | S235JR | 2.5 | LaserCut-Fine-B | PRG-12046 |
| 18 | JOB-018 | Traeger_18 | 350 | 1.4301 | 4.5 | LaserCut-Standard-A | PRG-33008 |
| 19 | JOB-019 | Winkel_19 | 625 | AlMg3-EN-AW5754 | 7.0 | LaserCut-Fine-B | PRG-12045 |
| 20 | JOB-020 | Konsole_20 | 525 | CuZn37 | 9.0 | Mill-Contour-C | PRG-22010 |

**Checks:**

- **20 jobs** (indices 0–19, `FixedSimulationCatalog.JobCount = 20`)
- **All different** `JobName` and `PartName` pairs
- **Target quantities:** min 50 (`JOB-001`), max 1000 (`JOB-015`), spread across 50–1000

**Not separate catalog fields (metadata only on `SimulationJob`):** `Priority` derived as `CatalogIndex % 5 + 1`. No `CycleTime` field in catalog.

---

## 3. Job change mechanics

### Index model

| Runtime field | Role |
|---------------|------|
| `MachineRuntimeState.CurrentJobCatalogIndex` | 0-based index into fixed catalog (0 = `JOB-001`) |
| `FixedSimulationCatalog.GetNextCatalogIndex(n)` | `(n + 1) % 20` |
| `PhysicalJobState.JobIndex` | `CatalogIndex + 1` (1-based, for phase transitions) |

### First production job

- `AssignJobToMachine` / `AssignJobIfMissingAsync` uses catalog index **0** when `CurrentJobCatalogIndex < 0` → **JOB-001**.

### Automatic change (target reached)

1. `IncrementCounter` → `ActualCounter >= TargetCounter` → `HandleJobCompletion`
2. Current job marked completed via `JobDispatcher.CompleteJob`
3. `IsProducing = false`, `MachineState.Idle` briefly
4. If `AutoRestartCompletedJobs` and engine allows (`ShouldAutoContinueJobs`): `ScheduleJobChange` with `GetNextCatalogIndex(CurrentJobCatalogIndex)`
5. Example: after `JOB-004` (index 3) → next index **4** (`JOB-005`)

### Manual change („Nächsten Job laden“)

1. `VirtualMachineHmiViewModel` → `SimulationEngine.ChangeJobAsync`
2. If job change already active → no-op
3. Completes current assigned job if not already completed
4. `nextIndex = GetNextCatalogIndex(CurrentJobCatalogIndex)` (or 0 if unset)
5. `ScheduleJobChange` — **same pause path as automatic**

### Cyclic wrap

- After `JOB-020` (index 19): `GetNextCatalogIndex(19) = 0` → **JOB-001**

### Why the same job is no longer re-loaded immediately

**Before R6:** `ChangeJobAsync` called `GetNextJobForMachine` (priority/pending pool) or reassigned without sequential index → could pick the same or arbitrary pending job.

**After R6:**

- Assignment uses **`GetJobByCatalogIndex`** with explicit **`CurrentJobCatalogIndex`**
- Next job is always **`(currentIndex + 1) % 20`**, not `GetNextJobForMachine` random/priority selection
- `ReuseCompletedJobs = false` in default settings; completed jobs do not recycle in-place without index advance

### Job lookup

- `JobDispatcher.GetJobByCatalogIndex(catalogIndex, config)` returns pending/assigned job or creates new `SimulationJob` from `FixedSimulationCatalog.CreateSimulationJob`.

---

## 4. Job change pause (setup)

| Parameter | Value |
|-----------|--------|
| Minimum | **60 s** = 1 simulation minute (`MinJobChangePauseSeconds`) |
| Maximum | **300 s** = 5 simulation minutes (`MaxJobChangePauseSeconds`) |
| Selection | `SimulationRandom.NextInRange(_random, 60, 300)` — **seeded** via engine `_random` (`SimulationRandom.Create(_currentSeed)`) |
| Wall-clock wait | `pauseSeconds * 1000 / SimulationSpeedFactor` (min factor 0.1) |
| Production `MachineState` | **`Setup`** during pause |
| `IsProducing` | `false` |
| `IsJobChangeActive` | `true` until pause completes |
| Counters | **Stable** — `ShouldTickProduction` returns false when `IsJobChangeActive` |
| After pause | `CompleteScheduledJobChange` → `ActualCounter = 0`, new `TargetCounter`, `MachineState.Running`, `IsProducing = true` |

Preview fields for HMI during pause: `NextJobNamePreview`, `NextPartNamePreview`, `NextTargetQuantityPreview`, `JobChangeEndsAtUtc`, `JobChangePauseSeconds`.

---

## 5. Physical behaviour during setup (actual code)

Triggered by `PhysicalSignalPublishingCoordinator.BeginJobChange`:

| Effect | Mechanism |
|--------|-----------|
| Phase | `CurrentPhase = ProcessPhase.Setup` |
| Pause gate | `IsJobChangePauseActive = true`; `ProcessPhaseScheduler.TryAdvance` **does not advance phases** while active |
| Setup duration override | `OverrideSetupDuration = TimeSpan.FromSeconds(pauseSeconds)` for `ProcessPhase.Setup` min duration |
| Phase demand | `GetPhaseDemand(Setup) = 0.25` (vs Processing 0.65, PeakLoad 0.95) → lower `ProcessDemand` hidden state |
| Feed / process signals | Lower demand propagates via existing hidden-state → signal dependency chain; **not** hard-forced to zero |
| FeedRate targets | Not re-applied until `ApplyProductionJob` after pause (new job `FeedRateFactor`) |
| Temperature | Existing thermal model: cooling tendency in `Cooling`/`Idle` phases; gradual decay via `RecoveryRate` in `HiddenProcessStateEngine` — **no instant reset** |
| Cooling / fans | No explicit “stop cooling” — follows phase demand and hidden-state recovery rules |
| Production counters (physical) | `PhysicalJobCoordinator.TickProductionCounters` **skipped** while `IsJobChangePauseActive` |
| After pause | `ApplyProductionJob` → `ApplyDefinition`, stable signals updated, `CurrentPhase = RampUp` |

**Not implemented:** dedicated “vibration to zero” override; vibration follows normal signal calculation from reduced load.

---

## 6. Job parameters → physical simulation

| Job field | Physical effect | Metadata only |
|-----------|-----------------|-----------------|
| `MaterialName` | Updates `Process.MaterialName` string signal | — |
| `MaterialThicknessMm` | Updates `Process.MaterialThickness` numeric; drives **`ProcessLoadFactor`** = `0.72 + thickness × 0.05` applied to `ProcessDemand` in `HiddenProcessStateEngine` | — |
| `RecipeName` | Updates `Process.RecipeName`, `Production.RecipeName` | No direct hidden-state formula |
| `ProgramName` | Updates `Production.ActiveProgram` | No direct hidden-state formula |
| `FeedRateFactor` | `1.0 / (0.65 + thickness × 0.06)` → sets `Process.FeedRate` / `Process.FeedRateTarget` to `1200 × FeedRateFactor` | — |
| `TargetQuantity` | `PhysicalJobState.TargetQuantity`; synced from production via `SyncProductionCounters` | — |
| `JobName` / `PartName` | `PhysicalJobState` + production OPC nodes | — |
| `Priority` | Production dispatch metadata | **No physics** |
| `CycleTime` | — | **Not in catalog / no effect** |

Derived formulas live in `FixedProductionJobDefinition` (`ProcessLoadFactor`, `FeedRateFactor`).

---

## 7. OPC-UA behaviour (production semantic nodes)

Published via `MachineValuePublisher.PublishAll` / `PublishMachine` on `NodeSemanticType`:

| Node / semantic | Updated on job assign / during production |
|-----------------|-------------------------------------------|
| `JobName` | Yes |
| `PartName` | Yes |
| `ActualCounter` | Yes (increments in production; stable during setup) |
| `TargetCounter` | Yes on new job |
| `MachineState` | Yes (`Setup` during pause, `Running` in production) |
| `LastProductionChange` | Yes on counter increment |
| `RemainingCounter` | **Not a `NodeSemanticType` enum member** — HMI shows `Actual / Target`; remaining is implicit |

**Physical profile signals** (via `ApplyStableSignals` on job apply):

- `Process.MaterialThickness`, `Process.MaterialName`, `Process.RecipeName`
- `Production.ActiveProgram`, `Production.RecipeName`, `Production.MaterialDesignation`
- `Process.FeedRate`, `Process.FeedRateTarget`

No new OPC-UA nodes were added for R6.

---

## 8. HMI

- **„Nächsten Job laden“** → `ChangeJobAsync`
- During setup: `JobChangeText`, `JobChangeRemainingText`, status **EINRICHTEN**
- Next job preview from catalog index / preview fields

Files: `VirtualMachineHmiViewModel.cs`, `VirtualMachineHmiWindow.cs` (see `handoff/ap-06-r6-fixed-jobpool/source/`).

---

## 9. Changed source files (R6)

Listed in `changed-files.txt`. Snapshots in `handoff/ap-06-r6-fixed-jobpool/source/`.

---

## 10. Build

See `build-proof.txt`.

---

## 11. Git

See `git-closure.md`. R6 implementation changes were **not committed** at handoff documentation time; HEAD remained AP-06-R5.

---

## 12. Product Owner verification

Manual checks:

1. 20 distinct jobs visible in production flow  
2. Quantities 50–1000  
3. JOB-001 first, then sequential advance  
4. JOB-020 → JOB-001 wrap  
5. Setup pause ~1–5 min (scaled by time factor)  
6. Counters frozen during setup  
7. Load/temperature decay during setup  
8. New material/thickness/program after job load  
9. OPC-UA Job/Part/counters follow runtime  

**Final functional verification by Product Owner.**
