# AP-05-R1 Final Ground Truth Closure Report

## 1. R0-Befunde

- `DetectableAt` lag vor `ScenarioStart` (Szenario-relative Zeit vs. Experimentzeit vermischt).
- Doppelte `ScenarioStarted`-Events durch doppelte Event-Subscription (Service + Hub).
- Control-Runs erreichten `Faulted`-Phase ohne echten Fault.
- EXP-001/002: Fault-Runs ohne vollständige Fault→Recovery-Kette.
- Outcomes nicht konsistent aus Events abgeleitet.
- Kein echter OPC-UA-Leakage-Scan, keine SameSeed/DifferentSeed-E2E-Nachweis.
- Experiment-WPF-Seite fehlte.

## 2. Zeitbasis-Korrektur

`GroundTruthEvent` speichert jetzt `ExperimentSimulationTimestamp`, `RunRelativeTimestamp`, `ScenarioRelativeTimestamp` (plus kompatible Aliase). `ExperimentRunner` aktualisiert den Experiment-Clock pro Tick; Manifest-Felder werden aus Events in Experimentzeitbasis befüllt.

## 3. Detectability-Korrektur

Detectability wird nur nach Szenariostart erfasst; bei Threshold-Fault wird fehlende Detectability vor Fault nachgezogen, damit Chronologie für Kurzserien stabil bleibt.

## 4. Event-Deduplizierung

Recorder subscribed nur noch auf `FaultScenarioEventHub` (eine Quelle). Lifecycle-Events werden pro Run dedupliziert.

## 5. ControlRun-Semantik

`NonFaultingControlRun` überspringt `Faulted`-Phasen, endet in `Recovering`/`Completed`, ohne `MachineFaulted`.

## 6. EXP-001 Ergebnis

- NormalRuns = 1, ControlRuns = 1, FaultRuns = 3, ActualFaults = 3, RecoveredFaults = 3
- Alle Fault-Runs: `FaultRecovered` mit vollständiger Event-Kette

## 7. EXP-002 Ergebnis

- NormalRuns = 1, ControlRuns = 1, FaultRuns = 2, ActualFaults = 2, RecoveredFaults = 2
- Hydraulic-Leak Kurzserie: Threshold → Fault → Recovery

## 8. Run-Validator

`GroundTruthRunValidator` prüft Chronologie, Duplikate, Run-Typ-Pflichtfelder und Outcome-Ableitung.

## 9. Reproduzierbarkeit

SameSeed: identische Run-/Event-/Simulationszeit-Sequenz. DifferentSeed: mindestens zwei Variationsmerkmale (RunSeed, Intensity, ScenarioStart).

## 10. DifferentSeed Variation

Nachgewiesen über Mini-Experiment `REP-MINI` / `REP-MINI-B`.

## 11. OPC-UA Leakage Test

309 Signale/Nodes geprüft (BrowseName, DisplayName, Description, Values). `Matches = []`.

## 12. Maschinenisolation

Ground-Truth-Events nur für die jeweilige `MachineId`.

## 13. Experiment-WPF-Seite

`ExperimentsView` + erweitertes `ExperimentsViewModel`, Navigation „Experimente“ in `MainViewModel`.

## 14. Metrics Regression

Synthetic Metrics Suite grün (`TruePositive`, `Missed`, `FalsePositive`, `LeadTime`, `ControlWarning`, `RepetitionTrend`).

## 15. NormalDuration

`MetricsEngine` erfasst Normaldauer in GroundTruthOnly aus tatsächlicher Normal-Run-Dauer.

## 16. VerificationRunId

`ap5r1-20260810151443-09d74bc67642415a9a207353207`

## 17. Build/Test

- `dotnet build Werkflow.OpcUaSimulator.sln -c Release` — OK
- `dotnet test ... --filter "Category!=Integration"` — 181/181 bestanden

## 18. RealVigilLearningEvaluation

`NotExecuted` (GroundTruthOnly, keine erfundenen VIGIL-Metriken).

## 19. Commit-SHA

`6ff0b1df77a5a577a694834837df1ef86d693242`

## 20. Tag / TagTarget

- Tag: `opcua-simulator-ground-truth-evaluation-ap5-verified`
- TagTarget-SHA: `6ff0b1df77a5a577a694834837df1ef86d693242`
- CommitEqualsTagTarget: `true`
- Vorheriger AP-5-Tag `opcua-simulator-ground-truth-evaluation-ap5-complete` unverändert

## 21. Git-Status

Nur AP-5-R1-Commit auf Branch `feature/physical-learning-simulator`. Uncommitted: AP-04-/R0-Handoff-Artefakte (nicht Teil R1).

## 22. AP5R1Passed

`true` (E2E-Verification JSON)

## 23. AP5OverallPassed

`true`

## 24. Freigabeempfehlung

AP 5 ist mit R1 semantisch geschlossen. **Kein** echter langfristiger VIGIL-Lernversuch starten. Freigabe für nachgelagerte VIGIL-Integration unter echten Ground-Truth-Vorgaben.
