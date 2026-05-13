# ADR 0046: Guarded CLI handoff for file move commands

## Status

Accepted

## Context

The CLI already supports guarded file-level `quarantine` and `restore` commands. Both commands move files and require `--manual-confirmation` plus `--i-understand-this-moves-files`.

The WPF UI has enough context to help users assemble the required plan, restore metadata, and optional operation log paths after review and preflight. However, executing file-moving commands directly from the UI would widen the blast radius and weaken the current read-only UI execution boundary.

## Decision

The WPF UI may build and copy guarded CLI handoff commands for `quarantine` and `restore`, but it must not run them.

The handoff builder is a pure helper. It requires both explicit confirmations before it returns a command:

- manual confirmation
- acknowledgement that the command moves files

The generated command text may include `quarantine`, `restore`, `--manual-confirmation`, `--i-understand-this-moves-files`, optional `--operation-log`, and the restore legacy metadata flag when explicitly selected. The existing UI runner remains limited to `scan`, `plan`, and `preflight`; it still rejects `quarantine`, `restore`, `delete`, and `clean`.

The WPF surface presents this as a CLI handoff, not as an in-app cleanup action. It provides Build and Copy actions only. No Run button is added for file-moving commands.

## Consequences

- Users get a clearer next step after preflight without the WPF UI performing file moves.
- File-moving execution remains CLI-only and keeps the existing double-confirmation contract.
- Command text can still be dangerous if pasted and run, so the UI requires explicit acknowledgement before building it.
- This decision does not authorize delete or clean commands, directory quarantine, directory restore, registry edits, service changes, or privilege escalation.
