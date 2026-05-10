# ADR 0044: Local recent document history

Date: 2026-05-10

## Status

Accepted

## Context

The WPF UI can open and generate scan reports, cleanup plans, and preflight checklists, but users must repeatedly browse for JSON files during review. A local recent-file list improves this workflow, but paths can be sensitive because they may reveal user names, project names, or scanned locations.

## Decision

Add a local recent document history for WPF UI JSON documents:

- Scan reports
- Cleanup plans
- Preflight checklists

The history stores only document kind, full local file path, and timestamp. It does not store report contents, evidence, risk results, plan items, or preflight checks. It is written to `%LocalAppData%\WinSafeClean\recent-documents.json` with a bounded entry count.

The UI provides:

- A recent document selector.
- `Open Recent`.
- `Clear`.

Missing recent files are removed from the list when selected. Corrupt history JSON is ignored and treated as empty. Recent history is a convenience feature only; failure to read or write it must not block report loading.

## Consequences

Users can reopen recent reports and plans without browsing through the filesystem. Sensitive path history remains local to the current Windows profile and can be cleared from the UI. This feature does not scan, delete, quarantine, restore, or modify any report content.
