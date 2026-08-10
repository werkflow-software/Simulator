# AP-05-R4 Final Event Hygiene Report

## 1. R3-Restbefund

R3 behauptete edge-triggered `ThresholdApproaching`, aber EXP-002 `fault-1` zeigte ~102 `ThresholdApproaching` bei nur ~43 `ThresholdEntered` / ~42 `ThresholdExited`. Viele Approaching-Events ohne dokumentierten Zustandswechsel.

## 2. Ursache des ThresholdApproaching-Spams

`OnThresholdExited()` setzte `IsApproaching = false`. Nach `Satisfied → Approaching` (mit `ThresholdExited`) wurde bei jedem Tick erneut `ThresholdApproaching` emittiert, solange das Signal in der Vorwarnzone blieb. Ein separates `bool IsApproaching` war nicht mit Enter/Exit-Semantik synchron.

## 3. Threshold-Zustandsmodell

Pro `ThresholdRule` interner Beobachtungszustand (`FaultThresholdObservationState`):

- `Normal`
- `Approaching`
- `Satisfied`
- `Confirmed`

## 4. Approaching Edge-Semantik

`ThresholdApproaching` nur bei Transition `Normal → Approaching` oder nach `Satisfied → Approaching` (einmalig mit `ThresholdExited`). Kein Event bei `Approaching → Normal` oder während unverändertem `Approaching`. Nicht emittiert wenn `IsThresholdSatisfied = true`.

## 5. Hydraulic Regression

R4-HYD Mini-FaultRun (1 Fault, seed 202, TimeFactor 50):

- Detectability vor FirstReached: **true**
- Enter/Exit-Kontinuität: **43 Enter / 42 Exit**
- Confirmed-Streak: **15 s** (MinimumDuration 15 s)
- `ThresholdConfirmed` + `MachineFaulted` + Recovery: **grün**

## 6. Laser Regression

R4-LAS Mini-FaultRun:

- Approaching: **1** (kein Spam)
- Enter: **1**, Confirmed-Streak **15 s**, Fault: **grün**

## 7. Event-Counts (Hydraulic fault-1)

| Metrik | Wert |
|--------|------|
| ThresholdApproachingCount | 67 |
| ThresholdEnteredCount | 43 |
| ThresholdExitedCount | 42 |
| ThresholdConfirmedCount | 1 |

## 8. DuplicateApproachingCount

**0** (Hydraulic und Laser)

## 9. InvalidTransitionCount

**0** (Hydraulic und Laser)

## 10. ThresholdContinuity Regression

EXP-002 Short FaultRuns: **grün** (inkl. Hygiene-Validator)

## 11. Detectability Regression

**true** (Hydraulic + Laser)

## 12. Tests

212/212 (`Category!=Integration`), inkl. 13 neue AP5-R4-Tests.

## 13. VerificationRunId

`ap5r4-20260810193614-700d869a7d7549d5845448115a6`

## 14. Build

```
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

Ergebnis: **Build erfolgreich, 212 Tests bestanden**

## 15. Commit-SHA

`55c7837d93e0d85de9124906a3d39177c65a5e75`

## 16. Tag / TagTarget

Tag: `opcua-simulator-ground-truth-evaluation-ap5-final-r4`

TagTargetSHA: `55c7837d93e0d85de9124906a3d39177c65a5e75`

CommitEqualsTagTarget: **true**

## 17. Git-Status

R4-Commit erstellt; unrelated unstaged Handoff/AP-4-Artefakte nicht Teil dieses Commits. Siehe `git-closure.txt`.

## 18. AP5R4Passed

**true**

## 19. AP5OverallPassed

**true**

## 20. Finale Freigabeempfehlung

**AP 5 ist endgültig abgeschlossen.** `ThresholdApproaching` ist nachweislich edge-triggered (`DuplicateApproachingCount = 0`). ThresholdEntered/Exited/FirstReached/Confirmed-Streak und Detectability bleiben korrekt. Keine weitere AP-5-Korrektur erforderlich. Nächster Schritt: erster echter VIGIL-Lernversuch.

## ThresholdConfirmed Metadata

Strukturierte Keys: `MinimumDuration`, `ConfirmedStreakStartedAt` (plus `detail` = RuleId für Event-Hygiene-Gruppierung).
