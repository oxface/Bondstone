# 0003 Package Boundaries And Target Framework

Status: Archived
Application: Not Applicable
Date: 2026-06-03

## Context

Bondstone is being extracted from existing in-repository projects into NuGet
packages. The package boundaries should preserve clear dependency direction and
allow consumers to take only the core abstractions or the provider and
transport adapters they need.

The initial code already separates core module abstractions, EF Core
persistence, PostgreSQL-specific persistence behavior, and Rebus transport
behavior. Preserving that separation reduces extraction risk and creates a
natural package graph.

The target framework also needs to be explicit before packaging. The current
planned runtime is .NET 10.

## Decision

Ship one NuGet package per initial project, with package IDs matching project
names:

- `Bondstone`
- `Bondstone.EntityFrameworkCore`
- `Bondstone.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Rebus`

Target `net10.0` for the initial extraction.

Use this dependency direction:

```text
Bondstone
Bondstone.EntityFrameworkCore -> Bondstone
Bondstone.EntityFrameworkCore.Postgres -> Bondstone.EntityFrameworkCore
Bondstone.Transport.Rebus -> Bondstone
```

Provider-specific packages should contain provider-specific behavior only.
Transport-specific packages should contain transport adapter behavior only.
Core domain, command, messaging, and module abstractions should stay in
`Bondstone` unless a later ADR defines a narrower package split.

Use central package management through `Directory.Packages.props`.

## Consequences

Consumers can reference only the packages needed for their runtime shape.

The package graph keeps PostgreSQL concerns out of the core package and Rebus
concerns out of persistence packages.

The initial target is modern and narrow. Wider target framework support is
intentionally deferred until a later compatibility ADR evaluates demand and
maintenance cost.

Publishing multiple packages requires coordinated versioning and release
automation.

## Application Notes

- Current contract: Packages target `net10.0`, package IDs match project
  names, and package dependencies flow from core to provider, hosting,
  persistence, and transport adapters.
- Stable docs: Current package IDs, target framework, dependency direction, and
  versioning rules are described in [docs/packaging.md](../packaging.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) points agents to packaging
  docs before package-boundary or framework changes.
- Application evidence: Current package projects are included in the solution,
  package metadata is centrally managed, package IDs match project names, and
  dependency direction matches [docs/packaging.md](../packaging.md). The
  supported transport package set is now direct Local, RabbitMQ, and Azure
  Service Bus according to ADR 0036.
- Pending or deferred: None for this package-boundary application. Wider target
  framework support and independent package versioning remain separate future
  compatibility/release decisions.

## Verification

Read back [docs/packaging.md](../packaging.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm check`, which covered formatting, restore, build, fast tests, and pack
for the current package implementation. Phase 01 audit verification rechecked
current package projects and project-reference direction.
