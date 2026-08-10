# Build and Test Evidence (AP-04-R4)

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "FullyQualifiedName~PhysicalAp4R4"
```
