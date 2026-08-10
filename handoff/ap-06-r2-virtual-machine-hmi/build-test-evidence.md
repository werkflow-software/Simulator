# AP-06-R2 Build / Test Evidence

```text
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

## Results (2026-08-10)

| Step | Result |
|------|--------|
| `dotnet build -c Release` | Success (0 errors) |
| `dotnet test --filter "Category!=Integration"` | **252 passed**, 0 failed |
| `PhysicalAp6R2` tests | **20 passed** |
| WPF app smoke (`dotnet run`) | Application started |

## Verification export

`PhysicalAp6R2Tests.AP6R2_Evidence_ExportVerificationJson` → `AP-06-R2-virtual-machine-hmi-functional-ui-verification.json` (`ap6R2Passed: true`).
