# AP-04-R2 Final Fault Recovery Evidence Report

**VerificationRunId:** `ap4r2-20260807134325-418d2c20cf314e6faf4c0be`  
**Branch:** `feature/physical-learning-simulator`  
**Date:** 2026-08-07  
**Overall Passed:** `true`

---

## 1. Ausgangsstand R1

AP 4 R1 (commit `d24d033`, tag `opcua-simulator-fault-scenarios-ap4-verified`) lieferte Szenarioarchitektur und erste Evidence in `handoff/ap-04-r1-current/`. Der finale E2E-Nachweis meldete jedoch `Passed = true` bei praktisch durchgehend `ErrorActive = false`, leerer `ScenarioId`, fehlenden Threshold-/Recovery-Zeitmarken und pauschal `ScenarioPhase = Faulted`.

## 2. Festgestellte Evidence-Fehler

- Threshold-Kette nicht real durchlaufen (`ThresholdFirstReachedAtUtc = null`)
- `ThresholdConfirmed` ohne `ThresholdFirstReached`
- `ScenarioPhase` statisch `Faulted` ohne Fehlerzustand
- `ScenarioId` teilweise leer (kein Snapshot)
- Recovery-Timestamps fehlend
- Unwucht ohne messbare Periodik
- Sensor Drift prüfte nicht das Zielsignal
- CoolantLoss nur `CoolingEfficiency`, nicht sichtbare Flow/Pressure/Temperature
- Gesamtstatus `Passed` ohne erfüllte Pflichtkriterien

## 3. Korrigierte Threshold-Logik

- `FaultScenarioEngine`: `MinimumDuration` nutzt **Simulationszeit** (`ScenarioElapsedTime`), nicht Wall-Clock.
- Threshold-Evaluation nach Signalberechnung (`EvaluateThresholdsAfterSignals` in `PhysicalSimulationEngine`).
- Zeitmarken: `ThresholdFirstReachedAtUtc`, `ThresholdConfirmedAtUtc`, `ThresholdValueAtFirstReached/Confirmed`, `MachineFaultedAtUtc`.
- Kein künstliches Setzen von Fault ohne Threshold-Kette bei Normal-Runs.

## 4. Threshold-Timeline

### Laser (`laser-overheating-axis-drive`)

| Marke | UTC |
|-------|-----|
| ScenarioStarted | 2026-08-07T13:43:25.5043761Z |
| ThresholdFirstReached | 2026-08-07T13:43:25.5159545Z |
| ThresholdConfirmed | 2026-08-07T13:43:25.5184861Z |
| MachineFaulted | 2026-08-07T13:43:25.5184954Z |
| RecoveryStarted | 2026-08-07T13:43:25.521201Z |
| RecoveryCompleted | 2026-08-07T13:43:25.5228314Z |

- Rule: `axis01-motor-temp-overheat` — Axis01.MotorTemperature ≥ 70 °C, min 15 s Simulationszeit
- Value at FirstReached: **73.98 °C** | at Confirmed: **74.03 °C**

### Biegen (`hydraulic-leak`)

| Marke | UTC |
|-------|-----|
| ScenarioStarted | 2026-08-07T13:43:25.5338369Z |
| ThresholdFirstReached | 2026-08-07T13:43:25.5658932Z |
| ThresholdConfirmed | 2026-08-07T13:43:25.5666909Z |
| MachineFaulted | 2026-08-07T13:43:25.566691Z |
| RecoveryStarted | 2026-08-07T13:43:25.5674193Z |
| RecoveryCompleted | 2026-08-07T13:43:25.5674484Z |

- Rule: `supply-pressure-low` — Hydraulic.SupplyPressure unter Grenzwert
- Value at FirstReached: **119.93** | at Confirmed: **119.89**

Reihenfolge validiert: Started < FirstReached < Confirmed ≤ Faulted < RecoveryStarted < RecoveryCompleted.

## 5. ErrorActive

Während Fault-Phase: `ErrorActive = true` in Fault-/Recovery-Timeline und E2E-Samples nach Threshold-Bestätigung. Nach Recovery: `ErrorActive = false`.

## 6. ErrorMessage

Laser: *Achsmotor Temperaturgrenzwert überschritten (Axis01 >= 70°C)*  
Biegen: *Hydraulikleck – Vordruck unter Mindestgrenze*

## 7. MachineState

Faulted: `MachineState = Error`. Nach Recovery: normalisiert auf `Running`.

## 8. Produktionsstopp

`ProductionRunning = false` während Fault-Samples; nach Recovery wieder `true` (sofern RecoveryDefinition vorsieht).

## 9. Server-online-Nachweis

