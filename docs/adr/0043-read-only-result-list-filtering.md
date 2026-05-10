# ADR 0043: Read-only result list filtering

Date: 2026-05-10

## Status

Accepted

## Context

After scan and plan reports load in the WPF UI, users may need to review many items. Without search, risk filtering, action/type filtering, or alternate sort orders, the review loop is slow and users can miss the relevant candidates.

Filtering must not become a hidden risk model or change cleanup decisions.

## Decision

Add display-only filtering and sorting to the WPF Scan Report and Cleanup Plan item lists:

- Scan Report: search, risk filter, item type filter, and sort by size, path, risk, or type.
- Cleanup Plan: search, risk filter, action filter, and sort by path, action, or risk.

Filtering searches only already-loaded view model fields such as path, reasons, blockers, evidence, quarantine preview paths, and restore metadata paths. It does not rescan the file system, collect new evidence, alter risk levels, alter suggested actions, or generate cleanup plans.

The filtering logic is implemented in a tested pure view-model helper, while WPF applies the same predicate and sort choices to the visible list views.

## Consequences

Users can quickly narrow large reports to relevant files, risk levels, and cleanup candidates. Safety behavior is unchanged because filtering only changes visibility and ordering in the UI.
