# Implementation Plan: RabbitMQ Transport

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/002-rabbitmq-transport/spec.md`

## Summary

RabbitMQ transport is an existing Bondstone package that provides thin
native-driver envelope dispatch and opt-in receive workers over
`RabbitMQ.Client`. It lets applications route Bondstone durable envelopes
through RabbitMQ while preserving application ownership of RabbitMQ topology,
credentials, retry, dead-letter, prefetch/concurrency, and monitoring policy.
The current receive worker ingests deliveries into Bondstone's durable incoming
inbox before acknowledging native deliveries.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `RabbitMQ.Client`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`

**Storage**: RabbitMQ transport owns no storage. Durable incoming inbox ingestion uses provider-neutral persistence contracts resolved from host/module persistence.

**Testing**: xUnit unit and integration tests in `tests/Bondstone.Transport.RabbitMq.Tests`; RabbitMQ integration coverage uses Testcontainers.

**Target Platform**: Packable .NET library package for hosts that already own RabbitMQ topology.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve thin adapter positioning; do not make Bondstone own RabbitMQ topology, provisioning, retry, dead-letter policy, credentials, prefetch/concurrency, or monitoring.
- Preserve manual acknowledgement after direct receive or durable incoming inbox ingestion succeeds.
- Preserve durable incoming inbox ingestion as the public receive worker mode.
- Preserve durable command and event semantics from `../../docs/architecture.md`.
- Preserve package dependency direction from `../../docs/packaging.md`.
- Preserve public API compatibility review for exposed setup APIs and option types.

**Scale/Scope**:

- Source/docs: 12 files, 518 lines under `src/Bondstone.Transport.RabbitMq`.
- Tests/docs: 7 files, 1,739 lines under `tests/Bondstone.Transport.RabbitMq.Tests`.
- Total migrated scope: 19 files and 2,257 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. RabbitMQ transport is a packable library adapter, not broker topology or application runtime ownership.
- **Durable Identities And Message Semantics**: Pass. Command and event receive ingestion resolve durable handler/subscriber identities through Bondstone registries.
- **Package Boundaries And Public API Compatibility**: Pass with caution. Public setup APIs and option types are packable package surface; future API changes require public API review.
- **Persistence And Transport Ownership**: Pass. RabbitMQ adapter translates native deliveries into durable envelopes/inbox records and leaves RabbitMQ topology/policy application-owned.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests plus RabbitMQ Testcontainers integration tests.

## Project Structure

### Documentation (this feature)

```text
specs/002-rabbitmq-transport/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Transport.RabbitMq/
├── Bondstone.Transport.RabbitMq.csproj
├── README.md
├── AGENTS.md
├── BondstoneRabbitMqBuilderExtensions.cs
├── RabbitMqEnvelopeDestination.cs
├── RabbitMqEnvelopeDispatcher.cs
├── RabbitMqEnvelopeDispatcherOptions.cs
├── RabbitMqReceiveWorker.cs
├── RabbitMqReceiveWorkerLogEvents.cs
├── RabbitMqReceiveWorkerOptions.cs
├── RabbitMqReceiveWorkerRegistration.cs
└── Properties/AssemblyInfo.cs
```

### Tests

```text
tests/Bondstone.Transport.RabbitMq.Tests/
├── Bondstone.Transport.RabbitMq.Tests.csproj
├── README.md
├── AGENTS.md
├── RabbitMqBuilderTests.cs
├── RabbitMqFixture.cs
├── RabbitMqIntegrationTests.cs
└── RabbitMqReceiveWorkerTests.cs
```

**Structure Decision**: Keep the migrated feature aligned with existing package and test project boundaries. No source movement is part of this migration.

## Reconstructed Implementation Approach

### Phase 1: Public Setup API

The feature exposes `UseRabbitMqDispatcher(...)` from `BondstoneBuilder` and
`BondstoneOutboxBuilder`, configures `RabbitMqEnvelopeDispatcherOptions`,
registers `RabbitMqEnvelopeDispatcher` as `IDurableEnvelopeDispatcher`, and
marks the outbox transport as `RabbitMq`.

It also exposes `UseRabbitMqReceiveWorker(...)` from `BondstoneBuilder`, builds
`RabbitMqReceiveWorkerOptions`, stores immutable worker registrations, and
adds `RabbitMqReceiveWorker` as an `IHostedService`.

### Phase 2: Outbound Dispatcher

The dispatcher validates a configured destination resolver, serializes durable
message envelopes with Bondstone's serializer, and publishes to a host-provided
RabbitMQ `IChannel` with the resolved exchange, routing key, and mandatory
flag.

### Phase 3: Receive Worker Options And Native Settlement

Receive worker options require a queue name, optionally carry a consumer tag,
control native requeue on failure, and default source transport names to
`rabbitmq:{QueueName}`. The hosted worker starts one manual-ack RabbitMQ
consumer per registration, cancels consumers during shutdown, acknowledges
successful deliveries, and negatively acknowledges failures while logging
`ReceiveFailed` event id `2001`.

### Phase 4: Durable Incoming Inbox Ingestion

The worker deserializes native delivery bodies as Bondstone envelopes, creates
durable incoming inbox records with command handler or event subscriber inbox
keys, resolves the receiver module's durable incoming inbox boundary, saves
ingestion state, and acknowledges only after the durable boundary succeeds.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Unit"`
  - `dotnet test tests/Bondstone.Transport.RabbitMq.Tests/Bondstone.Transport.RabbitMq.Tests.csproj --configuration Release --filter "Category=Integration"`
- **Default gate**: `pnpm check`
- **Integration gate**: `pnpm backend:test:integration` when validating RabbitMQ Testcontainers coverage.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Dispatcher option validation exists but lacks focused unit tests for missing
  `ResolveDestination`, null exchange, empty routing key, and `Mandatory`
  forwarding.
- Receive worker lifecycle has limited direct coverage for multiple
  registrations, configured consumer tags, and cancel-on-stop behavior.
- Unsupported durable message kind rejection exists in
  `RabbitMqReceiveWorker.CreateIncomingInboxRecord(...)` but lacks a focused
  unit test.
