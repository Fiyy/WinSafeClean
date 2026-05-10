# ADR 0041: Read-only UI path pickers and workflow hints

Date: 2026-05-10

## Status

Accepted

## Context

The WPF Read-Only Ops tab can run `scan`, `plan`, and `preflight`, but path entry is still text-only. That makes a safety-first workflow feel harder than the equivalent terminal command because users must manually type target paths, output report paths, CleanerML paths, cleanup plan paths, and restore metadata paths.

Users also need a clearer path from scan evidence to cleanup review. The product must improve this without adding a direct delete shortcut or weakening the read-only MVP boundary.

## Decision

Add WPF file and folder pickers for Read-Only Ops inputs:

- Scan and plan target paths can be selected as folders or files.
- CleanerML evidence can be selected as a folder or file.
- Preflight plan and restore metadata use JSON file pickers.
- Scan, plan, and preflight outputs use save-file pickers.

When a user selects a target input, the UI suggests a non-existing JSON output path on the desktop using `winsafeclean-{operation}-{yyyyMMdd-HHmmss}.json`, appending a numeric suffix if needed. The UI also adds a `Next: Plan` action and automatically prepares plan inputs after a successful scan by copying the scan path and read-only options into the plan section.

When a cleanup plan item is selected, the UI can prepare preflight inputs by generating a restore metadata input JSON from the loaded cleanup plan and selected quarantine preview. This file is saved as a separate preflight input, not at the final restore metadata path inside the quarantine root; the final restore metadata path remains part of the metadata payload and is still checked by preflight.

The Read-Only Ops page states the safety boundary: scan creates evidence, plan reviews cleanup candidates, and direct delete is unavailable in this safety build.

## Consequences

The common UI path becomes browse target, run scan, review report, run plan, review cleanup candidates, prepare preflight, then run preflight. Users no longer need to handwrite every path for the read-only workflow.

This does not add UI execution for `quarantine`, `restore`, `delete`, or `clean`. Future UI support for quarantine or restore still requires a separate ADR, tests, and explicit guarded confirmation UX.
