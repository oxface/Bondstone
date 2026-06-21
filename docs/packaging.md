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
- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`

For a consumer-facing capability and namespace matrix, see
[package-discovery.md](package-discovery.md).

`Bondstone.Transport.Local` is an explicit local queue adapter for samples,
tests, and local development. It exercises outbox dispatch and module receive
idempotency semantics through the provider-neutral receive pipelines, but it
is not a production broker adapter or hidden fallback.

`Bondstone.Transport.RabbitMq` and `Bondstone.Transport.ServiceBus` are thin
broker adapters. They provide native-driver envelope dispatchers and opt-in
receive workers, but they do not own broker topology, provisioning,
subscription storage, retry policy, dead-letter policy, prefetch/concurrency
strategy, or monitoring. Normal host composition uses one outbound dispatcher
per service provider. Multi-transport outbound dispatch is an explicit
advanced composition scenario, not implicit built-in adapter accumulation.

`Bondstone.Persistence.Postgres`, `Bondstone.Transport`,
`Bondstone.Capabilities.DomainEvents`, and
`Bondstone.Capabilities.DomainEvents.EntityFrameworkCore` were removed from
the active package set after MVP. Reintroducing a separate provider-neutral
transport package, broad broker runtime package, or capability package
requires BMAD PRD/architecture review and a real consumer need.

Module-local domain event contracts now live in `Bondstone` under
`Bondstone.DomainEvents`. EF Core domain event collection, EF domain event
record mapping, and explicit module opt-in live in
`Bondstone.Persistence.EntityFrameworkCore`.

`Bondstone.Persistence` contains provider-neutral durable persistence
contracts and records: durable envelopes, trace context, operation state,
outbox, inbox, transport-facing persistence contracts, and passive
module-runtime registration records. Module-aware runtime resolution and
module execution stay in `Bondstone`.

## V2 Replacement And Migration Guidance

The current docs and package READMEs describe the v2 replacement line. The v1
public-MVP line is deprecated/delisted for new adoption; registry state should
preserve that replacement posture when v2 packages are published. Do not
direct new consumers to v1 package IDs, old README guidance, or old setup
examples.

The exact package version comes from source-controlled release metadata. At
the time of this document, `Directory.Build.props` records
`VersionPrefix` as `1.1.0`, and Release Please owns subsequent changes to that
value. For a v2 readiness PR or checkpoint, leave `VersionPrefix` unchanged
unless the repository is intentionally creating the Release Please release PR.
The Release Please release PR should update `Directory.Build.props`,
`.github/.release-please-manifest.json`, `CHANGELOG.md`, the GitHub release,
and the version tag together. Do not invent a v2 NuGet version or release date
in docs, READMEs, or release notes before the source metadata changes.

The active v2 package IDs are the package set listed above. Removed or
non-current v1 surfaces include:

- `Bondstone.Persistence.Postgres`;
- `Bondstone.Transport`;
- `Bondstone.Capabilities.DomainEvents`;
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`;
- broad broker runtime ownership, provider-neutral transport diagnostics, and
  Rebus-specific package ownership;
- non-EF persistence providers until a real consumer need and BMAD architecture
  update bring one back;
- finalized metric instruments and stable misconfiguration error codes, which
  are not current package behavior.

Consumers migrating from v1 should:

- replace old package references with the active package IDs that match their
  host capabilities;
- start from [setup.md](setup.md) for the current `AddBondstone` composition
  path and [package-discovery.md](package-discovery.md) for namespace and
  package choice;
- move PostgreSQL durable persistence to
  `Bondstone.Persistence.EntityFrameworkCore` plus
  `Bondstone.Persistence.EntityFrameworkCore.Postgres`;
- move module-local domain event contracts to `Bondstone` and optional EF
  domain event record mapping to
  `Bondstone.Persistence.EntityFrameworkCore`;
- replace old provider-neutral transport or Rebus package usage with an
  app-owned `IDurableEnvelopeDispatcher`,
  `IDurableMessageEnvelopeSerializer`, and durable inbox ingestion boundary,
  or use the thin RabbitMQ/Azure Service Bus adapter packages when the
  application already owns native topology and retry policy;
