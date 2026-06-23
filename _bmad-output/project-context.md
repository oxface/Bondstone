---
project_name: "Bondstone"
user_name: "Dude"
date: "2026-06-18"
status: "complete"
optimized_for_llm: true
source_of_truth:
  prd: "_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md"
  architecture: "_bmad-output/planning-artifacts/architecture.md"
  epics: "_bmad-output/planning-artifacts/epics.md"
---

# Project Context for AI Agents

This file is the lean implementation context for Bondstone agents. It points
to durable BMAD artifacts instead of duplicating full architecture.

## Source Of Truth

- PRD: `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics and stories: `_bmad-output/planning-artifacts/epics.md`
- Consumer/repository docs: `docs/README.md`

Use BMAD architecture for internal durable behavior and package-boundary
rules. Use `/docs` for package setup, operations, observability, testing,
public API, samples, repository workflow, and GitHub issue conventions.

## Technology Stack

- Repository type: .NET library/framework monorepo for durable module
  boundaries, durable command sending, EF Core backed inbox/outbox
  persistence, operation observation, and transport adapters.
- Primary language/runtime: C# on `net10.0`; .NET SDK `10.0.108` with
  `rollForward: latestFeature`.
- Build settings: nullable enabled, implicit usings enabled,
  `AnalysisLevel=latest`, warnings as errors, code style enforced in build,
  central package management.
- Package orchestration: `pnpm@10.33.4`, Node `>=24.0.0`.
- EF Core packages: `Microsoft.EntityFrameworkCore.*` `10.0.8`.
- PostgreSQL: `Npgsql` `10.0.3`,
  `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2`.
- Transport adapters: `RabbitMQ.Client` `7.2.1`,
  `Azure.Messaging.ServiceBus` `7.20.1`.
- Testing: xUnit, Testcontainers, PublicApiGenerator.

## Critical Runtime Rules

- Bondstone is a library/framework, not a product app, UI app, SaaS platform,
  workflow engine, or general-purpose bus.
- Keep module boundaries explicit. Cross-module restart-safe state changes use
  durable commands or integration events.
- `IModuleCommandExecutor` is the immediate module command boundary, not a
  generic mediator.
- Durable command send accepts work and returns metadata. Target handler
  results are observed through operation APIs, not returned directly.
- Commands, integration events, and domain events are distinct. Do not collapse
  them into one abstraction.
- Durable message identities, handler identities, subscriber identities, and
  domain-event identities must be stable and explicit. Do not derive durable
  identities from CLR type or handler class names.
- `IDurableCommandSender` and `IDurableEventPublisher` require a current
  module execution context established by Bondstone runtime code.
- Module-owned EF persistence must keep source state, inbox markers, operation
  state, domain-event records, outgoing outbox rows, and durable inbox rows in
  the owning module transaction boundary where applicable.
- EF Core with PostgreSQL is the supported production durable persistence path.
  Consumers own EF migrations.
- Transport adapters are thin native-driver envelope adapters. Broker topology,
  provisioning, retries, dead-letter policy, prefetch/concurrency, credentials,
  and monitoring remain host-owned.
- Native broker delivery must not be acknowledged or completed before durable
  inbox ingestion succeeds.
- `Bondstone.Transport.Local` is explicit local/dev/test infrastructure only.
  It is not production broker durability and not a hidden fallback.
- Domain events are module-local facts. They are not integration events,
  transport messages, or automatically published outbox messages.
- Cleanup, retention, replay, purge, stale-row recovery, broker dead-letter
  movement, and topology management remain application-owned unless a future
  BMAD PRD/architecture adds an explicit feature.

## Code Rules

- Use nullable-aware C#; do not suppress nullable warnings without proving the
  invariant.
- Use `ct` as the parameter/local name for `CancellationToken` values,
  including public APIs.
- Treat public/protected API changes as compatibility-sensitive. Inventory
  setup APIs, documented advanced composition APIs, and public implementation
  types before hiding, renaming, or removing members.
- Production package collaboration must use explicit contracts or
  package-local implementation. Do not add `InternalsVisibleTo` for runtime
  package collaboration.
- Prefer source-compatible explicit abstractions over reflection or CLR-name
  derived identities for durable behavior.
- Keep edits narrow and reviewable. Avoid broad rewrites, metadata churn, or
  generated artifacts unless the task explicitly requires them.
- Use centralized .NET configuration in `Directory.Build.props` and
  `Directory.Packages.props`; do not scatter package versions into projects.

## Testing Rules

- Read `docs/testing.md` and the nearest `tests/**/AGENTS.md` before adding,
  moving, or reclassifying tests.
- Use xUnit `[Trait("Category", "...")]` consistently:
  `Unit`, `Application`, `Integration`, and `Package`.
- Default verification runs fast categories through `pnpm backend:test`.
- Run `pnpm backend:test:integration` for PostgreSQL, RabbitMQ, Service Bus,
  or sample smoke behavior that requires real infrastructure.
- Public API baselines in `tests/Bondstone.PublicApi.Tests` guard packable
  packages. Refresh only after reviewing compatibility impact.
- EF Core InMemory is not proof of relational durability, uniqueness,
  transactions, locking, claiming, retries, or PostgreSQL behavior.

## Workflow Rules

- Prefer repository scripts:
  `pnpm check`, `pnpm verify`, `pnpm backend:restore`,
  `pnpm backend:build`, `pnpm backend:test`,
  `pnpm backend:test:integration`, and `pnpm backend:pack`.
- `pnpm check` is the default quality gate: Prettier check, restore, Release
  build, fast tests, and pack/package artifact checks.
- Do not run `git commit` or `git push` directly unless the user explicitly
  asks.
- Do not overwrite or revert user changes. Work with the current tree and ask
  only when local edits make the requested change ambiguous.
- GitHub Issues and GitHub Projects track backlog work, real-project findings,
  cleanup tasks, prioritization, and ownership.
- README files orient humans; AGENTS files orient agents. Keep both as indexes
  that reference BMAD artifacts and consumer docs instead of duplicating
  durable architecture rules.

## Do Not Miss

- Do not make local transport a fallback for missing broker configuration.
- Do not make broker receive complete native deliveries before durable
  ingestion succeeds.
- Do not add automatic domain-event dispatch or automatic domain-to-
  integration-event publication.
- Do not add provider-neutral broker runtime ownership without a BMAD PRD and
  architecture update.
- Do not direct new consumers to removed/non-current package IDs.
- Do not bulk-copy implementation code from the historical template repository
  or preserve compatibility with it as a design constraint.

Last Updated: 2026-06-18
