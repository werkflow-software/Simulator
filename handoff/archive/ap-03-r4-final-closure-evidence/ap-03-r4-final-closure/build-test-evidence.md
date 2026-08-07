# AP 3 R4 – Build- und Testnachweis

## Build

```bash
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln
```

Ergebnis: **0 Fehler, 0 Warnungen**

## Unit- und Modelltests

```bash
dotnet test Werkflow.OpcUaSimulator.sln --filter "Category!=Integration"
```

| Suite | Ergebnis |
|-------|----------|
| Unit / Modell (119 Tests) | 119/119 bestanden |

## Integration (R4, kurz)

```bash
dotnet test Werkflow.OpcUaSimulator.Tests --filter "Physics_R4_EndToEnd_TwoMachines"
dotnet test Werkflow.OpcUaSimulator.Tests --filter "Physics_R4_IsolatedCorrelationCalibration"
```

| Test | Ergebnis |
|------|----------|
| R4 90s Zwei-Maschinen-E2E | bestanden |
| R4 isolierte Korrelationskalibrierung (9 Paare) | bestanden |

## Finaler Export-Lauf (~5 Minuten)

```powershell
$env:PHYSICS_VERIFY_SHORT="1"
$env:PHYSICS_VERIFY_EXPORT="1"
dotnet test Werkflow.OpcUaSimulator.Tests --filter "Physics_R4_EvidenceExport"
```

Siehe `AP-03-R4-opcua-end-to-end.json`: **VerificationRunId** `ap3-r4-20260806161452-836b3d46ea8e48969e`, **Passed = true**, **TotalOpcUaUpdates = 163017**.

## Nachweisdateien

Alle unter `handoff/ap-03-r4-final-closure/`:

- `AP-03-R4-isolated-correlation-calibration.json`
- `AP-03-R4-correlation-verification.json`
- `AP-03-R4-normal-range-statistics.json`
- `AP-03-R4-phase-and-job-verification.json`
- `AP-03-R4-opcua-end-to-end.json`
- `LaserProcessingMachine300.json`
- `BendingHydraulicMachine300.json`
- `build-test-evidence.md`
- `AP-03-R4-final-correlation-phase-and-evidence-closure-report.md`
