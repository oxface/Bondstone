# Bondstone Project Profile

Generated during the SpecKit brownfield bootstrap on 2026-06-23 from the
current repository structure, package metadata, tests, docs, and surviving
legacy planning artifacts. This is SpecKit brownfield memory, not consumer
documentation and not constitution governance.

## Tech Stack

| Category              | Detected                                                                 |
| --------------------- | ------------------------------------------------------------------------ |
| Primary language      | C# (`.cs`)                                                               |
| Target framework      | `net10.0`                                                                |
| SDK                   | .NET SDK `10.0.108` with `rollForward: latestFeature`                    |
| Package orchestration | `pnpm@10.33.4`, Node `>=24.0.0`                                          |
| Build system          | `Bondstone.slnx`, MSBuild, central package management                    |
| Persistence           | EF Core `10.0.8`, PostgreSQL via Npgsql                                  |
| Transport adapters    | RabbitMQ.Client, Azure.Messaging.ServiceBus, local adapter               |
| Testing               | xUnit, Testcontainers, PublicApiGenerator                                |
| CI/CD                 | GitHub Actions: verify, semantic PR title, Release Please, NuGet publish |
| Formatting/tooling    | Prettier, Husky, commitlint, `.editorconfig`                             |

## Architecture

- Pattern: .NET library/framework monorepo with packable packages.
- `src/`: package projects and package-local READMEs/agent indexes.
- `tests/`: package and integration-boundary test projects.
- `samples/`: modular monolith samples and adoption proofs.
- `docs/`: consumer-facing and repository-operation documentation.
- `.agents/skills/`: agent workflows.
- `.specify/`: source-controlled SpecKit constitution, indexes, and extension
  choices; source-controlled Bondstone template overrides live under
  `.specify/templates/`; ignored local SpecKit tooling may provide scripts,
  extension payloads, integration state, and caches.

## Package Map

| Package                                              | Path                                                     | Purpose                                                                                    |
| ---------------------------------------------------- | -------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `Bondstone`                                          | `src/Bondstone`                                          | Core module model, command/event contracts, durable send/publish APIs, runtime composition |
| `Bondstone.Persistence`                              | `src/Bondstone.Persistence`                              | Provider-neutral durable persistence and operation contracts                               |
| `Bondstone.Persistence.EntityFrameworkCore`          | `src/Bondstone.Persistence.EntityFrameworkCore`          | Generic EF Core mappings and stores                                                        |
| `Bondstone.Persistence.EntityFrameworkCore.Postgres` | `src/Bondstone.Persistence.EntityFrameworkCore.Postgres` | PostgreSQL EF Core provider behavior                                                       |
| `Bondstone.Hosting`                                  | `src/Bondstone.Hosting`                                  | Hosted outbox and durable inbox workers                                                    |
| `Bondstone.Transport.Local`                          | `src/Bondstone.Transport.Local`                          | Local/dev/test transport adapter                                                           |
| `Bondstone.Transport.RabbitMq`                       | `src/Bondstone.Transport.RabbitMq`                       | RabbitMQ native-driver envelope adapter                                                    |
| `Bondstone.Transport.ServiceBus`                     | `src/Bondstone.Transport.ServiceBus`                     | Azure Service Bus native-driver envelope adapter                                           |

## Test Profile

Observed xUnit category counts at bootstrap:

- `Unit`: 465
- `Integration`: 82
- `Application`: 53
- `Package`: 1

Default fast test command:

```bash
pnpm backend:test
```

Infrastructure-backed integration test command:

```bash
pnpm backend:test:integration
```

Quality gate:

```bash
pnpm check
```

## Conventions

- Branches observed: `main`, `feat/*`, `docs/*`, release and Dependabot
  branches.
- Recent commits are mostly Conventional Commit style (`docs:`, `fix:`).
- `CancellationToken` parameters and locals use `ct`.
- Public API changes are reviewed through
  `tests/Bondstone.PublicApi.Tests`.
- README files orient humans; AGENTS files orient agents.

## Existing Governance

- Root and scoped `AGENTS.md` files.
- `docs/repository.md`, `docs/packaging.md`, `docs/testing.md`,
  `docs/github-workflow.md`, and related consumer/repository docs.
- Surviving legacy architecture and agent-context artifacts.

These surviving artifacts were inputs for migration to the SpecKit
constitution, durable docs, and feature specs. They are not the desired
long-term artifact location.
