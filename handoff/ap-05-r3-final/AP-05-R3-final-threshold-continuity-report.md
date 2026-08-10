# AP-05-R3 Final Threshold Continuity Report

## 1. R2-Restbefund

EXP-002 HydraulicLeak: langer Abstand zwischen `ThresholdFirstReached` und `ThresholdConfirmed` ohne sichtbare Exit/Re-Entry-Kontinuität; viele `ThresholdApproaching`-Events.

## 2. Ursache

Threshold war zeitweise erfüllt, dann verlassen, später erneut erreicht. `ThresholdApproaching` wurde fälschlich bei erfülltem Threshold (vor MinimumDuration) emittiert.

## 3–6. ThresholdEntered / Exited / FirstReached

- `ThresholdEntered` bei jedem `false→true`
- `ThresholdExited` bei jedem `true→false`
- `ThresholdFirstReached` einmal pro Run (erstes dokumentiertes Enter nach Detectability-Gate)

## 7. ConfirmedThresholdStreakStartedAt

Aus letztem `ThresholdEntered` vor `ThresholdConfirmed` ohne intervenierendes `ThresholdExited` abgeleitet.

## 8. MinimumDuration

`ThresholdConfirmedAt - ConfirmedStreakStartedAt >= MinimumDuration` (15s für Laser/Hydraulic).

## 9. HydraulicLeak-Nachweis

EXP-002 FaultRuns mit Enter/Exit-Sequenz und bestätigtem Streak dokumentiert in Evidence JSON.

## 10. Laser Regression

EXP-001 FaultRuns weiterhin `FaultRecovered` mit korrekter Kontinuität.

## 11. ThresholdApproaching-Semantik

Nur bei Annäherung zum Threshold, **nicht** wenn bereits erfüllt. Einmalig bei `not approaching → approaching`, Reset bei Verlassen oder Erfüllung.

## 12. Detectability Regression

`Detectable < ThresholdFirstReached` unverändert grün.

## 13. Tests

199/199 (`Category!=Integration`).

## 14. VerificationRunId

Siehe `AP-05-R3-threshold-continuity-verification.json`.

## 15. Build/Test

Release Build OK.

## 16. Commit-SHA

`e51981a25c64040bc5fafd04e4b1abb95743b416`

## 17–18. Tag / Git-Status

Siehe `git-closure.txt`.

## 19. AP5R3Passed

`true`

## 20. AP5OverallPassed

`true`

## 21. Freigabeempfehlung

**AP 5 endgültig abgeschlossen.** Danach ersten echten VIGIL-Lernversuch gegen den Simulator planen.
