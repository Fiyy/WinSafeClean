# ADR 0042: Read-only UI guided workflow

Date: 2026-05-10

## Status

Accepted

## Context

ADR 0041 improved path entry and scan-to-plan handoff, but the Read-Only Ops page still leaves users to infer the current stage from scattered controls. A safer cleanup assistant should make the next read-only step visible without introducing a direct delete or file-moving shortcut.

## Decision

Add a WPF Read-Only Ops workflow panel that summarizes three stages:

- Scan
- Plan
- Preflight

The panel shows each stage as pending, ready, needing input, or done. It also exposes one primary action derived from the current state:

- `Run Scan`
- `Run Plan`
- `Review Plan`
- `Run Preflight`
- `Review Preflight`

The workflow presenter is implemented as a tested pure model so stage transitions can be verified without WPF automation. The primary action never invokes `quarantine`, `restore`, `delete`, or `clean`. It only runs existing read-only UI operations or switches to an already loaded review tab.

When the user types or selects a new scan target, scan/plan/preflight completion state is reset. When preflight inputs change, preflight completion state is reset.

## Consequences

The UI now has a single visible next action for the read-only review loop, reducing the need to hunt across sections and tabs. The workflow still stops at preflight review; file-moving actions remain outside the WPF UI until a separate guarded execution design is accepted.
