# AP-04-R3 Evidence Completeness

VerificationRunId: `ap4r3-20260809210206-014c6ff6636f478aaa229b5`
Ap4R3Passed: **True**
Ap4OverallPassed: **True**

## Laser Recovery
Samples: 68, Passed: True
RecoveryStarted: 2026-08-09T21:02:07.1248785Z
RecoveryCompleted: 2026-08-09T21:02:07.1438346Z

## Hydraulic Recovery
Samples: 51, Passed: True

## Sensor Drift
Samples: 40, SensorDelta: 0,000, HiddenDelta: 0,000, Passed: True

## CoolantLoss
Samples: 40, Passed: True
  - CoolingEfficiency decrease: delta=-0,385 passed=True
  - Cooling.PrimaryCircuit.Flow decrease: delta=-0,132 passed=True
  - Cooling.PrimaryCircuit.Pressure decrease: delta=-0,006 passed=True
  - Cooling.PrimaryCircuit.Temperature increase: delta=2,429 passed=True

## HydraulicLeak
Samples: 50, Passed: True
  - HydraulicEfficiency decrease: delta=-0,104 passed=True
  - Hydraulic.SupplyPressure decrease: delta=-7,125 passed=True
  - Hydraulic.PumpCurrent increase: delta=2,460 passed=True
  - Hydraulic.OilTemperature increase: delta=6,348 passed=True
  - Bending.PressForce change: delta=-3,330 passed=True
  - Bending.CycleTime increase: delta=0,111 passed=True
