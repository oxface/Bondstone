# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]

**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. Use it as
the execution workflow for Bondstone package, docs, sample, and repository
changes.

## Summary

[Primary requirement and technical approach. Name the package, doc, sample, or workflow boundary being changed.]

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: [Bondstone packages and external dependencies, e.g. EF Core, Npgsql, RabbitMQ.Client, Azure.Messaging.ServiceBus, Microsoft.Extensions.*]

**Storage**: [N/A | EF Core mappings | PostgreSQL behavior | operation/outbox/inbox records | app-owned migrations]

**Testing**: xUnit with `Category` traits (`Unit`, `Application`, `Integration`, `Package`); Testcontainers when provider infrastructure is required

**Target Platform**: Packable .NET library packages and repository tooling

**Project Type**: .NET library/package monorepo

**Performance Goals**: [Domain-specific target or "Not specified"]

**Constraints**:

- Preserve Bondstone as a library for durable module boundaries, not an application platform, workflow engine, generic bus, broker topology manager, or code generator.
- Preserve package dependency direction and explicit public/package-local contracts.
- Preserve durable identity and message semantics for commands, integration events, domain events, outbox, inbox, and operation observation.
- Keep topology, credentials, retry, dead-letter, retention, migrations, and monitoring application-owned unless architecture changes explicitly approve otherwise.
- Treat public/protected API changes as compatibility-sensitive.

**Scale/Scope**: [Files, packages, docs, samples, and test projects affected]

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

- **Library Boundary First**: [Pass/Fail with evidence]
- **Durable Identities And Message Semantics**: [Pass/Fail/N/A with evidence]
- **Package Boundaries And Public API Compatibility**: [Pass/Fail/N/A with evidence]
- **Persistence And Transport Ownership**: [Pass/Fail/N/A with evidence]
- **Evidence-Based Verification**: [Pass/Fail with focused verification plan]

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

Remove optional artifacts that are not needed for this feature.

### Source Code

```text
src/
├── Bondstone/
├── Bondstone.Persistence/
├── Bondstone.Persistence.EntityFrameworkCore/
├── Bondstone.Persistence.EntityFrameworkCore.Postgres/
├── Bondstone.Hosting/
├── Bondstone.Transport.Local/
├── Bondstone.Transport.RabbitMq/
└── Bondstone.Transport.ServiceBus/

tests/
├── Bondstone.Tests/
├── Bondstone.Composition.Tests/
├── Bondstone.Hosting.Tests/
├── Bondstone.Persistence.EntityFrameworkCore.Tests/
├── Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests/
├── Bondstone.PublicApi.Tests/
├── Bondstone.Transport.Local.Tests/
├── Bondstone.Transport.RabbitMq.Tests/
├── Bondstone.Transport.ServiceBus.Tests/
├── Bondstone.Samples.Tests/
└── Bondstone.Package.Tests/

samples/
└── ModularMonolith*/

docs/
└── [affected docs]
```

**Structure Decision**: [List only the concrete paths touched by this feature and why they own the change]

## Phase 0: Research

- [Architecture, package, public API, testing, or docs question]
- [Decision and alternatives rejected]

## Phase 1: Design

- **Contracts/Public API**: [Types, methods, package IDs, namespaces, or explicit "No public API change"]
- **Persistence/Data Model**: [EF mappings, table shape, migrations ownership, or N/A]
- **Transport/Hosting**: [Dispatch, receive, worker, settlement, retry, topology boundary, or N/A]
- **Diagnostics/Operations**: [Logs, tracing, setup codes, operation observation, docs, or N/A]
- **Docs/Samples**: [Docs and samples that must change]

## Verification Strategy

- **Focused checks**: [Package-specific `dotnet test` or docs/template checks]
- **Default gate**: `pnpm check`
- **Integration gate**: `pnpm backend:test:integration` when provider behavior, Testcontainers, persistence semantics, broker adapter behavior, or sample extraction proofs are affected
- **Package gate**: `pnpm backend:pack` when package artifacts, package metadata, or public API baselines are affected

## Complexity Tracking

> Fill only if a constitution gate fails or the design adds notable complexity.

| Concern   | Why Needed | Simpler Alternative Rejected Because |
| --------- | ---------- | ------------------------------------ |
| [Concern] | [Reason]   | [Reason]                             |
