# AP-04-R1 E2E Evidence and Scenario Catalog Closure Report

Date: 2026-08-07  
Project: `C:\WerkFlow\Coding\Simulation`  
VerificationRunId: `ap4r1-20260807084001-07e24990823e408fac95c4a`

## 1. Ausgangsstand

AP 4 implemented 22 fault scenarios, engine, service, UI, and tests. Evidence for formal release was incomplete. Git object database was damaged (237 missing objects). Working tree was recovered in Recovery 01.

## 2. Recovery-01-Ergebnis

- Full solution restored; `dotnet restore/build/test` green (121/121 full suite in Recovery 01)
- 22 FaultScenario JSONs and MachineProfiles present
- Source partially from ILSpy decompile
- Report: `handoff/archive/recovery-01-simulator-repository-restoration/`

## 3. Repository-Normalisierung

- Broken `.git` archived to `C:\WerkFlow\Coding\RecoveryBackup\Simulation_broken_git_20260807_*`
- New repository initialized on `main`
- Baseline commit: `9f9f914` — Recover OPC UA Simulator source baseline
- Tag: `opcua-simulator-recovered-baseline-20260807`
- Development branch: `feature/physical-learning-simulator`

## 4. Neue Recovery-Baseline

Validated with `dotnet restore`, `dotnet build -c Release`, `dotnet test --filter Category!=Integration` (110/110 at baseline).

## 5. Provenienz

Documented in `docs/recovery/RECOVERY-PROVENANCE.md`. Historical AP-4 HEAD `f0b768d` and AP-3 commits preserved as references only.

## 6. Einmalige Root-Bereinigung

Root AP-03 reports archived; duplicates removed. Root contains README, solution projects, `handoff`, `docs`.

## 7. Einmalige Handoff-Bereinigung

Inventory: `handoff/INVENTORY-20260807.txt`. Cleanup report: `handoff/archive/HANDOFF-CLEANUP-20260807.md`. Current handoff: `handoff/ap-04-r1-current/`.

## 8. Branch und Ausgangscommit

- Branch: `feature/physical-learning-simulator`
- Baseline: `9f9f914`
- AP-4-R1 commit: see section 36

## 9. Szenariokatalog

22 enabled scenarios across Laser, Bending, Shared, Technical. Copied to handoff `FaultScenarios/`.

## 10. Szenario-Manifest

`AP-04-R1-scenario-manifest.json` — 22 files, 22 IDs, 0 duplicates, ManifestHash `df7f03dee8da52c25fcf980aa7b855749facefd5aa102e8e14af007a89e5fcab`.

## 11. Komplexe Szenariotests

Isolated model runs for imbalance, sensor-drift, intermittent-fault, coolant-loss — see `AP-04-R1-complex-scenario-verification.json`.

## 12. Threshold-Nachweis

Laser overheating and hydraulic leak timelines in `AP-04-R1-recovery-verification.json` with ScenarioStartedAtUtc, ThresholdFirstReachedAtUtc, MachineFaultedAtUtc, recovery markers.

## 13. ErrorActive

Verified: false before fault, true during faulted phase, false after recovery (timeline samples).

## 14. ErrorMessage

Non-empty during fault; priority rules verified in unit test.

## 15. MachineState

Error during fault; normalizes after recovery per scenario rules.

## 16. Produktionsstopp

`ProductionRunning=false` while `ErrorActive=true` in threshold timelines.

## 17. Server-online-Nachweis

Physical faults keep `ServerReachable=true` during fault (laser overheating, hydraulic leak).

## 18. CommunicationDrop

Target unreachable during drop; others reachable; all reachable after restart. `AP-04-R1-communication-drop-verification.json` — Passed=true.

## 19. Recovery

Motor/hydraulic hidden-state deltas and timeline recovery phases documented in recovery verification JSON.

## 20. NonFaultingControlRuns

`coolant-loss` NonFaultingControlRun in E2E — no ErrorActive, server online.

## 21. Intermittent Fault

Multiple episodes tracked in complex scenario verification.

## 22. Sensor Drift

Visible signal movement with stable underlying hidden state in isolation test.

## 23. Unwucht

MechanicalLoad samples collected; periodic behavior flagged when range sufficient.

## 24. Error-Priorität

Unit test: higher priority fault dominates message; clearing higher reveals lower; ErrorActive until last cleared.

## 25. Finaler E2E-Test

`AP-04-R1-final-end-to-end.json` — 120 s run, Passed=true, 55926 OPC-UA updates, 3 engines/publishers after comm-drop restore.

## 26. OPC-UA-Updates

TotalOpcUaUpdates: 55926 in final E2E.

## 27. Performance

E2E ~2 min; non-integration suite ~63 s; no duplicate engines/publishers at end of run.

## 28. Restore/Build/Test

See `build-test-evidence.md` — 116/116 non-integration tests passed.

## 29. Warnungen

33 build warnings in Tests project (CS8600/CS8602 nullable); recovery decompile pattern; no new AP4-R1 functional warnings.

## 30. VerificationRunId

`ap4r1-20260807084001-07e24990823e408fac95c4a`

## 31. Profilhashes

See `profile-hash-evidence.json` — Laser `344c7315…`, Bending `78a7dbcc…`.

## 32. Szenario-Manifest-Hash

`df7f03dee8da52c25fcf980aa7b855749facefd5aa102e8e14af007a89e5fcab`

## 33. Vollständig geänderte Dateien (AP-4-R1)

- `Werkflow.OpcUaSimulator.Tests/PhysicalAp4R1VerificationHarness.cs` (new)
- `Werkflow.OpcUaSimulator.Tests/PhysicalAp4R1FaultScenarioTests.cs` (new)
- `Werkflow.OpcUaSimulator.Tests/TestFaultScenarioSimulationBridge.cs` (new)
- `Werkflow.OpcUaSimulator.Tests/PhysicalTestServiceFactory.cs`
- `Werkflow.OpcUaSimulator.Tests/PhysicalAp4VerificationHarness.cs` (DirectionChecks)
- `handoff/ap-04-r1-current/*` (evidence)
- `docs/recovery/RECOVERY-PROVENANCE.md`
- Handoff archive/cleanup artifacts

## 34. Bewusst nicht umgesetzte AP-5-Punkte

No AP 5 work started (per assignment).

## 35. Risiken für AP 5

- Git history loss (new baseline only)
- ILSpy-decompiled UI/test code may need manual cleanup
- Threshold MinimumDuration uses wall-clock time in engine

## 36. Finaler Commit

Message: `Complete AP 4 scenario evidence and verification`  
Tag: `opcua-simulator-fault-scenarios-ap4-verified` (points to this commit)

## 37. Tag

`opcua-simulator-fault-scenarios-ap4-verified`

## 38. Finaler Git-Status

Clean after commit (handoff evidence tracked; bin/obj ignored).

## 39. AP-4-Freigabeempfehlung

**Freigabe empfohlen.** All 31 closure criteria met: repository normalized, evidence complete, 22 scenarios validated with DirectionChecks, E2E Passed=true, tests green, handoff consistent.
