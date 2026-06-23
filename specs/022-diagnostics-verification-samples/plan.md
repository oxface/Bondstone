# Implementation Plan: Diagnostics, Verification, And Samples

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

## Summary

This migrated feature captures cross-cutting verification and proof surfaces: diagnostics helpers, diagnostic tests, public API baselines, package artifact tests, and sample adoption tests.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: OpenTelemetry-compatible `ActivitySource`/`Meter`, xUnit, PublicApiGenerator, Testcontainers for integration samples

**Scale/Scope**: 1,506 lines across diagnostics, public API/package tests, and sample tests, plus sample applications.

## Constitution Check

_GATE: Passed._

- Diagnostics avoid high-cardinality values.
- Public API compatibility is explicitly tested.
- Samples prove composition without becoming source of architecture truth.

## Project Structure

```text
src/Bondstone/Messaging/BondstoneMessagingDiagnostics.cs
src/Bondstone.Persistence/Persistence/BondstonePersistenceDiagnostics.cs
src/Bondstone.Persistence/Diagnostics/
tests/Bondstone.Tests/Diagnostics/
tests/Bondstone.PublicApi.Tests/
tests/Bondstone.Package.Tests/
tests/Bondstone.Samples.Tests/
samples/
```

## Reconstructed Implementation Approach

1. Define diagnostics helpers and low-cardinality tags.
2. Add activity/metric helper tests.
3. Maintain public API baselines for packable packages.
4. Verify package artifacts after packing.
5. Exercise sample modular monolith and broker adapter flows.

## Verification Strategy

```bash
pnpm backend:build
pnpm backend:test
pnpm backend:pack
pnpm backend:test:integration
```

## Gaps And Follow-Up Candidates

- Publishing, deprecation, and release metadata changes remain maintainer-owned release actions.
