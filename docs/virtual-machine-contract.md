# Virtual Machine Contract

## Definition

```text
Virtual Machine = Machine 1
Endpoint = opc.tcp://localhost:4840
```

Diese Maschine ist die primäre virtuelle Produktionsmaschine für realitätsnahe inMotion- und spätere VIGIL-Lernversuche.

Der Simulator kennt VIGIL nicht. Die spätere reale Kette ist:

```text
Virtual Machine → OPC UA → inMotion → VIGIL
```

## Feste Identität

| Feld | Wert |
|------|------|
| Display Name | Werkflow Virtual Laser 01 |
| MachineId | `a1111111-1111-4111-8111-111111111111` |
| Port | 4840 |
| Endpoint | `opc.tcp://localhost:4840` |
| Physical Profile | `laser-processing-machine-300` (LaserProcessingMachine300) |
| Mode | Physical Simulation |
| Purpose | Virtual Machine / inMotion / VIGIL Learning Test |

Die MachineId bleibt über Neustarts stabil. `ConfigurationService.NormalizeMachine` und `DefaultMachines.CreateVirtualMachine()` erzwingen diese Werte für Port 4840.

## Vier Maschinen

Die vier unabhängigen Simulator-Maschinen bleiben erhalten:

| Maschine | Endpoint |
|----------|----------|
| Machine 1 (Virtual Machine) | opc.tcp://localhost:4840 |
| Machine 2 | opc.tcp://localhost:4841 |
| Machine 3 | opc.tcp://localhost:4842 |
| Machine 4 | opc.tcp://localhost:4843 |

## HMI

- Öffnen über **Virtuelle Maschine** im Hauptfenster
- Ein Fenster pro Virtual Machine (Single Instance)
- X versteckt HMI; Maschine läuft weiter
- **Maschine beenden** stoppt Physical Simulation, Fault Runtime und OPC-UA-Server

## Keine Ground-Truth-Leaks

FaultScenario-Steuerung und Ground-Truth-Informationen sind HMI-intern. Sie werden nicht über OPC UA publiziert.
