# 0021 Fluent Service Composition Guardrails

Status: Amended
Application: Applied
Date: 2026-06-05

## Context

Bondstone now has separate packages for core contracts, PostgreSQL
persistence, Rebus transport, and hosted workers. That package split keeps
ownership clear, but independent service-registration methods can produce
half-functional configurations:

- a hosted outbox worker without an outbox persistence provider;
- a hosted outbox worker without an outbox transport;
- a transport adapter without any dispatcher or worker, which may be valid for
  manual composition but should be intentional.

Compile-time prevention of every invalid combination is not practical across
independent NuGet packages without turning Bondstone into a rigid framework or
forcing provider and transport packages to reference each other. Bondstone
still needs a preferred registration path that makes composition explicit and
can fail early for harmful hosted setups.

## Decision

`Bondstone` provides a lightweight `AddBondstone` builder and an outbox
capability model.

Provider, transport, and hosting packages add extension methods to the shared
builder:

- PostgreSQL persistence marks outbox persistence capability;
- Rebus outbox transport marks outbox transport capability;
- hosted outbox worker registration marks dispatcher and worker capability.

The builder validates after configuration. Hosted or dispatcher-based outbox
processing requires both outbox persistence and outbox transport capabilities.
Transport-only or persistence-only registration remains valid because a
consumer may be writing outbox rows, testing transport mapping, or composing a
manual worker.

Low-level registration methods remain available for advanced composition and
tests, but the preferred application setup uses `AddBondstone`.

## Consequences

Common host setup becomes harder to misconfigure while package dependencies
remain one-directional:

```text
Bondstone.Hosting -> Bondstone
Bondstone.Transport.Rebus -> Bondstone
Bondstone.EntityFrameworkCore.Postgres -> Bondstone.EntityFrameworkCore
Bondstone.EntityFrameworkCore.Postgres -> Bondstone
```

The capability model is a runtime guardrail, not a type-state DSL. Consumers
can still bypass it with low-level DI calls when they need custom composition.

The first validation rules cover outbox dispatching only. Inbox workers,
cleanup workers, transport receive pipelines, module-scoped transaction
helpers, and stricter validation modes remain future decisions.

## Amendment 2026-06-07

Package-specific `AddBondstone` extensions may register configuration
validators with the shared builder when a validation rule must compare state
owned by multiple packages after host configuration completes. Validators run
once at the end of `AddBondstone` composition and receive a snapshot of the
core module and command-route metadata needed for durable topology checks.

These validators are not a replacement for .NET options validation. Runtime
option objects should continue to use `IValidateOptions<TOptions>` or
equivalent options validation. Bondstone configuration validators are for
composition and topology checks that span registries, capabilities, routes,
and package-owned builder state rather than one options object.

Validation should remain grouped by lifecycle. Public API argument guards,
options validation, `AddBondstone` composition validation, runtime envelope
and persistence-model checks, and domain/entity invariants are separate
validation surfaces and should not be forced through one mechanism.

This keeps runtime package collaboration on explicit contracts instead of
friend assembly access. `InternalsVisibleTo` remains reserved for test
assemblies.

## Related Decisions

- [0003 Package Boundaries And Target Framework](0003-package-boundaries-and-target-framework.md)
- [0017 Outbox Dispatcher Composition](0017-outbox-dispatcher-composition.md)
- [0018 Rebus Outbox Transport Adapter](0018-rebus-outbox-transport-adapter.md)
- [0020 Neutral Hosted Worker Package](0020-neutral-hosted-worker-package.md)

## Application Notes

- Current contract: `AddBondstone` is the preferred host registration entry
  point. Package-specific builder extensions register their services, mark
  capabilities, and may register configuration validators. The builder runs
  validators once after host configuration completes. The outbox validator
  rejects dispatcher or worker processing when persistence or transport
  capability is missing.
- Stable docs: Current composition guidance is described in
  [docs/architecture/hosting.md](../architecture/hosting.md), module and
  durable messaging validation in
  [docs/architecture/modules.md](../architecture/modules.md) and
  [docs/architecture/messaging.md](../architecture/messaging.md), Rebus
  receive topology validation in
  [docs/architecture/transport-rebus.md](../architecture/transport-rebus.md),
  package rules in [docs/packaging.md](../packaging.md), and extraction state
  in [docs/extraction-plan.md](../extraction-plan.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) requires ADR review before
  public API, package-boundary, provider, transport, or durable runtime
  changes. It also reserves `InternalsVisibleTo` for test assemblies rather
  than production package collaboration.
- Application evidence: Core builder types, PostgreSQL/Rebus/Hosting builder
  extensions, configuration validator contracts, outbox/module/Rebus receive
  topology validators, and focused tests are added.
- Pending or deferred: Broader inbox and maintenance-worker validation remains
  deferred.

## Verification

Read back this ADR and affected stable docs. For the original application, ran
no-restore solution build, fast unit/application tests, pack, formatting, and
diff checks. For the 2026-06-07 amendment, ran focused builder/Rebus tests,
`git diff --check`, and `pnpm check`.
