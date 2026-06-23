# Bondstone Constitution

## Core Principles

### I. Library Boundary First

Bondstone is a .NET library/framework for durable module boundaries, not a
product application, SaaS platform, generic message bus, workflow engine,
broker topology manager, or code generator. Every feature MUST preserve the
library's consumer-owned host model: applications compose modules, persistence,
transport adapters, hosted workers, topology, credentials, retries, monitoring,
retention, migrations, and recovery policy.

### II. Durable Identities And Message Semantics

Durable command, integration-event, handler, subscriber, and domain-event
identities MUST be stable and explicit. Runtime behavior MUST NOT derive
durable identities from CLR type names or handler class names. Commands,
integration events, and domain events are distinct concepts and MUST NOT be
collapsed into one abstraction. Durable command send accepts work and exposes
results through operation observation, not direct remote handler return values.

### III. Package Boundaries And Public API Compatibility

Package dependency direction MUST remain layered from core contracts to
provider/runtime implementations. Runtime packages MUST collaborate through
explicit contracts or package-local implementation; production packages MUST
NOT use `InternalsVisibleTo` for package collaboration. Public and protected
API changes are compatibility-sensitive and MUST include inventory, migration
notes when user-facing, and public API baseline review before broad hiding,
renaming, removing, or parameter-name changes.

### IV. Persistence And Transport Ownership

EF Core with PostgreSQL is the supported production durable persistence path.
Consumers own EF migrations and schema rollout. Bondstone owns durable message
identity, serialization, outbox/inbox records, receive binding validation,
operation state, and module handler execution. Transport adapters MUST stay
thin native-driver envelope adapters; broker topology, provisioning, retry,
dead-letter policy, prefetch/concurrency, credentials, and native monitoring
remain application-owned. Native broker deliveries MUST NOT be acknowledged or
completed before durable inbox ingestion succeeds.

### V. Evidence-Based Verification

Changes MUST be verified with the narrowest meaningful check and, when risk or
surface area warrants it, the repository quality gate. Tests protect public
contracts, package boundaries, durable persistence behavior, transport
behavior, tracing/causation behavior, and service-extraction assumptions. EF
Core InMemory is not proof of relational durability, PostgreSQL behavior,
uniqueness, transactions, locking, claiming, retries, or terminal state.

## Repository Constraints

Bondstone uses a library-maintenance monorepo layout:

- `src/` contains packable library packages.
- `tests/` contains package and integration-boundary test projects.
- `samples/` contains sample applications and adoption proofs, not core package
  behavior.
- `docs/` contains consumer-facing and repository-operation documentation.
- `.agents/skills/` contains reusable agent workflows.
- `.specify/memory/constitution.md` contains the SpecKit constitution.
- `docs/architecture.md` contains durable runtime architecture and
  package-boundary direction.
- `.specify/memory/project-profile.md` contains brownfield project profile facts.
- `.specify/AGENTS.md`, `.specify/README.md`, and `.specify/extensions.yml`
  record source-controlled SpecKit routing and extension choices. Installed
  templates, scripts, and extension payloads are local tooling unless
  explicitly unignored.

Current package IDs are:

- `Bondstone`
- `Bondstone.Hosting`
- `Bondstone.Persistence`
- `Bondstone.Persistence.EntityFrameworkCore`
- `Bondstone.Persistence.EntityFrameworkCore.Postgres`
- `Bondstone.Transport.Local`
- `Bondstone.Transport.RabbitMq`
- `Bondstone.Transport.ServiceBus`

Use centralized .NET configuration in `Directory.Build.props` and
`Directory.Packages.props`. Do not scatter package versions into project
files. Use `ct` as the parameter or local name for `CancellationToken` values,
including public APIs.

## Development Workflow

Feature work SHOULD use SpecKit artifacts as change-scoped intent and use the
Bondstone project profile as brownfield context:

1. Create or refine a feature spec under `specs/`.
2. Produce a plan that records affected packages, package-boundary impact,
   compatibility impact, test strategy, docs impact, and constitution checks.
3. Generate tasks with exact file paths and verification commands.
4. Implement in narrow, reviewable steps.
5. Use convergence/refinement/archive workflows to keep feature artifacts and
   durable project memory aligned.

GitHub Issues and Projects remain the backlog, prioritization, ownership, and
completion-tracking surface. SpecKit feature specs are implementation deltas;
long-lived architecture and governance belong in `.specify/memory/` and stable
consumer/repository docs.

Default verification commands:

- `pnpm check`
- `pnpm verify`
- `pnpm backend:restore`
- `pnpm backend:build`
- `pnpm backend:test`
- `pnpm backend:test:integration`
- `pnpm backend:pack`

Use xUnit `Category` traits consistently:

- `Unit`
- `Application`
- `Integration`
- `Package`

## Governance

This constitution supersedes generated template defaults and feature-local
plans. If a feature conflicts with this constitution, either change the feature
or amend the constitution in a separate explicit governance change. Amendments
MUST explain the reason, affected docs/templates, migration impact, and
verification required.

When runtime architecture, durable messaging, persistence, hosting, transport
behavior, package boundaries, public API strategy, documentation ownership, or
verification strategy changes, update the relevant durable docs and, when
governance changes, this constitution in the same change.

**Version**: 1.0.0 | **Ratified**: 2026-06-23 | **Last Amended**: 2026-06-23
