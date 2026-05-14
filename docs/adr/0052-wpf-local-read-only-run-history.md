# ADR 0052: WPF local read-only run history

## Status

Accepted

## Context

Guided Review can run the safe `scan`, `plan`, and `preflight` commands and load JSON outputs back into the WPF UI. Users still lack a local view of what they ran, which output file was produced, and whether the command succeeded.

The existing recent-document history records opened JSON documents, but it does not distinguish UI command runs from manually opened files and does not record failed read-only command attempts.

## Decision

Add a local WPF Run History for UI-run read-only commands. Each entry stores only metadata:

- operation kind
- target or input path
- output path
- output format
- exit code
- started and completed timestamps

The history is stored under the same local application data root as recent documents. It does not store report contents, stdout, stderr, evidence bodies, cleanup decisions beyond the operation kind, or any file-moving command.

Run History can reopen successful JSON outputs in the matching WPF tab. Markdown outputs and failed runs remain visible as history rows but are not loaded back into the UI.

## Consequences

- Users can recover the output from recent safe UI runs without searching the filesystem.
- Failed read-only attempts remain visible with an exit code, which helps troubleshooting.
- Local path privacy considerations remain: Run History stores local paths until cleared by the user.
- No quarantine, restore, delete, clean, or other file-moving operation is executed or added to the UI runner.
