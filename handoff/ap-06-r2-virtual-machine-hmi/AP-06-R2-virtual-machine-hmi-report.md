# AP-06-R2 Virtual Machine HMI – Functional + UI Report

## Problem (R1 handoff gap)

HMI window opened with OPC UA ONLINE and “Produziert”, but Job/Part empty, overview/tabs showed empty or generic DataGrids. Layout resembled debug UI, not industrial machine HMI.

## Root causes fixed

1. **Wrong semantic signal IDs** – overview used non-profile IDs (`Cooling.CoolantTemperature`, `Electrical.PowerDemand`, etc.).
2. **Jobs never assigned** – `StartMachineServerAsync` used `assignJob: false`.
3. **Technical vs physical mode** – physical ticks require `SignalGenerationMode.Physical` on machine start.
4. **UI structure** – generic signal tables instead of fixed machine areas and semantic overview metrics.
5. **Commands** – missing `NotifyCanExecuteChanged` / state-aware `CanExecute`.

## Solution

### Core (`Werkflow.OpcUaSimulator.Core`)

- `HmiSemantic` enum – stable semantic keys (X/Y/Z, Feed, temperatures, production, etc.).
- `HmiSemanticRegistry` – candidate signal ID lists for laser profile.
- `HmiSemanticResolver` – binds semantics to runtime values (signals + `MachineRuntimeState`).
- `ISimulationEngine.AssignJobIfMissingAsync` – assigns job when JobName empty.

### App HMI

- `VirtualMachineHmiViewModel` – semantic metric collections, axis/motor/temperature groups, `LiveSignalCount`, command `CanExecute`, physical mode + job assignment on start.
- `VirtualMachineHmiWindow` – industrial layout: header, left operation panel, large center metrics, right machine message + SIMULATION/TEST fault area, bottom navigation tabs.

### Tests

- `PhysicalAp6R2Tests` – 19 functional/UI binding tests + evidence export.
- `PhysicalAp6R2VerificationHarness` – automated verification JSON with runtime binding, overview, commands, tabs, OPC UA consistency, AP5 regression.

## Verification highlights

From `AP-06-R2-virtual-machine-hmi-functional-ui-verification.json`:

- Live signals: **309 / 309**
- Job visible: **true** (`JOB-R2-TEST`)
- Part visible: **true** (`PART-R2-A`)
- Overview key values visible: **15**
- Representative bindings: X/Y/Z, MotorTemperature, Cooling, Power, Vibration, Counter, MachineState – all **bound**
- AP5 leakage regression: **passed**
- Full test suite: **252 passed** (non-integration)

## Files changed (R2 scope)

- `Werkflow.OpcUaSimulator.Core/VirtualMachine/HmiSemantic*.cs`
- `Werkflow.OpcUaSimulator.Core/Interfaces/ISimulationEngine.cs`
- `Werkflow.OpcUaSimulator.Core/Services/SimulationEngine.cs`
- `Werkflow.OpcUaSimulator.App/VirtualMachine/**`
- `Werkflow.OpcUaSimulator.Tests/PhysicalAp6R2*.cs`

## Not changed (per spec)

Physics, fault scenarios, ground truth, OPC UA NodeIds, port 4840, MachineId, 309 signal definitions.
