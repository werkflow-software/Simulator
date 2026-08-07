# AP 4 – Build- und Testnachweis

## Build

```text
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln
Ergebnis: 0 Fehler, 2 Warnungen (unused event, vor AP 4)
```

## Tests

```text
dotnet test Werkflow.OpcUaSimulator.Tests --filter "Category!=Integration"
Ergebnis: 127/127 bestanden
```

AP-4-spezifische Tests: 10/10 bestanden (inkl. Evidence-Export mit `AP4_VERIFY_EXPORT=1`).

## VerificationRunId

`ap4-20260806200140-1933baa2a2c14238a8a51`

## E2E-Kurzlauf

| Feld | Wert |
|------|------|
| Dauer | 3 min (AP4_E2E_SECONDS=180) |
| TotalOpcUaUpdates | 113917 |
| TotalEngineTicks | 2915 |
| Passed | true |

## Szenariokatalog

22 deklarative Szenarien validiert (`AP-04-scenario-catalog-validation.json`).

## Reproduktion Evidence-Export

```powershell
$env:AP4_VERIFY_EXPORT="1"
$env:AP4_E2E_SECONDS="180"
$env:PHYSICS_VERIFY_SHORT="1"
dotnet test Werkflow.OpcUaSimulator.Tests --filter "FullyQualifiedName~AP4_EvidenceExport"
```
