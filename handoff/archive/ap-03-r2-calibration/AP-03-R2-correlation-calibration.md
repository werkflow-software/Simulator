# AP 3 R2 – Korrelationskalibrierung

**Quelle:** `AP-03-R2-correlation-calibration.json`

## Korrigierte Beziehungen

| Beziehung | Erwartung | Maßnahme |
|-----------|-----------|----------|
| Friction → MotorCurrent | positiv, ≥ 0,25 | Linear(6, 1,5), nominale Kombination |
| MechanicalLoad → Load | positiv, nicht trivial | Saturating statt Linear |
| ProcessDemand → SpindleSpeed | positiv, nicht ~1,0 | Saturating + reduzierte Gewichte |
| PressLoad → SupplyPressure | positiv | Linear(95, 125), konkurrierende Dep entfernt |

## Bewertungsmodell

Je Beziehung: Min-/Max-Pearson, erwartete Richtung, Lag-Bereich, Ergebnis Passed/Review/Failed.

## Friction → Axis01.MotorCurrent (Laser)

- Erwartung: positiv, Pearson 0,25–0,92
- Kurztest: nachweisbar positiv (Modelltest und E2E-Stichprobe)

## Hinweis

Einzelne Biege-Korrelationen können in kurzen Läufen Review ergeben, wenn Hidden States durch gekoppelte Hidden-State-Dependencies moduliert werden. Schwache oder fehlende Richtung führt zu Failed.
