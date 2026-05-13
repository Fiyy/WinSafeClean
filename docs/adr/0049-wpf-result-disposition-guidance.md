# ADR 0049: WPF result disposition guidance

## Status

Accepted

## Context

Users can now run scan and plan from the WPF UI, filter results, and build guarded CLI handoff commands. The remaining usability gap is deciding what a selected result means operationally: a scan item is evidence, a plan item may be keep/report-only/quarantine-review, and the UI still must not imply direct deletion.

## Decision

Add a pure `ResultDispositionAdvisor` ViewModel helper for selected Scan Report and Cleanup Plan items.

The advisor returns a title, explanation, next-step text, and whether `Prepare Preflight` is allowed. The WPF details panes display this disposition guidance for selected scan and plan rows. `Prepare Preflight` is enabled only when the selected plan item is `ReviewForQuarantine` and includes both quarantine and restore metadata preview paths.

## Consequences

- Selected results now explain whether the user should keep the item, only review evidence, or prepare preflight.
- The guidance is display-only and does not lower risk, change plan actions, move files, delete files, or execute CLI commands.
- Non-candidate plan items cannot accidentally start the preflight preparation flow from the details pane.
- `delete` and `clean` remain unavailable in both CLI and UI.
