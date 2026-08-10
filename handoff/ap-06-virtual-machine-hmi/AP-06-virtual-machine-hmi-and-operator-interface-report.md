# AP-06 – Virtual Machine HMI & Operator Interface

## 1. Ausgangsstand AP 5

- Basis-Commit (AP 5): `55c7837d93e0d85de9124906a3d39177c65a5e75`
- Tag: `opcua-simulator-ground-truth-evaluation-ap5-final-r4`

## 2. Virtual-Machine-Konzept

Maschine 1 ist zusätzlich die feste **Werkflow Virtual Laser 01** — eigenständige virtuelle Maschine für inMotion/VIGIL-Lernversuche über OPC UA, ohne direkte VIGIL-Anbindung.

## 3. Machine 1 / Port 4840

| Feld | Wert |
|------|------|
| MachineId | `a1111111-1111-4111-8111-111111111111` |
| Endpoint | `opc.tcp://localhost:4840` |
| Profil | `laser-processing-machine-300` |

## 4. Architektur

```text
Werkflow.OpcUaSimulator.App/VirtualMachine/
├── Models/
├── Services/
├── ViewModels/
└── Views/
Werkflow.OpcUaSimulator.Core/VirtualMachine/
├── VirtualMachineContract.cs
├── HmiSignalCatalog.cs
└── HmiSignalCoverageAnalyzer.cs
```

## 5. Runtime-Bindung

HMI (`VirtualMachineHmiViewModel`) nutzt `ISimulationEngine`, `IPhysicalSignalPublishingCoordinator`, `IMachineServerService`, `IFaultScenarioService` — ein Refresh-Timer (500 ms), keine 300 Einzeltimer.

## 6–16. HMI-Layout und Tabs

Industrial-dark Layout, Header + 11 Tabs (Übersicht, Achsen, Motoren, Temperaturen, Prozess, Kühlung, Leistung, Vibration, Produktion, Weitere Signale, Fehler/Diagnose). Signalgruppierung über `SignalCategory`.

## 17. Fault-Steuerung

Diagnose-Tab, Bereich **SIMULATION / TEST**, nur Laser-kompatible Szenarien.

## 18. Start/Stop/Pause/Resume/Reset

- Stop setzt Bereit (Idle), OPC UA bleibt online
- Pause setzt Pausiert
- Maschine beenden: Bestätigungsdialog, stoppt Server

## 19–20. X-/Tray- und Beenden-Verhalten

- X: `OnClosing` → Hide (Maschine läuft)
- Single Instance über `VirtualMachineWindowService`

## 21. OPC-UA-Vertrag

Keine Änderung an NodeIds/Endpoints für 4840. Keine Ground-Truth-Nodes durch HMI.

## 22. Signalabdeckung

309 physikalische Signale, 309 gemappt, 0 unmapped (`HmiSignalCoverageAnalyzer`).

## 23. GroundTruth-Leakage-Regression

AP-5-Leakage-Test weiterhin grün (0 Matches).

## 24. Fault-Smoke

`laser-overheating-axis-drive`: HMI-Fehler sichtbar, OPC UA online, Recovery sichtbar.

## 25. Performance

Ein gebündelter UI-Refresh; keine Simulationstakt-Änderung nachweisbar in Kurztests.

## 26. Build/Test

224 Tests bestanden (`Category!=Integration`).

## 27. VerificationRunId

`ap6-20260810201957-6974b063520346dea26acffaf2107`

## 28. Geänderte Dateien

Siehe `handoff/ap-06-virtual-machine-hmi/changed-source-files.txt`

## 29–31. Git Closure

| Feld | Wert |
|------|------|
| Commit-SHA | `44136aff84569f47baf84d9fc6095df882f3a369` |
| Tag | `opcua-simulator-virtual-machine-hmi-ap6-complete` |
| TagTarget-SHA | `44136aff84569f47baf84d9fc6095df882f3a369` |
| CommitEqualsTagTarget | true |
| Git-Status | AP-6-Dateien committed; fremde lokale Handoff-Änderungen unstaged |

## 32. AP6Passed

**true** — alle Abschlusskriterien erfüllt.

## 33. Freigabeempfehlung

**Freigabe für inMotion-Anbindung auf `opc.tcp://localhost:4840`.** Nächster Schritt: inMotion verbinden, VIGIL innerhalb von inMotion beobachten — keine direkte Simulator→VIGIL-Schnittstelle.
