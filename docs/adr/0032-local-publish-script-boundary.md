# ADR 0032: Local publish script boundary

Date: 2026-05-08

## Status

Accepted

## Context

After the WPF MVP shell, the project needs a repeatable way to create local CLI and UI binaries. Packaging must not weaken the project's safety model by hiding command execution, requesting elevation, or invoking cleanup operations.

## Decision

Add `scripts/publish.ps1` as a local publish helper for `WinSafeClean.Cli` and `WinSafeClean.Ui`.

The script:

- Publishes both projects to `artifacts/publish` by default.
- Runs the existing test script before publishing unless `-SkipTests` is explicitly passed.
- Supports `Release` / `Debug`, `win-x64` / `win-arm64`, and framework-dependent or self-contained output.
- Does not run the generated binaries.
- Does not request elevation.
- Does not call `scan`, `plan`, `preflight`, `quarantine`, `restore`, `delete`, or `clean`.
- Rejects publish output inside the Windows directory, source tree, test tree, docs tree, or local toolchain directory.
- Keeps generated artifacts out of git with `artifacts/` in `.gitignore`.

## Consequences

Release preparation is now reproducible from the repository while keeping publishing separate from cleanup execution. A future installer, signing flow, or release archive can build on this script, but must keep the same boundary: packaging may compile and copy files, not perform cleanup actions or privilege escalation.
