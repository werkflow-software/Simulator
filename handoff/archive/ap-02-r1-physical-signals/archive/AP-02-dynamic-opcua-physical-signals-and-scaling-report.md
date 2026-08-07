# AP-02 – Dynamische OPC-UA-Signale und Skalierungsnachweis

## 1. Ausgangsstand

AP 1 lieferte Core-Modelle, JSON-Loader, Validierung, Runtime-Factory und ein 20-Signal-Referenzprofil ohne OPC-UA-Veröffentlichung. Die Produktionssimulation blieb unverändert.

## 2. Branch und Commit

- **Branch:** `feature/physical-learning-simulator`
- **Basis-Commit (AP 1):** `63d922c`
- **AP-2-Arbeitsstand:** uncommitted (dieser Bericht dokumentiert den Implementierungsstand)

## 3. Git-Bereinigung

Aus dem Git-Index entfernt (Dateien lokal unverändert, nur `--cached`):

| Kategorie | Anzahl (ca.) |
|-----------|----------------|
| `.vs/` | 18 Dateien |
| `bin/` (alle Projekte, Debug/Release) | ~350 Dateien |
| `obj/` (alle Projekte) | ~140 Dateien |
| `publish/` | ~16 Dateien |

`.gitignore` war bereits vorhanden; die Artefakte stammten aus dem Baseline-Commit. Tag `opcua-simulator-before-physical-simulation-ap1` wurde nicht verändert.

## 4. Analysierte OPC-UA-Architektur

- `MachineServerService` → `MachineOpcUaHost` → `SimulatorServer` → `SimulatorNodeManager`
- Bestehende Produktionsnodes unter `Simulation/<Pfad aus NodeId>`
- Wertupdates über `SetVariableValue` + `ClearChangeMasks(SystemContext, false)`
- Maschinenspezifischer Namespace über `MachineConfiguration.NamespaceUri`

## 5. Neue Komponenten

```text
Werkflow.OpcUaSimulator.OpcUa/PhysicalSignals/
├── Mapping/PhysicalSignalTypeMapper.cs
├── Nodes/PhysicalSignalAddressSpaceBuilder.cs (+ IPhysicalSignalNodeFactory)
├── Publishing/PhysicalSignalPublisher.cs
├── Registry/PhysicalSignalNodeRegistry.cs
└── PhysicalSignalPublishingCoordinator.cs

Werkflow.OpcUaSimulator.Core/PhysicalSimulation/Services/
├── TechnicalSignalValueGenerator.cs
├── PhysicalMachineSessionFactory.cs
└── PhysicalMachineProfileJsonExporter.cs
```

## 6. Core-zu-OPC-UA-Typmapping

| Core-Typ | OPC-UA-Typ |
|----------|------------|
| Double | Double |
| Float | Float |
| Int32 | Int32 |
| Int64 | Int64 |
| Boolean | Boolean |
| String | String |
| DateTime | DateTime (UTC) |

Zentrale Komponente: `PhysicalSignalTypeMapper`. Ungültige Typen/Werte werfen `PhysicalProfileException` und stoppen nicht den gesamten Server.

## 7. NodeId- und Ordnerkonzept

- `SignalDefinition.NodeId` → `NodeId` im Maschinen-Namespace (z. B. `Axis01.MotorTemperature`)
- Hierarchie: `Simulation/Machine/Physical/<Pfadsegmente>/<Signal>`
- Ordner werden gecacht und nur einmal erzeugt
- Hidden States und Dependencies werden nicht veröffentlicht

## 8. Registry-Konzept

- Pro Maschine/Server-Instanz: `PhysicalSignalNodeRegistry`
- Lookup: SignalId → Node, NodeId → Node
- Doppelregistrierung wird abgelehnt
- `Clear()` beim Serverstopp

## 9. Publisher- und Scheduler-Konzept

- Ein Hintergrund-Loop pro Maschine (50 ms Poll)
- `_nextDue` pro SignalId respektiert individuelle `UpdateInterval`
- Pause/Resume/Stop über `PhysicalPublisherState`
- Metriken: Updates/s, Ø-Dauer, Max-Dauer, Fehler, übersprungene identische Werte
- **Technischer Testwertgenerator** (`TechnicalSignalValueGenerator`): deterministisch, innerhalb Normalbereich, keine Hidden States – **keine reale Physik**

## 10. Profilzuordnung

- `MachineConfiguration.PhysicalProfileId` (stabile ID, z. B. `technical-learning-machine-300`)
- Maschine 1 und 2: Standardzuordnung auf 300-Signal-Profil
- Maschine 3/4: kein physisches Profil (nur Produktionssimulation)
- Fehlendes Profil → kontrollierter Abbruch des Maschinenstarts

