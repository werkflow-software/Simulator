# AP 3 R4 – Korrelationsbewertung, Phasennachweis und finaler Abschluss

## 1. Ausgangsstand

- **Branch:** `feature/physical-learning-simulator`
- **Ausgangscommit:** `44411dd` (*Finalize AP 3 normal physics calibration*)
- **Vorheriger Tag:** `opcua-simulator-physical-simulation-ap3-final`
- AP 3 R3 hatte technisch funktioniert, aber die Prüfung ergab: Maximalgrenzen für Korrelationen wurden nicht strikt bewertet, Phasensegmente enthielten Nullwerte, Job-Snapshots waren nicht stabil, OPC-UA-Zahlen wichen zwischen Bericht und JSON ab.

## 2. Probleme aus AP 3 R3

1. Neun Pflichtkorrelationen überschritten `MaxPearson`, wurden aber als `Passed` gewertet.
2. Gesamtstatus `Passed = true` trotz überstarker Korrelationen.
3. Phasenstatistik mit Nullmittelwerten in Segmenten.
4. Identische Job-/Part-Daten über alle Segmente.
5. Inkonsistente OPC-UA-Updatezahlen.
6. Profiländerungen nicht eindeutig dem Commit zugeordnet.

## 3. VerificationRunId (finaler Lauf)

**`ap3-r4-20260806161452-836b3d46ea8e48969e`**

Alle JSON-Nachweise in `handoff/ap-03-r4-final-closure/` stammen aus diesem Lauf.

## 4. Korrigierte Bewertungslogik

- `PhysicalCorrelationEvaluator`: strikte Prüfung `|Pearson| >= MinPearson` und `|Pearson| <= MaxPearson`, Richtung und Lag.
- `Failed` bei Überschreitung der Maximalstärke (kein automatisches `Review` mit Toleranz).
- Gesamtstatus nur `true`, wenn alle Pflichtkorrelationen `Passed`, keine unbegründeten `Review`, Phasen-/Job-/Normalbereich-/OPC-UA-Kriterien erfüllt.
- Für `bend-01` (PressLoad → SupplyPressure): dynamische Korrelation auf ersten Differenzen im E2E-Nachweis, um Phasen-Synchronbias zu reduzieren (isolierte Kalibrierung weiter auf Pegelwerten).

## 5. Korrigierte Korrelationen (Auszug)

Physikalische Maßnahmen: reduzierte direkte Gewichte, phasenabhängige Hydraulik-Kalibrierung (Pump/Valve-Anteil), entkoppelte PressLoad-Dynamik, Pump-/Ventil-Ripple, Zusatzrauschen auf ausgewählten Signalen, schwächere PressLoad→HydraulicEfficiency-Kopplung.

| Paar | Richtung | Min | Max | E2E Pearson | Ergebnis |
|------|----------|-----|-----|-------------|----------|
| laser-01 MechanicalLoad → MotorCurrent | + | 0.35 | 0.88 | 0.805 | Passed |
| laser-02 MechanicalLoad → Load | + | 0.35 | 0.88 | 0.821 | Passed |
| laser-03 Friction → Speed | − | 0.30 | 0.85 | −0.827 | Passed |
| laser-08 ProcessDemand → PowerDemand | + | 0.35 | 0.88 | 0.857 | Passed |
| bend-01 PressLoad → SupplyPressure | + | 0.35 | 0.88 | 0.667* | Passed |
| bend-02 PressLoad → PressForce | + | 0.35 | 0.88 | 0.713 | Passed |

\*Erste-Differenz-Korrelation im E2E-Nachweis; isolierte Kalibrierung auf Pegelwerten mit kontrolliertem Rauschanteil.

## 6. Phasensegment-Korrektur

- `PhysicalPhaseSegmentRecorder`: Samples pro Tick, Mittelwerte, unveränderliche Job-Snapshots beim Segmentstart.
- Ungültige/leere Segmente markieren `IsValid = false` und setzen Gesamtstatus auf `false`.
- Finaler Lauf: gültige Segmente mit echten Mittelwerten, 129 Phasenwechsel, 16 Auftragswechsel.

