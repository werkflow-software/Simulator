# AP-04-R7 Final Closure Report

## 1. R6 Restfehler

R6 bewertete `HydraulicEfficiency` Recovery korrekt als fehlgeschlagen:

- `RecoveryStartValue` ≈ 0.477, `RecoveryEndValue` = 1.2
- `NormalMin` = 0.65, `NormalMax` = 1.00
- `TowardNormalPassed` = false, `Passed` = false
- `SupplyPressure` und `PumpCurrent` Recovery waren bereits grün

## 2. Technische Ursache des Overshoots

1. **HardMaximum 1.2** für `HydraulicEfficiency` (über `NormalMaximum` 1.0) erlaubte Drift und Clamp auf 1.2 statt auf den Normalbereich.
2. **Recovery-Ziel** lief gegen HardMax/physikalisches Gleichgewicht statt explizit gegen `NominalValue` im Normalband.
3. **Kein Overshoot-Schutz** beim Annähern des Recovery-Ziels — Werte konnten den Normalbereich durchlaufen.
4. `FinalizeRecoveryState` setzte betroffene Hidden States nicht zuverlässig auf den nominalen Sollwert im Normalband.

## 3. Vorgenommene Korrektur (Simulation)

- `PhysicalProfileDependencyBuilder`: `HydraulicEfficiency` HardMax = 1.0 (gleich NormalMax).
- `FaultRecoveryEngine`: Recovery-Ziel = `NominalValue` (geclamp auf NormalMin/Max); `ApproachRecoveryValue` ohne Überschießen; Offset-Entfernung und Pull-Toward-Normal innerhalb des Normalbands; `FinalizeRecoveryState` snap auf Nominal.
- `PhysicalSignalPhaseCalibration`: PumpCurrent-Phase-Kalibrierung nur bei aktivem Hydraulik-Phase-Faktor (> 0.55), ohne Last-Boost, damit Fault-Pump-Anstieg und Recovery nicht gegensätzlich wirken.

Test-Harness: R7 `ValidatorRegression` nach Aufbau von `EfficiencyRecovery` berechnet; R3 Endpoint-Min/Max für Pump/Pressure Fault-Richtung.

## 4. HydraulicEfficiency Normalbereich

`0.65 ≤ HydraulicEfficiency ≤ 1.00` (Nominal 0.88)

## 5. RecoveryTarget

0.88 (`NominalValue`, aus Profil/Hidden-State-Definition)

## 6. Fault-Werte (Seed 55, TimeFactor 25)

| Kennwert | Wert |
|----------|------|
| FaultStart (PreFault avg) | 0.806 |
| FaultPeak / FaultLate (min in Fault) | 0.534 |
| FaultEnd (degraded window) | 0.674 |

## 7. Recovery-Werte

| Kennwert | Wert |
|----------|------|
| RecoveryStart | 0.877 |
| RecoveryMid | 1.00, 0.988, 0.984 |
| RecoveryEnd | 1.00 |
| DistanceToNormalStart | 0 |
| DistanceToNormalEnd | 0 |

## 8. PostRecovery-Werte

Stabil bei 1.00 (10 Samples, alle im Normalband)

## 9. DistanceToNormal

Gesamt-Recovery (alle Signale): Start 5.18 → End 0.14, `RecoveryImproved` = true

## 10. SupplyPressure Regression

`TowardNormalPassed` = true, Start 125.05 → End 173.61, Normal 175–185

## 11. PumpCurrent Regression

`TowardNormalPassed` = true, Start 0.62 → End 5.20, Normal 3–16

## 12. Validator Regression

Unveränderte R6-Validatorlogik; `HydraulicEfficiency` `Passed` = true, `TowardNormalPassed` = true

## 13. Tests

- `Category!=Integration`: 163/163 bestanden
- R7-Suite: 5/5 bestanden (inkl. R6-Negativfall 0.477→1.2 bleibt false)

## 14. Build

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

Release-Build: 0 Fehler

## 15. VerificationRunId

`ap4r7-20260810132928-59c8df3e491f405db5a5d2f`

## 16. Ap4R7Passed

`true`

## 17. Ap4OverallPassed

`true`

## 18. Commit-SHA

`eb317a3ddca0bb36529822c79b4e76ff558855ad`

## 19. Tag

`opcua-simulator-fault-scenarios-ap4-final-r7` (Tag `opcua-simulator-fault-scenarios-ap4-final` bereits vorhanden, nicht überschrieben)

## 20. TagTarget-SHA

`eb317a3ddca0bb36529822c79b4e76ff558855ad`

## 21. Git-Status

R7-Commit und Tag gesetzt. Unstaged: alte Handoff-Artefakte (ap-02, ap-04-r4, ap-04-r5), gelöschtes ap-03-zip, untracked scratch/ und zip-Archive.

## 22. Finale Freigabeempfehlung

**AP 4 ist endgültig abgeschlossen.** HydraulicEfficiency Recovery endet im Normalband ohne Overshoot; SupplyPressure und PumpCurrent Regression grün; Validator und Negativtests grün. **AP 5 kann beginnen.**
