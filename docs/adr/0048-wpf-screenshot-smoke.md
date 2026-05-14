# ADR 0048: WPF screenshot smoke verification

## Status

Accepted

## Context

The WPF UI now has more workflow controls: recent documents, read-only operation inputs, privacy advice, result filters, visible-row export, and guarded CLI handoff. Hidden startup smoke proves that the application starts, but it cannot catch clipped toolbars, overlapping list columns, missing filter controls, or unreachable handoff controls.

## Decision

Add `scripts/smoke-wpf-ui.ps1` as a published WPF UI visual smoke helper.

The script starts the published WPF executable, constrains the window to the primary screen working area, captures screenshots to `artifacts\smoke`, validates that each screenshot is nonblank, and then closes the test process unless `-KeepOpen` is specified.

The script captures:

- startup Scan Report layout and top toolbar
- loaded Cleanup Plan layout using a repository fixture
- Guided Review top section with privacy advice
- Guided Review guarded CLI handoff section after scrolling

To load the Cleanup Plan layout without file dialogs, the script temporarily writes a single recent-document entry pointing to the repository cleanup-plan fixture. It backs up and restores the user's existing recent-document history after the smoke run.

## Consequences

- Layout regressions can be caught before a release candidate is packaged.
- Smoke artifacts remain under ignored `artifacts\smoke`.
- The script does not run scan, plan, preflight, quarantine, restore, delete, or clean commands.
- The script temporarily touches local UI recent-document history but restores it after the app exits.
