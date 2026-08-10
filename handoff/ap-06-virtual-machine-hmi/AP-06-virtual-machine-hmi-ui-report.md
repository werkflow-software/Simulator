# AP-06 Virtual Machine HMI – UI Report

## Fensterstruktur

- **VirtualMachineHmiWindow** – separates WPF-Fenster (1600×900, Min. 1280×720)
- **Header:** Maschinenname, Status, Job/Teil, Zähler, OPC-UA-Status, Uhrzeit, Vollbild, Maschine beenden
- **Tab-Navigation:** Übersicht + 9 Signal-Tabs + Fehler/Diagnose

## Tabs

| Tab | Inhalt |
|-----|--------|
| Übersicht | Maschinenbedienung, Hauptwerte, TEST SCENARIO ACTIVE |
| Achsen | Achsenpanels (Axis01–Axis06) |
| Motoren | Drive-Signale |
| Temperaturen | Thermal-Signale |
| Prozess | Process-Signale |
| Kühlung | Cooling-Signale |
| Leistung | Electrical-Signale |
| Vibration | Vibration-Signale |
| Produktion | Production-Signale |
| Weitere Signale | Pneumatic, Hydraulic, Quality, Optical, Safety, Environment, Diagnostic, Auxiliary |
| Fehler / Diagnose | Maschinenmeldungen + SIMULATION / TEST |

## Signalgruppierung

Signale werden über `HmiSignalCatalog` und `SignalCategory` aus dem Profil gruppiert — keine NodeId-Navigation in der Bedienansicht.

## Maschinensteuerung

- Start / Stop (Bereit, Server bleibt online) / Pause / Resume / Reset / Normalbetrieb
- Maschine starten (Server) / Maschine beenden (mit Bestätigungsdialog)

## Fault-Steuerung

Bereich **SIMULATION / TEST** im Diagnose-Tab: Laser-kompatible Szenarien, Start/Pause/Resume/Stop, Intensität/TimeFactor über ViewModel.

## Tray / Fenster

- X auf HMI: `OnClosing` cancel → Hide (Maschine läuft weiter)
- `VirtualMachineWindowService`: Single Instance, ShowOrFocus
- Tray: `SimulatorTrayService` (HMI-Wiederöffnung über Hauptfenster)

## Beenden

Expliziter Button **Maschine beenden** mit Sicherheitsabfrage; stoppt Production, Fault Runtime und OPC-UA-Server.