## 11. 300-Signal-Profil

- Datei: `Werkflow.OpcUaSimulator.App/MachineProfiles/TechnicalLearningMachine300.json`
- **307 sichtbare Signale** (Bereich 285–320)
- Kategorien: Production, Axis (6×), Drive, Thermal, Process, Cooling, Fluid, Electrical, Quality
- Realistische Normalbereiche (z. B. Motortemperatur 50–54 °C, Spindeldrehzahl 2950–3050 1/min)

## 12. UI-Erweiterungen

Neue Seite **Physikalische Signale**:

- Maschinen-/Profil-Diagnose, Publisher-Metriken
- Virtualisierte Signal-Tabelle mit Suche und Filtern
- Manueller Wert-Override (nur bei pausiertem Publisher oder aktivem Override)

## 13. DataChange-Nachweis

Implementierung:

- `entry.Variable.Value` setzen
- `Timestamp = DateTime.UtcNow`
- `StatusCode = Good`
- `ClearChangeMasks(SystemContext, false)`
- Identische Werte werden übersprungen (`SkippedIdenticalValues`)

Manuelle Prüfung mit UaExpert empfohlen (Subscription auf `Simulation/Machine/Physical/...`).

## 14. Performance-Messwerte

Lokal erfasst in `PhysicalPublisherMetrics` pro Maschine:

- PublishedSignalCount, UpdatesPerSecond
- AveragePublishDurationMs, MaxPublishDurationMs
- FailedUpdates, SkippedIdenticalValues

Automatisierte 10-Minuten-Langzeittests (Test A–E) sind als manuelle Betriebsprüfung vorgesehen; Unit-Tests decken Mapping, Profil, Generator, Registry-Trennung und Session-Factory ab.

## 15. Test A bis E

| Test | Beschreibung | Ergebnis |
|------|--------------|----------|
| A | 1 Maschine, ~300 Signale, 10 min | Manuell mit UaExpert / App-Start |
| B | 2 Maschinen, ~600 Signale | Architektur vorbereitet (M1+M2 mit Profil) |
| C | Pause/Fortsetzen | Implementiert, Unit-Logik getestet |
| D | Serverneustart | Registry-Clear + Rebuild implementiert |
| E | DataChange | ClearChangeMasks implementiert, UaExpert empfohlen |

## 16. Restore-, Build- und Testergebnis

```
dotnet restore  → OK
dotnet build    → 0 Fehler, 0 Warnungen
dotnet test     → 29/29 bestanden
```

## 17. Geänderte/neu hinzugefügte Dateien (Auswahl)

**Core:** `MachineConfiguration.cs`, `DefaultMachines.cs`, `IPhysicalSignalServices.cs`, `TechnicalSignalValueGenerator.cs`, `PhysicalMachineSessionFactory.cs`, `PhysicalPublisherMetrics.cs`, `TechnicalLearningMachine300ProfileFactory.cs`, `PhysicalMachineProfileJsonExporter.cs`, `SimulationEngine.cs`

**OpcUa:** `PhysicalSignals/*`, `SimulatorNodeManager.cs`, `MachineOpcUaHost.cs`, `MachineServerService.cs`

**App:** `PhysicalSignalsViewModel.cs`, `PhysicalSignalsView.xaml`, `App.xaml`, `MainViewModel.cs`, `TechnicalLearningMachine300.json`

**Tests:** `PhysicalSignalOpcUaTests.cs`

**Docs:** `README.md`, dieser Bericht

## 18. Bewusst nicht umgesetzt

- Echte Maschinenphysik, Hidden-State-Berechnung, Dependencies
- Fehlerszenarien, Drift, Recovery, Ground Truth
- EUInformation-Standardstruktur (DisplayName-Fallback)
- OPC-UA-Schreibzugriff für externe Clients
- Charts, Profil-Editor

## 19. Risiken und offene Punkte für AP 3

- `SignalRuntimeState.CurrentValue` ist numerisch (`double`); nicht-numerische Typen werden primär auf OPC-UA-Ebene gehalten
- Langzeit-Stabilität (10+ min, 2×300 Signale) sollte im Betrieb verifiziert werden
- EUInformation kann ergänzt werden, sobald der Stack `AnalogItem`-Typen unterstützt

## 20. Aktueller Git-Status

- Branch: `feature/physical-learning-simulator`
- Git-Bereinigung: staged deletions (`git rm --cached`) für bin/obj/.vs/publish
- AP-2-Implementierung: unstaged

## 21. Commit-ID

Noch nicht committed. Nach Commit bitte `git log -1` ergänzen.
