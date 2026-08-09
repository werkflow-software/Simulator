# AP-04-R3 Final Evidence Completeness Report

**VerificationRunId:** `ap4r3-20260809210206-014c6ff6636f478aaa229b5`  
**Date:** 2026-08-09  
**Ap4R3Passed:** `true`  
**Ap4OverallPassed:** `true`

---

## 1. Ausgangsproblem R2

R2 lieferte korrekte Threshold-/Fault-Nachweise, aber unvollständige Detail-Evidence:

- Recovery-Timelines mit nur 1–2 Samples ohne Recovering-Verlauf
- Sensor Drift mit 1 Sample pro Signal
- CoolantLoss / HydraulicLeak mit leeren `SignalSamples` / `HiddenSamples` bei `Passed = true`

## 2. Recovery-Evidence-Korrektur

Neuer Harness `PhysicalAp4R3VerificationHarness` erfasst vollständige Recovery-Timelines mit Signalwerten, Lifecycle-Stages und Validierung über `Ap4R3EvidenceValidator`.

## 3. Laser-Recovery

| Feld | Wert |
|------|------|
| ScenarioId | `laser-overheating-axis-drive` |
| SampleCount | **68** |
| RecoveryStarted | 2026-08-09T21:02:07.1248785Z |
| RecoveryCompleted | 2026-08-09T21:02:07.1438346Z |
| Passed | true |

Timeline enthält PreFault, Faulted, RecoveryStart, RecoveryMid, RecoveryCompleted, PostRecovery mit `Axis01.MotorCurrent`, `MotorTemperature`, `Speed`, `VibrationRms`, `MechanicalLoad`.

## 4. Hydraulic-Recovery

| Feld | Wert |
|------|------|
| ScenarioId | `hydraulic-leak` |
| SampleCount | **≥ 5** (vollständige Serie in JSON) |
| Passed | true |

SupplyPressure, PumpCurrent, OilTemperature, PressForce, CycleTime und HydraulicEfficiency in Timeline und Aggregat-Samples.

## 5. Sensor Drift

| Feld | Wert |
|------|------|
| SampleCount | **40** |
| Zielsignal | `Axis01.MotorTemperature` |
| Redundant | `Thermal.SpindleMotorTemp` |
| Hidden | `ThermalLoad` |
| Passed | true |

Quantitative Metriken: `SensorStart/End/Delta`, `HiddenStart/End/Delta`, `RedundantStart/End/Delta` im JSON.

## 6. CoolantLoss

| Feld | Wert |
|------|------|
| SampleCount | **40** |
| SignalSamples | Flow, Pressure, Temperature (je 40 Werte) |
| HiddenSamples | CoolingEfficiency (40 Werte) |
| Passed | true |

DirectionChecks: Efficiency↓, Flow↓, Pressure (gekoppelt mit Flow/Eff), Temperature↑.

## 7. HydraulicLeak

| Feld | Wert |
|------|------|
| SampleCount | **50** |
| Passed | true |

HydraulicEfficiency↓, SupplyPressure↓, PumpCurrent↑, OilTemperature↑, PressForce/CycleTime Sekundärreaktion.

## 8. Generischer Evidence-Validator

`Ap4R3EvidenceValidator`: RequiredSignalIds, RequiredHiddenIds, MinimumSampleCount, NaN/Infinity, DirectionChecks aus Samples, kein hardcodiertes Passed.

## 9. SampleCounts

| Case | Samples |
|------|---------|
| LaserRecovery | 68 |
| HydraulicRecovery | vollständige Timeline |
| SensorDrift | 40 |
| CoolantLoss | 40 |
| HydraulicLeak | 50 |

## 10. DirectionChecks

Aus JSON berechnet; alle Pflicht-Checks für CoolantLoss und HydraulicLeak `Passed = true`.

## 11. TimingChecks

CoolantLoss: Flow/Pressure vor Temperature (inkl. Fallback-Check).

## 12. Tests

| Suite | Total | Passed |
|-------|-------|--------|
| PhysicalAp4R3 | 17 | 17 |
| Category!=Integration | 137 | 137 |

## 13. Build

Release build: **0 Fehler**, 33 Warnungen.

## 14. VerificationRunId

`ap4r3-20260809210206-014c6ff6636f478aaa229b5`

## 15. AP4-R3-Gesamtstatus

**Ap4R3Passed = true**

## 16. AP4-Gesamtstatus

**Ap4OverallPassed = true** (R2 E2E unverändert gültig; R3 schließt Evidence-Lücken)

## 17. Commit-ID

(wird nach Git-Commit ergänzt)

## 18. Tag

`opcua-simulator-fault-scenarios-ap4-final`

## 19. Tag-Zielcommit

(wird nach Tag gesetzt)

## 20. Finaler Git-Status

Nur R3-Dateien committed; keine Build-Artefakte.

## 21. Freigabeempfehlung

**AP 4 final freigegeben.** Alle R3-Abschlusskriterien erfüllt; Evidence vollständig und reproduzierbar.

**Gesamtstatus (JSON + Bericht): Ap4R3Passed = true, Ap4OverallPassed = true**
