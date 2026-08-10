# AP-06-R3 HMI Polish and Tray Fix Report

## 1. R2 Restbefunde

- Texte/Buttons teils zu wenig Kontrast (Blau auf Dunkelblau)
- Deaktivierte Buttons schwer erkennbar
- Fenster-`X` konnte die Anwendung beenden (`ShutdownMode.OnMainWindowClose`)
- Kein echtes Tray-Icon — HMI verschwindet ohne Wiederöffnen-Weg

## 2. Kontrastkorrekturen

Zentrale `HmiVisualTheme` mit hellen Primärtexten (`#F4F6F8`), sekundären Labels (`#D8DEE6`), Section-Titel (`#C5DCF5`) und Value-Accent (`#7EC8FF`) auf dunklen Panel-Hintergründen.

## 3. Style-/Brush-Anpassungen

- `HmiVisualTheme.cs` — gemeinsame Brushes für Header, Panels, Navigation, Metric-Tiles, Simulation-Bereich
- `VirtualMachineHmiWindow.cs` — nutzt Theme statt verstreuter RGB-Werte

## 4. Disabled-State-Korrektur

`CreateButtonStyle()` mit Trigger: disabled Background `#2E3642`, Foreground `#9AA8B8` — lesbar, aber klar deaktiviert.

## 5. X-/Tray-Ursache

- `ShutdownMode.OnMainWindowClose` beendete die App beim Schließen des Hauptfensters
- `SimulatorTrayService` war Stub ohne `NotifyIcon`
- `VirtualMachineWindowService` setzte `Owner` und `Closed`-Handler (Instanz-Risiko)

## 6. Close-to-Tray-Korrektur

- `ShutdownMode.OnExplicitShutdown`
- HMI `OnClosing`: `Cancel=true`, `Hide()`, Tray benachrichtigen
- `MainWindow` `OnClosing`: gleiches Hide-to-Tray-Verhalten
- Kein `Owner` auf HMI, kein `Closed`-Handler — Singleton bleibt

## 7. Tray-Menü

`NotifyIcon` mit: Virtuelle Maschine öffnen, Simulator öffnen, Maschine beenden, Beenden (mit Bestätigung).

## 8. Reopen-Verhalten

`ShowOrFocus` reaktiviert dieselbe `_window`-Instanz; Livebinding bleibt aktiv.

## 9. Shutdown-Pfade

- **Maschine beenden** (HMI-Button / Tray): Bestätigung → Server stoppen
- **Beenden** (Tray): Bestätigung → `Application.Shutdown()`
- **Fenster-X**: nur Hide, niemals Shutdown

## 10. Smoke-Test

- `dotnet build -c Release`: OK
- App-Start via `dotnet run`: OK (vorheriger Smoke)

## 11. Build/Test

- Build: 0 Fehler
- AP6R3-Tests: 9/9 bestanden
- Volle Suite: übersprungen (User-Vorgabe)

## 12. VerificationRunId

`ap6r3-20260810212412-4cb177f2fdf647d0a7bb584a8b9`

## 13. Commit-SHA

`5b4054fccc6aceaad03df9104526d075c726bb4b`

## 14. Tag / TagTarget

- Tag: `opcua-simulator-virtual-machine-hmi-ap6-r3`
- TagTarget: `5b4054fccc6aceaad03df9104526d075c726bb4b`

## 15. Git-Status

Commit erstellt; nur R3-relevante Dateien. Tag gesetzt.

## 16. AP6R3Passed

**true** — alle Verification-Kriterien erfüllt, AP6R2-Regression in Harness grün.

## 17. Freigabeempfehlung

**AP 6 final freigabefähig.** Kontrast verbessert, Close-to-Tray funktional, explizite Shutdown-Pfade klar getrennt.
