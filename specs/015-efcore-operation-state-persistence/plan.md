# Implementation Plan: EF Core Operation State Persistence

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

## Summary

This migrated feature captures generic EF Core operation-state persistence: entity mapping, operation-state store behavior, module-scoped store behavior, expiration candidate lookup, and generic EF Core setup registration.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone.Persistence`, EF Core `10.0.8`, Microsoft dependency injection

**Storage**: Generic EF Core `DbContext`; provider-specific SQL behavior is out of scope.

**Testing**: xUnit `Unit` and `Application` tests using EF Core InMemory for mapping/change-tracker behavior.

## Constitution Check

_GATE: Passed for migrated behavior._

- Library boundary: generic EF Core package.
- Durable semantics: operation-state read model, not orchestration.
- Package boundary: no PostgreSQL SQL in this feature.
- Verification: package-local tests cover mapping and store behavior.

## Project Structure

```text
src/Bondstone.Persistence.EntityFrameworkCore/Operations/
├── OperationStateEntity.cs
├── OperationStateEntityConfiguration.cs
├── EntityFrameworkCoreDurableOperationStateStore.cs
└── EntityFrameworkCoreModuleDurableOperationStateStore.cs

tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Operations/
├── OperationStateEntityTests.cs
└── EntityFrameworkCoreDurableOperationStateStoreTests.cs
```

## Reconstructed Implementation Approach

1. Map `DurableOperationState` to an EF Core entity and table metadata.
2. Implement get/save behavior over the current DbContext without saving changes.
3. Implement module-scoped operation state store wrapper.
4. Implement expiration candidate lookup over pending/running stale rows.
5. Register the store and mapping through generic EF Core setup.

## Verification Strategy

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```

## Gaps And Follow-Up Candidates

- PostgreSQL operation-state persistence semantics require provider integration tests.
- Consumer migration rollout remains application-owned.
