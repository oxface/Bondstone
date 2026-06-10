# Agent Index

This repository is the maintenance home for Bondstone, a .NET library for
durable module boundaries, durable command sending, EF Core backed inbox/outbox
persistence, and transport adapters.

Start with:

- [README.md](README.md) when it exists for package purpose, install examples,
  and verification entrypoints.
- [docs/README.md](docs/README.md) when it exists for durable developer and
  architecture documentation.
- [docs/adr/README.md](docs/adr/README.md) when it exists before creating,
  updating, superseding, or archiving ADRs.
- [docs/architecture/README.md](docs/architecture/README.md) before changing
  runtime architecture, durable messaging, persistence, hosting, or transport
  behavior.
- [docs/backlog/README.md](docs/backlog/README.md) before choosing or
  continuing a backlog campaign or future-work item.
- [docs/testing.md](docs/testing.md) before moving or writing tests.
- [docs/samples.md](docs/samples.md) before adding or changing sample
  applications.
- [.agents/skills/AGENTS.md](.agents/skills/AGENTS.md) before adding or
  changing repository agent skills.

## Operating Rules

- Do not vibe-code. Read the relevant docs and implementation first, state the
  intended change, then make the smallest coherent edit.
- Implement gradually. Prefer narrow, reviewable steps over broad rewrites, and
  verify each risky step before expanding scope.
- Do not run git commit and push directly. You can use status and diff commands
  freely.
- Do not overwrite or revert user changes. Work with the current files and ask
  when local edits make the requested change ambiguous.
- Keep generated artifacts, packed packages, temporary sample outputs, coverage,
  and build artifacts out of committed source unless the user explicitly asks to
  preserve them.
- Do not use `InternalsVisibleTo` for production package collaboration. Reserve
  friend assemblies for tests; runtime packages should collaborate through
  explicit contracts or package-local implementation.

## Repository Direction

- Target .NET 10 unless an accepted ADR changes the target framework policy.
- NuGet package IDs should match project names unless an accepted ADR records a
  different packaging decision.
- Use coordinated package versioning and NuGet release automation according to
  [docs/packaging.md](docs/packaging.md).
- Build according to the stable docs. Do not bulk-copy Bondstone code or
  preserve the historical template repository as a compatibility constraint.
- Source, tests, docs, repository automation, and samples should be arranged
  for library maintenance, while avoiding validation and frontend/browser
  tooling that Bondstone does not need.
- Product behavior, domain examples, sample services, and runtime integration
  scenarios belong in samples or tests, not in core library packages.
- Use `ct` for `CancellationToken` parameters in C# source and tests according
  to [docs/repository.md](docs/repository.md).

## Architecture Direction

- Bondstone should support modular monoliths first, including a low-friction
  path for splitting modules into services when a module needs independent
  deployment or scalability.
- Bondstone should also be usable in microservice setups that need internal
  durability, inbox/outbox processing, stable message identities, and
  provider-owned transport adapters.
- Do not introduce a generic mediator or message-bus layer as a default
  Bondstone feature. Durable command sending is for asynchronous outbox
  delivery; ordinary in-process module calls can use typed `.Contracts`
  references.
- Durable commands and integration events are the durable boundary for
  cross-persistence state changes. Direct `.Contracts` calls are mainly for
  reads, local composition, or operations that tolerate failure. Domain events
  are module-local/private unless module code explicitly publishes an
  integration event.
- First-class integration event behavior is described in
  [docs/architecture/messaging.md](docs/architecture/messaging.md): publish
  dispatch, subscriber execution, per-subscriber inbox behavior, diagnostics,
  and tests are part of the current durable event loop. Keep native broker
  setup app-owned, and do not fold domain event persistence or broad
  choreography samples into this event loop without a later ADR.
- Transport adapter topology should describe durable message topology, such as
  command queues, event destinations, and event subscriptions, while broker
  connection, worker, retry, dead-letter, serializer, and subscription-storage
  setup stays provider-native.
