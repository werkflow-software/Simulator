# AP 3 R2 – OPC UA End-to-End

**Quelle:** `AP-03-R2-opcua-publishing-verification.json`, `AP-03-R2-short-end-to-end-verification.json`

## Kette

`PhysicalSimulationEngine → SignalRuntimeState → PhysicalSignalPublisher → OPC-UA-Node → DataChange`

## Kurztest (5 min, PHYSICS_VERIFY_SHORT=1)

| Kennzahl | Laser | Biegen |
|----------|-------|--------|
| OPC-UA-Updates | ~78 000+ | ~81 000+ |
| Engine-Ticks | ~2 400 | ~2 400 |
| Phasenwechsel | 64 | 64 |
| Auftragswechsel | 8 | 8 |
| Distinct Phasen | 8 | 8 |
| Ø Publish-Dauer | < 1 ms | < 1 ms |
| Fehlgeschlagene Updates | 0 | 0 |

## Publisher-Fix

Identische-Werte-Prüfung vergleicht nun den **letzten OPC-UA-Knotenwert** mit dem neuen Wert (nicht Runtime mit sich selbst). Nach Resume werden Update-Sequenzen fortgesetzt, damit Technical-Generator-Signale wieder wechseln.

## DataChange

Abonnierte Signale je Maschine: Achsgeschwindigkeit, Motorstrom, Motortemperatur, Prozessleistung/Presskraft, Kühlung/Hydraulik, Qualität, Zähler. SourceTimestamp steigt nachweisbar.
