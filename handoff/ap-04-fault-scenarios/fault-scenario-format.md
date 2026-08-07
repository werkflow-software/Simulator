# AP 4 – Fault Scenario JSON Format

Deklaratives Szenarioformat für `FaultScenarios/**/*.json`.

## Pflichtfelder

| Feld | Beschreibung |
|------|--------------|
| `scenarioId` | Eindeutige ID (kebab-case) |
| `scenarioVersion` | Version string |
| `displayName` | Anzeigename in der UI |
| `description` | Bedienerverständliche Beschreibung (keine OPC-UA-Veröffentlichung) |
| `machineProfileIds` | Kompatibele Profil-IDs |
| `category` | mechanical, thermal, cooling, hydraulic, … |
| `severity` | low, medium, high, critical |
| `defaultDuration` | ISO TimeSpan (`00:05:00`) |
| `phases` | Phasen mit `durationFraction` oder `duration` |
| `effects` | Liste der Wirkungen |
| `recovery` | Recovery-Definition |
| `isEnabled` | Szenario aktiv |

## Effect

| Feld | Beschreibung |
|------|--------------|
| `targetType` | `hiddenState`, `signalQuality`, `machineConnection`, … |
| `targetId` | Hidden-State- oder Signal-ID |
| `effectType` | `additiveDrift`, `efficiencyLoss`, `signalFreeze`, `connectionDrop`, … |
| `direction` | `increase`, `decrease`, `oscillate`, `stabilize` |
| `magnitude` | Stärke |
| `ratePerSimulationMinute` | Driftrate |

Physikalische Szenarien verwenden `hiddenState` als Ziel.

## ThresholdRule

| Feld | Beschreibung |
|------|--------------|
| `sourceType` | `hiddenState`, `signal` |
| `sourceId` | Quell-ID |
| `comparison` | `greaterThan`, `lessThan`, … |
| `thresholdValue` | Grenzwert |
| `minimumDuration` | Mindestdauer bis Fehler |
| `faultCode` / `faultMessage` | Standard-Fehlernodes |
| `disabledInControlRun` | Für NonFaultingControlRun deaktiviert |

## Kette

```text
FaultScenario → Hidden Process State → Physical Engine → Signals → OPC-UA Publisher
```

Interne Szenarioinformationen werden nicht über OPC UA veröffentlicht.
