# ADR 0040: Microsoft Store package evidence provider

Date: 2026-05-10

## Status

Accepted

## Context

Microsoft Store apps commonly store package installation files under `Program Files\WindowsApps` and per-user data under `AppData\Local\Packages`. These paths are application-owned and can contain package binaries, settings, caches, and state. Treating items under those roots as generic temporary files can break apps or user data.

The project already has a `MicrosoftStorePackage` evidence type, but the default Windows evidence provider set did not collect package ownership evidence.

## Decision

Add a read-only Microsoft Store package evidence provider. The default source reads top-level directories from common package roots:

- `%ProgramFiles%\WindowsApps` as package install locations
- `%LocalAppData%\Packages` as per-user package data roots

The source skips missing or inaccessible roots and reparse-point package directories. It does not use package management APIs, does not request elevation, does not uninstall or repair packages, and does not modify files or registry values.

The provider emits `MicrosoftStorePackage` evidence when the scanned path is equal to, or inside, a discovered package root. Microsoft Store package evidence is treated as installed-application evidence by cleanup plan generation. It can force a conservative `Keep` action and must not make an item eligible for quarantine or deletion.

## Consequences

Reports can now explain Microsoft Store package ownership for installed package files and package data roots. This intentionally favors false negatives over false cleanup permission: inaccessible package roots are skipped, and package evidence only increases caution.
