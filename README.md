# Werkflow OPC UA Simulator

Testwerkzeug für **Werkflow inMotion**. Simuliert vier oder mehr industrielle Maschinen als unabhängige OPC-UA-Server mit konfigurierbaren Nodes, Aufträgen, Ereignissen und Testszenarien.

## Zweck

Die Anwendung ermöglicht es, OPC-UA-Clients (z. B. Werkflow inMotion oder UaExpert) gegen realistische Maschinendaten zu testen – inklusive Produktion, Fehlern, Stillständen und Verbindungsabbrüchen.

## Voraussetzungen

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (oder neuer) mit WPF-Workload
- Optional: UaExpert oder Werkflow inMotion als OPC-UA-Client

## Projektaufbau

| Projekt | Beschreibung |
|---------|--------------|
| `Werkflow.OpcUaSimulator.App` | WPF-Oberfläche (MVVM, DI) |
| `Werkflow.OpcUaSimulator.Core` | Modelle, Simulationslogik, Konfiguration |
| `Werkflow.OpcUaSimulator.OpcUa` | OPC-UA-Server (OPC Foundation Stack) |
| `Werkflow.OpcUaSimulator.Tests` | Unit-Tests |

## Start in Visual Studio

1. `Werkflow.OpcUaSimulator.slnx` öffnen
2. Startprojekt: `Werkflow.OpcUaSimulator.App`
3. **F5** drücken

## Start über dotnet

```bash
dotnet restore
dotnet build
dotnet run --project Werkflow.OpcUaSimulator.App
```

## Standardendpoints

| Maschine   | Endpoint                 |
| ---------- | ------------------------ |
| Maschine 1 | opc.tcp://localhost:4840 |
| Maschine 2 | opc.tcp://localhost:4841 |
| Maschine 3 | opc.tcp://localhost:4842 |
| Maschine 4 | opc.tcp://localhost:4843 |

## Virtual Machine HMI

Maschine 1 ist die primäre virtuelle Produktionsmaschine für realitätsnahe inMotion- und spätere VIGIL-Lernversuche. Der Simulator kennt VIGIL nicht; die Maschine wird ausschließlich über OPC UA bereitgestellt.

| Eigenschaft | Wert |
|-------------|------|
| Maschine | Werkflow Virtual Laser 01 |
| Endpoint | `opc.tcp://localhost:4840` |
| Security | None / Anonymous |
| Profil | LaserProcessingMachine300 |
| MachineId | stabil (`a1111111-1111-4111-8111-111111111111`) |

Die **Virtuelle Maschine**-Schaltfläche im Hauptfenster öffnet die HMI-Bedienoberfläche (`VirtualMachineHmiWindow`). Die HMI liest Livewerte aus derselben Runtime wie OPC UA und Physical Simulation — keine separate Zustandsmaschine.

**Tray / Fenster:** Schließen über das X versteckt nur die HMI; Maschine und OPC-UA-Server laufen weiter. Ein explizites **Maschine beenden** mit Sicherheitsabfrage stoppt Simulation und Server.

### inMotion-Testablauf

1. OPC-UA-Simulator starten
2. **Virtuelle Maschine** öffnen
3. **Werkflow Virtual Laser 01** starten (falls noch nicht aktiv)
4. inMotion starten
5. inMotion mit `opc.tcp://localhost:4840` verbinden
6. OPC-UA-Signale konfigurieren
7. Normalbetrieb beobachten
8. Bei Bedarf FaultScenario ausschließlich im Simulator-HMI (Bereich **SIMULATION / TEST**) starten

### VIGIL-Grundsatz

> Der Simulator besitzt keine direkte VIGIL-Anbindung.
> VIGIL verarbeitet die Maschine später ausschließlich über den regulären inMotion-Datenpfad.
> Ground Truth und FaultScenario-Informationen dürfen niemals an inMotion/VIGIL übertragen werden.

Vertrag und Details: `docs/virtual-machine-contract.md`

## Standardnodes

| Semantik | Standard-NodeId | Datentyp |
|----------|-----------------|----------|
| Teilename | Machine.PartName | String |
| Jobname | Machine.JobName | String |
| Fehler aktiv | Machine.ErrorActive | Boolean |
| Fehlermeldung | Machine.ErrorMessage | String |
| Istzähler | Machine.ActualCounter | Int32 |
| Sollzähler | Machine.TargetCounter | Int32 |
| Maschinenstatus | Machine.MachineState | Int32 |
| Heartbeat | Machine.Heartbeat | UInt64 |
| Produktionsfortschritt | Machine.LastProductionChange | DateTime |

**Maschinenstatus-Werte:** 0=Offline, 1=Bereit, 2=Produziert, 3=Warnung, 4=Fehler, 5=Pausiert, 6=Rüsten

