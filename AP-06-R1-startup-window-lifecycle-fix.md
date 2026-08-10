# AP-06-R1 – Startup / Window Lifecycle Fix

## Ursache

Beim AP-6-Startup schlug die DI-Auflösung von `MainWindow` fehl:

1. **Zirkuläre Abhängigkeit:** `VirtualMachineWindowService` ↔ `SimulatorTrayService` (über `IHmiTrayNotifier`)
2. **`Func<VirtualMachineWindowService>` nicht registriert:** `SimulatorTrayService` benötigte `Func<…>`, aber MS DI liefert das nicht automatisch
3. **Falsche Registrierungsreihenfolge:** `SimulatorTrayService` wurde vor `VirtualMachineWindowService` registriert

Fehler beim Start:

```text
Unable to resolve service for type 'Func<VirtualMachineWindowService>'
while attempting to activate 'SimulatorTrayService'
```

→ `OnStartup` Exception vor sichtbarem Hauptfenster (nur Startup-Fehler-Dialog).

Zusätzlich startete `VirtualMachineHmiViewModel` im Konstruktor sofort Timer/Refresh — unnötig beim App-Start.

## Änderungen

| Datei | Änderung |
|-------|----------|
| `SimulatorTrayService.cs` | `Func<VirtualMachineWindowService>` statt direkter Referenz (bricht Zirkel) |
| `VirtualMachineHmiViewModel.cs` | `EnsureActivated()` — Timer/Refresh nur bei erstem HMI-Öffnen |
| `VirtualMachineWindowService.cs` | ruft `EnsureActivated()` vor Show |
| `App.cs` | `Func<VirtualMachineWindowService>` registriert; Reihenfolge Window → Factory → Tray; MainWindow explizit sichtbar |

## Startup vorher / nachher

| | Vorher | Nachher |
|---|--------|---------|
| DI | Zirkel Tray ↔ WindowService | Aufgelöst via `Func<>` |
| HMI VM | Aktiv beim App-Start | Aktiv nur bei Benutzerklick |
| MainWindow | Show() oft nicht erreicht | Show + Visible + Taskbar |

## Verhalten

- **Normaler Start:** MainWindow sichtbar, `Application.MainWindow` = Simulator-Hauptfenster
- **Virtual Machine:** nur auf Button-Klick, Single Instance
- **HMI X:** Hide only (`OnClosing` cancel), Maschine läuft weiter
- **Reopen:** ShowOrFocus auf dieselbe Instanz

## Build / Test

```powershell
dotnet build Werkflow.OpcUaSimulator.sln -c Release
dotnet test Werkflow.OpcUaSimulator.sln -c Release --filter "Category!=Integration"
```

AP-6-R1 Lifecycle-Tests: `PhysicalAp6R1StartupTests`

## Commit

`Fix simulator startup window lifecycle` — `fd18e37381f0f420c815b0c9bf8a64ddfac19145`

## Git-Status

Nur R1-Lifecycle-Dateien committed; fremde Handoff-Änderungen unstaged.
