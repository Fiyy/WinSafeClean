# ADR 0036: UI space use hints

Date: 2026-05-10

## Status

Accepted

## Context

Directory size calculation and Largest Items help users see what is large, but users still need a lightweight explanation of why a path might be using space. The project must avoid turning simple path names like `Cache` or `Temp` into cleanup permission.

## Decision

Add WPF-only space use hints for scan report items. The hints identify common patterns such as:

- protected Windows areas
- temporary-looking paths
- cache-like paths
- log-like paths
- crash dump-like paths
- user download locations

These hints are display-only. They do not change risk levels, suggested actions, evidence, cleanup plans, or CLI report schema.

## Consequences

The UI can explain large items more clearly without weakening the safety model. Future expansion of these hints into evidence or risk scoring requires separate tests and review, because path naming alone is not enough to prove safe cleanup.
