# AP-05 Ground Truth and VIGIL Evaluation Report

## 1. AP-4 Ausgangsstand

AP 4 abgeschlossen auf Branch `feature/physical-learning-simulator`.

- Commit: `eb317a3ddca0bb36529822c79b4e76ff558855ad`
- Tag: `opcua-simulator-fault-scenarios-ap4-final-r7`

## 2. AP-4-R7 autoritative VerificationRunId

Im R7-Abschlussbericht und JSON unterschiedliche RunIds. Autoritative Evidence:

`AP-04-R7-final-hydraulic-recovery-verification.json` mit

`VerificationRunId: ap4r7-20260810133032-9a810e981eec4da09db84d6`

## 3. Architektur

`PhysicalSimulation/Evaluation/` mit GroundTruth, Experiments, Recording, Metrics, Vigil, Export.

## 4–12. Framework

- **GroundTruthRecorder** — interne Timeline, nie OPC UA
- **GroundTruthEvent** — ExperimentId, RunId, Seed, FaultRepetitionIndex, Sim/Real-Timestamps
- **DetectabilityDefinition** — reproduzierbarer `DegradationBecameDetectable`
- **ExperimentDefinition** — Warmup, Normal, Fault/Control counts, Variation, VigilMode
- **ExperimentRunner** — Warmup → Normal → Fault/Control-Serien, ResetBetweenRuns
- **Variation** — Intensity, StartOffset, Seeds per Run
- **Reproduzierbarkeit** — BaseSeed + RunSeed derivation

## 13. VIGIL-Adapter

- `IVigilEventSource`, `NullVigilEventSource`, `RecordedVigilEventSource`
- `VigilEvent` — nur tatsächlich gelieferte Felder

## 14–16. Modi

- **GroundTruthOnly** — keine erfundenen VIGIL-Metriken (`VigilEvaluationAvailable = false`)
- **VigilEvaluation** — Metriken nur bei verbundener Quelle

## 17. Metrics Engine

LeadTime, DetectionRate, FalsePositiveRate, Repetition trends. Synthetic tests markiert als `SyntheticTestEvidence`.

## 18–21. Kurzserien

- EXP-001 (Laser Overheating): 3 Fault, 1 Control — GroundTruthOnly
- EXP-002 (Hydraulic Leak): 2 Fault, 1 Control

## 22. Informationsleck

Ground Truth nicht in OPC-UA-Metadaten der Events. Keine ScenarioId/ExperimentId über Signale.

## 23–27. Tests & Build

173/173 non-integration tests green. AP5 suite: 10 tests.

## 28. VerificationRunId

Siehe `AP-05-ground-truth-short-verification.json`

## 29. RealVigilLearningEvaluation

`NotExecuted` — kein echter VIGIL-Langzeittest in AP 5.

## 30. Risiken / offene Punkte

- Echter VIGIL-Langzeitversuch nach Freigabe (siehe `AP-05-long-term-vigil-test-readme.md`)
- UI-Navigation „Experimente“ als ViewModel vorbereitet; vollständige WPF-Seite optional nachrüstbar

## 31. Freigabeempfehlung

**AP 5 technisch bestanden.** Evaluationsframework bereit für reale VIGIL-Kopplung. AP 4 nicht erneut geöffnet.
