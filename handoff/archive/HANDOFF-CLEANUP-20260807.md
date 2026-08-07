# Handoff cleanup 2026-08-07 (AP 4 R1)

## Summary

- Wrote `handoff/INVENTORY-20260807.txt` (pre-cleanup snapshot).
- Merged `handoff/recovery-01-simulator-repository-restoration/` into `handoff/archive/recovery-01-simulator-repository-restoration/` (report + recovery scripts).
- **AP-03-R1** (`AP-03-R1-profile-consistency-and-physical-verification-report.md` at repo root): no prior archive copy; copied to `archive/ap-03-r1-physical-verification/`; deleted repo root copy.
- **AP-03-R3** (`AP-03-R3-final-normal-physics-calibration-report.md` at repo root): archive copy differed (SHA256); saved additional copy as `AP-03-R3-final-normal-physics-calibration-report-root.md` in `archive/ap-03-r3-final-calibration-r3-evidence/`; deleted repo root copy.
- Moved/merged into `handoff/archive/` (same folder names): `ap-02-physical-signals`, `ap-02-r2-datachange`, `ap-03-r1-physical-verification`, `ap-03-r2-calibration`, `ap-04-fault-scenarios`, `ap-04-r1-e2e-evidence-and-scenario-catalog-closure`.
- Removed redundant ZIPs after file-count match with archive folder: `ap-03-r4-final-closure.zip`, `ap-04-fault-scenarios.zip`, `ap-04-r1-e2e-evidence-and-scenario-catalog-closure.zip`, `recovery-01-simulator-repository-restoration.zip`.
- **Kept** `ap-03-r3-final-calibration.zip`: zip has 7 files, archive `ap-03-r3-final-calibration-r3-evidence/` has 8 (extra `-root` report copy).
- Removed `fix-*.ps1` and `recovery-01-restore.ps1` from handoff root (copied into `archive/recovery-01-simulator-repository-restoration/` first).
- Created empty `handoff/ap-04-r1-current/` for new AP-04-R1 evidence.

## Final handoff root
- ap-03-r3-final-calibration.zip (99342 bytes)- **ap-04-r1-current/**- **archive/**- INVENTORY-20260807.txt (1023 bytes)
## Archive top-level
- **ap-01-physical-simulation/**- **ap-02-physical-signals/**- **ap-02-r1-physical-signals/**- **ap-02-r2-datachange/**- **ap-03-r1-physical-verification/**- **ap-03-r2-calibration/**- **ap-03-r3-final-calibration-r3-evidence/**- **ap-03-r4-final-closure-evidence/**- **ap-03-virtual-machine-physics/**- **ap-04-fault-scenarios/**- **ap-04-r1-e2e-evidence-and-scenario-catalog-closure/**- HANDOFF-CLEANUP-20260807.md- **recovery-01-simulator-repository-restoration/**
## Pre-cleanup inventory

See `INVENTORY-20260807.txt` at handoff root.

