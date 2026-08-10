# AP-05 Long-Term VIGIL Learning Test (Documentation Only)

Do not start without explicit approval.

## Recommended workflow

1. Start VIGIL with empty or fresh learning state
2. Run simulator `NormalBaseline` experiment (30–60 simulation minutes)
3. Run `Fault #1` → Recovery → Normal
4. Repeat faults with variation (5–10 repetitions)
5. Export VIGIL events from the real instance
6. Import via `RecordedVigilEventSource` or connect `IVigilEventSource`
7. Run evaluation (`VigilEvaluation` mode)
8. Compare LeadTime, DetectionRate, FalsePositiveRate across repetitions

## Duration guidance

- Normal learning: 30–60 simulation minutes per fault type
- Fault repetitions: 5–10 with ±10–20% variation
- Control runs between fault runs

## AP-5 scope

AP 5 delivers the evaluation framework. `RealVigilLearningEvaluation = NotExecuted` until real VIGIL events are attached.
