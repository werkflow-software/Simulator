# AP-06-R3 Build / Test Evidence

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp6R3"
```

## Results

| Step | Result |
|------|--------|
| `dotnet build -c Release` | Success (0 errors) |
| `PhysicalAp6R3` tests | **9 passed**, 0 failed |
| Full suite (`Category!=Integration`) | Skipped per user request |

Verification export: `PhysicalAp6R3Tests.AP6R3_Evidence_ExportVerificationJson`
