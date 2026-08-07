# Build and Test Evidence (AP-04-R1)

Date: 2026-08-07

## Commands

```powershell
dotnet restore Werkflow.OpcUaSimulator.sln
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

AP-4-R1 evidence export:

```powershell
$env:AP4R1_VERIFY_EXPORT="1"
$env:AP4R1_E2E_SECONDS="90"
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~AP4R1_EvidenceExport"
```

## Results

- Non-integration tests: **116/116 passed** (~63 s)
- AP4-R1 unit tests: **6/6 passed**
- AP4-R1 integration E2E (60 s): passed
- Evidence export run: **Passed=true** (VerificationRunId `ap4r1-20260807084001-07e24990823e408fac95c4a`)
- Build warnings: 33 (nullable CS8600/CS8602 in test project; recovery decompile pattern, not new AP4-R1 regressions)
