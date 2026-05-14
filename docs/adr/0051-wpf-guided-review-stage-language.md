# ADR 0051: WPF guided review stage language

## Status

Accepted

## Context

The WPF UI can already run the safe read-only scan, plan, and preflight flow, but the product surface still exposes internal command names and parameter-heavy forms. First-time users need a clearer sense of the current stage without weakening the safety boundary.

## Decision

Rename the WPF `Read-Only Ops` tab to `Guided Review` and present the flow as user-facing stages:

- Evidence Scan
- Cleanup Plan
- Safety Check
- Guarded CLI Handoff

Extend `ReadOnlyWorkflowPresenter` with current-stage title, current-stage detail, and a persistent safety boundary message. The UI uses those fields in the Workflow panel and keeps the single primary action model.

The implementation only changes presentation and workflow guidance. The UI runner still allows only `scan`, `plan`, and `preflight`; it does not execute `quarantine`, `restore`, `delete`, or `clean`.

## Consequences

- First-time users get a clearer path from choosing a target through evidence, plan review, and safety checking.
- Internal CLI terminology remains available in command previews while the main UI uses safer product language.
- The same pure workflow presenter remains covered by unit tests.
- No deletion or automatic file-moving capability is added.
