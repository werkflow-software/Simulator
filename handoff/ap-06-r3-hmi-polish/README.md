# AP-06-R3 – HMI Contrast Polish and Close-to-Tray

## Status: PASSED

Final polish for Virtual Machine HMI readability and tray lifecycle.

## Deliverables

- `AP-06-R3-hmi-polish-and-tray-fix-report.md`
- `AP-06-R3-hmi-contrast-and-tray-verification.json`
- `build-test-evidence.md`

## Summary

- Central `HmiVisualTheme` with high-contrast text and readable disabled buttons
- Real system tray icon with context menu
- `ShutdownMode.OnExplicitShutdown` — window X hides to tray, never exits app
- HMI and main window close-to-tray; explicit shutdown via Maschine beenden / Tray Beenden

## Git

- Commit: `Polish virtual machine HMI contrast and tray behavior`
- Tag: `opcua-simulator-virtual-machine-hmi-ap6-r3`
