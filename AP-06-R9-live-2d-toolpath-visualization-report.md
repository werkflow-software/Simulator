# AP-06-R9 – Live 2D Toolpath Visualization Report

## 1. Ausgangslage

Nach R8 zeigte die Virtual Machine HMI Achswerte, Prozessphasen und Bedienelemente, aber keine geometrische Schneidplanansicht. Die Kinematik erzeugte synthetische Rechteck-Toolpaths pro Produktionsteil ohne festen Blechlayout und ohne Kopplung zur Visualisierung.

## 2. CuttingPlan-Modell

Neues leichtgewichtiges Modell unter `PhysicalSimulation/CuttingPlans/`:

- `CuttingPlan` – PlanId, JobId, Blechabmessung 3000×1500 mm, Teileliste
- `CuttingPlanPart` – Index, Label, Konturen, Laufzeitstatus (`NotStarted` / `InProgress` / `Completed`)
- `CuttingPlanContour` – Vertices (geschlossene Polylinie), PiercePoint, Inner/Außen, Status (`Unprocessed` / `Active` / `Completed`)
- Hilfsgeometrie: Rechteck, L-Form, Rahmen

## 3. 20 Planübersicht

| Plan | Job | Teile auf Blech | Charakteristik |
|------|-----|-----------------|----------------|
| PLAN-001 | JOB-001 | 12 | kleine Halter, 2 Innenlöcher |
| PLAN-002 | JOB-002 | 6 | große Abdeckungen, Innenausschnitt |
| PLAN-003 | JOB-003 | 20 | kleine Flansche, Mittelloch |
| PLAN-004 | JOB-004 | 4 | große Gehäuseplatten, mehrere Innenkonturen |
| PLAN-005 | JOB-005 | 10 | L-Halter |
| PLAN-006 | JOB-006 | 8 | Rahmen |
| PLAN-007 | JOB-007 | 15 | gemischte Rechteckgrößen |
| PLAN-008 | JOB-008 | 3 | sehr große Platten |
| PLAN-009 | JOB-009 | 16 | kleine Streuteile |
| PLAN-010 | JOB-010 | 6 | U-Kanäle |
| PLAN-011 | JOB-011 | 9 | Mittelplatten, 1 Loch |
| PLAN-012 | JOB-012 | 5 | große Rahmen |
| PLAN-013 | JOB-013 | 14 | schmale Streifen |
| PLAN-014 | JOB-014 | 7 | Winkelhalter |
| PLAN-015 | JOB-015 | 11 | Montageplatten variabel |
| PLAN-016 | JOB-016 | 4 | XL-Abdeckungen |
| PLAN-017 | JOB-017 | 18 | Lochrasterplatten |
| PLAN-018 | JOB-018 | 6 | gemischte Großformen |
| PLAN-019 | JOB-019 | 13 | Mischlayout L/Rahmen/Rechteck |
| PLAN-020 | JOB-020 | 2 | sehr große Konsolen |

Alle Pläne deterministisch in `CuttingPlanCatalog.cs`.

## 4. Kopplung Plan → Toolpath → Kinematics

```
CuttingPlan (SheetPart)
  → CuttingPlanToolpathBuilder
  → LaserToolpathPlan (Rapid / Pierce / CutLine mit SheetPartIndex, ContourIndex)
  → LaserKinematicsEngine
  → Axis X/Y/Z, Feed, LaserPower, PartCounter
```

`LaserKinematicsEngine.LoadPartPlan` wählt Layout-Teil `PartIndex % PartCount`, baut Toolpath aus Plan-Geometrie. Keine zweite Simulation, keine Zufalls-Rechtecke mehr im Produktionspfad.

## 5. Kopfposition

HMI-Canvas liest `LaserKinematicsState.X/Y` direkt. Marker (Fadenkreuz + Kreis) wird alle 500 ms mit `PlanVisualToken` aktualisiert. Y-Achse im Canvas gespiegelt (Maschinenkoordinaten → Bildschirm).

## 6. Kontur-/Teilfortschritt

- Bei Pierce: Kontur `Active`, Teil `InProgress`
- Bei Konturabschluss: Kontur `Completed`
- Bei Sheet-Part-Ende: Teil `Completed`, `PendingPartCompletions` → Produktionszähler
- Sheet-Zyklus: nach vollem Blechdurchlauf Reset der Layout-States, Wiederholung bis `TargetQuantity`

## 7. HMI-Planansicht

`CuttingPlanCanvasControl` (WPF Canvas in Viewbox, 3000×1500):

- Blechumriss, Raster 500 mm / 100 mm
- Teilkonturen mit Zustandsfarben
- Rapid-Linie (gestrichelt) bei Rapid/Repositioning
- Pierce-Marker während Einstechen
- Zoom + / − / Fit

## 8. neue HMI-Struktur

Übersicht-Tab: links Achsen, **Mitte 2D-Plan** (zentral), rechts Bedienung. Plan-Header mit Plan/Job/Material, Fortschritt, Phasenbanner, Prozessindikatoren.

## 9. Farbkonzept

Planfläche hell (`#F8FAFC`), Konturen anthrazit/grün/türkis für aktiv/fertig. Keine großflächigen blauen Panels; Akzentfarbe nur dezent für aktive Kontur und Section-Titles.

## 10. geänderte Dateien

**Core – CuttingPlans**

- `CuttingPlanModels.cs`
- `CuttingPlanGeometry.cs`
- `CuttingPlanCatalog.cs`
- `CuttingPlanToolpathBuilder.cs`

**Core – Kinematik**

- `LaserToolpathModels.cs` (Segment-Metadaten)
- `LaserToolpathGenerator.cs` (`RequiresNozzleChange` public)
- `LaserKinematicsEngine.cs` (Plan-Laden, Konturtracking)
- `LaserKinematicsState.cs` (ActiveCuttingPlan, DisplayCuttingPlan, SheetPartIndex)
- `PhysicalJobState.cs` (`CatalogIndex`)
- `PhysicalJobCoordinator.cs`

**App – HMI**

- `CuttingPlanViewModel.cs`
- `CuttingPlanCanvasControl.cs`
- `VirtualMachineHmiViewModel.cs`
- `VirtualMachineHmiWindow.cs`
- `HmiVisualTheme.cs`

**Tests**

- `LaserKinematicsPlausibilityTests.cs` (Init-Reihenfolge, PLAN-003, simultane XY-Bewegung)

## 11. Build

```
dotnet build Werkflow.OpcUaSimulator.sln -c Release
→ 0 Fehler
```

`LaserKinematicsPlausibilityTests` → bestanden.

## 12. Commit

Message: `Add live 2D cutting plan visualization and HMI layout refinement`

## 13. Git-Status

Siehe Handoff `git-closure.txt` nach Commit.

## 14. Final functional/UI verification by Product Owner

Ausstehend – PO prüft live: Kopfmarker, Rapid/Pierce/Cutting, Teil-/Konturfortschritt, Jobwechsel, Layout-Referenz.
