# Release Notes Template

## Version

`v0.0.0`

## Date

`YYYY-MM-DD`

## Summary

- Short release summary.

## Safety Boundary

- `scan`, `plan`, and `preflight` are read-only.
- `quarantine` and `restore` move files only with `--manual-confirmation` and `--i-understand-this-moves-files`.
- `delete` and `clean` are not available.
- Directory quarantine and directory restore are not supported.
- WPF UI opens existing JSON reports/plans/checklists and builds read-only command text; it does not execute cleanup commands.

## Added

- 

## Changed

- 

## Fixed

- 

## Validation

- `pwsh -NoProfile -File .\scripts\test.ps1 -Restore`
- `pwsh -NoProfile -File .\scripts\publish.ps1 -SkipTests -WhatIf`
- `pwsh -NoProfile -File .\scripts\publish.ps1 -SkipTests`
- Published CLI `--version`.
- Published CLI read-only smoke.
- Published WPF UI startup smoke.

## Known Limitations

- Published output is a local folder, not an installer.
- No code signing or auto-update flow is included.
- WPF UI does not store scan history.
- Sensitive reports should still be generated with `--privacy redacted` before sharing.
