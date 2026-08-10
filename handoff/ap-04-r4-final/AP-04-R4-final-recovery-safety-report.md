# AP-04-R4 Final Recovery Safety Report

VerificationRunId: `ap4r4-20260810095508-d6c70dae5b134ce5b99fd3e`

Ap4R4Passed: **true**  
Ap4OverallPassed: **true**

## 1. R3-Befund

R3 meldete `Passed=true`, während `MotorTemperature` bei RecoveryCompleted und PostRecovery weiterhin ~75–82°C lag (FaultThreshold 70°C). `ErrorActive=false`, `MachineState=Running`, `ProductionRunning=true` — fachlich unzulässig.

## 2. Laser Safe-Recovery-Regel

- FaultThreshold: **70°C** (`Axis01.MotorTemperature >= 70`)
- SafeRecoveryThreshold: **65°C** (Hysterese, `lessThan`)
- SafeRecoveryTolerance: **2.5°C**
- Recovery wird nur abgeschlossen, wenn Signal unter SafeRecoveryThreshold bleibt und `MinimumStableDuration` erfüllt ist.

## 3. MinimumStableDuration

- Laser / Hydraulic: **45s** Simulationszeit (stabiler Safe-State-Timer, Reset bei erneutem Überschreiten)

## 4. PostRecovery-Sicherheit

- Mindestens **6** PostRecovery-Samples
- Kein Sample `>= FaultThreshold` (Laser: 70°C)
- PostRecovery-Werte innerhalb SafeRecoveryThreshold + Toleranz
- Post-Recovery-Phase: **Idle** (kein erneutes Aufheizen während Stabilisierung)

## 5. Laser Fault DirectionChecks

| Signal | Richtung | Passed |
|--------|----------|--------|
| MotorTemperature | increase | true |
| MotorCurrent | increase | true (thermisch gekoppelt) |
| AxisSpeed | decrease | true |

## 6. Laser Recovery DirectionChecks

| Signal | Richtung | Passed |
|--------|----------|--------|
| MotorTemperature | decrease | true |
| MotorCurrent | toward-normal | true |
| AxisSpeed | increase | true |

## 7. Hydraulic Fault DirectionChecks

| Signal | Richtung | Passed |
|--------|----------|--------|
| HydraulicEfficiency | decrease | true |
| SupplyPressure | decrease | true |
| PumpCurrent | increase | true (sekundär, gekoppelt) |

## 8. Hydraulic Recovery DirectionChecks

| Signal | Richtung | Passed |
|--------|----------|--------|
| HydraulicEfficiency | increase | true |
| SupplyPressure | increase | true |
| PumpCurrent | decrease / toward-normal | true |

## 9. DistanceToNormal

Hydraulic Recovery: aggregierte Distanz zu Normalzielen (Efficiency 1.0, SupplyPressure 150, PumpCurrent 8.0). Verbesserung nachgewiesen über Recovery- vs. PostRecovery-Fenster.

## 10. Sensor Drift Korrektur

- `sensor-drift.json`: `additiveDrift` auf `Axis01.MotorTemperature` (kein `signalFreeze`)
- `SensorOffsets` in `FaultScenarioEngine` + Recovery-Skalierung
- Nachweis: **80** Samples, **60** DistinctValues, BiasDelta **~18.7°C**, HiddenDelta **~0.25°C**

## 11. Abgrenzung zu SignalFreeze

- `signal-freeze` Szenario unverändert (MotorCurrent freeze auf Technical-Learning-Maschine)
- SensorDrift: DistinctValues >> 1, messbare Bias-Entwicklung
- SignalFreeze: DistinctValues = 1 (automatisierter Test)

## 12. Validator-Korrektur

`Ap4R4EvidenceValidator` prüft: Recovery-Timeline, SafeRecovery, PostRecovery, getrennte Fault/Recovery DirectionChecks, SensorDrift, DistanceToNormal, rekursive Passed-Propagation.

## 13. Negative Validator-Tests

11 negative Fälle in `PhysicalAp4R4EvidenceTests` (Safe-Threshold, PostRecovery, MinimumStableDuration, DirectionCheck, DistanceToNormal, SensorDrift vs Freeze).

## 14. R4-Verifikationslauf

- A: Laser Overheating (Fault → Recovery → SafeStable → PostRecovery)
- B: Hydraulic Leak (Fault + Recovery)
- C: Sensor Drift Modelltest
- Laufzeit: **< 2 Minuten** real (beschleunigte Ticks, TimeFactor 25)

## 15. VerificationRunId

`ap4r4-20260810095508-d6c70dae5b134ce5b99fd3e`

## 16. Build/Test

| Suite | Passed | Failed | Total |
|-------|--------|--------|-------|
| Category!=Integration | 149 | 0 | 149 |
| PhysicalAp4R4 | 12 | 0 | 12 |

Build Release: **0 Errors**, 455 Warnings (pre-existing App warnings)

## 17. Commit

`223675a00312809b66bfa46da343b01c8638ccdb` — Finalize AP 4 recovery safety and sensor drift

## 18. Tag

`opcua-simulator-fault-scenarios-ap4-verified-final-r4` (bestehendes Tag `opcua-simulator-fault-scenarios-ap4-verified-final` nicht überschrieben)

## 19. Tag-Zielcommit

`223675a00312809b66bfa46da343b01c8638ccdb`

## 20. Git-Status

Branch `feature/physical-learning-simulator`. R4-Commit und Tag gesetzt. Nicht-R4-Artefakte (smoke-test, scratch) nicht Teil des Commits.

## 21. Ap4R4Passed / Ap4OverallPassed

**true / true**

## 22. Finale Freigabeempfehlung

**AP 4 final schließen.** Recovery-Sicherheit, Sensor-Drift, Validator und Evidence sind widerspruchsfrei. Keine weitere AP-4-Runde erforderlich.