## Verbindung mit UaExpert

1. Simulation starten oder einzelne Server unter **Maschinen** starten
2. In UaExpert: Server → Add → `opc.tcp://localhost:4840`
3. Security: **None**, Anonymous
4. Nodes unter `Objects → Simulation → Machine` lesen

## Verbindung mit Werkflow inMotion

Jede Maschine als eigene Anlage mit dem jeweiligen Endpoint hinzufügen. NodeIds können in der Oberfläche an die inMotion-Konfiguration angepasst werden.

## Die vier Standardmaschinen

| Maschine | Profil |
|----------|--------|
| Maschine 1 | Schnell (~500–1500 ms), geringe Fehlerrate |
| Maschine 2 | Normal (~2–5 s), gelegentliche Warnungen |
| Maschine 3 | Langsam (~5–12 s), Stillstände bei laufendem Server |
| Maschine 4 | Störanfällig, hohe Fehler- und Offline-Rate |

## Testszenarien

Unter **Übersicht → Testszenarien** verfügbar:

1. **Normale Produktion** – alle Maschinen produzieren fehlerfrei
2. **Zählerstillstand** – Heartbeat läuft, Zähler steht
3. **Maschinenfehler** – ErrorActive, Fehlertext, Status Error
4. **Keine Verbindung** – Server wird gestoppt und wieder gestartet
5. **Auftrag fast fertig** – Istzähler bei 90 %
6. **Auftrag abgeschlossen** – Sollzähler erreicht
7. **Wechselnde Störungen** – zufällige Mischung

## Speicherort der Konfiguration

```
%LocalAppData%\Werkflow\OpcUaSimulator\
├── settings.json
├── machines.json
├── jobs.json
└── error-messages.json
```

## Sicherheitshinweis

Der Standardendpoint verwendet **SecurityMode: None** und anonymen Zugriff – ausschließlich für lokale Test- und Entwicklungsumgebungen.

## Bekannte Einschränkungen

- NodeId-Änderungen erfordern einen Serverneustart
- Sichere Endpoints (Sign & Encrypt) sind vorbereitet, aber nicht standardmäßig aktiv
- Client-Anzahl basiert auf aktiven OPC-UA-Sessions
- Reproduzierbarkeit des Zufallsmodus hängt von Seed und Konfiguration ab

## Physikalische Langzeitsimulation

Ab AP 1 besitzt der Simulator eine zusätzliche, klar getrennte Ebene für künftige physikalische Maschinensimulation (Ziel: ca. 300 OPC-UA-Signale pro Maschine, versteckte Abhängigkeiten, kontrollierte Fehlerverläufe).

### Zweck

Die neue Architektur bereitet deklarative Maschinenprofile, sichtbare physikalische Signale, interne Prozesszustände und Signalabhängigkeiten vor. Die bestehende Produktionssimulation (Aufträge, Zähler, ErrorActive, Heartbeat, Testszenarien) bleibt unverändert und wird nicht ersetzt.

### Trennung

| Ebene | Inhalt |
|-------|--------|
| Bestehende Produktionssimulation | Jobs, PartName, ActualCounter/TargetCounter, ErrorActive, MachineState, Heartbeat, Szenarien |
| Physical Simulation (neu) | JSON-Maschinenprofile, Signale, HiddenProcessStates, Dependencies, Normalbereiche, Rauschen, Trägheit |

### Maschinenprofile als JSON

Profile liegen unter:

```text
Werkflow.OpcUaSimulator.App/MachineProfiles/ReferenceMachine.json
Werkflow.OpcUaSimulator.App/MachineProfiles/TechnicalLearningMachine300.json
```

Beim Build werden sie nach `MachineProfiles/` im Ausgabeverzeichnis kopiert. Laden und Validierung erfolgen über `IPhysicalMachineProfileLoader` / `IPhysicalMachineProfileValidator` (Core, ohne WPF/OPC-UA-Abhängigkeiten).

### Sichtbare Signale vs. interne Ground Truth

- **Sichtbare Signale** (`SignalDefinition`): werden ab AP 2 als OPC-UA-Nodes unter `Simulation/Machine/Physical/...` veröffentlicht.
- **Versteckte Prozesszustände** und **Abhängigkeiten**: ausschließlich interne Simulatorinformationen. Sie werden **nicht** über OPC UA veröffentlicht.

### Stand nach AP 2

