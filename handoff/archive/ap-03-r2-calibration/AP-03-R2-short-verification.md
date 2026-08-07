# AP 3 R2 – Kurzverifikation (Modell)

**Erzeugt:** 2026-08-06  
**Quelle:** `AP-03-R2-short-model-verification.json`

## Ergebnis

| Prüfpunkt | Laser | Biegen |
|-----------|-------|--------|
| Phasenwechsel | ≥ 59 | ≥ 59 |
| Distinct Phasen | 8 | 8 |
| Auftragswechsel | ≥ 7 | ≥ 7 |
| Modelltest Passed | ja | ja |

## Dependency-Checks

- Friction → MotorCurrent: positiv (bestanden)
- Friction → Speed: negativ (bestanden)
- MechanicalLoad → MotorCurrent: positiv (bestanden)
- Thermische Trägheit > Strom-Trägheit (bestanden)

## Hinweis

Der schnelle Modelltest nutzt `PhysicalVerificationMode.Short` mit erhöhtem `TimeFactor` (12×). Endzustände können phasenbedingt außerhalb des statischen Normalbereichs liegen; die phasenbezogene Auswertung im 5-Minuten-Lauf ist maßgeblich.
