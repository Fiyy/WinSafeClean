# ADR 0050: WPF quick start scan targets

## Status

Accepted

## Context

The WPF UI has folder and file pickers, but first-time users still need to decide which path to scan before the workflow becomes obvious. This creates friction before the safe read-only evidence flow starts.

## Decision

Add WPF Read-Only Ops quick start scan targets for common user-owned locations: Downloads, Desktop, user Temp, and Local AppData.

The target provider is pure ViewModel/operation logic. It normalizes paths, removes duplicates, skips empty values, and filters out paths classified as protected Windows paths by `PathRiskClassifier`.

Selecting a quick start target only fills the Scan target path and suggests a report output path. It does not run scan, plan, preflight, quarantine, restore, delete, or clean.

## Consequences

- First-time users can start from common locations without manually typing paths.
- Protected Windows locations are not offered as quick start targets.
- The quick start buttons are convenience shortcuts only; all command execution still goes through the existing read-only runner and output protections.
- No deletion or file-moving capability is added.
