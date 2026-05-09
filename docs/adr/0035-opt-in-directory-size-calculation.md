# ADR 0035: Opt-in directory size calculation

Date: 2026-05-09

## Status

Accepted

## Context

Earlier scan reports exposed file sizes but kept directory items at `0 B`. That kept scans cheap and predictable, but made it harder to answer the core product question of which directories explain disk growth.

Recursive directory size calculation can be expensive and may encounter denied paths, long paths, or reparse points. It must not weaken the project's safety model by following junctions into protected locations or by changing the default cost of a scan.

## Decision

Add opt-in directory size calculation through `FileSystemScanOptions.IncludeDirectorySizes` and the CLI flag `--directory-sizes`.

When enabled:

- Directory items report the sum of reachable descendant file sizes.
- Reparse point directories are skipped.
- Blocked protected Windows directories are not descended into for size calculation.
- Access, path length, IO, or security failures stop size calculation for that directory and add a cautionary reason to the item.
- The operation remains read-only.

The default remains unchanged: directory items report `0 B` unless the user explicitly requests directory size calculation.

## Consequences

Users can now identify large directories from scan reports and the WPF Largest Items summary without changing the report schema. Large scans may take longer when `--directory-sizes` is enabled, so UI and docs must present it as an explicit option rather than a default behavior.
