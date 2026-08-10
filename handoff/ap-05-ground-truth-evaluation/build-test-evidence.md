# AP-05 Build/Test

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp5"
```

## Result

- Build Release: 0 errors
- Category!=Integration: 173 passed
- AP5 suite: 10 passed
