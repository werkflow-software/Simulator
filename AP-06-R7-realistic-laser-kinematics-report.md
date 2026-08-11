# AP-06-R7 – Realistic Laser Kinematics Report

## Vorheriges Bewegungsproblem

X/Y-Positionen wurden als unabhängige Rauschwerte um feste Nominalwerte (~600 mm) erzeugt, ohne kinematische Trajektorie. `Process.FeedRate` blieb nahezu konstant (~950 mm/min), unabhängig von Rapid, Piercing oder Idle. Der Produktionszähler lief über Wall-Clock-Interval, nicht über abgeschlossene Bearbeitung.

## Maschinenarbeitsraum

| Parameter | Wert |
|-----------|------|
| MachineType | 2D Flatbed Fiber Laser |
| WorkingAreaX | 0 … 3000 mm |
| WorkingAreaY | 0 … 1500 mm |
| Safe cutting margin X | 50 … 2950 mm |
| Safe cutting margin Y | 50 … 1450 mm |

Z nutzt einen kleinen vertikalen Bereich (Fokus-/Kopfhöhe, nicht Flachbett-X/Y-Skala).

## Home / Park / Service Position

| Position | X | Y |
|----------|---|---|
| Home / Park | 30 mm | 50 mm |
| Nozzle Service | 100 mm | 80 mm |

Zentral in `VirtualMachineKinematicsConfig`.

## Toolpath-Modell

Interner synthetischer Werkzeugweg pro Teil (`LaserToolpathGenerator.CreatePartPlan`):

- `RapidMove` → `Pierce` → `CutLine` (Rechteck-Kontur, 4 Segmente)
- Deterministisch aus Job + Seed + PartIndex
- Kein CAD/DXF/Nesting

## Verwendete Prozessphasen

`LaserMotionPhase` (intern) wird auf bestehende `ProcessPhase` gemappt:

| Motion | ProcessPhase |
|--------|----------------|
| Idle | Idle |
| Setup, JobChange, NozzleChange | Setup |
| RapidPositioning, Repositioning, Piercing | RampUp |
| Cutting | Processing |

Zyklischer `ProcessPhaseScheduler` ist für die VM deaktiviert; Phasen folgen der Kinematik.

## Feed-Semantik

`Process.FeedRate` = **Schneidvorschub in mm/min** (nicht Rapid-Geschwindigkeit).

| Phase | FeedRate |
|-------|----------|
| Idle / Setup / JobChange / NozzleChange | 0 |
| Rapid | 0 (Bewegung über Axis Speed mm/s) |
| Piercing | 0 |
| Cutting | > 0, job-/materialabhängig |

## X / Y / Z Semantik

- **X/Y**: kinematische Kopfposition aus Toolpath + Beschleunigungsrampe
- **Z** (`Axis03.Position`): sichere Rapid-Position (~25 mm), Cut-Höhe (~2 mm + Dickenanteil), Service (~20 mm)
- **Focus** (`Process.FocusPosition`): abgeleitet von Z

## Piercing / Rapid / Cutting / JobChange

Typischer Ablauf: Cut complete → Rapid (Feed=0) → Pierce (stationär, Feed=0) → Cut (Feed>0) → …

Jobwechsel: Rapid zur Nozzle-Service-Position → optional Düsenwechsel (~12 s) → Setup-Pause (R6, 1–5 min sim) → neuer Toolpath.

## Jobabhängigkeit

Toolpath variiert mit CatalogIndex, Material, Dicke, FeedRateFactor; Part-Offset pro PartIndex.

## Physikkopplung

- `ProcessDemand` aus `LaserKinematicsEngine.GetMotionDemand` × `ProcessLoadFactor`
- `Friction` aus Achsgeschwindigkeit und Cutting-Phase
- Temperatur/Kühlung/Elektrik weiter über bestehende Hidden States und Signal-Dependencies

## Geänderte Dateien

**Neu:**

- `Werkflow.OpcUaSimulator.Core/VirtualMachine/VirtualMachineKinematicsConfig.cs`
- `Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Kinematics/*` (Engine, Generator, Models, Phase)
- `Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Models/LaserKinematicsState.cs`
- `Werkflow.OpcUaSimulator.Tests/LaserKinematicsPlausibilityTests.cs`

**Geändert:**

- `PhysicalSimulationContext.cs`, `PhysicalSimulationEngine.cs`
- `HiddenProcessStateEngine.cs`, `SignalCalculationEngine.cs`
- `SimulationEngine.cs`, `IPhysicalSignalPublishingCoordinator.cs`
- `PhysicalSignalPublishingCoordinator.cs`
- `PhysicalAp3R4SegmentTests.cs`

## Build

```
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

Ergebnis: **erfolgreich** (0 Fehler).

## Kurzer Plausibilitätscheck

`LaserKinematicsPlausibilityTests.VirtualMachine_Kinematics_UseWorkspaceAndPhaseFeed` – **bestanden**:

- Start nahe Home (X≈30, Y≈50)
- X/Y nutzen großen Arbeitsbereich (>300 mm Spanne)
- Feed > 0 während Cutting
- Phasen Rapid / Pierce / Cutting sichtbar

## Commit

`Implement realistic laser kinematics and process motion`

## Git-Status

Siehe aktueller `git status` nach Commit.

## Final functional verification by Product Owner

HMI-Beobachtung der Virtual Machine: X/Y/Z-Bewegung über Arbeitsraum, Service-Position beim Jobwechsel, Feed=0 in Idle/Pierce/Rapid, Counter nach Teilabschluss.