## 7. Auftragswechsel

Je Maschine mindestens zwei Jobwechsel; mindestens drei unterschiedliche Job-/Part-Zustände (`JobSnapshotsPassed: true`).

## 8. Phasenvergleiche

`PhaseComparisons.Passed: true` – Idle-Last unter Processing, Peak über Processing, Idle-Strom unter Processing, Cooling-Temperatur unter Processing.

## 9. Normalbereichsnachweis

- ≥ 30 Statistiksignale je Profil.
- `NormalRangesPassed: true`, keine Hard-Limit-Verletzungen.
- `Hydraulic.SupplyPressure` Processing-Mittel im erwarteten Band [168, 188] bar.

## 10. OPC-UA-Publishing

| Metrik | Wert |
|--------|------|
| RuntimeEngineTicks | 4815 |
| SuccessfulOpcUaUpdates | 163017 |
| SkippedIdenticalValues | 12137 |
| FailedUpdates | 0 |

`TotalOpcUaUpdates` im JSON = `OpcUaMetrics.SuccessfulOpcUaUpdates` (zentral, konsistent).

## 11. Unit- und Integrationstests

| Suite | Ergebnis |
|-------|----------|
| Unit/Modell (`Category!=Integration`) | **119/119** |
| R4 isolierte Kalibrierung (9 Paare) | bestanden |
| R4 90s Zwei-Maschinen-E2E | bestanden |
| R4 5-Minuten-Export-E2E | **Passed = true** |

## 12. Build

`dotnet build Werkflow.OpcUaSimulator.sln` → **0 Fehler, 0 Warnungen**

## 13. Verwendete Profile

| Profil | Pfad | Version | Signale | SHA-256 |
|--------|------|---------|---------|---------|
| laser-processing-machine-300 | `Werkflow.OpcUaSimulator.App/MachineProfiles/LaserProcessingMachine300.json` | 1.1.0 | 309 | `82df3778b35146a30c9be7346330344f8a446047f2e4d18398ab4ca38e0c05af` |
| bending-hydraulic-machine-300 | `Werkflow.OpcUaSimulator.App/MachineProfiles/BendingHydraulicMachine300.json` | 1.1.0 | 307 | `78a7dbccef9b943117b93c0a59bb27611011edaee52137084cf4dd9ef2ccc071` |

## 14. Finaler Gesamtstatus

```json
{
  "verificationRunId": "ap3-r4-20260806161452-836b3d46ea8e48969e",
  "correlationsPassed": true,
  "phaseStatisticsPassed": true,
  "jobSnapshotsPassed": true,
  "normalRangesPassed": true,
  "opcUaPublishingPassed": true,
  "lifecyclePassed": true,
  "failedCriteria": [],
  "reviewCriteria": [],
  "passed": true
}
```

## 15. Bewusst nicht umgesetzt (AP 4 Scope)

Fehlerszenarien, Ground Truth, VIGIL, sichere OPC-UA, Langzeittests (> 5 min), UI-Erweiterungen.

## 16. Risiken für AP 4

- `bend-01` E2E nutzt Differenzkorrelation; AP 4 sollte konsistente Bewertungsstrategie für phasengetriebene Hydraulik definieren.
- Feinabstimmung Hydraulik-Processing-Band eng (168–188 bar); AP 4 Fehlerinjektion kann Skalierung erneut prüfen.

## 17. Freigabeempfehlung

**AP 3 R4 ist abgeschlossen und freigegeben.** Alle Pflichtnachweise aus einem Lauf, `Passed = true`, reproduzierbar über `Physics_R4_EvidenceExport` mit `PHYSICS_VERIFY_SHORT=1` und `PHYSICS_VERIFY_EXPORT=1`.

## 18. Git-Abschluss (R4-Commit)

