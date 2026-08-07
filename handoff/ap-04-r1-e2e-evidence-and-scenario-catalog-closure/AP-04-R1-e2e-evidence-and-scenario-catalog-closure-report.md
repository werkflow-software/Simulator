# AP 04 R1 – E2E-Evidence, Szenariokatalog-Abschluss und Repository-Pfadprüfung

**Datum:** 2026-08-07  
**Verbindlicher Arbeitsordner:** `C:\WerkFlow\Coding\Simulation`  
**VerificationRunId (finaler E2E):** *nicht erzeugt – siehe Blocker unten*  
**Gesamtstatus:** **Passed = false**

---

## 1. Neuer Arbeitsordner

| Prüfpunkt | Ergebnis |
|-----------|----------|
| Ordner existiert | **Ja** – `C:\WerkFlow\Coding\Simulation` |
| README vorhanden | **Ja** |
| Handoff-Struktur | **Ja** – `handoff/` mit AP-02 bis AP-04 und `archive/` |
| Testprojekt (Quellcode) | **Ja** – `Werkflow.OpcUaSimulator.Tests/` (25 `.cs`, inkl. AP-4-Harness) |
| FaultScenarios (JSON) | **Ja** – 22 Dateien unter `Werkflow.OpcUaSimulator.App/FaultScenarios/` |
| MachineProfiles | **Teilweise** – `LaserProcessingMachine300.json`, `BendingHydraulicMachine300.json`, `TechnicalLearningMachine300.json` (lokal geändert) |
| Solution-Datei | **Fehlt** – `Werkflow.OpcUaSimulator.sln` nicht vorhanden |
| Core-/OpcUa-/Tests-`.csproj` | **Fehlen** (nur `Werkflow.OpcUaSimulator.App.csproj`) |
| Vollständiges Quell-Repository | **Nein** – siehe Abschnitt 2 |

---

## 2. Einmalige Repository-Pfadprüfung

### 2.1 Git-Stand

| Prüfpunkt | Ergebnis |
|-----------|----------|
| `.git` vorhanden | **Ja** |
| Branch | `feature/physical-learning-simulator` |
| HEAD / Ausgangscommit | `f0b768d` – *Implement AP 4 controlled fault scenarios* |
| Tags (lokal) | `opcua-simulator-fault-scenarios-ap4-complete`, `opcua-simulator-physical-simulation-ap3-final`, `opcua-simulator-physical-simulation-ap3-verified-final` |
| Remote konfiguriert | **Nein** – `.git/config` enthält keine `remote` |
| `.gitignore` im Working Tree | **Fehlt** (als gelöscht gegen HEAD markiert) |
| Git-Objekte vollständig | **Nein** – `git restore` / `git cat-file` für Blob-Objekte schlägt fehl (`unable to read sha1 file`, nur ~186 lose Objekte, keine Pack-Dateien) |

**Fazit:** Das Verschieben nach `C:\WerkFlow\Coding\Simulation` hat nicht nur den Pfad geändert, sondern einen **unvollständigen Working Tree** und eine **beschädigte Git-Objekt-Datenbank** hinterlassen. `git checkout`/`restore` aus `f0b768d` ist **nicht möglich**, weil die Blob-Inhalte fehlen.

### 2.2 Fehlende / unvollständige Projektbestandteile

| Bereich | Status |
|---------|--------|
| `Werkflow.OpcUaSimulator.sln` | fehlt |
| `Werkflow.OpcUaSimulator.Core.csproj` | fehlt |
| `Werkflow.OpcUaSimulator.OpcUa.csproj` | fehlt |
| `Werkflow.OpcUaSimulator.Tests.csproj` | fehlt |
| Core-Quellcode | **~26 `.cs`** (AP-4 FaultScenarios + Teil-Physik), statt vollständigem Core |
| OpcUa-Quellcode | **1 `.cs`** (`PhysicalSignalPublishingCoordinator.cs`); `PhysicalSignals/Mapping`, `Publishing`, `Registry`, `PhysicalMachineSessionFactory` usw. **fehlen** |
| App-Quellcode | **5 `.cs`** + AP-4 ViewModels; MainWindow, Views, Services **fehlen** |
| Vorcompilierte DLLs (`App/bin/Release`) | vorhanden, aber **veraltet** (kein AP-4 FaultScenarios in `Core.dll`, `OpcUa.dll` nur ~30 KB) |
| Vorcompilierte Tests (`Tests/bin/Release`) | nur **16 Tests** (kein AP-4-Suite) |

### 2.3 Hart codierte / veraltete Pfade

| Fund | Typ | Bewertung |
|------|-----|-----------|
| `handoff/ap-04-fault-scenarios/AP-04-scenario-catalog-validation.json` | `C:\Users\tobib\OneDrive\Desktop\WerkFlow\Coding\Simulation\...` | **Veralteter Entwicklerpfad** in Evidence-JSON |
| `obj/**/*.nuget.dgspec.json`, `*.FileListAbsolute.txt` | OneDrive-Pfad | Build-Artefakte – nicht fachlich, bei Neu-Build obsolet |
| `PhysicalAp4VerificationHarness.EvidenceDirectory` | relativ `handoff/ap-04-fault-scenarios` | **Korrekt** (relativ) |
| `PhysicalTestServiceFactory.ResolveFaultScenariosDirectory` | relative Suchpfade | **Korrekt** |

