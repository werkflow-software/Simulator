# AP 4 – Kurzer Szenario-End-to-End-Nachweis

**VerificationRunId:** `ap4-20260806200140-1933baa2a2c14238a8a51`  
**Passed:** `true`  
**Dauer:** 3 min (beschleunigt, `PHYSICS_VERIFY_SHORT=1`, `AP4_E2E_SECONDS=180`)

## Maschinen

| Maschine | Profil | Szenarien |
|----------|--------|-----------|
| Laser | laser-processing-machine-300 | Überhitzung, Kontrolllauf, intermittierend |
| Biegen | bending-hydraulic-machine-300 | Hydraulikleck |
| Technisch | technical-learning-machine-300 | CommunicationDrop |

## Nachweis

- Physikalische Drift über OPC UA sichtbar
- Fehlernodes `ErrorActive`, `ErrorMessage`, `MachineState` gesetzt bei Grenzwert
- Server bei physischem Fehler online
- CommunicationDrop stoppt nur Zielserver
- Recovery sichtbar nach Szenarioende
- TotalOpcUaUpdates: **113917**

Vollständige Zeitreihen und Maschinenmetriken: `AP-04-short-scenario-end-to-end.json`