- Startup transport topology validation is described in
  [docs/architecture/messaging.md](docs/architecture/messaging.md),
  [docs/architecture/transport-rabbitmq.md](docs/architecture/transport-rabbitmq.md),
  and
  [docs/architecture/transport-servicebus.md](docs/architecture/transport-servicebus.md):
  validate configured durable routes and receive bindings against registered
  Bondstone handlers/subscribers without turning validation into broker
  provisioning. Same-queue in-process event fan-out remains valid, while split
  subscribers should use provider-native broker fan-out such as RabbitMQ
  exchange bindings or Service Bus topic subscriptions.
- Provider retry and recovery boundaries are described in
  [docs/architecture/messaging.md](docs/architecture/messaging.md),
  [docs/architecture/transport-rabbitmq.md](docs/architecture/transport-rabbitmq.md),
  and
  [docs/architecture/transport-servicebus.md](docs/architecture/transport-servicebus.md):
  Bondstone owns persisted outbox retry and terminal failure state, while
  direct provider receive adapters own settlement ordering and diagnostics
  without owning broker retry/dead-letter policy.
- Direct provider transport adapters are the current direction according to
  [docs/architecture/README.md](docs/architecture/README.md) and
  [docs/packaging.md](docs/packaging.md). Do not adapt another bus abstraction
  as a supported transport package.
- `Bondstone.Transport.Local` is an explicit local queue adapter for samples,
  tests, and local development. It must not become a hidden fallback or be
  presented as production broker durability.
- Azure Service Bus and RabbitMQ transport rules are described in
  [docs/architecture/transport-servicebus.md](docs/architecture/transport-servicebus.md)
  and [docs/architecture/transport-rabbitmq.md](docs/architecture/transport-rabbitmq.md).
  Keep these slices provider-native, app-owned, proof-oriented, and explicit
  about receive/broker reliability follow-ups.
- `Bondstone.Persistence.Postgres` behavior is described in
  [docs/architecture/persistence-postgres.md](docs/architecture/persistence-postgres.md)
  and [docs/packaging.md](docs/packaging.md). Keep it PostgreSQL-specific and
  Dapper-backed internally, not a generic Dapper abstraction; it should prove
  durable module messaging persistence and mixed-persistence samples without
  depending on EF Core.
- Durable behavior, public API shape, package boundaries, provider support,
  transport support, migration strategy, and compatibility policy require ADRs
  before broad implementation.
- Durable decisions recorded in ADRs must be applied into stable developer docs
  and agent instructions. ADRs are the decision trail; stable docs are the
  current operating contract.

## ADR And Planning Rules

- Do not use SDD/spec-driven product workflow for Bondstone library work unless
  an accepted ADR introduces it for a bounded purpose.
- Before editing code or automation, check whether ADR work is required when a
  change affects public API, package boundaries, target frameworks, provider or
  transport support, migration policy, compatibility, release/publishing,
  sample architecture, repository workflow, or agent harness behavior.
- Use ADRs for durable technical decisions. Prefer small ADRs with a clear
  status, application state, context, decision, consequences, and links to
  applied docs.
- Treat accepted ADR decision content as append-only. Do not rewrite accepted
  `Context`, `Decision`, or `Consequences` except for mechanical fixes; add a
  dated amendment for compatible clarification, or supersede when the decision
  changes.
- Updating, superseding, archiving, or removing an ADR must preserve the
  decision trail and update the stable docs and agent instructions that depend
  on it.
- Reusable ADR workflows must become repository skills under `.agents/skills`.
  Start with skills for creating ADRs, updating ADRs, superseding ADRs, and
  archiving/removing ADRs.

## Common Commands

Prefer the repository package scripts when available:

- `pnpm check`
- `pnpm verify`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test`
- `pnpm backend:test:integration`
- `pnpm backend:pack`

The direct .NET equivalents are:

- `dotnet restore Bondstone.slnx`
- `dotnet build Bondstone.slnx --configuration Release --no-restore`
- `dotnet test Bondstone.slnx --configuration Release --no-build --filter "Category=Unit|Category=Application"`
- `dotnet pack Bondstone.slnx --configuration Release --no-build`

Keep CI, Husky hooks, and docs aligned with the package-script entrypoints.
