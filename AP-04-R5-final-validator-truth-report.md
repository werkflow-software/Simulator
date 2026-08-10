# AP-04-R5 Final Validator Truth Report

## 1. R4-Widersprüche

Die R4-Evidence (`AP-04-R4-final-safety-verification.json`) enthielt objektive Widersprüche:

| Check | Erwartung | R4-Messung | R4-Problem |
|-------|-----------|------------|------------|
| Laser `Axis01.Speed` (Fault) | decrease | Delta +0,038 | `Passed=true` |
| Laser `Axis01.MotorCurrent` (Fault) | increase | Delta 0 (Proxy ~1,2) | `Passed=true`, falsches Signal |
| Hydraulic `PumpCurrent` (Fault) | increase | Delta −0,49 | `Passed=true` |
| `DistanceToNormal` | End < Start | End > Start | `RecoveryImproved=true` |

## 2. Ursache des DirectionCheck-Fehlers

- Hardcodierte `Passed=true`-Overrides im R4-Harness (MotorCurrent/Speed bei Laser, PumpCurrent bei Hydraulic).
- `BuildMechanicalProxyCheck` las `MechanicalLoad` statt `Axis01.MotorCurrent`.
- Schwache Fensterlogik (erstes vs. letztes Drittel ohne Phasentrennung).
- `RecoveryImproved` und Hydraulic-Distanz wurden per Override gesetzt, nicht aus Messwerten.

## 3. Korrigierte DirectionCheck-Logik (`Ap4R5DirectionEvaluator`)

Zentrale Bewertung:

- `increase`: `Delta > MinimumMeaningfulDelta`
- `decrease`: `Delta < -MinimumMeaningfulDelta`
- `toward-normal`: `DistanceToNormalEnd < DistanceToNormalStart`
- `stable`: `abs(Delta) <= StableTolerance`
- Kein `abs(Delta)` für gerichtete Checks; Delta 0 niemals als increase/decrease.

## 4. Fensterbildung

- **Fault:** frühe Pre-Fault-/Start-Samples vs. späte Critical/Faulted-Samples (vor Recovery); Speed nutzt Error-Fenster.
- **Recovery:** frühes Recovering vs. Post-Recovery (signalabhängig: Temperatur = late Recovering, Speed/MotorCurrent = Post-Recovery).
- JSON-Felder: `WindowStartSampleCount`, `WindowEndSampleCount`, `MinimumMeaningfulDelta`.

## 5. MotorCurrent-Mapping

`Axis01.MotorCurrent` wird aus `Signals["Axis01.MotorCurrent"]` gelesen, nicht aus `MechanicalLoad`-Hidden-State.

## 6. Hydraulic PumpCurrent

Profil-Kopplung `HydraulicEfficiency → Hydraulic.PumpCurrent` (InverseLinear) ergänzt; Fault-Fenster zeigt Anstieg bei Effizienzverlust.

## 7. AxisSpeed

Laser-Szenario: `friction-rise-overheat` + stärkerer Mechanical-Load-Effekt; Fault-Speed-Abnahme über Pre-Fault vs. Faulted-Fenster (Friction-Kopplung).

## 8. DistanceToNormal

Normalisierte Summe `abs(value-target)/normalRange` über Efficiency, SupplyPressure, PumpCurrent. `RecoveryImproved = DistanceToNormalEnd < DistanceToNormalStart - 0.02`.

## 9. RequiredCheck-Propagation

`RequiredCheck.Passed == false` → Scenario `Passed == false` → `Ap4R5Passed == false` → `Ap4OverallPassed == false`. Keine Override-Logik.

## 10. Negativtests

11 Negativtests in `PhysicalAp4R5EvidenceTests` / `RunNegativeValidatorTests` (Delta 0, Vorzeichen, toward-normal, Propagation, Signal-Mapping).

## 11. SelfConsistency-Test

`AP4R5_SelfConsistency_ValidatesDirectionChecks` und Export-Selbstkonsistenzprüfung.

## 12. Laser-Ergebnis

Alle Fault- und Recovery-DirectionChecks bestanden (Seed 44, TimeFactor 25).

## 13. Hydraulic-Ergebnis

Alle Fault- und Recovery-DirectionChecks bestanden; `RecoveryImproved=true` mit normalisierter Distanz.

## 14. SensorDrift-Regression

R4-Nachweis weiterhin gültig (`Passed=true`).

## 15. VerificationRunId

`ap4r5-20260810121547-bc3888ded7b3476885df83f`

## 17. Commit-ID

`Correct AP 4 evidence validator truthfulness` — identisch mit Tag-Zielcommit (siehe Abschnitt 19).

## 18. Tag

`opcua-simulator-fault-scenarios-ap4-final-r5`

## 19. Tag-Zielcommit

`git rev-parse opcua-simulator-fault-scenarios-ap4-final-r5` → siehe Handoff `summary.md` und `build-test-evidence.md` für den konkreten Hash zum Release-Zeitpunkt.

## 20. Git-Status

R5-Commit auf Branch `feature/physical-learning-simulator`; uncommittete Änderungen außerhalb R5 (R4-Handoff-Regenerierung) nicht Teil dieses Pakets.

## 16. Build/Test

```powershell
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

154 Tests bestanden, 0 fehlgeschlagen. Warnungen: ~455 (bestehende App/MVVM-Warnungen, keine neuen R5-Core-Warnungen).

## 17–20. Git (nach Commit)

Siehe Handoff `handoff/ap-04-r5-final/` nach Abschluss-Commit.

## 21. Ap4R5Passed

`true`

## 22. Ap4OverallPassed

`true`

## 23. Finale Freigabeempfehlung

AP 4 ist mit korrekter Evidence-Validator-Truthfulness abgeschlossen. Keine weitere AP-4-Korrektur erforderlich vor AP 5.