**Keine fachlichen Pfadanpassungen im Quellcode erforderlich** – sobald das Repository wieder vollständig ist. Evidence-JSON mit absolutem OneDrive-Pfad sollte bei R1-Nachweisen bereinigt werden.

### 2.4 Verifikation nach Pfadwechsel

```text
dotnet restore                    → MSB1003 (keine Solution)
dotnet build App.csproj           → 80+ Fehler (fehlende Core/OpcUa-Projekte und Quellen)
dotnet test (vorcompilierte DLL)  → 16/16 bestanden (alte Test-Suite, ohne AP-4)
```

**Pfadmigration ist nicht abgeschlossen.** Build und AP-4-R1-Verifikation sind blockiert.

### 2.5 Vorgenommene Git-/Pfadanpassungen

- **Keine fachlichen Commits** – AP 4 R1 konnte nicht abgeschlossen werden.
- **Keine Historie umgeschrieben**, keine Tags verändert.
- Temporäre Analyse-Artefakte: `_recovery/` (Decompile), `_zip_extract_*` – **nicht** für Commit vorgesehen.

---

## 3. Ausgangsstand AP 4 (Referenz `f0b768d`)

AP 4 war auf dem vollständigen Stand laut Tag `opcua-simulator-fault-scenarios-ap4-complete` umgesetzt:

- 22 deklarative Szenarien, Lifecycle, Recovery, CommunicationDrop, NonFaultingControlRun
- Unit-/Integrationstests und kurzer E2E-Harness (`PhysicalAp4VerificationHarness`)
- Handoff `handoff/ap-04-fault-scenarios/`

**Bekannte Evidence-Lücken (R1-Auftrag):** ErrorActive/ErrorMessage/MachineState-Timelines, Threshold-Zeitpunkte, echte Endpoint-Prüfung bei CommunicationDrop, vollständiger Recovery-Nachweis, komplexe DirectionChecks, Szenario-JSONs im Handoff, finaler R1-Commit.

---

## 4. Geschützte lokale Änderungen (nicht gestaged/committed)

| Datei / Bereich | Status |
|-----------------|--------|
| `Werkflow.OpcUaSimulator.App/MachineProfiles/TechnicalLearningMachine300.json` | **M** (lokal) |
| `AP-03-R1-profile-consistency-and-physical-verification-report.md` | **M** |
| Diverse `handoff/ap-03-*` JSONs | **M** |
| `bin/`, `obj/` | untracked |
| `_recovery/`, `_zip_extract_*` | untracked (Analyse) |

Diese wurden **nicht** automatisch bereinigt, gestaged oder committed.

---

## 5. In AP 4 R1 erledigte Teilarbeiten

| Aufgabe | Status |
|---------|--------|
| Einmalige Pfad-/Repository-Prüfung dokumentiert | **Erledigt** (dieser Bericht) |
| Szenario-JSONs im Handoff | **Erledigt** – `handoff/ap-04-r1-e2e-evidence-and-scenario-catalog-closure/FaultScenarios/` |
| `AP-04-R1-scenario-manifest.json` | **Erledigt** – 22 Dateien, ManifestHash siehe unten |
| `fault-scenario-format.md` | **Kopiert** aus AP-4-Handoff |
| Threshold-/ErrorNode-/E2E-/Recovery-/CommunicationDrop-Nachweise | **Nicht erledigt** (Build blockiert) |
| Erweiterte Unit-/Integrationstests | **Nicht erledigt** |
| Finaler Git-Commit / Tag `opcua-simulator-fault-scenarios-ap4-verified` | **Nicht erledigt** |

---

## 6. Szenario-Manifest

| Kennzahl | Wert |
|----------|------|
| Szenario-Dateien | 22 |
| Eindeutige ScenarioIds | 22 |
| ManifestHash (SHA-256) | `169911a4ed45eeff9a0c337a9b959e7c71ae8c9e3cf83960f560326d67d88a9f` |
| VerificationRunId im Manifest | `ap4-r1-path-check-only-no-e2e` (kein finaler E2E-Lauf) |

Datei: `handoff/ap-04-r1-e2e-evidence-and-scenario-catalog-closure/AP-04-R1-scenario-manifest.json`

---

## 7. Profile und Hashes (aktueller Working Tree)

| Profil | SHA-256 |
|--------|---------|
| `LaserProcessingMachine300.json` | `344c73153e1ad3bd2127864182c26961792f1fb47b291d7e97ae3982fe4c8583` |
| `BendingHydraulicMachine300.json` | `78a7dbccef9b943117b93c0a59bb27611011edaee52137084cf4dd9ef2ccc071` |
| `TechnicalLearningMachine300.json` (lokal geändert) | `7ba6d8a51e0ac56f3972ccef5dac286c1910f215d02bb87a93d7cdbdac44971f` |

---

## 8. Offene AP-4-R1-Nachweise (alle blockiert)

