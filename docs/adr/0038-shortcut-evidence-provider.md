# ADR 0038: Windows shortcut evidence provider

Date: 2026-05-10

## Status

Accepted

## Context

Windows `.lnk` shortcuts in Desktop and Start Menu locations can be the visible entry point for applications and tools. If a scanned file is the target of a shortcut, treating it as a cleanup candidate can break a user-facing launch path even when the file also has cache-like evidence.

The project already models `ShortcutReference` evidence, but the default Windows evidence provider set did not collect it.

## Decision

Add a read-only shortcut evidence provider. The default Windows source scans common user and machine Desktop, Start Menu, and Programs folders for `.lnk` files, skips reparse-point directories, and reads shortcut metadata through Windows Shell Link COM interfaces in read-only mode.

The provider emits `ShortcutReference` evidence when a shortcut target path matches the scanned path. Invalid, unreadable, or malformed shortcut files are skipped.

Shortcut evidence is treated as active reference evidence by cleanup plan generation. It can force a conservative `Keep` action and must not make an item eligible for quarantine or deletion.

## Consequences

Reports can explain visible user launch references that were previously missing. Shortcut evidence only increases caution. The provider does not resolve targets, execute shortcuts, modify shortcut files, write registry values, use `Win32_Product`, or perform cleanup actions.
