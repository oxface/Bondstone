# Packaging

This document records the current Bondstone package boundary and target
framework direction.

## Target Framework

Bondstone targets `net10.0`.

## Package Set

Ship one NuGet package per package project. Package IDs match project names:

- `Bondstone`
- `Bondstone.Capabilities.DomainEvents`
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Persistence.Postgres`
- `Bondstone.Transport`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.ServiceBus`
- `Bondstone.Transport.RabbitMq`

`Bondstone.Transport.ServiceBus` and `Bondstone.Transport.RabbitMq` are the
production-oriented direct transport adapters. They include outgoing durable
outbox dispatch, provider-native receive topology, opt-in hosted receive
workers, and provider-backed receive integration tests. Broker administration
remains app-owned. Multi-transport outbox selection is implemented through
provider route candidates and `RoutedDurableOutboxTransport`.

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It exercises outbox/inbox receive semantics
through the provider-neutral receive pipelines, but it is not a production
broker adapter or hidden fallback.

`Bondstone.Persistence.Postgres` is PostgreSQL-specific and Dapper-backed
internally. It provides durable module messaging persistence without EF Core;
it is not a generic Dapper provider abstraction.

`Bondstone.Capabilities.DomainEvents` is intentionally small. It contains
module-local domain event capability contracts only; it is not a domain event
bus, a durable messaging package, or a provider runtime package.

`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` is the first
provider bridge for that capability. It owns EF Core change-tracker
collection, EF domain event record mapping, and the system pipeline behavior
that stages and clears domain events around observed EF module transactions.

`Bondstone.Persistence` contains provider-neutral durable persistence
contracts and records: durable envelopes, trace context, operation state,
outbox, inbox, transport-facing persistence contracts, and passive
module-runtime registration records. Module-aware runtime resolution and
module execution stay in `Bondstone`.

`Bondstone.Transport` contains provider-neutral transport topology diagnostic
contracts and shapes. Broker/client adapters depend on it; application setup
normally uses the concrete transport packages.

## Package Dependencies

Use this dependency direction:

- `Bondstone` stays at the core with no dependency on provider, transport
  adapter, or hosting packages. It may depend on the neutral
  `Bondstone.Persistence` and `Bondstone.Transport` contract packages.
- `Bondstone.Hosting` depends on `Bondstone` and `Bondstone.Persistence`.
- `Bondstone.Persistence` is provider-neutral and depends only on shared .NET
  abstractions.
- `Bondstone.Transport` depends on `Bondstone.Persistence`.
- `Bondstone.Capabilities.DomainEvents` depends only on `Bondstone`.
- `Bondstone.Persistence.EntityFrameworkCore` depends on `Bondstone` and
  `Bondstone.Persistence`.
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` depends on
  `Bondstone`, `Bondstone.Capabilities.DomainEvents`,
  `Bondstone.Persistence`, and `Bondstone.Persistence.EntityFrameworkCore`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` depends on
  `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence`, and
  may reference `Bondstone` directly for shared builder extension methods.
- `Bondstone.Persistence.Postgres` depends on `Bondstone`,
  `Bondstone.Persistence`, `Dapper`, and `Npgsql`.
- `Bondstone.Transport.Local` depends on `Bondstone`, `Bondstone.Persistence`,
  and `Bondstone.Transport`.
- `Bondstone.Transport.ServiceBus` depends on `Bondstone`,
  `Bondstone.Persistence`, `Bondstone.Transport`, and
  `Azure.Messaging.ServiceBus`.
- `Bondstone.Transport.RabbitMq` depends on `Bondstone`,
  `Bondstone.Persistence`, `Bondstone.Transport`, and `RabbitMQ.Client`.

Provider-specific packages contain provider-specific behavior only. Transport
packages adapt broker/client SDKs directly instead of adapting another bus
library. Reusable hosted worker composition belongs in `Bondstone.Hosting`.
Core command, messaging, and module execution abstractions stay in
`Bondstone`. Provider-neutral durable persistence contracts are in
`Bondstone.Persistence`; provider-neutral transport diagnostics are in
`Bondstone.Transport`; module-local domain event contracts are in
`Bondstone.Capabilities.DomainEvents`. EF Core domain event collection and
persistence belongs in
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` as provider bridge
behavior. Changing that package split requires ADR review.

`Bondstone` owns the lightweight `AddBondstone` service-composition builder.
Provider, transport, and hosting packages add extension methods to that
builder while preserving the dependency direction above. The builder is a
guardrail for common host setup, not a replacement for lower-level
registration methods needed by advanced consumers and tests.

## Public API Surface

Normal user-facing API is the setup and module contract surface: message
markers and identity attributes, module registration builders, `AddBondstone`
composition, provider/transport builder extensions, durable send/publish
contracts, receive pipeline contracts, result types, options, and documented
diagnostic shapes.

Some public low-level persistence, transport, receive, dispatcher, resolver,
and concrete provider types remain available for advanced composition, tests,
custom schedulers, and app-owned provider consumers. Their visibility does not
automatically make them the preferred setup path or an open-ended extension
point. Stable docs should steer normal users to the builder and module-owned
helpers first.

After publication, broad public type removal, visibility reduction, renaming,
or parameter-name churn is compatibility-sensitive. Do not perform that work
without a public API inventory, a compatibility plan, and ADR review when the
change affects public API shape or package boundaries.

## Version And Dependency Management

Use central package management through `Directory.Packages.props`.

Use coordinated versioning for the current package set. Release Please manages
the root changelog, release pull request, GitHub release, and version tag.

Release Please uses the `simple` release type and updates the central
`VersionPrefix` in `Directory.Build.props`. `dotnet pack` uses that
source-controlled MSBuild version without translating the GitHub tag in the
publish workflow.

The NuGet publish workflow runs when a GitHub release is published. It also
supports manual dispatch for recovery or first-publication use; manual dispatch
publishes the version recorded in `Directory.Build.props` at the selected ref.

The publish workflow packs `Bondstone.slnx` and publishes every `.nupkg`
created under `artifacts/packages`. Packable package projects included in the
solution are covered by the same publish loop.

Shared package metadata belongs in `Directory.Build.props`. Project-specific
descriptions, dependencies, and package assets belong in the package project
file.

Package artifacts should include symbols and the root package README.
Publishing uses GitHub Actions with NuGet trusted publishing. The workflow uses
GitHub OIDC through `NuGet/login@v1` to obtain a short-lived NuGet API key
instead of storing a long-lived `NUGET_API_KEY` secret.

The repository publishes to nuget.org only. GitHub Packages is not a current
target because nuget.org is the normal public .NET package registry and avoids
GitHub Packages authentication/source setup for consumers.

The repository configures a NuGet trusted publishing policy for
`.github/workflows/publish-nuget.yml`. The workflow uses `NUGET_USER` as a
GitHub repository variable containing the nuget.org username or organization
profile name used for the trusted publishing policy.

Configure `RELEASE_PLEASE_TOKEN` with a GitHub App token or personal access
token. This token is required because Release Please-created releases must
trigger the separate publish workflow. GitHub's default workflow token can
create the release without triggering downstream workflows.

Real publish verification has succeeded. Published packages appear on
nuget.org, not GitHub Packages.

Initial packages have been published to nuget.org. Keep this document focused
on package boundaries, dependency direction, versioning, and publishing rules.
Future packaging ideas are tracked in
[backlog/00-plans.md](backlog/00-plans.md).
