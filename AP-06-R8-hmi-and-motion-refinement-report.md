# AP-06-R8 – HMI and Motion Refinement Report

## Kinematik-Anpassungen

- **Simultane X/Y-Interpolation** über vektorielle `MoveTowardPoint` (Vx/Vy proportional zur Bahnlänge)
- **Z parallel zu XY** via `MoveTowardPointWithZ` / `MoveZ` mit eigener Vz-Rampe (Rapid, Service, Pierce, Cut)
- **Ecken-/Feature-Drosselung**: `IsCornerEntry`, Ramp über erste 45 mm, kurze Segmente langsamer
- **Diagonale Cut-Segmente** in größeren Teilen für gleichzeitige X/Y-Bewegung beim Schneiden
- **Laserleistung phasenabhängig**: Pierce/Cutting vs. Rapid/Idle über `Process.LaserPowerActual` / `PowerDemand`
- **Bahngeschwindigkeit** intern als `PathSpeedMmPerS`, getrennt von `Process.FeedRate` (mm/min Schneidvorschub)

## Simultane Achsbewegung

| Phase | X/Y | Z |
|-------|-----|---|
| Rapid / Reposition / Service | simultan interpoliert | parallel |
| Cutting | Kontur (inkl. Diagonalen) | Arbeitshöhe |
| Pierce | stationär | absenken |
| Setup / Idle | 0 | Service / Hold |

## Process-State-Darstellung (HMI)

- Prominente Phase: **LEERLAUF**, **POSITIONIEREN**, **EINSTECHEN**, **SCHNEIDEN**, **JOBWECHSEL**, **DÜSENWECHSEL**, …
- Indikatoren: Laser aktiv / Schnitt aktiv / Positionierung aktiv (JA/NEIN)
- Statusbalken im Header (grün/gelb/grau/rot je Phase)
- Nächste Aktion, Bahngeschwindigkeit, Vx/Vy, Fokus

## HMI-Layout

- **Links**: Achswerte X/Y/Z (Inset-Darstellung), Geschwindigkeiten, Vorschub
- **Mitte**: Phasen-Banner, Prozessstatus, Produktion, Kennwerte
- **Rechts**: Bedienbuttons (Start/Stop/Pause/Resume/Reset/Job/Normalbetrieb), Simulation, Diagnose
- **Footer-Tabs**: Übersicht, Achsen, Prozess, Produktion, Temperaturen, Kühlung, Elektrik, Vibration, Fehler/Diagnose, Weitere

## Farbkonzept

- Hellgrau / Silber-Basis statt dunkelblauem Dashboard
- Dunkle Rahmen, türkise Akzentlinien in Section-Titles
- Status: Grün = Running/Cutting, Gelb = Setup, Rot = Error, Grau = Idle
- Pill-Buttons (oval) im industriellen Stil

## Feed-Semantik

`Process.FeedRate` = Schneidvorschub mm/min → nur bei **SCHNEIDEN** > 0. Rapid über Axis Speed + Bahngeschwindigkeit.

## Geänderte Dateien

**Kinematik:** `LaserKinematicsEngine.cs`, `LaserKinematicsState.cs`, `LaserToolpathModels.cs`, `LaserToolpathGenerator.cs`, `LaserMotionPhaseLabels.cs`

**HMI:** `HmiVisualTheme.cs`, `VirtualMachineHmiViewModel.cs`, `VirtualMachineHmiWindow.cs`

**Registry:** `HmiSemanticRegistry.cs` (FeedRate ohne Axis-Speed-Fallback)

**Tests:** `LaserKinematicsPlausibilityTests.cs`

## Build

```
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

Ergebnis: **erfolgreich** (0 Fehler).

## Plausibilitätscheck

`LaserKinematicsPlausibilityTests` — bestanden (inkl. simultane Vx/Vy > 0).

## Commit

`Refine laser motion and redesign HMI for clearer process states`

## Final functional verification by Product Owner

PO prüft in der HMI: Phase groß sichtbar, Farben weniger blau, X/Y gleichzeitig bei Diagonal-Rapid/Cut, Feed nur beim Schneiden > 0.