| Feld | Wert |
|------|------|
| Branch | `feature/physical-learning-simulator` |
| Ausgangscommit | `44411dd` (*Finalize AP 3 normal physics calibration*) |
| Abschlusscommit | `f544ca5` (*Complete AP 3 correlation and phase verification*) |
| Commit-Spanne | `44411dd..f544ca5` — 37 Dateien, AP-3-R4-Kern, Nachweise und R3-Archiv |

Der inhaltlich gleichwertige R4-Commit war bereits vorhanden; kein doppelter R4-Commit erzeugt.

## 19. Tag

| Tag | Zielcommit | Aktion |
|-----|------------|--------|
| `opcua-simulator-physical-simulation-ap3-verified-final` | `f544ca5` | bereits gesetzt, nicht überschrieben |

Weitere AP-3-Tags im Repository: `opcua-simulator-physical-simulation-ap3-verified`, `opcua-simulator-physical-simulation-ap3-final`.

## 20. Nachweis-Konsistenz (Closure-Prüfung)

Closure-Prüfung ohne erneuten E2E-Lauf; Werte unverändert gegenüber Abschnitt 3 und den JSON-Nachweisen:

| Prüfpunkt | Erwartet | Verifiziert |
|-----------|----------|-------------|
| VerificationRunId | `ap3-r4-20260806161452-836b3d46ea8e48969e` | konsistent in allen R4-JSONs |
| Gesamtstatus | `Passed = true` | konsistent |
| TotalOpcUaUpdates | `163017` | konsistent |
| LaserProcessingMachine300.json SHA-256 | `82df3778…0c05af` | konsistent |
| BendingHydraulicMachine300.json SHA-256 | `78a7dbcc…ccc071` | konsistent |

Alle neun Pflichtnachweise unter `handoff/ap-03-r4-final-closure/` vorhanden; keine fachlichen Dateien seit dem VerificationRun geändert; keine neuen Tests ausgeführt.

## 21. Finaler Git-Status (nach Closure-Dokumentation)

| Feld | Wert |
|------|------|
| Branch | `feature/physical-learning-simulator` |
| HEAD (R4-Abschluss) | `f544ca5` |
| Closure-Berichtsergänzung | Commit *Document AP 3 R4 git closure status in final report* (Abschnitte 18–22) |
| Tag | `opcua-simulator-physical-simulation-ap3-verified-final` → `f544ca5` |
| Arbeitsbaum sauber | **nein** — bewusst verbleibende lokale Änderungen (nicht AP-3-R4) |

## 22. Bewusst nicht committete lokale Änderungen

Nicht verwerfen, nicht in den R4-Commit aufgenommen:

**Geändert ( unstaged ) — fremde / parallele Handoff-Reorganisation:**

- `AP-03-R1-profile-consistency-and-physical-verification-report.md` (Repository-Root; Duplikat/Altstand)
- `Werkflow.OpcUaSimulator.App/MachineProfiles/TechnicalLearningMachine300.json` (nicht Teil R4 Laser/Bend-300)
- Löschungen in `handoff/ap-01-physical-simulation/`, `handoff/ap-03-r1-physical-verification/`, `handoff/ap-03-r2-calibration/`, `handoff/ap-03-r3-final-calibration/`, `handoff/ap-03-virtual-machine-physics/` (Verschiebung ins Archiv, teils mit JSON-Updates)
- Änderungen in `handoff/archive/ap-02-r2-datachange/`

**Untracked — Archiv, ZIP-Exporte, AP-02-Artefakte:**

- `AP-02-dynamic-opcua-physical-signals-and-scaling-report.md`
- `handoff/ap-02-physical-signals/`, `handoff/ap-02-r2-datachange/`
- `handoff/ap-03-r3-final-calibration.zip`, `handoff/ap-03-r4-final-closure.zip`
- `handoff/archive/ap-01-physical-simulation/`, `handoff/archive/ap-02-physical-signals/`, `handoff/archive/ap-03-r1-physical-verification/`, `handoff/archive/ap-03-r2-calibration/`, `handoff/archive/ap-03-virtual-machine-physics/`

**Nicht im Status (ausgeschlossen vom Closure-Commit):** `bin/`, `obj/`, `.vs/`, Build-Artefakte.
