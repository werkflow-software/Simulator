# Recovery Provenance

## Why Git history was restarted

Recovery 01 (2026-08-07) found the repository at `C:\WerkFlow\Coding\Simulation` with a **damaged Git object database**:

- `git fsck --full` reported **237 missing objects** (blobs/trees/commits)
- No pack files; loose objects incomplete
- `git restore` / `cat-file` failed for many blobs
- No remote repository configured

The damaged `.git` was archived under `C:\WerkFlow\Coding\RecoveryBackup\Simulation_broken_git_*` before this baseline was created.

## Recovery 01 summary

- Working tree reconstructed from ILSpy decompile (Debug DLLs 2026-08-06), backup copies, and protected handoff files
- `dotnet restore`, `dotnet build` (Release), `dotnet test`: **121/121 passed**
- 22 FaultScenario JSONs and 3 MachineProfiles restored
- Full report: `handoff/archive/recovery-01-simulator-repository-restoration/RECOVERY-01-simulator-repository-restoration-report.md`

## Historical references (provenance only — not recoverable commits)

| Reference | Value |
|-----------|-------|
| Branch (historical) | `feature/physical-learning-simulator` |
| AP-4 HEAD (historical) | `f0b768d8d8ecae087ccce7d42ca72a4edc18fc66` — Implement AP 4 controlled fault scenarios |
| AP-3 R4 commit (historical) | `f544ca5` |
| AP-3 closure commit (historical) | `7a4e390` |
| Tag (historical) | `opcua-simulator-fault-scenarios-ap4-complete` |
| Tag (historical) | `opcua-simulator-physical-simulation-ap3-final` |
| Tag (historical) | `opcua-simulator-physical-simulation-ap3-verified-final` |

## Source reconstruction note

Parts of the recovered source (especially WPF App views as BAML, some test helpers) were reconstructed via **ILSpy decompile** from build artifacts. This baseline preserves functional equivalence verified by tests, not bit-identical history.

## New baseline

| Field | Value |
|-------|-------|
| Recovery date | 2026-08-07 |
| Baseline tag | `opcua-simulator-recovered-baseline-20260807` |
| Baseline commit | See `git log -1` on `main` after normalization |
| Tests at baseline | 121/121 (full suite); baseline validation may use `--filter Category!=Integration` |
