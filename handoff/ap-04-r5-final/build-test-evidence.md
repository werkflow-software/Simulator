# Build and Test Evidence (AP-04-R5)

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp4R5"
```

154 non-integration tests passed (0 failed).

## Git

- Tag: `opcua-simulator-fault-scenarios-ap4-final-r5` auf R5-Release-Commit
