# AP 3 R1 – Profilkonsistenz und physikalischer Nachweis

**Datum:** 2026-08-05  
**Branch:** `feature/physical-learning-simulator`  
**Basis-Commit:** `65274b2` (AP 3 Abschluss)  
**R1-Commit:** `90560fc` – *Complete AP 3 physical model verification and profile correction*  
**Tag:** `opcua-simulator-physical-simulation-ap3-verified`

---

## 1. Ausgangsstand

AP 3 hatte physikalische Engine, zwei Profile, Hidden States und Integrationstests geliefert. Die Handoff-Prüfung ergab Profil- und Nachweislücken (490-Signal-Biegeprofil, fehlende `Hydraulic.FilterLoad`-Referenz in JSON, keine `hiddenStateDependencies` im Export, uneindeutige Effektgrenzen, keine belastbaren Langzeitnachweise).

## 2. Festgestellte AP-3-Probleme

| # | Problem | R1-Status |
|---|---------|-----------|
| 1 | Biegeprofil ~490 Signale | **Korrigiert → 307** |
| 2 | Dependency auf fehlendes `Hydraulic.FilterLoad` | **Behoben (Option A)** |
| 3 | Hidden-State-Deps nicht im JSON | **Exporter/Loader ergänzt** |
| 4 | Fast nur Linear/InverseLinear | **≥9 Typen in Profilen** |
| 5 | `0/0` Effektgrenzen mehrdeutig | **Nullable-Semantik** |
| 6–11 | Langzeit, Statistik, Korrelation, Performance | **Harness + JSON/MD** |

## 3. Branch und Ausgangscommit

- Branch: `feature/physical-learning-simulator`
- Ausgangscommit: `65274b2` – *Implement AP 3 virtual machine physics*
- Tag AP 3: `opcua-simulator-physical-simulation-ap3-complete`

## 4. Geschützte lokale Änderungen

- `TechnicalLearningMachine300.json` – nur `initialDateTimeUtc` (AP-2-Regenerierung), **nicht** in R1-Commit aufgenommen
- Fremde Handoff-Löschungen (`ap-01`) unverändert gelassen

## 5–6. Korrigierte Profile

| Profil | Version | Signale | Hidden States | Signal-Deps | Hidden-State-Deps |
|--------|---------|---------|---------------|-------------|-------------------|
| Laser | 1.1.0 | **309** | 12 | 60 | 15 |
| Biegen | 1.1.0 | **307** | 12 | 40 | 15 |

## 7. Signalanzahl und Kategorieverteilung (Biegen, Auszug)

Axis ~44, Hydraulic ~27, Process/Bending ~35, Thermal ~12, Electrical ~31, Quality ~14, Diagnostic ~21, Vibration 12, Safety 6, Environment 3, übrige Production/Cooling/Pneumatic.

## 8. Korrigierte ungültige Dependency

**Entscheidung Option A:** `Hydraulic.FilterLoad` bleibt fachlich sinnvolles Signal (% Filterbelastung). Ursache des Handoff-Fehlers: `TechnicalLearningMachine300SemanticCorrector` entfernte das Signal im Biegeprofil. Korrektur: Corrector für Biegeprofil **nicht** mehr anwenden; Signal und Dependency konsistent.

## 9. Hidden-State-Abhängigkeiten

Explizites JSON-Array `hiddenStateDependencies` mit je 15 Einträgen. Biegen: u. a. `PressLoad → StructuralThermalLoad` (DelayedLinear), `HydraulicEfficiency → ValveResponse` (Hysteresis). Laser: u. a. `ProcessDemand → MechanicalLoad`, `MechanicalLoad → ThermalLoad`.

## 10. Dependency-Typverteilung

Je Profil mindestens: Linear, InverseLinear, DelayedLinear, Threshold, Saturating, Polynomial, Sigmoid, PiecewiseLinear, RateLimited, Hysteresis (nicht alle gleich häufig).

## 11. Effektgrenzen

Variante A (nullable): `null` = keine Grenze; Zahl = aktive Grenze; beide `0` = Nullwirkung. Validator, Evaluator und Tests angepasst.

## 12. Zyklenerkennung und Stabilität

Validator erkennt nicht erlaubte **2-Knoten-Rückkopplungen**. Begrenzte längere Ketten mit Saturating/RateLimited/Hysteresis und explizit erlaubten Kanten (z. B. PressLoad↔HydraulicEfficiency) bleiben stabil.

## 13. Unit-Tests

**78/78** bestanden (`Category!=Integration`), inkl. neuer `PhysicalAp3R1ProfileTests`.

## 14. Integrationstests

**3/3** Physics-Integration (Kurzmodus 90 s). Vollmodus: `PHYSICS_VERIFY_FULL=1`.

## 15–16. Langzeittest A / B (Vollmodus ausgeführt)

| Test | Start UTC | Ende UTC | Dauer | Engine-Ticks | Hard Limits | Plausibilität |
|------|-----------|----------|-------|--------------|-------------|---------------|
| A – Laser (1 Maschine) | 2026-08-05 21:04:17 | 21:34:18 | **30:00** | 14 462 | 0 | 0 |
| B – Laser+Biegen | 2026-08-05 21:34:18 | 22:04:20 | **30:00** | 14 499 / 14 499 | 0 | 0 |

`FullMode: true` in `AP-03-R1-single-machine-longrun.json` und `AP-03-R1-dual-machine-longrun.json`. Gesamtlauf `PHYSICS_VERIFY_FULL=1`: **3/3 Tests, ~2 h**.

## 17–21. Statistik, Korrelation, Kontrollpaare, Verzögerung, Phasen

Maschinenlesbar unter `handoff/ap-03-r1-physical-verification/`. Lesbare Zusammenfassungen: `AP-03-R1-*.md`.

## 22. Performance

Ø Engine-Tick Laser ~0,84 ms, Peak ~13 ms (Kurzmodus, 309 Signale). Speichermessreihe in `AP-03-R1-performance-and-memory.json`.

## 23–24. Build/Test

```
dotnet restore && dotnet build && dotnet test
```

Build: **0 Fehler, 0 Warnungen**. Tests: **78 Unit + 3 Integration** (Kurzmodus).

## 25. Geänderte Dateien (Auszug)

- `BendingHydraulicMachine300ProfileFactory.cs`, `PhysicalProfileDependencyBuilder.cs`
- `JsonPhysicalMachineProfileLoader.cs`, `PhysicalMachineProfileJsonExporter.cs`
- `DependencyEvaluator.cs`, `PhysicalMachineProfileValidator.cs`
- `PhysicalPhysicsR1VerificationHarness.cs`, `PhysicalAp3R1ProfileTests.cs`
- `MachineProfiles/*.json`, `README.md`, Handoff `ap-03-r1-physical-verification/`

## 26–27. Bewusst nicht umgesetzt / Risiken AP 4

Keine Fehlerszenarien, Ground Truth, VIGIL (Scope AP 4). Risiko: sehr lange Hidden-State-Ketten weiterhin nur mit Runtime-Sättigung begrenzt – Monitoring in AP 4.

## 28–31. Git-Abschluss

- Tag (neu): `opcua-simulator-physical-simulation-ap3-verified`
- Freigabeempfehlung: **AP 3 freigegeben** (Vollmodus 30+30 min bestanden, 78 Unit + 3 Integration Tests grün)

---

*Messdaten: `handoff/ap-03-r1-physical-verification/` – keine manuell erfundenen Werte.*
