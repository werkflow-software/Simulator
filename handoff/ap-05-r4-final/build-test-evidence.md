# AP-05-R4 Build and Test Evidence

## Build

```
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

Result: **SUCCESS** (0 errors)

## Tests

```
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

Result: **212 passed**, 0 failed, 0 skipped

Duration: ~1m 38s

## R4-Specific Tests

`PhysicalAp5R4EvidenceTests` (13 tests): all passed

## Verification

`AP5R4_EventHygieneVerification_Passes`: passed

Evidence: `AP-05-R4-event-hygiene-verification.json`
