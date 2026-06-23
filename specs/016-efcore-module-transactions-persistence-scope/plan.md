# Implementation Plan: EF Core Module Transactions And Persistence Scope

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

## Summary

This migrated feature captures generic EF Core module persistence composition: persistence scopes, module transaction runner, module runtime registry integration, model-builder aggregation, service collection setup, and mapping validation.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: EF Core `10.0.8`, `Bondstone`, `Bondstone.Persistence`, Microsoft dependency injection

**Storage**: Generic EF Core DbContext; provider-specific transaction semantics are out of scope.

**Scale/Scope**: 1,731 lines across generic EF Core persistence source/tests.

## Constitution Check

_GATE: Passed._

- Generic EF Core package stays provider-neutral.
- Module persistence stays module-owned.
- Provider-specific PostgreSQL behavior remains separate.

## Project Structure

```text
src/Bondstone.Persistence.EntityFrameworkCore/Persistence/
├── BondstoneEntityFrameworkCoreServiceCollectionExtensions.cs
├── BondstoneEntityFrameworkCoreModuleBuilderExtensions.cs
├── BondstoneModelBuilderExtensions.cs
├── EntityFrameworkCorePersistenceScope.cs
├── EntityFrameworkCoreModuleTransactionRunner.cs
├── EntityFrameworkCoreModuleRuntimeRegistry.cs
├── EntityFrameworkCoreModuleRuntimeDescriptor.cs
└── Contracts/IEntityFrameworkCorePersistenceScope.cs

tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Persistence/
├── EntityFrameworkCorePersistenceScopeTests.cs
├── EntityFrameworkCoreModuleTransactionBehaviorTests.cs
├── BondstoneModelBuilderExtensionsTests.cs
└── BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests.cs
```

## Reconstructed Implementation Approach

1. Provide an EF Core persistence scope over the current DbContext.
2. Add module transaction runner behavior around module execution.
3. Register module EF Core persistence metadata and runtime descriptors.
4. Aggregate durable mappings through model-builder extensions.
5. Validate required mappings and expose clear diagnostics.

## Verification Strategy

```bash
pnpm backend:build
dotnet test tests/Bondstone.Persistence.EntityFrameworkCore.Tests/Bondstone.Persistence.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --filter "Category=Unit|Category=Application"
```

## Gaps And Follow-Up Candidates

- Real PostgreSQL transaction behavior is provider-backed and migrated separately.
