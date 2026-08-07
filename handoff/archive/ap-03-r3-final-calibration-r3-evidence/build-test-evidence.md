# AP 3 R3 – Build- und Testnachweis

## Build

```bash
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln
```

Ergebnis: **0 Fehler, 0 Warnungen**

## Tests

```bash
dotnet test Werkflow.OpcUaSimulator.sln --filter "Category!=Integration"
dotnet test Werkflow.OpcUaSimulator.sln --filter "Category=Integration&FullyQualifiedName~R3"
```

| Suite | Ergebnis |
|-------|----------|
| Unit (98 Tests) | 98/98 bestanden |
| Integration R3 (2 Tests) | 2/2 bestanden |

## Kurz-End-to-End (Export)

```powershell
$env:PHYSICS_VERIFY_SHORT="1"
$env:PHYSICS_VERIFY_EXPORT="1"
dotnet test Werkflow.OpcUaSimulator.sln --filter "FullyQualifiedName~Physics_R3_EvidenceExport"
```

- Dauer: ~5 Minuten
- `AP-03-R3-opcua-end-to-end.json`: **Passed = true**
- OPC-UA-Updates: **162806**
- Statistiksignale: **92** (45 Laser + 47 Biegen)

## Nachweisdateien

Alle unter `handoff/ap-03-r3-final-calibration/`:

- `AP-03-R3-model-calibration.json`
- `AP-03-R3-normal-range-statistics.json`
- `AP-03-R3-phase-statistics.json`
- `AP-03-R3-correlation-verification.json`
- `AP-03-R3-opcua-end-to-end.json`
