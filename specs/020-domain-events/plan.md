# Implementation Plan: Domain Events

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

## Summary

This migrated feature captures module-local domain event contracts and optional EF Core persistence support. Domain events remain local facts and are not automatically staged in the durable outbox.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, EF Core `10.0.8`, optional PostgreSQL integration tests

**Scale/Scope**: 1,586 lines across domain event source/tests.

## Constitution Check

_GATE: Passed._

- Domain events are module-local and distinct from integration events.
- EF Core persistence is optional and module-owned.

## Project Structure

```text
src/Bondstone/DomainEvents/
src/Bondstone.Persistence.EntityFrameworkCore/DomainEvents/
tests/Bondstone.Tests/DomainEvents/
tests/Bondstone.Persistence.EntityFrameworkCore.Tests/DomainEvents/
tests/Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/DomainEvents/
```

## Reconstructed Implementation Approach

1. Define domain event marker/source contracts and identity attribute.
2. Add EF Core collection behavior for tracked domain event sources.
3. Add optional domain event record entity and mapping.
4. Add module builder opt-in for EF domain event persistence.
5. Verify PostgreSQL transaction participation.

## Verification Strategy

```bash
pnpm backend:build
dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application|Category=Integration"
```

## Gaps And Follow-Up Candidates

- Automatic integration-event publishing from domain events is intentionally absent.
