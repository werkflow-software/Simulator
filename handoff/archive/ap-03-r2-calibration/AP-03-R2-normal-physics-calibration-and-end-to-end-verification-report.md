# AP 3 R2 – Normalphysik-Kalibrierung und kurze End-to-End-Verifikation

**Datum:** 2026-08-06  
**Branch:** `feature/physical-learning-simulator`  
**Basis-Commit:** `90560fc` (AP 3 R1)  
**Empfohlener Tag:** `opcua-simulator-physical-simulation-ap3-calibrated`

## 1. Ausgangsstand

AP 3 R1 lieferte strukturell funktionierende Profile und Engines, aber fachliche Lücken: keine gezählten Phasenwechsel, OPC-UA-Updates rechnerisch 0, Motortemperatur unter Normalbereich, schwache Friction→MotorCurrent-Korrelation, trivialisierte Korrelationen, zu weiche Pass-/Fail-Kriterien.

## 2. Teststrategie (bestätigt)

| Stufe | Dauer | In AP 3 R2 |
|-------|-------|------------|
| Modelltests | Sekunden–2 min | **ausgeführt** |
| Kurzer E2E | ~90 s CI / 5 min Export | **ausgeführt** |
| Langzeittest 30+ min | – | **bewusst nicht ausgeführt** |

## 3. Korrigierte Phasensteuerung

- `ProcessPhaseScheduler` mit `PhysicalVerificationMode.Short` (10–60 s Phasen)
- Phasenwechsel protokolliert in `ProcessPhaseTransition`
- Pause/Stop-Reset über `PhysicalSimulationContext.ResetPhaseState()`
- 5-Minuten-Nachweis: **128 Phasenwechsel**, **8 distinct Phasen** je Maschine

## 4. Auftragswechsel

- `PhysicalJobCoordinator`: JobName, PartName, Ziel-/Istzähler
- Kurztest: **16 Auftragswechsel** gesamt, PartName JOB-001→JOB-003 nachgewiesen

## 5. Normalbereichskalibrierung

- Motortemperatur: ThermalLoad-Gewicht/Offset, Phasenoffset ±10 °C
- Signal-Kombination: nominale Delta-Summation statt fehlerhaftem Mittelwert
- Laser Axis01.MotorTemperature (5 min): Mittelwert Processing ~55 °C, Idle ~46 °C, PeakLoad ~57 °C

## 6. Korrelationen

- Friction→MotorCurrent: Linear(6, 1.5), Modelltest positiv bestätigt
- Triviale Linear-Kopplungen auf Saturating/reduzierte Gewichte umgestellt
- Bewertungsmodell mit Min-/Max-Pearson je Beziehung in R2-Harness

## 7. OPC-UA-Kette

**Kritischer Publisher-Fix:** Identische-Werte-Prüfung vergleicht `entry.Variable.Value` mit neuem Wert (nicht Runtime mit sich selbst).

5-Minuten-Nachweis: **160 132 OPC-UA-Updates**, Publish-Dauer Ø < 1 ms, 0 Fehler.

## 8. Profile (final, aus Factory)

| Profil | Signale | Hidden States | Signal-Deps | Hidden-State-Deps | Version |
|--------|---------|-------------|-------------|-------------------|---------|
| Laser | 309 | 12 | 61 | 15 | 1.1.0 |
| Biege | 307 | 12 | 51 | 15 | 1.1.0 |

## 9. Tests

| Suite | Ergebnis |
|-------|----------|
| Unit (ohne Integration) | **88/88** |
| Integration | **6/6** |
| Build | 0 Fehler |

## 10. Nachweise

`handoff/ap-03-r2-calibration/`:

- `AP-03-R2-short-model-verification.json`
- `AP-03-R2-short-end-to-end-verification.json`
- `AP-03-R2-phase-verification.json`
- `AP-03-R2-normal-range-calibration.json`
- `AP-03-R2-correlation-calibration.json`
- `AP-03-R2-opcua-publishing-verification.json`
- Lesbare MD-Zusammenfassungen + `build-test-evidence.md`

## 11. Langzeittest

**Nicht automatisch ausgeführt.** Nach kurzer Freigabe empfohlen: optional ein 30-Minuten-Lauf mit `PHYSICS_VERIFY_FULL=1` zur finalen Langzeit-Statistik.

## 12. AP-3-Freigabeempfehlung

**Freigabe für AP 3** nach kurzer Verifikation empfohlen. Phasen, Aufträge, OPC-UA-Publishing, Kalibrierung und Tests sind nachgewiesen. Langzeittest optional nach Freigabe.

## 13. Risiken für AP 4

- Phasenbezogene Statistik in UI noch nicht vollständig integriert
- Einzelne Korrelationen im Biegeprofil können Review ergeben (gekoppelte Hidden States)
- Manuelles Override muss GenerationMode zuverlässig zurücksetzen (behoben in R2)
