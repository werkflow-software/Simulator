# AP-05-R2 Final Ground Truth Lifecycle Report

## 1. R1 Restbefunde

- `Faulted`-Phase vor `MachineFaulted` durch zeitgesteuerte Szenario-Phasen
- Kein `ThresholdConfirmed` in Ground Truth
- `DetectableAt` konnte gleichzeitig mit Threshold/Fault liegen
- NormalRun-Manifest mit `ScenarioStart`/`RecoveryCompletedAt`
- RunManifest ohne `RunStartedAt`/`RunCompletedAt`

## 2. Faulted-Phase-Ursache

`AdvancePhase` setzte declarative `Faulted`-Phase nach Ablauf der Phasen-Timeline, unabhängig von ThresholdEngine.

## 3. Korrigierte Fault-Lifecycle-Semantik

`Faulted`-Phase nur nach `MachineFaulted`; Pre-Fault hält bei `Critical`.

## 4–7. Threshold-Kette

`ThresholdFirstReached` bei erstem Überschreiten (nach Detectability); `ThresholdConfirmed` vor `MachineFaulted`; MinimumDuration aus Regel erfüllt.

## 8. Detectability

Deferred Threshold-Events bis Detectability; strikt `Detectable < ThresholdFirstReached`.

## 9. NormalRun-Semantik

Alle Szenario-/Fault-Felder `null`; nur `NormalObservationStarted`.

## 10. RunStarted/RunCompleted

Separat von Szenario-/Recovery-Zeiten in RunManifest.

## 11. ControlRun Regression

Faultfrei; keine Regression.

## 12–13. EXP-001 / EXP-002

3/3 bzw. 2/2 FaultRuns `FaultRecovered` mit vollständiger Kette.

## 14. RunManifest/GroundTruth-Konsistenz

Manifest aus Events via `GroundTruthRunValidator.PopulateManifestFromEvents`.

## 15. SameSeed Regression

Grün inkl. `ThresholdConfirmed`.

## 16. Metrics Regression

Synthetic Suite grün.

## 17. VerificationRunId

Siehe `AP-05-R2-ground-truth-lifecycle-verification.json`.

## 18. Build/Test

193/193 Tests (`Category!=Integration`).

## 19. Commit-SHA

`2b6e819784c36247e342cda883da1e49152d3f6e`

## 20. Tag / TagTarget

- Tag: `opcua-simulator-ground-truth-evaluation-ap5-final`
- TagTarget-SHA: `2b6e819784c36247e342cda883da1e49152d3f6e`
- Vorherige Tags (`ap5-verified`, `ap5-complete`) unverändert

## 22. AP5R2Passed

`true`

## 23. AP5OverallPassed

`true`

## 24. Freigabeempfehlung

**AP 5 ist endgültig abgeschlossen.** Erst danach ersten echten VIGIL-Lernversuch planen.
