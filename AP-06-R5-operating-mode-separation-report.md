# AP-06-R5 – Classic Simulator / Virtual Machine Operating Mode Separation

**Date:** 2026-08-11  
**Status:** Implemented – pending Product Owner mode-switch verification

---

## 1. Ausgangsstand

Nach AP-06-R4 (Production Job Pool Binding) startete die App direkt mit `MainWindow` und erlaubte paralleles VM-HMI über Tray/Button. Kein expliziter Betriebsmodus, keine Session-Trennung.

## 2. Ziel

Nur **ein** Betriebsmodus pro Session: entweder Classic Simulator (4 Maschinen) oder Virtual Machine (ein Laser-HMI). Kein verstecktes `MainWindow` im VM-Modus.

## 3. ApplicationOperatingMode

```csharp
ApplicationOperatingMode.ClassicSimulator
ApplicationOperatingMode.VirtualMachine
```

Steuert Runtime-Start, Hauptfenster und Tray — **keine** Physik-/Signal-Logik.

## 4. Startup-Selektor

`OperatingModeSelectorWindow` beim Start:

| Option | Beschreibung |
|--------|--------------|
| **SIMULATOR** | 4 Maschinen – technisches Testumfeld |
| **VIRTUAL MACHINE** | Werkflow Virtual Laser 01 – realistisches HMI |
| **Beenden** | App beenden |

Kein weiteres Fenster im Hintergrund.

## 5. Classic-Modus

- `MainWindow` (bestehendes Dashboard)
- Maschinen 1–4, Ports 4840–4843
- Tray: **Simulator öffnen**, **Beenden**
- Kein VM-HMI-Einstieg (Button entfernt)

## 6. Virtual-Machine-Modus

- **Nur** `VirtualMachineHmiWindow` — `MainWindow` wird nicht erstellt
- Eine Maschine: Werkflow Virtual Laser 01, `opc.tcp://localhost:4840`, `laser-processing-machine-300`
- Keine Runtimes für Maschinen 2/3/4
- Tray: **Virtuelle Maschine öffnen**, **Maschine beenden**, **Anwendung beenden**
- Fenster-X: HMI → Tray, Maschine läuft weiter

## 7. Session-Isolation (ConfigurationService)

`InitializeAsync(ApplicationOperatingMode)`:

- **Classic:** volle `machines.json` / Default 4 Maschinen
- **VM:** `ApplyVirtualMachineMachineFilter()` — nur `VirtualMachineContract.MachineId`, Port 4840

## 8. ApplicationSessionCoordinator

- `StartClassicSimulatorAsync()` / `StartVirtualMachineAsync()`
- `EndSessionAndReturnToSelectorAsync()` — Stop Simulation, Fault-Reset, Tray dispose, Selector anzeigen
- VM **Maschine beenden** → Bestätigung → Stop → Selector

## 9. Tray-Verhalten

Modusabhängiges Menü via `IApplicationSessionContext`. VM ohne „Simulator öffnen“; Classic ohne VM-Einträge.

## 10. R4-Abhängigkeiten

VM-Modus nutzt weiterhin R4-Standalone-Pfade (`ShouldTickProduction`, `IsJobUnassigned`, `EnsureJobPoolReady`) — keine Abhängigkeit vom Classic-Dashboard.

## 11. HMI-Ergänzungen (minimal)

| Bereich | Inhalt |
|---------|--------|
| **Aufträge** | Nächster Auftrag, Pool-Status, „Auftrag wechseln“ (`ChangeJobAsync`) |
| **Fault** | SIMULATION/TEST + Intensität/Zeitfaktor-Slider |
| **Simulation** | Zeitfaktor 1x/2x/5x/10x, Seed, Prod.-Geschwindigkeit |
| **Reset** | „Neuen Maschinenlauf starten“ |
| **Ausgeschlossen** | Ground Truth, HiddenStates, DetectableAt |

## 12. Moduswechsel

Kein Live-Switch. Session beenden → Selector → anderer Modus. Port 4840 von beiden Modi nutzbar (nur eine Session aktiv).

## 13. Geänderte Dateien

Siehe `handoff/ap-06-r5-operating-mode-separation/changed-files.txt`

**Nicht geändert:** Physik, Faults, Ground Truth, 309 Signale, NodeIds.

## 14. Build

```
dotnet build Werkflow.OpcUaSimulator.sln -c Release
→ 0 Fehler
```

Siehe `handoff/ap-06-r5-operating-mode-separation/build-proof.txt`

## 15. Plausibilität (kurz)

| Check | Ergebnis |
|-------|----------|
| Start nur Selector | ✓ `App.OnStartup` → `ShowModeSelector` |
| VM ohne MainWindow | ✓ nur `GetRequiredService<MainWindow>` in Classic |
| VM eine Maschine | ✓ `ApplyVirtualMachineMachineFilter` |
| Tray VM reduziert | ✓ drei Einträge |
| Maschine beenden → Selector | ✓ `EndSessionAndReturnToSelectorAsync` |
| Classic Tray | ✓ Simulator öffnen + Beenden |

## 16. Commit

Message: `Separate classic simulator and virtual machine operating modes`  
Siehe `handoff/ap-06-r5-operating-mode-separation/git-closure.md`

## 17. Product Owner Verification

1. App starten → nur Modus-Selector sichtbar  
2. **SIMULATOR** → MainWindow, 4 Maschinen in Konfiguration  
3. Beenden → neu starten → **VIRTUAL MACHINE** → nur HMI, kein Dashboard  
4. Maschine starten, Produktion, Auftrag wechseln, Fault-Test  
5. **Maschine beenden** → zurück zum Selector  
6. **VIRTUAL MACHINE** erneut → Port 4840 erreichbar

## 18–34. Abschlusskriterien

| # | Kriterium | Status |
|---|-----------|--------|
| 18 | Modus-Enum | ✓ |
| 19 | Startup-Selector | ✓ |
| 20 | Classic MainWindow | ✓ |
| 21 | VM nur HMI | ✓ |
| 22 | Session-Isolation | ✓ |
| 23 | Tray modusabhängig | ✓ |
| 24 | Kein Live-Switch | ✓ |
| 25 | Port 4840 | ✓ |
| 26 | HMI Jobs | ✓ |
| 27 | HMI Fault | ✓ |
| 28 | HMI Simulation Settings | ✓ |
| 29 | HMI Reset | ✓ |
| 30 | Kein Ground Truth in HMI | ✓ |
| 31 | Build Release | ✓ |
| 32 | R4 standalone VM | ✓ |
| 33 | Commit R5-only | ✓ |
| 34 | AP6R5Passed | **true** (technisch) — PO UI-Verifikation offen |
