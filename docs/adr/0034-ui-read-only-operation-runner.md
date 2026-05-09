# ADR 0034: UI read-only operation runner

Date: 2026-05-09

## Status

Accepted

## Context

ADR 0033 expanded the WPF Read-Only Ops tab so users could prepare complete `scan`, `plan`, and `preflight` commands. The next usability gap is that users still need to copy those commands into a terminal, then manually open the generated JSON files back in the UI.

Running any command from the UI changes the safety boundary and must be constrained so it cannot become an implicit cleanup executor.

## Decision

Add a WPF read-only operation runner that can execute only:

- `scan`
- `plan`
- `preflight`

The runner invokes the existing CLI through the local .NET SDK and CLI project, captures stdout/stderr, and reports the CLI exit code. UI run buttons require an output path before execution. If the selected format is JSON and the CLI succeeds, the UI reads the generated file and loads it into the matching tab. Markdown output is written but not parsed.

The runner rejects:

- `quarantine`
- `restore`
- `delete`
- `clean`
- `--delete`
- `--fix`
- `--quarantine`
- `--clean`

The CLI remains responsible for output path safety checks such as refusing overwrites and protected Windows paths.

## Consequences

The UI can now complete the read-only report loop: configure, run, write, and load `scan`, `plan`, and `preflight` outputs. Mutating operations remain terminal-only and still require the CLI's explicit double confirmation. Any future UI support for `quarantine` or `restore` requires a separate ADR, tests, and a stronger confirmation design.
