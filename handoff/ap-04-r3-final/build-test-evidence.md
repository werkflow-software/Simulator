# Build and Test Evidence (AP-04-R3)

Date: 2026-08-09

## Build

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

Result: **0 errors**, 33 warnings.

## Unit / short tests

```powershell
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

| Metric | Value |
|--------|-------|
| Total | 137 |
| Passed | 137 |
| Failed | 0 |
| Duration | ~1 m 15 s |

## AP-04-R3 suite

```powershell
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp4R3"
```

| Result | Count |
|--------|-------|
| Passed | 17 |
| Failed | 0 |

Includes `AP4R3_CompletenessVerification_Passes` and validator unit tests.

## Evidence export

```powershell
$env:AP4R3_VERIFY_EXPORT="1"
dotnet test --filter "FullyQualifiedName~AP4R3_EvidenceExport"
```

VerificationRunId: `ap4r3-20260809210206-014c6ff6636f478aaa229b5`