Ohne vollständiges Repository und erfolgreichen Build können folgende Pflichtnachweise **nicht** erzeugt werden:

1. Threshold-Zeitpunkt (`ScenarioStartedAtUtc`, `ThresholdFirstReachedAtUtc`, …)
2. ErrorActive / ErrorMessage / MachineState Zeitreihen
3. Server-online bei physischem Fehler (Endpoint-Reachability)
4. Echter CommunicationDrop (Ziel-Endpoint unreachable, andere reachable)
5. Vollständiger Recovery-Nachweis (Laser + Biegen)
6. Komplexe Szenario-Detailtests (6 Szenarien)
7. NonFaultingControlRun-Nachweis
8. Error-Priorität automatisierter Test
9. Finaler 3–5-Minuten-E2E mit einheitlicher `VerificationRunId`
10. Erweiterte Unit-/Integrationstests (21 Punkte aus AP-4-R1-Spezifikation)

---

## 9. Restore/Build/Test

| Schritt | Ergebnis |
|---------|----------|
| `dotnet restore` | **Fehlgeschlagen** – keine Solution |
| `dotnet build` | **Fehlgeschlagen** – 80+ Compilerfehler |
| `dotnet test` (Rebuild) | **Nicht ausführbar** |
| `dotnet test` (alte `Tests.dll`) | 16 Tests bestanden (nicht AP-4-relevant) |

---

## 10. Git-Abschluss (geplant, nicht ausgeführt)

| Feld | Wert |
|------|------|
| Branch | `feature/physical-learning-simulator` |
| Ausgangscommit | `f0b768d` |
| Finaler AP-4-R1-Commit | **fehlt** |
| Tag `opcua-simulator-fault-scenarios-ap4-verified` | **nicht gesetzt** |
| Arbeitsbaum sauber | **Nein** – hunderte `D`-Einträge vs. HEAD, untracked `bin/obj`, Recovery-Ordner |
| Empfohlene Commit-Message (bei Recovery) | `Complete AP 4 scenario evidence and verification` |

---

## 11. Recovery – empfohlene Schritte

1. **Vollständige Quelle wiederherstellen** aus einer der folgenden Quellen (Priorität):
   - Backup des alten Pfads `C:\Users\tobib\OneDrive\Desktop\WerkFlow\Coding\Simulation` (falls noch verfügbar: Cloud-Rückgängig, andere Rechner, Backup-Software)
   - Remote-Repository (falls vorhanden) neu klonen nach `C:\WerkFlow\Coding\Simulation`
   - Vollständiges Handoff-ZIP mit Quellcode (aktuell **nicht** vorhanden)

2. **Git-Repository reparieren oder neu klonen:**
   ```powershell
   # Wenn Remote verfügbar:
   cd C:\WerkFlow\Coding
   Rename-Item Simulation Simulation_broken
   git clone <REMOTE_URL> Simulation
   git checkout feature/physical-learning-simulator
   ```

3. **Lokale geschützte Dateien** aus `Simulation_broken` selektiv zurückkopieren (nur bewusst gewollte Änderungen).

4. **Verifikation:**
   ```powershell
   dotnet restore
   dotnet build
   dotnet test
   ```

5. **AP 4 R1 erneut starten** – Evidence-Lauf mit einheitlicher `VerificationRunId`, dann Commit nur AP-4-R1-Dateien.

**Teil-Recovery per Decompile** (`_recovery/core_ap4` aus `Core.dll`) deckt nur die **vor-AP-4** Basis ab; AP-4 FaultScenarios und OpcUa `PhysicalSignals` sind in den vorhandenen DLLs **nicht enthalten** und können so nicht rekonstruiert werden.

---

## 12. Bewusst nicht umgesetzte AP-5-Punkte

Wie spezifiziert: keine Ground-Truth-Datenbank, keine VIGIL-Auswertung, keine False-Positive-Bewertung, keine AP-5-Arbeiten.

---

## 13. Risiken für AP 5

| Risiko | Beschreibung |
|--------|--------------|
| Repository-Integrität | Ohne vollständigen Stand keine reproduzierbare Evidence |
| Fehlende Remote | Kein Offsite-Backup der Git-Objekte |
| Veraltete DLLs | Vorcompilierte Artefakte täuschen AP-4-Funktionalität vor |
| Lokale Profil-Drift | `TechnicalLearningMachine300.json` geändert |

---

## 14. Eindeutiger Gesamtstatus

```json
{
  "passed": false,
  "reason": "Repository nach Verschiebung unvollständig; Git-Blobs fehlen; Solution/Projekte/Quellcode nicht buildfähig; finaler E2E und Evidence nicht ausführbar",
  "pathMigrationComplete": false,
  "partialDeliverables": [
    "AP-04-R1-scenario-manifest.json",
    "FaultScenarios/*.json im Handoff",
    "Dieser Abschlussbericht"
  ]
}
```

**AP 4 R1 ist nicht abgeschlossen.** Nach Wiederherstellung des vollständigen Repositorys kann die Evidence- und Verifikationsarbeit ohne erneute Pfadmigration fortgesetzt werden.
