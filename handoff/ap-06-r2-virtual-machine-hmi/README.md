# AP-06-R2 – Virtual Machine HMI Functional Binding + UI Redesign

## Status: PASSED

Functional runtime binding and industrial HMI layout for Werkflow Virtual Laser 01 (Machine 1, port 4840).

## Deliverables

| Artifact | Path |
|----------|------|
| Verification JSON | `AP-06-R2-virtual-machine-hmi-functional-ui-verification.json` |
| R2 report | `AP-06-R2-virtual-machine-hmi-report.md` |
| Build/test evidence | `build-test-evidence.md` |

## Summary

- **Runtime binding:** `HmiSemantic` registry + resolver maps stable semantics to laser profile signals; ViewModel refreshes live from `PhysicalMachineSession` and `MachineRuntimeState`.
- **Job/Part fix:** `AssignJobIfMissingAsync` on machine/production start; physical generation mode enabled on start.
- **UI:** Header + left controls + large center metrics + right diagnostics/simulation panel + bottom tab navigation (Übersicht, Achsen, Antriebe, …, Weitere Signale).
- **Tests:** 20 new AP6R2 tests; full suite 252 passed (`Category!=Integration`).

## Git

- Commit: `Rebuild virtual machine HMI with live runtime binding`
- Tag: `opcua-simulator-virtual-machine-hmi-ap6-r2`

## Manual UI smoke

WPF application starts successfully (`dotnet run` on Release build). Verify visually: Job/Part, positions, live metric tiles, tab data, command enablement.
