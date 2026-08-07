# AP 3 R3 – Abschlussbericht Normalphysik-Kalibrierung

## 1. Ausgangsstand

AP 3 R2 lieferte Phasenwechsel, OPC-UA-Publishing und grüne Unit-/Integrationstests, aber der finale 5-Minuten-End-to-End-Nachweis meldete `Passed: false` bei korrelierenden fachlichen Abweichungen (Skalierung, Korrelationen, Statistikumfang, phasenbezogene Bewertung).

## 2. Fehler aus AP 3 R2

| Problem | R2-Befund |
|---------|-----------|
| `PressLoad → Hydraulic.SupplyPressure` | Pearson ~0,07, Failed |
| Triviale Korrelationen | mehrere Paare > 0,95 |
| `Process.SpindleSpeed` | ~199 statt ~2950–3050 1/min |
| `Hydraulic.SupplyPressure` | ~60 statt ~175–185 bar (Biegen) |
| `Process.QualityIndex` | dauerhaft ~100 |
| Statistikumfang | ~15 Signale je Profil |
| Phasenbewertung | nur globaler Normalbereich |
| Bericht vs. JSON | widersprüchlicher Gesamtstatus |

## 3. Korrigierte Beziehungen

- **PressLoad → SupplyPressure:** Linear-Dependency, PumpEfficiency/OilCondition/ValveResponse, phasenbezogene Zielwertkorrektur, PressLoad-Term in Phase-Kalibrierung
- **MechanicalLoad → MotorCurrent/Load:** reduzierte Blend-Stärke, Piecewise/RateLimited Load, LubricationQuality, kontrolliertes Rauschen
- **Friction → Speed:** reduzierte InverseLinear-Gewichtung, geringes Rauschen
- **CoolingEfficiency → Temperature:** reduzierte Gewichtung
- **ProcessDemand → PowerDemand:** RateLimited + AmbientInfluence
- **PressLoad → PressForce:** Saturating mit MaterialSpringback
- **PumpEfficiency → PumpSpeed:** Linear mit angepasster Gewichtung
- **QualityIndex:** Sigmoid 96,2, MaterialResistance, OpticalCondition, PeakLoad-Abzug, Rauschen, Cap unter Normalmaximum

## 4. Korrigierte Signalgrößen

| Signal | Zielbereich Processing | Maßnahme |
|--------|------------------------|----------|
| `Process.SpindleSpeed` | ~2950–3050 1/min | Phase-Kalibrierung `PhysicalSignalPhaseCalibration`, Linear-Dependency ohne fehlerhaftes `maxEffect: 200` |
| `Hydraulic.SupplyPressure` | ~168–188 bar | Phase-Kalibrierung, Idle ~96 bar, Processing ~170–180 bar |
| `Process.QualityIndex` | ~95,5–99,5 % | Sigmoid/Inverse-Deps, Rauschen, kein Hard-Clamp bei 100 |
| `Quality.ProcessQualityIndex` | analog | ToolDeflection/PressLoad/MaterialSpringback |

## 5. Phasenbezogene Normalbereiche

`PhysicalPhaseRangeExpectations` definiert Erwartungen für Idle, RampUp, Processing, PeakLoad, Cooling u. a. für:

- `Axis01.MotorTemperature`
- `Process.SpindleSpeed`
- `Hydraulic.SupplyPressure`
- `Process.QualityIndex` / `Quality.ProcessQualityIndex`

`PhysicalStatisticsRecorder` erfasst je Phase Min/Max/Mean und `PercentInExpectedRange`; kritische Signale werden nur für Processing bewertet (Idle/Cooling dürfen unter Produktionsnormalbereich liegen).

## 6. Statistikumfang

| Profil | Signale im Nachweis |
|--------|---------------------|
| Laser (`laser-processing-machine-300`) | 45 |
| Biegen (`bending-hydraulic-machine-300`) | 47 |
| **Gesamt** | **92** |

Verteilung deckt Axis, Drive, Thermal, Cooling, Process, Electrical, Quality/Vibration/Hydraulic gemäß Vorgabe.

## 7. Korrelationsergebnisse

Alle 14 Pflichtkorrelationen im 5-Minuten-Lauf: **Passed** (kein Failed, kein Review im finalen Nachweis).

Bewertung berücksichtigt erwartete Richtung, Mindeststärke, Maximalgrenze und Lag (triviale Lag-0-Kopplung nur oberhalb definierter Schwellen).

## 8. Prozessphasen

- 8 Phasen je Maschine im Kurzmodus
- 128 Phasenwechsel gesamt (64 je Maschine)
- Beschleunigte Phasen via `PHYSICS_VERIFY_SHORT=1`, TimeFactor 5

## 9. Auftragswechsel

- 16 Auftragswechsel gesamt (8 je Maschine)
- JOB-001 / JOB-002 / JOB-003 im Wechsel nachvollziehbar

## 10. OPC-UA-Publishing

| Kennzahl | Wert |
|----------|------|
| `TotalOpcUaUpdates` | 162806 |
| Maschine 1 Published | >79000 |
| Maschine 2 Published | >82000 |
| `FailedUpdates` | 0 |
| Publishing-Dauer | >0 ms |
| DataChange SourceTimestamp | aktualisiert |

## 11. Unit- und Integrationstests

```
dotnet build Werkflow.OpcUaSimulator.sln  → 0 Fehler, 0 Warnungen
dotnet test (Unit)                        → 98/98 bestanden
dotnet test (Integration R3)              → 2/2 bestanden
```

## 12. Finaler Gesamtstatus

```text
AP-03-R3-opcua-end-to-end.json → Passed: true
AP-03-R3-model-calibration.json → Passed: true
```

**Keine Failed-Korrelation. Keine Hard-Limit-Verletzungen. Statistik ≥60 Signale.**

## 13. Build-Ergebnis

- `dotnet restore` / `dotnet build` / `dotnet test` erfolgreich
- Kein 30-Minuten-Langzeittest ausgeführt

## 14. Commit-ID

Git-Tag `opcua-simulator-physical-simulation-ap3-final` → `9aa312e283f3b4ffd0618561cbef96220a95a289`

## 15. Tag

`opcua-simulator-physical-simulation-ap3-final`

## 16. Git-Status

Branch `feature/physical-learning-simulator`. Commit enthält R3-Kalibrierung und Nachweise. Verbleibende lokale Änderungen: Handoff-Archiv-Umstrukturierung, JSON-Profile in `MachineProfiles/`, AP-01-Bericht – nicht Teil dieses Commits.

## 17. Risiken für AP 4

- Korrelationen nahe oberer Grenze bei Lag 0 – AP 4 Fehlerszenarien sollten Lag-/Phasenlogik erneut prüfen
- Kurzmodus-TimeFactor beschleunigt Thermik – Langzeit-Thermal noch separat validieren
- Review-Schwellen für Grenz-Korrelationen fachlich dokumentiert

## 18. Freigabeempfehlung

**Freigabe für AP 3 R3 empfohlen.**

Der maschinenlesbare End-to-End-Nachweis meldet `Passed: true`. Alle Abschlusskriterien aus AP 3 R3 sind erfüllt. AP-4-Arbeiten (Fehlerszenarien, Ground Truth, VIGIL) wurden nicht begonnen.
