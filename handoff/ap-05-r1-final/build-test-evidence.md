dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"

Results (2026-08-10):
- Build: Release OK
- Tests Category!=Integration: 181 passed, 0 failed
- AP5 R1 E2E harness: Passed=true
