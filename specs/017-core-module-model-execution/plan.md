# Implementation Plan: Core Module Model And Execution

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

## Summary

This migrated feature captures Bondstone's core module runtime: module registration, route registries, command/query execution, validators, event subscriber registration/execution, receive pipelines, runtime descriptors, and execution context flow.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence` contracts, Microsoft dependency injection

**Testing**: xUnit `Unit` tests in `tests/Bondstone.Tests/Modules`

**Scale/Scope**: 6,604 lines across module source and focused module tests.

## Constitution Check

_GATE: Passed._

- Core module model remains in `Bondstone`.
- Durable identities are explicit for durable commands/events.
- Persistence and transport remain separate package responsibilities.

## Project Structure

```text
src/Bondstone/Modules/
├── Contracts/
├── Events/
├── Execution/
├── Registration/
└── Routing/

tests/Bondstone.Tests/Modules/
├── ModuleCommandRegistrationTests.cs
├── ModuleQueryRegistrationTests.cs
├── ModuleEventRegistrationTests.cs
├── ModuleRegistrationTests.cs
├── ModuleReceivePipelineTests.cs
└── ModuleEventSubscriberExecutionTests.cs
```

## Reconstructed Implementation Approach

1. Register module metadata and durable messaging capability.
2. Register command/query handlers, validators, published events, and subscribers.
3. Resolve routes and subscribers by module/type/durable message identity.
4. Execute handlers through module runtime context and transaction hooks.
5. Route durable receive pipelines through command/event runtimes and direct inbox behavior.

## Verification Strategy

```bash
pnpm backend:build
dotnet test tests/Bondstone.Tests/Bondstone.Tests.csproj --configuration Release --no-build --filter "Category=Unit"
```

## Gaps And Follow-Up Candidates

- Broker delivery and hosted workers are separate transport/hosting features.
