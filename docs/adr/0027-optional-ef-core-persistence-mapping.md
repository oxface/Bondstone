# 0027 Optional EF Core Persistence Mapping

Status: Accepted
Application: Applied
Date: 2026-06-07

## Context

`Bondstone.EntityFrameworkCore` currently exposes `ApplyBondstonePersistence`
as the single user-facing model-mapping helper. That helper applies the
provider-neutral Bondstone persistence mappings for outbox, inbox, and
operation state.

This is convenient for early durable messaging, but it makes module-owned EF
persistence feel heavier than necessary. A module may want Bondstone's module
command transaction behavior without durable messaging. Another module may use
only outbound durable commands. A receive-only module may need inbox mapping
but not outbox mapping. Requiring every consumer-owned DbContext to map every
Bondstone persistence table blurs the distinction between module persistence
and durable messaging capabilities.

Bondstone should stay modular-monolith-first and library-shaped. Consumers
should opt into durable tables because their chosen module capabilities need
them, not because any Bondstone EF integration was enabled.

## Decision

Bondstone should add granular EF Core mapping helpers while keeping the current
convenience method.

The intended shape is:

```csharp
modelBuilder.ApplyBondstoneOutbox();
modelBuilder.ApplyBondstoneInbox();
modelBuilder.ApplyBondstoneOperationState();
modelBuilder.ApplyBondstonePersistence();
```

`ApplyBondstonePersistence` should remain the convenience method for hosts that
want the full current persistence shape. Granular helpers should let hosts map
only the durable persistence surfaces required by their configured module
capabilities.

Capability validation should connect module declarations to required
persistence pieces. Examples:

- a module that only opts into EF-backed module command transactions should
  not require inbox or outbox mappings;
- a module that sends durable commands should require outbox persistence;
- a module that receives durable commands should require inbox persistence;
- operation-state integration should require operation-state persistence when
  enabled;
- provider-specific packages should validate that provider SQL targets the
  mapped tables and schema.

This decision does not require a separate package split. It changes the EF
Core mapping surface and validation rules.

## Consequences

Persistence-only module scenarios become lighter and more explicit.

Durable messaging configuration becomes easier to validate because the
required storage pieces are tied to declared capabilities rather than implied
by a blanket model-mapping helper.

The convenience method still supports the current simple setup and avoids
breaking existing users that want all current tables.

Validation becomes more important. Bondstone will need a way to detect or
record which mapping helpers were applied, or otherwise produce useful errors
when configured module capabilities and actual EF model shape diverge.

Provider tests must cover granular mappings, schema-aware SQL, and missing
mapping diagnostics.

## Related Decisions

- [0009 EF Core Persistence Entities And Migrations](0009-ef-core-persistence-entities-and-migrations.md)
- [0010 PostgreSQL Provider And Integration Testing](0010-postgresql-provider-and-integration-testing.md)
- [0016 EF Core Persistence Scope](0016-ef-core-persistence-scope.md)
- [0021 Fluent Service Composition Guardrails](0021-fluent-service-composition-guardrails.md)
- [0025 Module Command Execution Boundary](0025-module-command-execution-boundary.md)

## Application Notes

- Current contract: `ApplyBondstonePersistence` remains the convenience helper
  for the full generic EF Core persistence shape. `ApplyBondstoneOutbox`,
  `ApplyBondstoneInbox`, and `ApplyBondstoneOperationState` map only their
  respective provider-neutral EF Core durable persistence tables.
- Stable docs: Current mapping behavior is described in
  [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md)
  and [docs/setup.md](../setup.md). Cross-cutting persistence positioning is
  described in [docs/architecture/persistence.md](../architecture/persistence.md).
  Sequencing for optional mapping work is tracked in
  [docs/archive/mvp-plan.md](../archive/mvp-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, persistence, provider, migration, or compatibility changes.
- Application evidence: The provider-neutral EF Core model builder extensions
  expose granular mapping helpers, `ApplyBondstonePersistence` composes those
  helpers, and focused EF Core model tests cover each granular helper. EF
  module transaction behavior validates that modules using the current
  `UseDurableMessaging` capability with EF persistence have outbox and inbox
  mappings in the module DbContext model. EF operation-state persistence now
  fails with a clear `ApplyBondstoneOperationState()` mapping error when
  operation tracking uses a DbContext that does not map operation state.
- Pending or deferred: None for the granular EF mapping decision. Broader
  provider-specific schema validation remains future provider-validation work.

## Verification

Read back the ADR and related stable docs. Focused EF Core model tests cover
the accepted granular mapping surface and the current durable-messaging EF
mapping validation.
