# AP 4 – Kontrollierte Fehlerszenarien und manuelle Szenariosteuerung

## 1. Ausgangsstand

- Branch: `feature/physical-learning-simulator`
- AP-3-R4-Commit: `f544ca5`, Closure: `7a4e390`
- Tag: `opcua-simulator-physical-simulation-ap3-verified-final`
- Normalphysik, Profile und OPC-UA-Veröffentlichung unverändert funktionsfähig

## 2. Geschützte lokale Änderungen

Nicht committet: `TechnicalLearningMachine300.json`, Root-R1-Bericht, Handoff-Verschiebungen, ZIP-Dateien, untracked Archivordner.

## 3. Architektur

Kette: `FaultScenario → Hidden State → Physical Engine → Signals → OPC-UA Publisher`

Core-Bereich `PhysicalSimulation/FaultScenarios/` ohne WPF/OPC-UA-Typen.

## 4. Komponenten

`IFaultScenarioService`, `FaultScenarioEngine`, `FaultEffectCalculator`, `FaultRecoveryEngine`, `FaultScenarioValidator`, `JsonFaultScenarioRepository`, `FaultScenarioSimulationBridge`

## 5–11. Szenariomodell

Deklaratives JSON mit Phasen (Initiating→Faulted), Effekten, Grenzwertregeln, Recovery, Intensität, Zeitfaktor, Parallelität (max. 3).

## 12. Szenariokatalog (22)

Laser: stiff-linear-guide, lubricant-shortage, bearing-degradation, imbalance, optics-contamination, focus-drift  
Bending: hydraulic-leak, valve-delay, oil-aging, tool-deflection  
Shared: laser-overheating-axis-drive, coolant-loss, filter-contamination, pump-wear, fan-degradation, material-resistance-increased, tool-wear, power-instability, sensor-drift, intermittent-fault  
Technical: communication-drop, signal-freeze

## 17. Kontrollläufe

5 Szenarien mit `supportsNonFaultingControlRun`: Überhitzung, Kühlmittelverlust, Materialwiderstand, Netzinstabilität, Lüfterdegradation.

## 18. UI

Neue Navigation **Fehlerszenarien**: Liste, Steuerung, Laufzeitinfo. Maschinenkarten zeigen aktive Szenarien und Schwere.

## 19–22. Tests

| Test | Ergebnis |
|------|----------|
| Katalogvalidierung | 22/22, Passed |
| Modelltests | Passed |
| Lifecycle | Passed |
| Kombination | Passed |
| Recovery | Passed |
| Kurz-E2E | Passed |

## 23. OPC-UA-Nachweis

VerificationRunId: `ap4-20260806200140-1933baa2a2c14238a8a51`  
TotalOpcUaUpdates: 113917, Passed: true

## 26. Build/Test

127/127 Unit-Tests (ohne Integration), Build 0 Fehler.

## 30. Bewusst nicht umgesetzt (AP 5)

VIGIL-Bewertung, Ground-Truth-Auswertung, Trainingsserien, Langzeittests, sichere OPC-UA.

## 35. Freigabeempfehlung

**AP 4 ist abgeschlossen und freigegeben.** 22 Szenarien, manuelle Steuerung, Grenzwertfehler, Recovery, kurzer E2E `Passed = true`.