- preserve stable durable message identities where payload changes are
  compatible, and introduce new identities for breaking payload changes;
- generate and review application EF migrations after package upgrades because
  Bondstone durable tables are mapped into application `DbContext` models.

Release notes for replacement releases must call out removed package IDs,
removed or renamed public APIs, durable table-shape changes, and any migration
steps consumers must perform in their own applications.

## V2 Release Checklist

Use this checklist before creating or merging the v2 release PR. These steps
prepare and verify source only; they do not publish packages, deprecate
packages, or delist packages remotely.

Release metadata:

- Let Release Please own the v2 release PR when using the normal path. Verify
  that the release PR updates `Directory.Build.props` `VersionPrefix` to the
  intended v2 version, updates `.github/.release-please-manifest.json` to the
  same version, updates `CHANGELOG.md`, and creates the matching version tag
  and GitHub release when merged.
- If a manual recovery release is used instead of Release Please, select a ref
  whose source-controlled `VersionPrefix` already records the intended package
  version. The manual publish workflow publishes the version in source
  metadata at that ref.
- Do not run the NuGet publish workflow, publish a GitHub release, deprecate
  packages, or delist packages until the release is explicitly approved.

Active package IDs:

- `Bondstone`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`

Removed package IDs and package surfaces:

- `Bondstone.Persistence.Postgres`
- `Bondstone.Transport`
- `Bondstone.Capabilities.DomainEvents`
- `Bondstone.Capabilities.DomainEvents.EntityFrameworkCore`
- provider-neutral broker runtime ownership, Rebus-specific package
  ownership, topology provisioning, retry/dead-letter orchestration,
  subscription storage, and provider-neutral transport diagnostics.

Major breaking-change notes:

- The package set changed to the active package IDs above. Consumers must
  replace removed package references and remove old broad transport or
  capability package usage.
- The non-EF PostgreSQL persistence package is removed. PostgreSQL-backed
  modules use EF Core mappings from
  `Bondstone.Persistence.EntityFrameworkCore` plus
  `Bondstone.Persistence.EntityFrameworkCore.Postgres`.
- Module-local domain event contracts live in `Bondstone` under
  `Bondstone.DomainEvents`; EF-backed domain event collection and record
  mapping live in `Bondstone.Persistence.EntityFrameworkCore`.
- The old outbox transport naming was replaced by envelope dispatcher naming:
  use `IDurableEnvelopeDispatcher`,
  `IDurableEnvelopeDispatchRoute`, and
  `RoutedDurableEnvelopeDispatcher` for app-owned or adapter-owned dispatch.
- Thin RabbitMQ and Azure Service Bus adapters are native-driver envelope
  adapters only. They do not own topology, provisioning, retries,
  dead-lettering, prefetch/concurrency policy, credentials, monitoring, or
  Rebus integration.
- RabbitMQ receive no longer exposes `AutoAck`; the worker uses manual
  acknowledgement and acknowledges only after Bondstone receive succeeds.
  Azure Service Bus receive rejects `AutoCompleteMessages = true` and
  completes messages only after Bondstone receive succeeds.

Public API cleanup highlights:

- Removed package-local implementation details from the public surface where
  they had no approved v2 role, including
  `Bondstone.Utility.StringExtensions`,
  `BondstoneLocalServiceCollectionExtensions`,
  `ModuleRuntimeFeatureCollection`, `IModuleTransactionFeature`,
  `DurableOutboxWorker`, `DurableOutboxWorkerOptionsValidator`, and
  PostgreSQL concrete SQL implementation components.
- Kept remaining public concrete helpers only where they are deliberate
  normal defaults, advanced composition APIs, or provider/runtime concrete
  APIs, as classified in [public-api.md](public-api.md).
- Treat any further broad public type removal, visibility reduction, rename,
  or parameter-name churn as compatibility-sensitive and outside the v2
  release-prep path unless BMAD PRD/architecture artifacts approve it.

Migration and operations notes:

- Applications own EF migrations and schema rollout. Every app that maps
  Bondstone EF tables must generate and review its own migrations after
  upgrading packages, including per-module schemas. Release notes must call
  out durable table-shape changes so apps can schedule migrations before
  deployment.
- Local transport is an explicit local queue adapter for samples, tests, and
  local development. It exercises Bondstone outbox dispatch and module
  receive idempotency semantics, but it is not production broker durability,
  topology management, retry, dead-letter handling, or durable inbox
  ingestion.
- Durable broker receive adapters and app-owned native receive loops should
  settle native deliveries only after durable incoming inbox ingestion
  succeeds. If ingestion fails, use the provider-native failure path rather
  than acknowledging the message as handled.
- Current observability includes the activity sources, activities, tags, log
  event ids, result diagnostics, and inspection contracts documented in
  [observability.md](observability.md). Finalized metrics, a stable metric
  instrument vocabulary, and stable misconfiguration error codes are not v2
  behavior.
- The durable incoming inbox is the current durable receive ledger. The hosted
  incoming inbox processing worker is opt-in behavior in `Bondstone.Hosting`.
  Module processing still writes the smaller receive idempotency marker as an
  implementation detail, as documented in [operations.md](operations.md).

NuGet registry follow-up after v2 publication:

- Verify all active v2 packages are visible on nuget.org with package README,
  XML documentation, symbols, dependency metadata, and the intended version.
- Deprecate v1 package versions that v2 replaces and point consumers to the
  matching active package ID or current package version where nuget.org
  deprecation metadata supports it.
- Delist v1 package versions or removed v1 package IDs only after v2 packages
  are verified and the maintainers intentionally choose that registry action.
  Preserve traceability for already-published packages and avoid implying
  removed package IDs still have a v2 replacement package with the same ID.

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
- `Bondstone.Transport.RabbitMq` depends on `Bondstone`,
  `Bondstone.Persistence`, and RabbitMQ.Client.
- `Bondstone.Transport.ServiceBus` depends on `Bondstone`,
  `Bondstone.Persistence`, and Azure.Messaging.ServiceBus.

Provider-specific packages contain provider-specific behavior only. Bondstone
ships thin RabbitMQ and Azure Service Bus adapter packages for native-driver
envelope plumbing. Applications still own native topology, provisioning,
retry, dead-letter policy, and monitoring. Applications that use Rebus or
another bus/runtime keep that integration app-owned: implement
`IDurableEnvelopeDispatcher` for outgoing outbox records and ingest native
deliveries into the durable inbox after mapping them into
`DurableMessageEnvelope`. Reusable generic hosted worker composition belongs
in `Bondstone.Hosting`; broker-specific receive workers live only in their
adapter packages and are opt-in. When an app needs outbound routing across
more than one transport, it should compose a single aggregate
`IDurableEnvelopeDispatcher`, typically using `IDurableEnvelopeDispatchRoute`
and `RoutedDurableEnvelopeDispatcher`, so one dispatcher service still owns
the outbox boundary.
Core command, messaging, and module execution abstractions stay in
`Bondstone`. Provider-neutral durable persistence contracts are in
`Bondstone.Persistence`; there is no active provider-neutral transport
diagnostics package; module-local domain event contracts are in `Bondstone`.
Provider-neutral setup-code diagnostics live in `Bondstone.Persistence` for
now because both core composition and provider-neutral persistence surfaces
emit the same coded setup exception contract, and `Bondstone` may depend on
that neutral contract package.
EF Core domain event collection and persistence belongs in
`Bondstone.Persistence.EntityFrameworkCore`. Changing that package split
requires BMAD PRD/architecture review.

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
without a public API inventory, a compatibility plan, and BMAD
PRD/architecture review when the change affects public API shape or package
boundaries.

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
Operational package-upgrade guidance for EF table-shape changes, migration
ownership, and retention lives in [operations.md](operations.md).