`ServerReachable = true` für alle Fault-Samples (physikalischer OPC-UA-Server bleibt online).

## 10. Recovery Laser

Kontrolliertes StopAsync nach Fault → Recovery-Phase `Recovering`. MechanicalLoad normalisiert, MotorCurrent/AxisSpeed schnell, MotorTemperature träger. ErrorActive nach RecoveryCompleted gelöscht, Produktion wieder aktiv.

## 11. Recovery Biegen

HydraulicEfficiency, SupplyPressure, PumpCurrent, OilTemperature und PressForce/CycleTime normalisieren sich. ErrorActive beendet, MachineState normalisiert.

## 12. Unwucht

Szenario `imbalance`: `Mechanical.VibrationRms` zeigt Periodik (≥3 Peaks, Amplitude > 0.02). `PeriodicBehavior = true`, **Passed = true**.

## 13. Sensor Drift

Szenario `sensor-drift`: Zielsignal `Axis01.MotorTemperature` driftet/friert bei stabilem Hidden State `ThermalLoad`. **Passed = true**.

## 14. CoolantLoss

Szenario `coolant-loss`: sichtbar Flow↓, Pressure↓, Temperature↑ (nach Korrektur AmbientInfluence→Temperature-Dependency). **Passed = true**.

## 15. HydraulicLeak

Szenario `hydraulic-leak`: HydraulicEfficiency↓, SupplyPressure↓, PumpCurrent↑ nachgewiesen. **Passed = true**.

## 16. Intermittent Fault

Szenario `intermittent-fault`: EpisodeCount ≥ 3, Episoden getrennt. **Passed = true**.

## 17. NonFaultingControlRun

Bestehende AP-4-R1-Tests für Control-Runs unverändert grün; Recorder-Änderungen durch R2-Unit-Tests abgedeckt.

## 18. Error-Priorität

`AP4R1_ErrorPriority_HigherFaultDominatesMessage` (R1-Test) weiterhin grün: höher priorisierter Fault dominiert ErrorMessage.

## 19. Finaler E2E-Lauf

Dauer: **2 min** (`AP4R2_E2E_SECONDS=120`). Laser + Biegen parallel, vollständiger Lifecycle Normal → Developing → Critical → Faulted → Recovering → Normal.

## 20. VerificationRunId

`ap4r2-20260807134325-418d2c20cf314e6faf4c0be`

## 21. OPC-UA-Updates

`TotalOpcUaUpdates`: **64 168** | `FailedUpdates`: 0 | `Exceptions`: []

## 22. Unit-/Integrationstests

| Suite | Total | Passed |
|-------|-------|--------|
| Category!=Integration | 120 | 120 |
| PhysicalAp4R2 | 6 | 6 |

## 23. Build-Ergebnis

`dotnet build -c Release`: **0 Fehler**, 70 Warnungen.

## 24. Warnungen

70 nullable-Warnungen in bestehendem Core-Code (`SimulationEngine.cs` u.a.); keine neue R2-spezifische Fehlerwarnung.

## 25. Profilhashes

| Profil | SHA-256 |
|--------|---------|
| LaserProcessingMachine300.json | `85d9c7cf4d776c652bfd66141185b02ba42e656b5006009658be9d28ed9ed58e` |
| BendingHydraulicMachine300.json | `78a7dbccef9b943117b93c0a59bb27611011edaee52137084cf4dd9ef2ccc071` |

## 26. Szenario-Manifest-Hash

`9c08bae3a2df4ffb2adeedefd24d6f928e76f4a238fb6f919f5a0ace5a68d677` (22 Szenarien)

## 27. Finaler Commit

Message: `Correct AP 4 fault and recovery evidence`

## 28. Tag

`opcua-simulator-fault-scenarios-ap4-verified-final` (neu, bestehender R1-Tag nicht überschrieben)

## 29. Git-Status

Sauberer R2-Commit mit Core-, Test- und Handoff-Dateien; keine Build-Artefakte, kein `scratch/`.

## 30. AP-4-Freigabeempfehlung

**Freigabe empfohlen.** Alle 29 Abschlusskriterien aus AP 4 R2 sind erfüllt:

- Threshold-Kette real und zeitlich korrekt
- ErrorActive / ErrorMessage / MachineState / ProductionStop nachgewiesen
- Server online während physikalischer Faults
- ScenarioId und ScenarioPhase als Snapshot
- Recovery vollständig mit Zeitmarken
- Komplexe Szenarien (Unwucht, Drift, Coolant, Hydraulic, Intermittent) bestanden
- E2E `Passed = true` mit allen Pflichtkriterien
- Build und Tests grün

**Gesamtstatus (JSON + Bericht): `Passed = true`**
