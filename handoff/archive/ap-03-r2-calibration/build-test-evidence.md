# AP 3 R2 – Build- und Testnachweis

**Datum:** 2026-08-06  
**Branch:** `feature/physical-learning-simulator`

## Build

```
dotnet restore
dotnet build Werkflow.OpcUaSimulator.sln
```

Ergebnis: **0 Fehler**, 0 Warnungen (nach xUnit2012-Fix).

## Tests

| Suite | Ergebnis |
|-------|----------|
| Unit-Tests (ohne Integration) | **88/88** bestanden |
| Integration (Kurzmodus) | **6/6** bestanden (inkl. R2 E2E 90 s) |
| Modellverifikation | bestanden |
| 5-Minuten-Export (`PHYSICS_VERIFY_SHORT=1` + `PHYSICS_VERIFY_EXPORT=1`) | Evidence erzeugt |

**Kein** automatischer 30-Minuten-Langzeittest ausgeführt.

## Verifikationsmodi

| Variable | Zweck |
|----------|--------|
| `PHYSICS_VERIFY_SHORT=1` | Beschleunigte Phasen, ~5 min E2E |
| `PHYSICS_VERIFY_EXPORT=1` | JSON-Nachweise nach `handoff/ap-03-r2-calibration/` |
| `PHYSICS_VERIFY_FULL=1` | Nur manuell – 30 min Langlauf (nicht in CI) |
