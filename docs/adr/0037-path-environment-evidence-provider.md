# ADR 0037: PATH environment evidence provider

Date: 2026-05-10

## Status

Accepted

## Context

Windows command lookup can depend on the `PATH` environment variable. If a directory is listed in `PATH`, or a scanned executable sits directly inside a listed directory, removing that item can break command-line workflows or application integrations even when the item also looks cache-like.

The project already models `PathEnvironmentReference` evidence, but the default Windows evidence provider set did not collect it.

## Decision

Add a read-only Windows PATH evidence provider. It reads process, user, and machine `PATH` values through environment APIs, splits entries with the platform path separator, expands environment variables, trims surrounding quotes, and normalizes candidate paths before comparison.

The provider emits `PathEnvironmentReference` evidence when:

- the scanned path exactly matches a PATH directory entry
- the scanned file's parent directory matches a PATH directory entry

Invalid PATH entries are ignored. The provider does not mutate environment variables, registry values, files, services, tasks, or installed applications, and it does not use `Win32_Product`.

`PathEnvironmentReference` is treated as active reference evidence by cleanup plan generation. It can force a conservative `Keep` action and must not make an item eligible for quarantine or deletion.

## Consequences

Reports can now explain command-search references that were previously invisible. This improves ownership context while preserving the safety model: PATH evidence only increases caution and never grants cleanup permission.
