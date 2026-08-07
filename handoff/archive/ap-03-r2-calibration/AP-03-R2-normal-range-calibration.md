# AP 3 R2 – Normalbereichskalibrierung

**Quelle:** `AP-03-R2-normal-range-calibration.json` (5-Minuten-Kurztest, beschleunigte Phasen)

## Laser – Axis01.MotorTemperature

| Kennwert | Wert |
|----------|------|
| Normalbereich | 50–54 °C |
| Mittelwert gesamt | ~52 °C |
| Anteil im Normalbereich (gemischt) | ~18 % (erwartet niedrig wegen Idle/Cooling) |
| Mittelwert Processing | ~55 °C |
| Mittelwert Idle | ~46 °C |
| Mittelwert PeakLoad | ~57 °C |

## Kalibrierungsmaßnahmen

1. `ThermalLoad → MotorTemperature`: Gewicht 42, Offset 18, verzögert 20 s
2. Phasenoffset über `ProcessPhaseScheduler.GetTemperaturePhaseOffset`
3. Nominal-relative Kombination mehrerer Signal-Dependencies statt fehlerhaftem Mittelwert

## Bewertung

- Keine Hard-Limit-Verletzungen
- Idle plausibel unter Produktionsnormalbereich
- Processing/PeakLoad plausibel erhöht
- Cooling zeigt Abfall
