# ADR 0045: Visible result copy and export

## Status

Accepted

## Context

Users need a lower-friction way to review or share the rows currently visible in the WPF Scan Report and Cleanup Plan lists after applying search, risk, type, action, and sort controls. Copying paths by hand is slow and increases the chance of using the wrong item.

The exported data can include local paths, risk labels, suggested actions, reasons, evidence, and quarantine preview paths. That data may reveal private folder names or application usage, so the UI must keep the action explicit and local.

## Decision

The WPF UI adds Copy Visible and Export CSV actions to the Scan Report and Cleanup Plan list headers.

The actions operate only on the rows currently visible in the UI list after filters and sorting are applied. Copy Visible writes CSV text to the Windows clipboard. Export CSV writes the same CSV text to a user-selected file path. Neither action changes scan reports, cleanup plans, candidate files, quarantine files, restore metadata, or cleanup state.

The CSV formatter is implemented as a pure ViewModel helper with tests for headers and CSV escaping. UI code is limited to collecting visible rows, invoking the formatter, and using explicit clipboard or Save dialog actions.

## Consequences

- Review and external handoff are easier without adding any cleanup execution path.
- Exported files and clipboard contents may contain sensitive local paths and evidence, so sharing remains the user's explicit decision.
- The feature does not redact data. Users should use existing redacted report generation when they need privacy-preserving files.
- The Scan and Plan list controls remain read-only; this ADR does not authorize quarantine, restore, delete, or clean actions from the UI.