- Dynamische OPC-UA-Node-Erzeugung aus Maschinenprofilen (`PhysicalSignals/` in OpcUa-Projekt)
- Core → OPC-UA-Typmapping (`PhysicalSignalTypeMapper`)
- Maschinenbezogene Registry und Publisher mit gruppiertem Scheduler (ein Loop pro Maschine)
- DataChange Notifications über `ClearChangeMasks`
- Profilzuordnung je Maschine über `PhysicalProfileId` in `MachineConfiguration`
- Skalierungsprofil `TechnicalLearningMachine300.json` (308 Signale, Version 1.1.0)
- Maschine 1 und 2 verwenden standardmäßig das 300-Signal-Profil
- Diagnose-UI **Physikalische Signale** (Metriken, Suche, Filter, Virtualisierung)
- Technischer Testwertgenerator `TechnicalSignalValueGenerator` (weiterhin verfügbar für Diagnose und AP-2-Regression)
- DataChange-Notifications für Continuous-, Counter- und Override-Signale praktisch verifiziert (AP 2 R2)

### AP 3 – Virtuelle Maschinenphysik (verifiziert, AP 3 R1)

- **Generierungsmodi je Maschine:** `Technical`, `Physical`, `Manual`
- **Zwei physikalische Maschinenprofile (285–320 Signale):**
  - `LaserProcessingMachine300` – **309 Signale** (Maschine 1)
  - `BendingHydraulicMachine300` – **307 Signale** (Maschine 2), Version **1.1.0**
- **12 Hidden Process States** je Profil – intern, **nicht über OPC UA**
- **≥15 Hidden-State-Dependencies** je Profil (JSON-Feld `hiddenStateDependencies`)
- **≥5 Dependency-Typen** je Profil (Linear, InverseLinear, DelayedLinear, Threshold, Saturating, Polynomial, Sigmoid, PiecewiseLinear, RateLimited, Hysteresis)
- **Effektgrenzen:** `minimumEffect`/`maximumEffect` = `null` → keine Grenze; numerischer Wert = aktive Begrenzung; `0`/`0` = Nullwirkung
- **Langzeittest Kurzmodus:** 90 s Integrationstests; **Vollmodus:** `PHYSICS_VERIFY_FULL=1` → 30 min + 30 min mit JSON-Nachweisen unter `handoff/ap-03-r1-physical-verification/`
- Diagnose-UI zeigt zusätzlich Signal- und Hidden-State-Dependency-Anzahl

Versteckte Prozesszustände und Abhängigkeiten werden **nicht** über OPC UA publiziert.

### AP 4 – Kontrollierte Fehlerszenarien (verifiziert)

- **22 deklarative Fehlerszenarien** unter `FaultScenarios/` (Laser, Bending, Shared, Technical)
- **Manuelle Steuerung** in der UI (Start, Pause, Fortsetzen, Beenden, Reset, Intensität, Zeitfaktor)
- **Wirkung über Hidden States** – keine direkte OPC-UA-Node-Manipulation für physikalische Fehler
- **Grenzwertfehler** über bestehende Standardnodes (`ErrorActive`, `ErrorMessage`, `MachineState`)
- **Recovery** mit zeitversetzter Rückkehr; **CommunicationDrop** nur für Zielmaschine
- **Kontrollläufe** (`NonFaultingControlRun`) für 5 Szenarien (AP 5 Vorbereitung)
- **Events** für Ground-Truth-Recorder (ScenarioStarted, ThresholdReached, RecoveryCompleted, …)
- Nachweise: `handoff/ap-04-fault-scenarios/`, VerificationRunId `ap4-20260806200140-1933baa2a2c14238a8a51`

Szenarioinformationen und interne Ursachen werden **nicht** über OPC UA veröffentlicht.

### Noch nicht implementiert (AP 5+)

- Ground Truth, VIGIL-Lernbewertung, automatische False-Positive-Bewertung
- Charts, vollständiger Profil-Editor

### Hierarchische Node-Struktur

Beispiel `Axis01.MotorTemperature`:

```text
Objects
└── Simulation
    ├── Production/...        (bestehende Standardnodes)
    └── Machine
        └── Physical
            └── Axis01
                └── MotorTemperature
```

### Aktualisierungsintervalle

Jedes Signal besitzt ein eigenes `UpdateInterval`. Der Publisher prüft fällige Signale in einem gemeinsamen Hintergrund-Loop (50 ms Takt) – keine 300 Einzelthreads.

### Publisher-Lifecycle

| Aktion | Verhalten |
|--------|-----------|
| Simulation starten | Profil laden → Nodes erzeugen → Publisher starten |
| Pause | Publisher pausiert, Werte bleiben, Server online |
| Fortsetzen | Publisher läuft weiter, kein Doppelstart |
| Stop / Einzelserver-Stop | Publisher beendet, Registry geleert |

### Engineering Units

Engineering Units werden im DisplayName angezeigt (`[°C]`, `[bar]` …), da der verwendete OPC-UA-Stack keine `EngineeringUnits`-Property auf `BaseDataVariableState` bereitstellt.

## NuGet-Pakete

- `OPCFoundation.NetStandard.Opc.Ua.Server`
- `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.Hosting`
- `System.Text.Json`
