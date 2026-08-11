# AP-06-R10 – Manual Job Selection, Runtime Controls & Remaining-Time Display

## 1. Ziel

Ergänzung der Virtual Machine HMI um manuelle Jobauswahl aus dem 20-Job-Pool, korrekte Stop/Pause/Resume-Semantik im laufenden Betrieb und Restzeit-Anzeigen auf der Hauptseite — ohne Redesign der Simulatorarchitektur.

## 2. Manuelle Jobauswahl

- Neuer Dialog `JobSelectionWindow` mit allen 20 Jobs aus `FixedSimulationCatalog`
- Anzeige pro Eintrag: JobName, PartName, Sollmenge, Material, Dicke
- Buttons **Laden** / **Abbrechen**
- HMI-Button **Auftrag wählen** neben **Nächsten Job laden**
- `SimulationEngine.SelectJobAsync(machineId, catalogIndex)` — gleicher Jobwechselpfad wie `ChangeJobAsync`, aber mit festem Index
- Bei laufender Produktion: `AbortProductionForJobChange` → Serviceposition → Setup/JobChange → neuer Job bei 0

## 3. Stop-Semantik

Stop ≠ Pause, Stop ≠ Maschine beenden.

- `SimulationEngine.StopProductionAsync` → `PhysicalSignalPublishingCoordinator.StopProduction`
- `LaserKinematicsEngine.StopAndResetProduction`: Toolpath/Plan zurücksetzen, Kopf idle, Laser sicher
- `ActualCounter = 0`, Planfortschritt vollständig unbearbeitet
- Job bleibt geladen, OPC-UA-Server bleibt online
- Publisher wird bei Stop aus Pause-Zustand wieder aktiviert (Ticks für Signale)
- Maschinenstatus: `Idle`, `IsProducing = false`

## 4. Pause-Semantik

- `PauseProductionAsync` friert Kinematik ein (`OnProductionPaused`: Feed/Laser = 0, Position bleibt)
- Publisher pausiert (kein Physik-Tick während Pause)
- Segment-/Teil-/Zählerfortschritt bleibt erhalten
- Restzeiten und Laufzeit werden eingefroren (`FrozenPartRemainingSeconds`, `FrozenJobRemainingSeconds`, `FrozenProductionElapsedSeconds`)

## 5. Resume-Semantik

- `ResumeProductionAsync` / Start im Zustand Paused setzt `IsProductionMotionActive = true`
- Fortsetzung exakt am gespeicherten Segment und Segmentfortschritt
- `OnProductionResumed` ohne Toolpath-Reload

## 6. CanExecute-Korrektur

| Zustand | Start | Stop | Pause | Resume |
|---------|-------|------|-------|--------|
| Idle (bereit) | ✓ | ✗ | ✗ | ✗ |
| Running | ✗ | ✓ | ✓ | ✗ |
| Paused | ✓ | ✓ | ✗ | ✓ |
| Jobwechsel | ✗ | — | — | — |

HMI `StopProductionAsync` ruft nicht mehr Pause + Idle, sondern `StopProductionAsync` auf dem Engine.

## 7. Teilzeitberechnung

`LaserToolpathTimeEstimator.EstimateRemainingPartSeconds`:

- verbleibende Rapid-, Pierce- und Cut-Segmente des aktuellen Toolpaths
- aktuelle Segmentposition (`DistanceAlongSegmentMm`)
- Feed-/Speed-Parameter aus Jobdefinition

## 8. Jobzeitberechnung

`LaserToolpathTimeEstimator.EstimateRemainingJobSeconds`:

- Restzeit aktuelles Teil
- noch nicht bearbeitete Teile (Layout-Zyklen + verbleibende Sollmenge)
- reine Produktionszeit bis Job fertig (ohne JobChange nach Abschluss)

## 9. Setup-/Jobwechselzeit

`LaserToolpathTimeEstimator.EstimateSetupRemainingSeconds` nutzt bestehenden 1–5-Minuten-Simulationstimer (`JobChangePauseUntil` / `OverrideSetupDuration`).

Düsenwechsel: `GetNozzleChangeRemainingSeconds` aus `NozzleChangeElapsedSeconds`.

## 10. HMI-Anzeige (ZEITEN-Block)

Übersicht, Mitte, nahe Planfortschritt:

| Feld | Binding |
|------|---------|
| Teil | `PartRemainingText` |
| Auftrag | `JobRemainingText` |
| Einrichten | `SetupRemainingText` |
| Düsenwechsel | `NozzleRemainingText` |
| Laufzeit | `JobElapsedText` |

Phasenabhängig: während JobChange Setup-Zeit aktiv, Teil/Auftrag = —; während Pause eingefroren; nach Stop volle Job-/Teilzeit.

Angezeigte Zeiten = Simulations-/Maschinenzeit (Schätzwerte).

## 11. Motion-Gate

`PhysicalSimulationContext.IsProductionMotionActive` verhindert Toolpath-Fortschritt ohne expliziten Start/Resume. Jobzuweisung für VM setzt `Idle` + Motion inactive; Start/Resume nach Jobwechsel aktiviert Motion.

## 12. Geänderte Dateien

| Datei | Änderung |
|-------|----------|
| `LaserToolpathTimeEstimator.cs` | Neu: Teil-/Job-/Setup-Restzeitschätzung |
| `LaserKinematicsEngine.cs` | Stop/Abort/Pause-Gates, Reset |
| `PhysicalSimulationContext.cs` | Motion/Pause/Elapsed-Frozen-Felder |
| `SimulationEngine.cs` | Stop/Pause/Resume/SelectJob, VM-Idle bei Zuweisung |
| `ISimulationEngine.cs` | Interface-Erweiterung |
| `PhysicalSignalPublishingCoordinator.cs` | Runtime-Controls, Zeitschätzungen |
| `IPhysicalSignalPublishingCoordinator.cs` | Interface-Erweiterung |
| `VirtualMachineHmiViewModel.cs` | Commands, CanExecute, Zeiten |
| `VirtualMachineHmiWindow.cs` | ZEITEN-Block, Auftrag wählen |
| `JobSelectionWindow.cs` | Neu: Jobauswahl-Dialog |

## 13. Build

```
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

Ergebnis: **Build erfolgreich** (Release).

## 14. Kurzer technischer Plausibilitätscheck

| Prüfpunkt | Ergebnis |
|-----------|----------|
| Job wählen zeigt 20 Jobs | Dialog listet `FixedSimulationCatalog` |
| Beliebiger Job ladbar | `SelectJobAsync(catalogIndex)` |
| Running → Pause stoppt Toolpath | Publisher pausiert, Motion gate |
| Resume gleiche Stelle | SegmentIndex + Distance preserved |
| Running → Stop → Counter 0 | `StopAndResetProduction` |
| Start nach Stop von Anfang | Motion active, Plan reset |
| OPC UA bei Stop online | Kein `StopMachineServerAsync` |
| Teil-/Job-Restzeit angezeigt | ZEITEN-Block |
| Setup-Restzeit bei JobChange | `SetupRemainingText` |
| Build grün | Release OK |

## 15. Commit

Message: `Add manual job selection runtime controls and remaining time display`

## 16. Git-Status

Commit enthält nur R10-relevante Quelländerungen und diesen Report.

## 17. Final functional verification

**Final functional verification by Product Owner** (ausstehend).
