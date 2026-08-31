# AP-04-R4 Final Recovery Safety Report

VerificationRunId: `ap4r4-20260831172953-d7640673b5d14da4b917b01`
Ap4R4Passed: **True**
Ap4OverallPassed: **True**

## 1. R3-Befund
Laser RecoveryCompleted meldete Passed=true bei MotorTemperature ~75–82°C oberhalb FaultThreshold 70°C.

## 2. Laser Safe-Recovery-Regel
SafeRecoveryThreshold: 65°C, FaultThreshold: 70°C

## 3. MinimumStableDuration
00:00:45

## 4. PostRecovery-Sicherheit
PostRecovery samples: 10

## 5–8. Direction Checks
Fault Axis01.MotorTemperature increase: passed=True delta=35,020
Fault Axis01.MotorCurrent increase: passed=True delta=0,435
Fault Axis01.Speed decrease: passed=True delta=-4,151
Recovery Axis01.MotorTemperature decrease: passed=True delta=-1,623
Recovery Axis01.MotorCurrent toward-normal: passed=True delta=0,303
Recovery Axis01.Speed increase: passed=True delta=4,625

## 9. DistanceToNormal (Hydraulic)
Start=5,174 End=0,088 Improved=True

## 10. Sensor Drift
Distinct=60 BiasDelta=18,201 Passed=True

## 21. Ap4R4Passed / Ap4OverallPassed
True / True

## 23. Freigabeempfehlung
AP 4 final freigegeben.
