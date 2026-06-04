# 0003 Package Boundaries And Target Framework

Status: Accepted
Application: Partially Applied
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

## Applied To

- Code:
  - [Bondstone.slnx](../../Bondstone.slnx)
  - [Directory.Build.props](../../Directory.Build.props)
  - [Directory.Packages.props](../../Directory.Packages.props)
  - [src/Bondstone/Bondstone.csproj](../../src/Bondstone/Bondstone.csproj)
  - [src/Bondstone.EntityFrameworkCore/Bondstone.EntityFrameworkCore.csproj](../../src/Bondstone.EntityFrameworkCore/Bondstone.EntityFrameworkCore.csproj)
  - [src/Bondstone.EntityFrameworkCore.Postgres/Bondstone.EntityFrameworkCore.Postgres.csproj](../../src/Bondstone.EntityFrameworkCore.Postgres/Bondstone.EntityFrameworkCore.Postgres.csproj)
  - [src/Bondstone.Transport.Rebus/Bondstone.Transport.Rebus.csproj](../../src/Bondstone.Transport.Rebus/Bondstone.Transport.Rebus.csproj)
- Stable docs:
  - [docs/packaging.md](../packaging.md)
  - [docs/README.md](../README.md)
- Agent instructions:
  - [AGENTS.md](../../AGENTS.md)
- Skills: Not applicable.

## Verification

Read back [docs/packaging.md](../packaging.md),
[docs/README.md](../README.md), and [AGENTS.md](../../AGENTS.md). Ran
`pnpm verify`, which covered restore, build, test, and pack for the empty
package scaffold. Real implementation and package API verification remain
pending.
