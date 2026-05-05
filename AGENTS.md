# Agent Instructions

This repository is a safety-first Windows cleanup assistant.

Before making changes, read:

1. `README.md`
2. `PROJECT_PRINCIPLES.md`
3. `GOALS.md`
4. `TDD_STRATEGY.md`
5. `docs/RISK_MODEL.md`
6. `PROGRESS.md`

## Non-negotiable Rules

- MVP is read-only. Do not implement real deletion in early phases.
- Do not use `Win32_Product` to enumerate installed MSI applications.
- Do not manually clean Windows Installer cache, WinSxS, DriverStore, System32, or servicing directories.
- Write tests before implementing core behavior.
- Every cleanup recommendation must include evidence and a risk level.
- Unknown evidence must increase caution, not decrease it.
- Use sub-agents for splittable, independent work when it reduces main conversation context load.
- Update `PROGRESS.md` after meaningful changes.
- Add an ADR under `docs/adr/` for architecture or safety decisions.

## Expected Workflow

1. Identify the smallest testable behavior.
2. Add or update tests first.
3. Implement the minimum code to pass tests.
4. Run relevant tests.
5. Update documentation and progress.
