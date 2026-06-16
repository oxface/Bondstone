# Packaging

This document records the current Bondstone package boundary and target
framework direction.

## Target Framework

Bondstone targets `net10.0`.

## Package Set

Ship one NuGet package per active package project included in `Bondstone.slnx`.
Package IDs match project names:

- `Bondstone`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Local`

For a consumer-facing capability and namespace matrix, see
[package-discovery.md](package-discovery.md).

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It exercises outbox/inbox receive semantics
through the provider-neutral receive pipelines, but it is not a production
broker adapter or hidden fallback.

`Bondstone.Persistence.Postgres`, `Bondstone.Transport`, and
`Bondstone.Transport.ServiceBus`,
`Bondstone.Transport.RabbitMq`,
`Bondstone.Capabilities.DomainEvents`, and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` were removed from
the active package set after MVP. Reintroducing a separate provider-neutral
transport package, broker adapter package, or capability package requires ADR
review and a real consumer need.

Module-local domain event contracts now live in `Bondstone` under
`Bondstone.DomainEvents`. EF Core domain event collection, EF domain event
record mapping, and explicit module opt-in live in
`Bondstone.Persistence.EntityFrameworkCore`.

`Bondstone.Persistence` contains provider-neutral durable persistence
contracts and records: durable envelopes, trace context, operation state,
outbox, inbox, transport-facing persistence contracts, and passive
module-runtime registration records. Module-aware runtime resolution and
module execution stay in `Bondstone`.

## Package Dependencies

Use this dependency direction:

- `Bondstone` stays at the core with no dependency on provider, transport
  adapter, or hosting packages. It may depend on the neutral
  `Bondstone.Persistence` contract package.
- `Bondstone.Hosting` depends on `Bondstone` and `Bondstone.Persistence`.
- `Bondstone.Persistence` is provider-neutral and depends only on shared .NET
  abstractions.
- `Bondstone.Persistence.EntityFrameworkCore` depends on `Bondstone` and
  `Bondstone.Persistence`.
- `Bondstone.Persistence.EntityFrameworkCore.Postgres` depends on
  `Bondstone.Persistence.EntityFrameworkCore`, `Bondstone.Persistence`, and
  may reference `Bondstone` directly for shared builder extension methods.
- `Bondstone.Transport.Local` depends on `Bondstone`, `Bondstone.Persistence`,
  and no provider-neutral transport package.

Provider-specific packages contain provider-specific behavior only. Bondstone
does not currently ship a broker adapter package. Applications that use Rebus,
RabbitMQ.Client, Azure Service Bus, or another transport implement
`IDurableEnvelopeDispatcher` for outgoing outbox records and call
`IDurableEnvelopeReceiver` after mapping native deliveries into
`DurableMessageEnvelope`. Reusable hosted worker composition belongs in
`Bondstone.Hosting`.
Core command, messaging, and module execution abstractions stay in
`Bondstone`. Provider-neutral durable persistence contracts are in
`Bondstone.Persistence`; there is no active provider-neutral transport
diagnostics package; module-local domain event contracts are in `Bondstone`.
EF Core domain event collection and persistence belongs in
`Bondstone.Persistence.EntityFrameworkCore`. Changing that package split
requires ADR review.

`Bondstone` owns the lightweight `AddBondstone` service-composition builder.
Provider, transport, and hosting packages add extension methods to that
builder while preserving the dependency direction above. The builder is a
guardrail for common host setup, not a replacement for lower-level
registration methods needed by advanced consumers and tests.

Result-returning command contracts and local module result execution live in
`Bondstone` with the rest of the command/module execution surface. Durable
result observation uses provider-neutral operation state from
`Bondstone.Persistence`, with typed operation result reading exposed from
`Bondstone` because it uses Bondstone's durable payload serialization options.
Local transport exposes accepted delivery rather than direct request/response
execution. App-owned broker integrations should preserve that same boundary.

## Public API Surface

Normal user-facing API is the setup and module contract surface: message
markers and identity attributes, result-returning command contracts, module
registration builders, `AddBondstone` composition, provider/local transport
builder extensions, durable send/publish contracts, envelope receive helpers,
receive pipeline contracts, result types, options, and documented diagnostic
shapes.

Some public low-level persistence, local transport, receive, dispatcher,
resolver, module runtime service, and concrete provider types
remain available for advanced composition, tests, custom schedulers, and
app-owned provider consumers. Their visibility does not automatically make
them the preferred setup path or an open-ended extension point. Stable docs
should steer normal users to the builder and module-owned helpers first.
The package-by-package classification inventory lives in
[public-api.md](public-api.md).

After publication, broad public type removal, visibility reduction, renaming,
or parameter-name churn is compatibility-sensitive. Do not perform that work
without a public API inventory, a compatibility plan, and ADR review when the
change affects public API shape or package boundaries.

The repository also keeps an automated public API baseline under
`tests/Bondstone.PublicApi.Tests`. Baseline diffs are review evidence for
public API changes, not automatic approval for breaking compatibility.

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

The pack step clears `artifacts/packages`, packs `Bondstone.slnx`, and verifies
package artifacts before publishing. The publish workflow publishes every
`.nupkg` created under `artifacts/packages`. Packable package projects included
in the solution are covered by the same publish loop.

Shared package metadata belongs in `Directory.Build.props`; packable-project
build and pack behavior that depends on project-local properties may live in
`Directory.Build.targets`. Project-specific descriptions, dependencies, and
package assets belong in the package project file.

Package artifacts should include symbols, the project-local package README,
and XML API documentation. Package README links that reference repository docs,
source paths, or tests should use absolute GitHub URLs so they work from NuGet
and package-manager UI surfaces. XML API documentation should cover normal
consumer-facing setup and contract APIs first; comprehensive comments for every
public advanced-composition or exposed implementation type remain incremental
cleanup.

`pnpm backend:pack` runs package artifact tests after packing and asserts that
each current packable package includes a matching XML documentation file beside
the assembly under `lib/net10.0/`.
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
