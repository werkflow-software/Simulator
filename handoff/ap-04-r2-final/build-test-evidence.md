# Build and Test Evidence (AP-04-R2)

Date: 2026-08-07

## Build

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

Result: **0 errors**, 70 warnings (pre-existing nullable warnings in Core).

## Unit / short tests

```powershell
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

| Metric | Value |
|--------|-------|
| Total | 120 |
| Passed | 120 |
| Failed | 0 |
| Duration | ~61 s |

## AP-04-R2 integration suite

```powershell
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp4R2"
```

| Test | Result |
|------|--------|
| AP4R2_TimelineValidator_RejectedWhenThresholdConfirmedWithoutFirstReached | Passed |
| AP4R2_TimelineValidator_RequiresErrorActiveDuringFault | Passed |
| AP4R2_FaultRecovery_LaserAndHydraulicPass | Passed |
| AP4R2_ComplexScenarios_PassDirectedChecks | Passed |
| AP4R2_FinalEndToEnd_ShortRun | Passed (~2 min) |
| AP4R2_EvidenceExport_WhenRequested | Passed (~2 min, `AP4R2_VERIFY_EXPORT=1`) |

Total: **6/6 passed**, duration ~4 min (full suite).

## Evidence export

```powershell
$env:AP4R2_VERIFY_EXPORT="1"
$env:AP4R2_E2E_SECONDS="120"
dotnet test --filter "FullyQualifiedName~AP4R2_EvidenceExport"
```

VerificationRunId: `ap4r2-20260807134325-418d2c20cf314e6faf4c0be`
