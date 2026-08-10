# AP-04-R7 Build/Test

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp4R7"
```

## Ergebnis (2026-08-10)

- Build Release: 0 Fehler
- `Category!=Integration`: 163 bestanden, 0 fehlgeschlagen
- R7-Suite: 5 bestanden, 0 fehlgeschlagen
