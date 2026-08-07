# RECOVERY 01 – Simulator Repository Restoration Report

**Date:** 2026-08-07  
**Target path:** `C:\WerkFlow\Coding\Simulation`  
**Status:** **Completed** (build + test verified; Git object database remains damaged)

---

## 1. Ausgangsproblem

Nach Verschieben des Projektordners nach `C:\WerkFlow\Coding\Simulation` war der Working Tree unvollständig:

- `Werkflow.OpcUaSimulator.sln` und mehrere `.csproj` fehlten
- Große Teile des Quellcodes fehlten
- `.git` vorhanden, aber Objektdatenbank unvollständig (keine Pack-Dateien, viele fehlende Blobs/Trees)
- `git restore` / `cat-file` scheiterten
- Kein Remote-Repository konfiguriert
- Build nicht möglich

---

## 2. Neuer Projektpfad

Verbindlicher Entwicklungsordner:

```text
C:\WerkFlow\Coding\Simulation
```

---

## 3. Gefundene Recovery-Quellen

| Pfad | Typ | Änderungsdatum | Größe (approx.) | Solution | Core.csproj | OpcUa.csproj | Tests.csproj | .git | HEAD | Branch | AP-4-Code |
|------|-----|----------------|-----------------|----------|-------------|--------------|--------------|------|------|--------|-----------|
| `C:\WerkFlow\Coding\Simulation` (defekt) | Arbeitskopie | 07.08.2026 | partiell | nein | nein | nein | nein | ja (defekt) | f0b768d | feature/physical-learning-simulator | partiell (DLLs) |
| `C:\Users\tobib\OneDrive\Desktop\WerkFlow\Coding\Simulation` | Arbeitskopie | — | — | **nicht gefunden** | — | — | — | — | — | — | — |
| `C:\WerkFlow\Coding\RecoveryBackup\Simulation_broken_20260807_091417` | Sicherung defekter Stand | 07.08.2026 | vollständige Kopie | nein | nein | nein | nein | ja (defekt) | f0b768d | feature/physical-learning-simulator | partiell |
| Debug/Release `bin` (defekter Stand) | Build-Artefakte | 06.08.2026 | Core ~434 KB, Tests ~502 KB | — | — | — | — | — | — | — | ja (in DLLs) |
| `handoff/ap-04-r1-e2e-evidence-and-scenario-catalog-closure/FaultScenarios/` | JSON-Szenarien | 07.08.2026 | 22 Dateien | — | — | — | — | — | — | — | Szenario-Definitionen |
| `handoff/archive/` | Archivierte AP-Berichte/Evidence | 06–07.08.2026 | mehrere MB | — | — | — | — | — | — | — | teilweise |
| Lokale Laufwerke (`C:\Users\tobib\OneDrive`, `C:\WerkFlow`, `D:\`, Desktop, Documents) | Dateisuche | — | — | **keine vollständige .sln gefunden** | — | — | — | — | — | — | — |

**Ergebnis der Quellsuche:** Keine vollständige Source-Kopie mit funktionierendem Git auf lokalen Laufwerken gefunden.

---

## 4. Verwendete Recovery-Quelle

**Priorität 4 (kombiniert):**

1. **ILSpy-Decompile** aus Debug-Build-DLLs (06.08.2026) aus dem defekten Stand:
   - `Werkflow.OpcUaSimulator.Core.dll` (AP-4 vollständig)
   - `Werkflow.OpcUaSimulator.OpcUa.dll` (PhysicalSignals)
   - `Werkflow OPC UA Simulator.dll` (WPF App, BAML)
   - `Werkflow.OpcUaSimulator.Tests.dll` (Test-Hilfsdateien)
2. **Backup-Arbeitskopie** (`RecoveryBackup/Simulation_broken_20260807_091417`) für:
   - ausgewählte Testdateien (AP-3 R3/R4)
   - `TestData/ReferenceMachine.json`
   - `MachineProfiles` aus `bin`
3. **Geschützte Handoff-Dateien** (AP-4-R1):
   - 22 `FaultScenarios/*.json`
   - `TechnicalLearningMachine300.json` (lokale Änderung)
4. **Neu erzeugt:** `Werkflow.OpcUaSimulator.sln`, alle vier `.csproj`, `.gitignore`

---

## 5. Sicherung des defekten Standes

```text
C:\WerkFlow\Coding\RecoveryBackup\Simulation_broken_20260807_091417
```

Erstellt vor jeder Wiederherstellung. Nicht überschrieben.

---

## 6. Wiederhergestellte Solution

```text
C:\WerkFlow\Coding\Simulation\Werkflow.OpcUaSimulator.sln
```

Enthält alle vier Projektverweise (App, Core, OpcUa, Tests).

---

## 7. Wiederhergestellte Projekte

| Projekt | csproj | C#-Dateien (nach Recovery) |
|---------|--------|----------------------------|
| Werkflow.OpcUaSimulator.Core | ja | 169 |
| Werkflow.OpcUaSimulator.OpcUa | ja | 19 |
| Werkflow.OpcUaSimulator.App | ja | 42 |
| Werkflow.OpcUaSimulator.Tests | ja | 57 |

---

## 8. Git-Integrität

`git fsck --full` meldet weiterhin **237 fehlende Objekte** (Blobs/Trees/Commits). Beispiele:

- `missing blob d0534afc…`, `missing tree d91b8b75…`, u.a.
- `fatal: Failed to traverse parents of commit 44411dd…`
- Keine Pack-Dateien in `.git/objects/pack/`

**Bewertung:** Git-Historie ist **nicht vollständig wiederhergestellt**. Working Tree und HEAD-Referenz (`f0b768d`) sind konsistent, aber alte Commits/Dateiversionen können nicht zuverlässig aus Git rekonstruiert werden.

**Kein improvisiertes Git-Reparaturversuch** durchgeführt. Kein Remote verfügbar.

---

## 9. Branch

```text
feature/physical-learning-simulator
```

---

## 10. HEAD

```text
f0b768d8d8ecae087ccce7d42ca72a4edc18fc66
Implement AP 4 controlled fault scenarios
```

Entspricht dem bekannten AP-4-Commit.

---

## 11. Tags

```text
opcua-simulator-fault-scenarios-ap4-complete
opcua-simulator-physical-simulation-ap3-final
opcua-simulator-physical-simulation-ap3-verified-final
```

Nicht überschrieben.

---

## 12. Restore-Ergebnis

```powershell
dotnet restore Werkflow.OpcUaSimulator.sln
```

**Erfolgreich** (nach Recovery und nach Bereinigung erneut verifiziert).

---

## 13. Build-Ergebnis

```powershell
dotnet build Werkflow.OpcUaSimulator.sln -c Release
```

| Metrik | Wert |
|--------|------|
| Fehler | 0 |
| Warnungen | 32 (Tests, Nullable) nach erstem vollständigen Build; 568 nach Bereinigung/Rebuild (überwiegend Nullable in decompiliertem Code) |
| App | erfolgreich |
| Core | erfolgreich |
| OpcUa | erfolgreich |
| Tests | erfolgreich |

---

## 14. Test-Ergebnis

```powershell
dotnet test Werkflow.OpcUaSimulator.sln -c Release
```

| Metrik | Wert |
|--------|------|
| Gesamt | 121 |
| Bestanden | 121 |
| Fehlgeschlagen | 0 |
| Übersprungen | 0 |
| Dauer | ~12 min (langlaufende Harness-Tests) |

**Hinweis:** Kein AP-4-R1-E2E-Lauf durchgeführt (nur reguläre Test-Suite).

---

## 15. AP-4-Komponenten vorhanden

| Komponente | Status |
|------------|--------|
| 22 FaultScenario-JSONs | ja (`Werkflow.OpcUaSimulator.App/FaultScenarios/`) |
| FaultScenarioEngine | ja (`Core/.../FaultScenarioEngine.cs`) |
| FaultScenarioService | ja |
| FaultRecoveryDefinition / Recovery-Lifecycle | ja (Modelle + Engine) |
| ThresholdRules | ja (`FaultThresholdRule.cs`) |
| Scenario Lifecycle | ja (`FaultScenarioLifecycleState`, Events) |
| ConnectionDrop / SignalFreeze | ja (`FaultEffectType.ConnectionDrop`, `SignalFreeze`) |
| NonFaultingControlRun | ja (`FaultScenarioRunMode.NonFaultingControlRun`) |
| AP-4-UI | ja (`FaultScenariosViewModel`, BAML-Views) |
| AP-4-Tests | ja (`PhysicalAp4FaultScenarioTests.cs`, `PhysicalAp4VerificationHarness.cs`) |
| MachineProfiles (3 aktiv) | ja (Bending, Laser, TechnicalLearning) |

---

## 16. Geschützte lokale Dateien (nicht blind überschrieben)

- `Werkflow.OpcUaSimulator.App/MachineProfiles/TechnicalLearningMachine300.json` (lokale Version erhalten)
- `handoff/ap-04-r1-e2e-evidence-and-scenario-catalog-closure/` (Manifest, Berichte, Szenario-Kopien)
- `handoff/` Archivstände
- Root `AP-03-R1-*.md` und `AP-03-R3-*.md` (kein identisches Archiv-Duplikat für R1; R3 Root ≠ Archiv-Hash)

22 Szenario-JSONs aus Handoff nach `App/FaultScenarios/` übernommen (Ordner war leer).

---

## 17. Entfernte Alt-/Temp-Dateien

| Pfad | Grund |
|------|-------|
| `_zip_extract_ap04/` | temporärer ZIP-Extract |
| `_zip_extract_r4/` | temporärer ZIP-Extract |
| `_recovery/` | Decompile-Artefakte nach erfolgreicher Recovery |
| `publish/` | alte Publish-Artefakte |
| `_recovery_search.ps1` | temporäres Suchskript |
| `_recovery_search_results.json` | Suchergebnis |
| `AP-02-dynamic-opcua-physical-signals-and-scaling-report.md` (Root) | identisch mit `handoff/archive/...` (SHA256 verifiziert) |
| `**/bin/`, `**/obj/` | Build-Artefakte |
| `.vs/` | falls vorhanden (gitignored) |

**Nicht entfernt:** Handoff-ZIPs (eigenständige Pakete), `AP-03-R1`/`AP-03-R3` Root-Berichte, README, Solution, Projektordner.

---

## 18. Erhaltene Dateien

- Vollständige Solution und alle vier Projektordner
- `README.md`
- `handoff/` (inkl. archive, AP-4/AP-4-R1 Pakete)
- `.git` (defekt, aber HEAD/Tags erhalten)
- `.gitignore` (aktualisiert)
- `MachineProfiles/` (3 JSONs unter App)
- `FaultScenarios/` (22 JSONs unter App)
- `TestData/ReferenceMachine.json` (unter Tests)

---

## 19. Finale Root-Struktur

```text
Simulation/
├── .git
├── .gitignore
├── Werkflow.OpcUaSimulator.sln
├── README.md
├── AP-03-R1-profile-consistency-and-physical-verification-report.md
├── AP-03-R3-final-normal-physics-calibration-report.md
├── Werkflow.OpcUaSimulator.App/
├── Werkflow.OpcUaSimulator.Core/
├── Werkflow.OpcUaSimulator.OpcUa/
├── Werkflow.OpcUaSimulator.Tests/
└── handoff/
```

---

## 20. Finaler Git-Status

- Branch: `feature/physical-learning-simulator`
- HEAD: `f0b768d`
- Working Tree: **massiv verändert** (Recovery-Rekonstruktion; viele Dateien als modified/deleted gegenüber letztem Commit, neue decompilierte Dateien untracked)
- Kein Recovery-Commit erstellt (keine explizite Anforderung; Änderungen sind Rekonstruktion, kein fachlicher AP-Commit)
- Empfehlung: Nach Review optional ein dokumentierter „RECOVERY-01 repository restoration“-Commit oder Neuaufbau `.git` aus Working Tree als letzte Option

---

## 21. Verbleibende Risiken

1. **Quellcode-Qualität:** Teils ILSpy-Decompile (App: BAML statt XAML; Compiler-Generated-Hilfen; Nullable-Warnungen).
2. **Git-Historie:** 237 fehlende Objekte; alte Commits nicht traversierbar; kein Remote.
3. **Keine Original-Source-Kopie:** Recovery basiert auf DLL-Decompile + fragmentierte Backups — Diff gegen Original-AP-4-Stand nicht vollständig verifizierbar.
4. **AP-4-R1-E2E:** Noch nicht ausgeführt; Evidence/Manifest aus vorherigem Teilarbeitstand unverändert.
5. **ReferenceMachine.json:** Nur aus Backup-`bin` wiederhergestellt, nicht aus Git.

---

## 22. AP 4 R1 fortsetzbar?

**Ja, mit Einschränkungen.**

| Abschlusskriterium RECOVERY 01 | Erfüllt |
|-------------------------------|---------|
| Vollständige Solution | ja |
| Alle vier Projekte | ja |
| Relevanter Quellcode | ja (decompile-basiert) |
| Git-Integrität geklärt | ja (defekt, dokumentiert) |
| Branch eindeutig | ja |
| AP-4-Stand bestimmt | ja (HEAD f0b768d, Tag AP-4) |
| `dotnet restore` | ja |
| `dotnet build` | ja |
| `dotnet test` | ja (121/121) |
| 22 AP-4-Szenarien | ja |
| MachineProfiles aktiv | ja |
| Root bereinigt | ja |
| Recovery-Bericht | ja (dieses Dokument) |

**AP 4 R1 wurde nicht fortgesetzt.** Der bestehende AP-4-R1-Auftrag (E2E-Evidence, Szenario-Katalog-Closure) kann wieder aufgenommen werden.

---

## Hilfsartefakte (Handoff)

- `handoff/recovery-01-restore.ps1` – initiales Restore-Skript
- `handoff/fix-viewmodels.ps1`, `fix-tests-*.ps1` – Build-Fix-Skripte
- Backup: `C:\WerkFlow\Coding\RecoveryBackup\Simulation_broken_20260807_091417`
