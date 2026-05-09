# ADR 0033: Read-only UI command options

Date: 2026-05-09

## Status

Accepted

## Context

After the first public release, the WPF Read-Only Ops tab could build only minimal `scan`, `plan`, and `preflight` command text. That kept the UI safe, but made common review workflows awkward because users still had to manually add output files, redaction, Markdown output, item limits, recursion, and CleanerML paths in a terminal.

The UI must remain a command preparation surface unless a future design adds a separately reviewed execution boundary.

## Decision

Expand the WPF Read-Only Ops tab and command builder so it can prepare richer read-only commands:

- `scan`: path, output file, format, privacy, recursion, max items, and optional CleanerML path.
- `plan`: path, output file, format, privacy, recursion, max items, and optional CleanerML path.
- `preflight`: plan path, restore metadata path, output file, format, and manual confirmation flag.

The UI still:

- Does not execute commands.
- Does not build `quarantine`, `restore`, `delete`, or `clean` commands.
- Leaves output path safety validation to the CLI, which already rejects overwrites and protected Windows paths.

## Consequences

The UI can now support a more complete report-review workflow while staying within the existing safety boundary. Future UI execution work must be treated as a separate safety decision because it would change the UI from command preparation into an operation runner.

ADR 0034 later adds a constrained read-only operation runner for `scan`, `plan`, and `preflight` only. That decision does not extend to mutating commands.
