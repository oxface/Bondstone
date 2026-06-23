# Implementation Plan: Service Bus Transport

**Branch**: `N/A - migrated existing functionality` | **Date**: 2026-06-23 | **Spec**: [spec.md](spec.md)

**Input**: Migrated feature specification from `specs/003-servicebus-transport/spec.md`

## Summary

Service Bus transport is an existing Bondstone package that provides thin
native-driver envelope dispatch and opt-in receive workers over
`Azure.Messaging.ServiceBus`. It lets applications route Bondstone durable
envelopes through Azure Service Bus while preserving application ownership of
Service Bus topology, credentials, retry, dead-letter, rules, concurrency, lock
renewal, and monitoring policy. The current receive worker ingests deliveries
into Bondstone's durable incoming inbox before completing native Service Bus
messages.

## Technical Context

**Language/Version**: C# on `net10.0`

**Primary Dependencies**: `Bondstone`, `Bondstone.Persistence`, `Azure.Messaging.ServiceBus`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`

**Storage**: Service Bus transport owns no storage. Durable incoming inbox ingestion uses provider-neutral persistence contracts resolved from host/module persistence.

**Testing**: xUnit unit and integration tests in `tests/Bondstone.Transport.ServiceBus.Tests`; Azure Service Bus integration coverage uses Testcontainers.

**Target Platform**: Packable .NET library package for hosts that already own Azure Service Bus topology.

**Project Type**: Library package in a .NET monorepo.

**Performance Goals**: No explicit performance target is documented for the migrated feature.

**Constraints**:

- Preserve thin adapter positioning; do not make Bondstone own Service Bus
  topology, provisioning, retry, dead-letter policy, credentials, rules,
  concurrency, lock renewal policy, or monitoring.
- Preserve manual completion after durable incoming inbox ingestion succeeds.
- Preserve durable incoming inbox ingestion as the Service Bus receive worker
  behavior.
- Preserve Service Bus `PeekLock` mode and disabled `AutoCompleteMessages`.
- Preserve durable command and event semantics from `../../docs/architecture.md`.
- Preserve package dependency direction from `../../docs/packaging.md`.
- Preserve public API compatibility review for exposed setup APIs and option types.

**Scale/Scope**:

- Source/docs: 11 files, 547 lines under `src/Bondstone.Transport.ServiceBus`.
- Tests/docs: 7 files, 1,324 lines under `tests/Bondstone.Transport.ServiceBus.Tests`.
- Total migrated scope: 18 files and 1,871 lines excluding ignored build output.

## Constitution Check

_GATE: Passed for migrated behavior based on current implementation evidence._

- **Library Boundary First**: Pass. Service Bus transport is a packable library adapter, not broker topology or application runtime ownership.
- **Durable Identities And Message Semantics**: Pass. Command and event receive ingestion resolve durable handler/subscriber identities through Bondstone registries.
- **Package Boundaries And Public API Compatibility**: Pass with caution. Public setup APIs and option types are packable package surface; future API changes require public API review.
- **Persistence And Transport Ownership**: Pass. Service Bus adapter translates native deliveries into durable envelopes/inbox records and leaves Service Bus topology/policy application-owned.
- **Evidence-Based Verification**: Pass. Behavior is covered by focused unit tests plus Service Bus Testcontainers integration tests.

## Project Structure

### Documentation (this feature)

```text
specs/003-servicebus-transport/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code

```text
src/Bondstone.Transport.ServiceBus/
├── Bondstone.Transport.ServiceBus.csproj
├── README.md
├── AGENTS.md
├── BondstoneServiceBusBuilderExtensions.cs
├── ServiceBusEnvelopeDispatcher.cs
├── ServiceBusEnvelopeDispatcherOptions.cs
├── ServiceBusReceiveWorker.cs
├── ServiceBusReceiveWorkerLogEvents.cs
├── ServiceBusReceiveWorkerOptions.cs
├── ServiceBusReceiveWorkerRegistration.cs
└── Properties/AssemblyInfo.cs
```

### Tests

```text
tests/Bondstone.Transport.ServiceBus.Tests/
├── Bondstone.Transport.ServiceBus.Tests.csproj
├── README.md
├── AGENTS.md
├── ServiceBusBuilderTests.cs
├── ServiceBusFixture.cs
├── ServiceBusIntegrationTests.cs
└── ServiceBusReceiveWorkerTests.cs
```

**Structure Decision**: Keep the migrated feature aligned with existing package and test project boundaries. No source movement is part of this migration.

## Reconstructed Implementation Approach

### Phase 1: Public Setup API

The feature exposes `UseServiceBusDispatcher(...)` from `BondstoneBuilder` and
`BondstoneOutboxBuilder`, configures `ServiceBusEnvelopeDispatcherOptions`,
registers `ServiceBusEnvelopeDispatcher` as `IDurableEnvelopeDispatcher`, and
marks the outbox transport as `ServiceBus`.

It also exposes `UseServiceBusReceiveWorker(...)` from `BondstoneBuilder`,
builds `ServiceBusReceiveWorkerOptions`, stores immutable worker
registrations, and adds `ServiceBusReceiveWorker` as an `IHostedService`.

### Phase 2: Outbound Dispatcher

The dispatcher validates a configured entity-name resolver, serializes durable
message envelopes with Bondstone's serializer, creates native Service Bus
messages with Bondstone metadata, caches `ServiceBusSender` instances by entity
name, and sends through a host-provided `ServiceBusClient`.

### Phase 3: Receive Worker Options And Native Processors

Receive worker options require either a queue name or a topic name plus
subscription name, clone processor options, enforce manual completion, reject
`ReceiveAndDelete`, and default source transport names to
`servicebus:{QueueName}` or `servicebus:{TopicName}/{SubscriptionName}`. The
hosted worker creates one Service Bus processor per registration, starts
processors, logs processor errors with `ReceiveFailed` event id `3001`, and
stops/disposes processors during shutdown.

### Phase 4: Durable Incoming Inbox Ingestion

The worker deserializes native message bodies as Bondstone envelopes, creates
durable incoming inbox records with command handler or event subscriber inbox
keys, resolves the receiver module's durable incoming inbox boundary, saves
ingestion state, and completes the Service Bus message only after the durable
boundary succeeds.

## Verification Strategy

- **Focused checks**:
  - `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Unit"`
  - `dotnet test tests/Bondstone.Transport.ServiceBus.Tests/Bondstone.Transport.ServiceBus.Tests.csproj --configuration Release --filter "Category=Integration"`
- **Default gate**: `pnpm check`
- **Integration gate**: `pnpm backend:test:integration` when validating Service Bus Testcontainers coverage.
- **Package gate**: `pnpm backend:pack` when package metadata or public API baselines are affected.

## Complexity Tracking

> Fill only if Constitution Check has violations that must be justified.

No constitution violations were identified for the migrated feature.

## Gaps And Follow-Up Candidates

- Dispatcher option validation exists but lacks focused unit tests for missing
  `ResolveEntityName`, empty entity names, default/custom `ContentType`,
  `CorrelationId`, application properties, sender caching, and sender disposal.
- Receive worker entity validation has coverage for missing entities but lacks
  focused coverage for the "both queue and topic/subscription configured" case.
- Receive worker lifecycle has limited direct coverage for multiple
  registrations, queue versus topic processor creation, processor start/stop
  and disposal, and cloning all supported processor option fields.
- Unsupported durable message kind rejection exists in
  `ServiceBusReceiveWorker.CreateIncomingInboxRecord(...)` but lacks a focused
  unit test.
