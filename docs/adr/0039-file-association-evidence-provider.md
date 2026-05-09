# ADR 0039: Windows file association evidence provider

Date: 2026-05-10

## Status

Accepted

## Context

Windows file extensions can point to ProgID and shell verb commands under `Software\Classes`. If a scanned executable is used as an `open`, `edit`, or other shell command target, removing it can break file-opening workflows even when the file also has cache-like evidence.

The project needs this ownership signal without writing registry values or triggering application repair behavior.

## Decision

Add a read-only file association evidence provider. The default Windows source reads HKCU and HKLM `Software\Classes`, follows extension default ProgID values, and inspects direct and ProgID shell verb `command` entries. It uses registry read APIs only and reads values with `DoNotExpandEnvironmentNames`; command parsing handles environment expansion later through the existing service command parser.

The provider emits `FileAssociationReference` evidence when a file association command executable path matches the scanned path.

File association evidence is treated as active reference evidence by cleanup plan generation. It can force a conservative `Keep` action and must not make an item eligible for quarantine or deletion.

## Consequences

Reports can explain file-opening registrations that were previously invisible. This improves application ownership context while preserving the safety model: file association evidence only increases caution, never grants cleanup permission, never writes registry values, and never uses `Win32_Product`.
