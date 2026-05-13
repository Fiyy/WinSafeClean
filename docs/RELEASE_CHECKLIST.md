# Release Checklist

WinSafeClean release preparation must preserve the safety-first boundary: publishing may build and package binaries, but must not execute cleanup operations, request elevation, or hide risky behavior.

## Preconditions

- `PROGRESS.md` is current.
- `docs/ROADMAP.md` reflects the release scope.
- New architecture or safety decisions have ADRs under `docs/adr/`.
- The working tree contains only intentional changes.

## Required Validation

Run:

```powershell
pwsh -NoProfile -File .\scripts\test.ps1 -Restore
pwsh -NoProfile -File .\scripts\publish.ps1 -SkipTests -WhatIf
pwsh -NoProfile -File .\scripts\publish.ps1 -SkipTests -CreateArchive
pwsh -NoProfile -File .\scripts\smoke-wpf-ui.ps1
```

Verify:

- `artifacts\publish\WinSafeClean.Cli\WinSafeClean.Cli.exe` exists.
- `artifacts\publish\WinSafeClean.Ui\WinSafeClean.Ui.exe` exists.
- `artifacts\release\WinSafeClean.Cli-<VERSION>-<RUNTIME>.zip` exists.
- `artifacts\release\WinSafeClean.Ui-<VERSION>-<RUNTIME>.zip` exists.
- `artifacts\release\SHA256SUMS.txt` contains both archives.
- Published CLI reports a version:

```powershell
.\artifacts\publish\WinSafeClean.Cli\WinSafeClean.Cli.exe --version
```

- Published CLI can run a read-only smoke command:

```powershell
.\artifacts\publish\WinSafeClean.Cli\WinSafeClean.Cli.exe scan --path . --max-items 1 --no-recursive --format json
```

- Published WPF UI starts and remains running during startup smoke.
- Published WPF UI screenshot smoke captures nonblank screenshots under `artifacts\smoke` for startup, loaded Cleanup Plan filters, Read-Only Ops privacy advice, and Guarded CLI Handoff.

## Safety Gate

Confirm the release process does not:

- Run `quarantine` or `restore`.
- Expose `delete` or `clean`.
- Request administrator elevation.
- Write outside `artifacts\publish` except normal build outputs under ignored `bin` / `obj`.
- Write release archives outside `artifacts\release` unless `-PackageRoot` was intentionally set to another safe path.
- Include `.tools`, `bin`, `obj`, `TestResults`, or unrelated local files in release artifacts.
- Change risk levels without matching tests and documentation.

## Documentation Gate

Confirm docs mention:

- CLI `scan`, `plan`, and `preflight` are read-only.
- `quarantine` and `restore` are real file moves requiring double confirmation.
- Directory quarantine and restore are not supported.
- WPF UI reads existing JSON reports/plans/checklists, may run read-only `scan`, `plan`, and `preflight`, and does not execute `quarantine`, `restore`, `delete`, or `clean`.
- Local publish script only runs tests and `dotnet publish`.

## Artifact Review

Inspect release output before publishing externally:

- CLI folder contains CLI executable, runtime config, deps file, and required project DLLs.
- UI folder contains UI executable, runtime config, deps file, and required project DLLs.
- No test assemblies are included.
- No restore metadata, operation logs, scan reports, private paths, or user data are included.
- No screenshot smoke artifacts from `artifacts\smoke` are included.

## GitHub Draft Release

Create a draft release after local validation:

```powershell
git push origin main
git push origin v<VERSION>
gh release create v<VERSION> `
  artifacts\release\WinSafeClean.Cli-<VERSION>-win-x64.zip `
  artifacts\release\WinSafeClean.Ui-<VERSION>-win-x64.zip `
  artifacts\release\SHA256SUMS.txt `
  --repo Fiyy/WinSafeClean `
  --title "WinSafeClean v<VERSION>" `
  --notes-file docs\releases\v<VERSION>.md `
  --draft
```

Verify:

```powershell
gh release view v<VERSION> --repo Fiyy/WinSafeClean --json tagName,name,isDraft,assets
```

Keep the release as a draft until a human has reviewed the safety boundary, known limitations, artifacts, and checksum manifest.

## Known Limitations To Carry Into Release Notes

- No real deletion command is available.
- Directory quarantine and directory restore are deferred.
- WPF UI does not execute `quarantine`, `restore`, `delete`, or `clean`.
- WPF UI does not yet include installer packaging, signing, auto-update, or scan history.
- Published output is a local folder, not an installer.
