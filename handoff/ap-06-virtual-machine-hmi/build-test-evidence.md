# AP-06 Build / Test Evidence

```text
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

**Ergebnis (2026-08-10):**

- Build: Release erfolgreich
- Tests: 224 bestanden, 0 Fehler, 0 übersprungen
- AP-6 Tests: 12 bestanden (Contract, Coverage, Leakage, Harness, Evidence)
